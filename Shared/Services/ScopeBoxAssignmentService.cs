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
    /// Service for scope box-based parameter filling.
    /// Finds elements within scope box bounds and assigns parameters.
    /// Supports multiple scope boxes - each element gets the name of the scope box it's in.
    /// </summary>
    public class ScopeBoxAssignmentService : IScopeBoxAssignmentService
    {
        private readonly ILogger _logger;

        public ScopeBoxAssignmentService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all available scope boxes in the document
        /// </summary>
        public IList<Element> GetScopeBoxes(Document document)
        {
            return GetScopeBoxesFromDocument(document);
        }

        /// <summary>
        /// Gets all available scope boxes in the specified document (for linked document support)
        /// </summary>
        /// <param name="document">The Revit document to get scope boxes from</param>
        /// <returns>List of scope boxes sorted by name</returns>
        public IList<Element> GetScopeBoxesFromDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            try
            {
                // Scope boxes are of type Element with Category OST_VolumeOfInterest
                var scopeBoxCollector = new FilteredElementCollector(document)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                var scopeBoxes = scopeBoxCollector
                    .OrderBy(sb => sb.Name)
                    .ToList();

                _logger.Info($"Found {scopeBoxes.Count} scope boxes in document");
                return scopeBoxes;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting scope boxes: {ex.Message}");
                return new List<Element>();
            }
        }

        /// <summary>
        /// Gets the bounding box of a scope box
        /// </summary>
        public BoundingBoxXYZ GetScopeBoxBoundingBox(Element scopeBox)
        {
            if (scopeBox == null)
                throw new ArgumentNullException(nameof(scopeBox));

            try
            {
                // Scope boxes have a bounding box that can be retrieved directly
                // The bounding box is in model coordinates
                var bbox = scopeBox.get_BoundingBox(null);

                if (bbox == null)
                {
                    _logger.Warn($"Scope box {scopeBox.Name} has no bounding box");
                    return null;
                }

                return bbox;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting bounding box for scope box {scopeBox.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if an element is contained within a scope box's bounding box
        /// </summary>
        public bool IsElementInScopeBox(Element element, BoundingBoxXYZ scopeBoxBoundingBox, double tolerance = 0.0)
        {
            return IsElementInScopeBox(element, scopeBoxBoundingBox, tolerance, null);
        }

        /// <summary>
        /// Checks if an element is contained within a scope box's bounding box
        /// with optional coordinate transformation for linked documents
        /// </summary>
        public bool IsElementInScopeBox(Element element, BoundingBoxXYZ scopeBoxBoundingBox, double tolerance, Transform transform)
        {
            if (element == null || scopeBoxBoundingBox == null)
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

                // Apply tolerance to the scope box bounds
                double minWithTolerance = scopeBoxBoundingBox.Min.X - tolerance;
                double maxWithTolerance = scopeBoxBoundingBox.Max.X + tolerance;

                // Check if element is completely inside the extended scope box bounds
                // We check all three dimensions (X, Y, Z)

                // X dimension
                if (elementBbox.Min.X < minWithTolerance || elementBbox.Max.X > maxWithTolerance)
                    return false;

                // Y dimension
                minWithTolerance = scopeBoxBoundingBox.Min.Y - tolerance;
                maxWithTolerance = scopeBoxBoundingBox.Max.Y + tolerance;
                if (elementBbox.Min.Y < minWithTolerance || elementBbox.Max.Y > maxWithTolerance)
                    return false;

                // Z dimension
                minWithTolerance = scopeBoxBoundingBox.Min.Z - tolerance;
                maxWithTolerance = scopeBoxBoundingBox.Max.Z + tolerance;
                if (elementBbox.Min.Z < minWithTolerance || elementBbox.Max.Z > maxWithTolerance)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if element {element.Id} is in scope box: {ex.Message}");
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
        /// Finds all elements within the selected scope box bounds.
        /// Returns a dictionary mapping elements to their assigned scope box name.
        /// Scope boxes are sorted by area (largest to smallest) to handle nesting properly.
        /// Smaller (nested) scope boxes will override larger ones for elements they contain.
        /// </summary>
        public IDictionary<Element, string> FindElementsInScopeBoxes(Document document, FillConfiguration config)
        {
            return FindElementsInScopeBoxes(document, document, config, null);
        }

        /// <summary>
        /// Finds all elements within the selected scope box bounds with linked document support.
        /// Returns a dictionary mapping elements to their assigned scope box name.
        /// Scope boxes are sorted by area (largest to smallest) to handle nesting properly.
        /// Smaller (nested) scope boxes will override larger ones for elements they contain.
        /// </summary>
        /// <param name="targetDocument">Document containing elements to fill parameters on</param>
        /// <param name="sourceDocument">Document containing scope boxes (may be a linked document)</param>
        /// <param name="config">Fill configuration with scope box settings</param>
        /// <param name="coordinateTransform">Transform from target to source document coordinates</param>
        /// <returns>Dictionary mapping elements to their assigned scope box name</returns>
        public IDictionary<Element, string> FindElementsInScopeBoxes(
            Document targetDocument,
            Document sourceDocument,
            FillConfiguration config,
            Transform coordinateTransform)
        {
            var result = new Dictionary<Element, string>();

            if (targetDocument == null || sourceDocument == null || config?.ScopeBoxMode == null)
                return result;

            try
            {
                // Get the selected scope box IDs
                var scopeBoxIds = config.ScopeBoxMode.SelectedScopeBoxIds;
                if (scopeBoxIds == null || scopeBoxIds.Count == 0)
                {
                    _logger.Warn("No valid scope boxes selected");
                    return result;
                }

                // Get tolerance
                var tolerance = config.ScopeBoxMode.Tolerance;

                // Get selected categories
                var selectedCategories = config.GetSelectedCategories();
                if (!selectedCategories.Any())
                {
                    _logger.Warn("No categories selected for scope box fill");
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

                // Collect scope box elements with their bounding boxes and areas (from source document)
                var scopeBoxesWithArea = new List<(Element scopeBox, BoundingBoxXYZ bbox, double area)>();
                foreach (var scopeBoxId in scopeBoxIds)
                {
                    var scopeBox = sourceDocument.GetElement(scopeBoxId);
                    if (scopeBox == null)
                    {
                        _logger.Warn($"Scope box with ID {scopeBoxId.IntegerValue} not found");
                        continue;
                    }

                    var boundingBox = GetScopeBoxBoundingBox(scopeBox);
                    if (boundingBox == null)
                    {
                        _logger.Warn($"Could not get bounding box for scope box {scopeBox.Name}");
                        continue;
                    }

                    double area = GetBoundingBoxArea(boundingBox);
                    scopeBoxesWithArea.Add((scopeBox, boundingBox, area));
                }

                // Sort by area (largest to smallest) to handle nesting
                // Larger boxes are processed first, smaller (nested) boxes can override
                scopeBoxesWithArea = scopeBoxesWithArea
                    .OrderByDescending(x => x.area)
                    .ToList();

                _logger.Info($"Processing {scopeBoxesWithArea.Count} scope boxes by area (largest to smallest)");

                // Process each scope box in sorted order
                foreach (var (scopeBox, boundingBox, _) in scopeBoxesWithArea)
                {
                    // Get the fill value (scope box name or custom name)
                    var fillValue = scopeBox.Name;
                    if (config.ScopeBoxMode.CustomScopeBoxNames != null &&
                        config.ScopeBoxMode.CustomScopeBoxNames.TryGetValue(scopeBox.Id, out var customName) &&
                        !string.IsNullOrWhiteSpace(customName))
                    {
                        fillValue = customName;
                    }

                    // Find elements within this scope box
                    // Note: We allow override - smaller boxes can reassign elements from larger boxes
                    int elementsInThisBox = 0;
                    foreach (var element in allElements)
                    {
                        if (IsElementInScopeBox(element, boundingBox, tolerance, coordinateTransform))
                        {
                            // Always assign/reassign - smaller boxes override larger ones
                            result[element] = fillValue;
                            elementsInThisBox++;
                        }
                    }

                    _logger.Info($"Found {elementsInThisBox} elements within scope box {scopeBox.Name} (area: {GetBoundingBoxArea(boundingBox):F2})");
                }

                _logger.Info($"Total {result.Count} elements assigned to scope boxes");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error finding elements in scope boxes: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Preview the scope box fill operation
        /// </summary>
        public ScopeBoxFillSummary PreviewFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new ScopeBoxFillSummary();

            if (document == null || config?.ScopeBoxMode == null)
            {
                summary.Errors++;
                summary.ErrorMessages.Add("Invalid document or configuration");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            // Get source document and transform for linked document support
            Document sourceDoc = document;
            Transform coordinateTransform = null;
            if (config.General?.SelectedLinkedDocument != null &&
                !config.General.SelectedLinkedDocument.IsCurrentDocument)
            {
                sourceDoc = config.General.SelectedLinkedDocument.LinkedDocument;
                coordinateTransform = config.General.SelectedLinkedDocument.GetInverseTransform();
                _logger.Info($"Using linked document for scope boxes: {sourceDoc.Title}");
            }

            try
            {
                progressAction?.Invoke(0, "Getting scope box information...");

                // Get the selected scope boxes
                var scopeBoxIds = config.ScopeBoxMode.SelectedScopeBoxIds;
                if (scopeBoxIds == null || scopeBoxIds.Count == 0)
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No scope boxes selected");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                // Build summary of scope boxes
                var scopeBoxNames = new List<string>();
                foreach (var scopeBoxId in scopeBoxIds)
                {
                    var scopeBox = sourceDoc.GetElement(scopeBoxId);
                    if (scopeBox != null)
                    {
                        scopeBoxNames.Add(scopeBox.Name);
                    }
                }

                summary.ScopeBoxName = string.Join(", ", scopeBoxNames);
                summary.FillValue = $"{scopeBoxIds.Count} scope box(es)";

                progressAction?.Invoke(10, $"Finding elements in {scopeBoxIds.Count} scope box(es)...");

                // Find elements within the scope boxes
                var elementsInScopeBoxes = FindElementsInScopeBoxes(document, sourceDoc, config, coordinateTransform);
                summary.ElementsFound = elementsInScopeBoxes.Count;

                // Get parameters to fill
                var parametersToFill = config.GetScopeBoxModeParameters();
                if (!parametersToFill.Any())
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No parameters mapped to Scope Box mode");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                progressAction?.Invoke(50, $"Analyzing {summary.ElementsFound} elements...");

                // Count how many parameters would be filled
                int parametersToProcess = summary.ElementsFound * parametersToFill.Count;
                int parametersWouldFill = 0;
                int parametersWouldSkip = 0;

                foreach (var kvp in elementsInScopeBoxes)
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
                _logger.Error($"Scope box preview error: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                summary.ProcessingDuration = stopwatch.Elapsed;
            }

            return summary;
        }

        /// <summary>
        /// Execute the scope box fill operation
        /// </summary>
        public ScopeBoxFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new ScopeBoxFillSummary();

            if (document == null || config?.ScopeBoxMode == null)
            {
                summary.Errors++;
                summary.ErrorMessages.Add("Invalid document or configuration");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            try
            {
                progressAction?.Invoke(0, "Getting scope box information...");

                // Get source document and transform for linked document support
                Document sourceDoc = document;
                Transform coordinateTransform = null;
                if (config.General?.SelectedLinkedDocument != null &&
                    !config.General.SelectedLinkedDocument.IsCurrentDocument)
                {
                    sourceDoc = config.General.SelectedLinkedDocument.LinkedDocument;
                    coordinateTransform = config.General.SelectedLinkedDocument.GetInverseTransform();
                }

                // Get the selected scope boxes
                var scopeBoxIds = config.ScopeBoxMode.SelectedScopeBoxIds;
                if (scopeBoxIds == null || scopeBoxIds.Count == 0)
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No scope boxes selected");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                // Build summary of scope boxes
                var scopeBoxNames = new List<string>();
                foreach (var scopeBoxId in scopeBoxIds)
                {
                    var scopeBox = sourceDoc.GetElement(scopeBoxId);
                    if (scopeBox != null)
                    {
                        scopeBoxNames.Add(scopeBox.Name);
                    }
                }

                summary.ScopeBoxName = string.Join(", ", scopeBoxNames);
                summary.FillValue = $"{scopeBoxIds.Count} scope box(es)";

                progressAction?.Invoke(10, $"Finding elements in {scopeBoxIds.Count} scope box(es)...");

                // Find elements within the scope boxes
                var elementsInScopeBoxes = FindElementsInScopeBoxes(document, sourceDoc, config, coordinateTransform);
                summary.ElementsFound = elementsInScopeBoxes.Count;

                // Get parameters to fill
                var parametersToFill = config.GetScopeBoxModeParameters();
                if (!parametersToFill.Any())
                {
                    summary.Errors++;
                    summary.ErrorMessages.Add("No parameters mapped to Scope Box mode");
                    summary.ProcessingDuration = stopwatch.Elapsed;
                    return summary;
                }

                progressAction?.Invoke(20, $"Starting fill operation on {summary.ElementsFound} elements...");

                // Start a transaction
                using (var transaction = new Transaction(document, "Fill Scope Box Parameters"))
                {
                    transaction.Start();

                    int processedElements = 0;
                    int totalOperations = elementsInScopeBoxes.Count * parametersToFill.Count;

                    foreach (var kvp in elementsInScopeBoxes)
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

                progressAction?.Invoke(100, "Fill complete");
                _logger.Info($"Scope box fill complete: {summary.ParametersFilled} parameters filled, {summary.ParametersSkipped} skipped");
            }
            catch (Exception ex)
            {
                summary.Errors++;
                summary.ErrorMessages.Add($"Fill failed: {ex.Message}");
                _logger.Error($"Scope box fill error: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                summary.ProcessingDuration = stopwatch.Elapsed;
            }

            return summary;
        }

        /// <summary>
        /// Finds all elements within the selected scope box bounds (legacy method for backward compatibility)
        /// </summary>
        [Obsolete("Use FindElementsInScopeBoxes instead")]
        public IDictionary<Element, string> FindElementsInScopeBox(Document document, FillConfiguration config)
        {
            return FindElementsInScopeBoxes(document, document, config, null);
        }
    }
}
