using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for zone-based parameter filling.
    /// Finds elements within zone bounds (scope boxes) and assigns parameters.
    /// Supports multiple zones - each element gets the name of the zone it's in.
    /// Zones are represented by scope boxes in Revit.
    /// </summary>
    public class ZoneAssignmentService : IZoneAssignmentService
    {
        private readonly ILogger _logger;

        public ZoneAssignmentService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all available zones (scope boxes) in the document
        /// </summary>
        public IList<Element> GetZones(Document document)
        {
            return GetZonesFromDocument(document);
        }

        /// <summary>
        /// Gets all available zones (scope boxes) from the specified document
        /// </summary>
        /// <param name="document">The Revit document to get zones from</param>
        /// <returns>List of zones sorted by name</returns>
        public IList<Element> GetZonesFromDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            try
            {
                // Zones are scope boxes - category: OST_VolumeOfInterest
                var zoneCollector = new FilteredElementCollector(document)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                var zones = zoneCollector
                    .OrderBy(z => z.Name)
                    .ToList();

                _logger.Info($"Found {zones.Count} zones in document");
                return zones;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting zones: {ex.Message}");
                return new List<Element>();
            }
        }

        /// <summary>
        /// Gets the bounding box of a zone (scope box)
        /// </summary>
        public BoundingBoxXYZ GetZoneBoundingBox(Element zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            try
            {
                // Zones (scope boxes) have a bounding box that can be retrieved directly
                // The bounding box is in model coordinates
                var bbox = zone.get_BoundingBox(null);

                if (bbox == null)
                {
                    _logger.Warn($"Zone {zone.Name} has no bounding box");
                    return null;
                }

                return bbox;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting bounding box for zone {zone.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if an element is contained within a zone's bounding box
        /// </summary>
        public bool IsElementInZone(Element element, BoundingBoxXYZ zoneBoundingBox)
        {
            return IsElementInZone(element, zoneBoundingBox, null);
        }

        /// <summary>
        /// Checks if an element is contained within a zone's bounding box
        /// with optional coordinate transformation for linked documents
        /// </summary>
        public bool IsElementInZone(Element element, BoundingBoxXYZ zoneBoundingBox, Transform transform)
        {
            if (element == null || zoneBoundingBox == null)
                return false;

            try
            {
                // Get the element's bounding box in the active view or model space
                var elementBbox = element.get_BoundingBox(null);

                if (elementBbox == null)
                {
                    // Some elements like line-based elements might not have a bounding box
                    // Try to get their curve or location
                    return false;
                }

                // Transform element bounding box to source document space if transform is provided
                if (transform != null)
                {
                    elementBbox = new BoundingBoxXYZ();
                    elementBbox.Min = transform.OfPoint(element.get_BoundingBox(null).Min);
                    elementBbox.Max = transform.OfPoint(element.get_BoundingBox(null).Max);
                }

                // No tolerance - use exact zone bounds
                // Check if element is completely inside the zone bounds
                // We check all three dimensions (X, Y, Z)

                // X dimension
                if (elementBbox.Min.X < zoneBoundingBox.Min.X || elementBbox.Max.X > zoneBoundingBox.Max.X)
                    return false;

                // Y dimension
                if (elementBbox.Min.Y < zoneBoundingBox.Min.Y || elementBbox.Max.Y > zoneBoundingBox.Max.Y)
                    return false;

                // Z dimension
                if (elementBbox.Min.Z < zoneBoundingBox.Min.Z || elementBbox.Max.Z > zoneBoundingBox.Max.Z)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if element {element.Id} is in zone: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculates the floor area of a bounding box (X * Y dimensions)
        /// </summary>
        private double GetBoundingBoxArea(BoundingBoxXYZ bbox)
        {
            if (bbox == null)
                return 0.0;

            double width = bbox.Max.X - bbox.Min.X;
            double depth = bbox.Max.Y - bbox.Min.Y;
            return width * depth;
        }

        /// <summary>
        /// Finds all elements within the selected zone bounds.
        /// Returns a dictionary mapping elements to their assigned zone name.
        /// Zones (scope boxes) are sorted by area (largest to smallest) to handle nesting properly.
        /// Smaller (nested) zones will override larger ones for elements they contain.
        /// </summary>
        public IDictionary<Element, string> FindElementsInZones(Document document, FillConfiguration config)
        {
            return FindElementsInZones(document, document, config, null);
        }

        /// <summary>
        /// Finds all elements within the selected zone bounds with linked document support.
        /// Returns a dictionary mapping elements to their assigned zone name.
        /// Zones (scope boxes) are sorted by area (largest to smallest) to handle nesting properly.
        /// Smaller (nested) zones will override larger ones for elements they contain.
        /// </summary>
        /// <param name="targetDocument">Document containing elements to fill parameters on</param>
        /// <param name="sourceDocument">Document containing zones (may be a linked document)</param>
        /// <param name="config">Fill configuration with zone settings</param>
        /// <param name="coordinateTransform">Transform from target to source document coordinates</param>
        /// <returns>Dictionary mapping elements to their assigned zone name</returns>
        public IDictionary<Element, string> FindElementsInZones(
            Document targetDocument,
            Document sourceDocument,
            FillConfiguration config,
            Transform coordinateTransform)
        {
            var result = new Dictionary<Element, string>();

            if (targetDocument == null || sourceDocument == null || config?.ZoneMode == null)
                return result;

            try
            {
                // Get the selected zone IDs
                var zoneIds = config.ZoneMode.SelectedZoneIds;
                if (zoneIds == null || zoneIds.Count == 0)
                {
                    _logger.Warn("No valid zones selected");
                    return result;
                }

                // Get selected categories
                var selectedCategories = config.GetSelectedCategories();
                if (!selectedCategories.Any())
                {
                    _logger.Warn("No categories selected for zone fill");
                    return result;
                }

                // Get all elements from selected categories once (from target document)
                var allElements = new List<Element>();
                foreach (var category in selectedCategories)
                {
                    try
                    {
                        var collector = new FilteredElementCollector(targetDocument)
                            .OfCategory(category)
                            .WhereElementIsNotElementType()
                            .WhereElementIsViewIndependent();

                        allElements.AddRange(collector.ToElements());
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error collecting elements for category {category}: {ex.Message}");
                    }
                }

                // Collect zone elements with their bounding boxes and areas (from source document)
                var zonesWithArea = new List<(Element zone, BoundingBoxXYZ bbox, double area)>();
                foreach (var zoneId in zoneIds)
                {
                    var zone = sourceDocument.GetElement(zoneId);
                    if (zone == null)
                    {
                        _logger.Warn($"Zone with ID {zoneId.IntegerValue} not found");
                        continue;
                    }

                    var boundingBox = GetZoneBoundingBox(zone);
                    if (boundingBox == null)
                    {
                        _logger.Warn($"Could not get bounding box for zone {zone.Name}");
                        continue;
                    }

                    double area = GetBoundingBoxArea(boundingBox);
                    zonesWithArea.Add((zone, boundingBox, area));
                }

                // Sort by area (largest to smallest) to handle nesting
                // Larger zones are processed first, smaller (nested) zones can override
                zonesWithArea = zonesWithArea
                    .OrderByDescending(x => x.area)
                    .ToList();

                _logger.Info($"Processing {zonesWithArea.Count} zones by area (largest to smallest)");

                // Process each zone in sorted order
                foreach (var (zone, boundingBox, _) in zonesWithArea)
                {
                    // Get the fill value - use custom name if configured, otherwise zone name
                    var fillValue = zone.Name;
                    if (config.ZoneMode.CustomZoneNames != null &&
                        config.ZoneMode.CustomZoneNames.TryGetValue(zone.Id, out var customName) &&
                        !string.IsNullOrWhiteSpace(customName))
                    {
                        fillValue = customName;
                    }

                    // Find elements within this zone
                    // Note: We allow override - smaller zones can reassign elements from larger zones
                    int elementsInThisZone = 0;
                    foreach (var element in allElements)
                    {
                        if (IsElementInZone(element, boundingBox, coordinateTransform))
                        {
                            // Always assign/reassign - smaller zones override larger ones
                            result[element] = fillValue;
                            elementsInThisZone++;
                        }
                    }

                    _logger.Info($"Found {elementsInThisZone} elements within zone {zone.Name} (area: {GetBoundingBoxArea(boundingBox):F2})");
                }

                _logger.Info($"Total {result.Count} elements assigned to zones");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error finding elements in zones: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Preview the zone fill operation
        /// </summary>
        public ZoneFillSummary PreviewFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new ZoneFillSummary();

            if (document == null || config?.ZoneMode == null)
            {
                summary.Errors++;
                summary.ErrorMessages.Add("Invalid document or configuration");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            try
            {
                progressAction?.Invoke(0, "Getting zone information...");

                // Get source document and transform for linked document support
                Document sourceDoc = document;
                Transform coordinateTransform = null;
                if (config.General?.SelectedLinkedDocument != null &&
                    !config.General.SelectedLinkedDocument.IsCurrentDocument)
                {
                    sourceDoc = config.General.SelectedLinkedDocument.LinkedDocument;
                    coordinateTransform = config.General.SelectedLinkedDocument.GetInverseTransform();
                }

                // Get the selected zones
                var zoneIds = config.ZoneMode.SelectedZoneIds;
                if (zoneIds == null || zoneIds.Count == 0)
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No zones selected");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                // Build summary of zones
                var zoneNames = new List<string>();
                foreach (var zoneId in zoneIds)
                {
                    var zone = sourceDoc.GetElement(zoneId);
                    if (zone != null)
                    {
                        zoneNames.Add(zone.Name);
                    }
                }

                summary.ZoneName = string.Join(", ", zoneNames);
                summary.FillValue = $"{zoneIds.Count} zone(s)";

                progressAction?.Invoke(10, $"Finding elements in {zoneIds.Count} zone(s)...");

                // Find elements within the zones
                var elementsInZones = FindElementsInZones(document, sourceDoc, config, coordinateTransform);
                summary.ElementsFound = elementsInZones.Count;

                // Get parameters to fill
                var parametersToFill = config.GetZoneModeParameters();
                if (!parametersToFill.Any())
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No parameters mapped to Zone mode");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                progressAction?.Invoke(50, $"Analyzing {summary.ElementsFound} elements...");

                // Count how many parameters would be filled
                int parametersToProcess = summary.ElementsFound * parametersToFill.Count;
                int parametersWouldFill = 0;
                int parametersWouldSkip = 0;

                foreach (var kvp in elementsInZones)
                {
                    var element = kvp.Key;
                    var fillValue = kvp.Value;

                    foreach (var paramName in parametersToFill)
                    {
                        var param = element.LookupParameter(paramName);
                        if (param != null && !param.IsReadOnly)
                        {
                            // Check if we would overwrite
                            if (!config.General.OverwriteExisting && param.HasValue)
                            {
                                parametersWouldSkip++;
                            }
                            else
                            {
                                parametersWouldFill++;
                            }
                        }
                        else
                        {
                            parametersWouldSkip++;
                        }
                    }
                }

                summary.ParametersFilled = parametersWouldFill;
                summary.ParametersSkipped = parametersWouldSkip;

                progressAction?.Invoke(100, "Preview complete");
            }
            catch (Exception ex)
            {
                summary.Errors++;
                summary.ErrorMessages.Add($"Preview failed: {ex.Message}");
                _logger.Error($"Zone preview error: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                summary.ProcessingDuration = stopwatch.Elapsed;
            }

            return summary;
        }

        /// <summary>
        /// Execute the zone fill operation
        /// </summary>
        public ZoneFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new ZoneFillSummary();

            if (document == null || config?.ZoneMode == null)
            {
                summary.Errors++;
                summary.ErrorMessages.Add("Invalid document or configuration");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            try
            {
                progressAction?.Invoke(0, "Getting zone information...");

                // Get source document and transform for linked document support
                Document sourceDoc = document;
                Transform coordinateTransform = null;
                if (config.General?.SelectedLinkedDocument != null &&
                    !config.General.SelectedLinkedDocument.IsCurrentDocument)
                {
                    sourceDoc = config.General.SelectedLinkedDocument.LinkedDocument;
                    coordinateTransform = config.General.SelectedLinkedDocument.GetInverseTransform();
                }

                // Get the selected zones
                var zoneIds = config.ZoneMode.SelectedZoneIds;
                if (zoneIds == null || zoneIds.Count == 0)
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No zones selected");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                // Build summary of zones
                var zoneNames = new List<string>();
                foreach (var zoneId in zoneIds)
                {
                    var zone = sourceDoc.GetElement(zoneId);
                    if (zone != null)
                    {
                        zoneNames.Add(zone.Name);
                    }
                }

                summary.ZoneName = string.Join(", ", zoneNames);
                summary.FillValue = $"{zoneIds.Count} zone(s)";

                progressAction?.Invoke(10, $"Finding elements in {zoneIds.Count} zone(s)...");

                // Find elements within the zones
                var elementsInZones = FindElementsInZones(document, sourceDoc, config, coordinateTransform);
                summary.ElementsFound = elementsInZones.Count;

                // Get parameters to fill
                var parametersToFill = config.GetZoneModeParameters();
                if (!parametersToFill.Any())
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No parameters mapped to Zone mode");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                progressAction?.Invoke(20, $"Starting fill operation on {summary.ElementsFound} elements...");

                // Start a transaction
                using (var transaction = new Transaction(document, "Fill Zone Parameters"))
                {
                    transaction.Start();

                    int processedElements = 0;
                    int totalOperations = elementsInZones.Count * parametersToFill.Count;

                    foreach (var kvp in elementsInZones)
                    {
                        var element = kvp.Key;
                        var fillValue = kvp.Value;

                        foreach (var paramName in parametersToFill)
                        {
                            var param = element.LookupParameter(paramName);

                            if (param != null && !param.IsReadOnly)
                            {
                                // Check if we should overwrite
                                if (config.General.OverwriteExisting || !param.HasValue)
                                {
                                    try
                                    {
                                        // Set the parameter value
                                        param.Set(fillValue);
                                        summary.ParametersFilled++;
                                    }
                                    catch (Exception ex)
                                    {
                                        summary.Errors++;
                                        summary.ErrorMessages.Add($"Failed to set {paramName} on element {element.Id}: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    summary.ParametersSkipped++;
                                }
                            }
                            else
                            {
                                summary.ParametersSkipped++;
                                if (param == null)
                                {
                                    _logger.Info($"Parameter {paramName} not found on element {element.Id}");
                                }
                                else if (param.IsReadOnly)
                                {
                                    _logger.Info($"Parameter {paramName} is read-only on element {element.Id}");
                                }
                            }

                            processedElements++;
                            if (processedElements % 100 == 0)
                            {
                                int progress = 20 + (int)((double)processedElements / totalOperations * 70);
                                progressAction?.Invoke(progress, $"Processed {processedElements}/{totalOperations} parameters...");
                            }
                        }
                    }

                    transaction.Commit();
                }

                // After filling elements in zones, handle elements not in any zone
                // Collect all elements from selected categories
                var allCategoryElements = new List<Element>();
                var selectedCategories = config.GetSelectedCategories();
                foreach (var category in selectedCategories)
                {
                    try
                    {
                        var collector = new FilteredElementCollector(document)
                            .OfCategory(category)
                            .WhereElementIsNotElementType()
                            .WhereElementIsViewIndependent();
                        allCategoryElements.AddRange(collector.ToElements());
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error collecting elements for category {category}: {ex.Message}");
                    }
                }

                // Get element IDs that were processed (in zones)
                var processedElementIds = new HashSet<int>(elementsInZones.Keys.Select(e => e.Id.IntegerValue));

                // Fill with NotAssignedValue for elements not in any zone
                string notAssignedValue = config.ZoneMode?.NotAssignedValue ?? "N/A";
                int elementsNotInZone = 0;
                int parametersFilledWithNA = 0;

                if (allCategoryElements.Count > 0)
                {
                    progressAction?.Invoke(80, "Filling N/A for elements outside zones...");

                    using (var transaction2 = new Transaction(document, "Fill N/A for Non-Zone Elements"))
                    {
                        transaction2.Start();

                        foreach (var element in allCategoryElements)
                        {
                            // Skip if already processed (in a zone)
                            if (processedElementIds.Contains(element.Id.IntegerValue))
                                continue;

                            elementsNotInZone++;

                            foreach (var paramName in parametersToFill)
                            {
                                var param = element.LookupParameter(paramName);

                                if (param != null && !param.IsReadOnly)
                                {
                                    // Check if we should overwrite
                                    if (config.General.OverwriteExisting || !param.HasValue)
                                    {
                                        try
                                        {
                                            param.Set(notAssignedValue);
                                            summary.ParametersFilled++;
                                            parametersFilledWithNA++;
                                        }
                                        catch (Exception ex)
                                        {
                                            summary.Errors++;
                                            summary.ErrorMessages.Add($"Failed to set {paramName} on element {element.Id}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        summary.ParametersSkipped++;
                                    }
                                }
                                else
                                {
                                    summary.ParametersSkipped++;
                                }
                            }
                        }

                        transaction2.Commit();
                    }

                    summary.ElementsNotInZone = elementsNotInZone;
                    summary.ParametersFilledWithNA = parametersFilledWithNA;
                    _logger.Info($"Filled {parametersFilledWithNA} parameters with '{notAssignedValue}' for {elementsNotInZone} elements outside zones");
                }

                progressAction?.Invoke(100, "Fill complete");
                _logger.Info($"Zone fill complete: {summary.ParametersFilled} parameters filled, {summary.ParametersSkipped} skipped");
            }
            catch (Exception ex)
            {
                summary.Errors++;
                summary.ErrorMessages.Add($"Fill failed: {ex.Message}");
                _logger.Error($"Zone fill error: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                summary.ProcessingDuration = stopwatch.Elapsed;
            }

            return summary;
        }
    }
}
