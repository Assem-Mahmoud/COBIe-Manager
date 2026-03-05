using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for filling room parameters based on room ownership/association
    /// </summary>
    public class RoomFillService : IRoomFillService
    {
        private readonly ILogger _logger;
        private readonly IRoomAssignmentService _roomAssignmentService;
        private readonly ILinkedDocumentService _linkedDocumentService;

        public RoomFillService(
            ILogger logger,
            IRoomAssignmentService roomAssignmentService,
            ILinkedDocumentService linkedDocumentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roomAssignmentService = roomAssignmentService ?? throw new ArgumentNullException(nameof(roomAssignmentService));
            _linkedDocumentService = linkedDocumentService ?? throw new ArgumentNullException(nameof(linkedDocumentService));
        }

        /// <summary>
        /// Gets the source document for room queries based on selected linked document
        /// </summary>
        private (Document sourceDoc, Transform transform) GetSourceDocumentAndTransform(Document hostDocument, FillConfiguration config)
        {
            if (config?.General?.SelectedLinkedDocument == null ||
                config.General.SelectedLinkedDocument.IsCurrentDocument)
            {
                return (hostDocument, null);
            }

            var linkedDoc = config.General.SelectedLinkedDocument.LinkedDocument;
            var transform = config.General.SelectedLinkedDocument.GetInverseTransform();
            return (linkedDoc, transform);
        }

        /// <summary>
        /// Analyzes elements and returns preview summary without modifying document
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration containing selected categories and parameters</param>
        /// <returns>Preview summary with estimated counts</returns>
        public RoomFillPreviewSummary PreviewFill(Document document, FillConfiguration config)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Clear the room cache to ensure fresh data
            _roomAssignmentService.ClearCache();

            // Clear any previous wall-room associations
            _wallRoomAssociations.Clear();

            _logger.Info("Starting room-only fill preview analysis");

            var summary = new RoomFillPreviewSummary();

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                summary.ValidationWarnings.Add(error ?? "Invalid configuration");
                _logger.Warn($"Preview configuration invalid: {error}");
                return summary;
            }

            // Get selected categories
            var selectedCategories = config.GetSelectedCategories();
            if (selectedCategories.Count == 0)
            {
                summary.ValidationWarnings.Add("At least one category must be selected");
                _logger.Warn("No categories selected for room fill preview");
                return summary;
            }

            // Get source document (may be linked)
            var (sourceDoc, transform) = GetSourceDocumentAndTransform(document, config);
            bool usingLinkedDocument = !document.Equals(sourceDoc);

            if (usingLinkedDocument)
            {
                _logger.Info($"Using linked document for room detection: {sourceDoc.Title}");
            }

            // Collect elements
            var elements = CollectElements(document, selectedCategories, summary);

            // Check if Walls category is selected - if so, also collect room boundary walls
            // The method now handles both current document (boundary segments) and linked documents (spatial matching)
            bool wallsCategorySelected = selectedCategories.Contains(BuiltInCategory.OST_Walls);
            if (wallsCategorySelected)
            {
                var roomBoundaryWalls = CollectRoomBoundaryWalls(document, sourceDoc, transform, elements, summary);
                elements = elements.Union(roomBoundaryWalls).ToList();
                _logger.Info($"Preview: Added {roomBoundaryWalls.Count} room boundary walls to processing list");
            }

            if (!elements.Any())
            {
                _logger.Info("No elements found for room fill preview");
                return summary;
            }

            _logger.Info($"Previewing {elements.Count} elements for room assignment");

            int processedCount = 0;
            int roomsFoundCount = 0;
            int noRoomFoundCount = 0;

            foreach (var element in elements)
            {
                processedCount++;

                // Report progress every 100 elements
                if (processedCount % 100 == 0)
                {
                    _logger.Debug($"Room fill preview progress: {processedCount}/{elements.Count} elements analyzed");
                }

                // Try to find room for element
                Room room = null;

                // For walls, use the pre-computed wall-room associations
                if (element.Category != null && element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls)
                {
                    int wallId = element.Id.IntegerValue;
                    if (_wallRoomAssociations.TryGetValue(wallId, out room))
                    {
                        _logger.Debug($"Preview: Element {element.Id} (Wall): Room found via boundary association");
                    }
                }
                else
                {
                    // For other elements, use the standard room detection with linked document support
                    // sourceDoc and transform are already declared in outer scope
                    double roomTolerance = GetRoomTolerance(config);
                    room = _roomAssignmentService.GetRoomForElement(element, sourceDoc, RoomDetectionMethod.PointInRoom, transform, roomTolerance);
                }

                if (room != null)
                {
                    roomsFoundCount++;
                }
                else
                {
                    noRoomFoundCount++;
                }
            }

            summary.EstimatedElementsToProcess = elements.Count;
            summary.EstimatedRoomsFound = roomsFoundCount;
            summary.EstimatedNoRoomFound = noRoomFoundCount;

            _logger.Info($"Room fill preview complete: {roomsFoundCount} elements with rooms, {noRoomFoundCount} without rooms");

            return summary;
        }

        /// <summary>
        /// Executes room parameter fill operation
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Processing summary with actual results</returns>
        public RoomFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Clear the room cache to ensure fresh data
            _roomAssignmentService.ClearCache();

            // Clear any previous wall-room associations
            _wallRoomAssociations.Clear();

            _logger.Info("Starting room-only fill operation");

            var stopwatch = Stopwatch.StartNew();
            var summary = new RoomFillSummary();
            var uniqueRooms = new HashSet<string>();

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                _logger.Error($"Configuration invalid: {error}");
                throw new InvalidOperationException($"Invalid configuration: {error}");
            }

            // Get selected categories
            var selectedCategories = config.GetSelectedCategories();
            if (selectedCategories.Count == 0)
            {
                _logger.Warn("No categories selected for room fill");
                return summary;
            }

            // Get the appropriate parameters based on FillMode using bitwise operations
            // Support multiple modes simultaneously by combining parameters from all enabled modes
            // Get ParameterItems instead of just names to preserve ApplicableMode information
            var allSelectedParameters = new List<ParameterItem>();
            var fillTypes = new List<string>();

            bool hasRoomNameMode = (config.FillMode & FillMode.RoomName) != 0;
            bool hasRoomNumberMode = (config.FillMode & FillMode.RoomNumber) != 0;

            // Collect parameters from all enabled modes
            if (hasRoomNameMode)
            {
                var roomNameParams = config.GetSelectedParameters().Where(p => p.ApplicableMode == FillMode.RoomName).ToList();
                allSelectedParameters.AddRange(roomNameParams);
                fillTypes.Add("room name");
                _logger.Info($"RoomName mode enabled: {roomNameParams.Count} parameters");
            }

            if (hasRoomNumberMode)
            {
                var roomNumberParams = config.GetSelectedParameters().Where(p => p.ApplicableMode == FillMode.RoomNumber).ToList();
                allSelectedParameters.AddRange(roomNumberParams);
                fillTypes.Add("room number");
                _logger.Info($"RoomNumber mode enabled: {roomNumberParams.Count} parameters");
            }

            // Fallback - if no modes enabled, use all room parameters
            if (!hasRoomNameMode && !hasRoomNumberMode)
            {
                var allRoomParams = config.GetSelectedParameters()
                    .Where(p => p.ApplicableMode == FillMode.RoomName || p.ApplicableMode == FillMode.RoomNumber)
                    .ToList();
                allSelectedParameters.AddRange(allRoomParams);
                fillTypes.Add("room");
            }

            if (allSelectedParameters.Count == 0)
            {
                _logger.Warn("No parameters selected for room fill");
                return summary;
            }

            string fillType = string.Join(" and ", fillTypes);
            var paramNames = allSelectedParameters.Select(p => p.ParameterName).ToList();
            var paramDetails = allSelectedParameters.Select(p => $"'{p.ParameterName}' (Mode={p.ApplicableMode})").ToList();
            _logger.Info($"Filling {allSelectedParameters.Count} {fillType} parameters: {string.Join(", ", paramDetails)}");

            // Get source document (may be linked)
            var (sourceDoc, transform) = GetSourceDocumentAndTransform(document, config);
            bool usingLinkedDocument = !document.Equals(sourceDoc);

            if (usingLinkedDocument)
            {
                _logger.Info($"Using linked document for room detection: {sourceDoc.Title}");
            }

            // Collect elements
            var previewSummary = new RoomFillPreviewSummary();
            var elements = CollectElements(document, selectedCategories, previewSummary);

            // Check if Walls category is selected - if so, also collect room boundary walls
            // The method now handles both current document (boundary segments) and linked documents (spatial matching)
            bool wallsCategorySelected = selectedCategories.Contains(BuiltInCategory.OST_Walls);
            if (wallsCategorySelected)
            {
                var roomBoundaryWalls = CollectRoomBoundaryWalls(document, sourceDoc, transform, elements, previewSummary);
                elements = elements.Union(roomBoundaryWalls).ToList();
                _logger.Info($"Added {roomBoundaryWalls.Count} room boundary walls to processing list");
            }

            if (!elements.Any())
            {
                _logger.Warn("No elements found to process");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            _logger.Info($"Processing {elements.Count} elements for room parameter fill");

            summary.TotalElementsScanned = elements.Count;

            // Create processing logger to track results
            var processingLogger = new ProcessingLogger(_logger);

            // Use transaction for parameter modification
            using (var transaction = new Transaction(document, "Auto-Fill Room Parameters"))
            {
                transaction.Start();

                try
                {
                    int processedCount = 0;

                    foreach (var element in elements)
                    {
                        processedCount++;

                        // Report progress every 100 elements
                        if (processedCount % 100 == 0)
                        {
                            var message = $"Processing element {processedCount} of {elements.Count}";
                            _logger.Debug(message);
                            progressAction?.Invoke(processedCount, message);
                        }

                        // Try to find room for element
                        Room room = null;

                        // For walls, use the pre-computed wall-room associations
                        if (element.Category != null && element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls)
                        {
                            int wallId = element.Id.IntegerValue;
                            if (_wallRoomAssociations.TryGetValue(wallId, out room))
                            {
                                _logger.Debug($"Element {element.Id} (Wall): Room found via boundary association - '{room.Number}: {room.Name}'");
                            }
                            else
                            {
                                _logger.Debug($"Element {element.Id} (Wall): No boundary room association found");
                            }
                        }
                        else
                        {
                            // For other elements, use the standard room detection with linked document support
                            // sourceDoc and transform are already declared in outer scope
                            double roomTolerance = GetRoomTolerance(config);
                            room = _roomAssignmentService.GetRoomForElement(element, sourceDoc, RoomDetectionMethod.PointInRoom, transform, roomTolerance);
                        }

                        if (room != null)
                        {
                            // Track unique rooms
                            var roomKey = $"{room.Number}|{room.Name}";
                            if (!uniqueRooms.Contains(roomKey))
                            {
                                uniqueRooms.Add(roomKey);
                            }

                            // Fill each selected parameter individually
                            int parametersAssigned = 0;
                            foreach (var paramItem in allSelectedParameters)
                            {
                                // Determine what value to fill based on the parameter's ApplicableMode
                                _logger.Debug($"Processing parameter '{paramItem.ParameterName}' with ApplicableMode={paramItem.ApplicableMode}");
                                string valueToFill = GetRoomValueForParameter(paramItem.ApplicableMode, room);

                                if (valueToFill != null)
                                {
                                    // Try to set the parameter
                                    if (TrySetParameter(element, paramItem.ParameterName, valueToFill, config.OverwriteExisting))
                                    {
                                        parametersAssigned++;

                                        // Track statistics based on parameter's ApplicableMode
                                        if (paramItem.ApplicableMode == FillMode.RoomName)
                                        {
                                            summary.RoomNameParametersFilled++;
                                        }
                                        else if (paramItem.ApplicableMode == FillMode.RoomNumber)
                                        {
                                            summary.RoomNumberParametersFilled++;
                                        }
                                    }
                                }
                            }

                            if (parametersAssigned > 0)
                            {
                                summary.ElementsUpdated++;
                            }
                        }
                        else
                        {
                            // No room found - fill with NotAssignedValue instead of skipping
                            // Get the appropriate NotAssignedValue based on which modes are enabled
                            string notAssignedValue = GetNotAssignedValue(config, hasRoomNameMode, hasRoomNumberMode);

                            _logger.Debug($"Element {element.Id}: No room found, filling with '{notAssignedValue}'");

                            // Fill each selected parameter with NotAssignedValue
                            int parametersAssigned = 0;
                            foreach (var paramItem in allSelectedParameters)
                            {
                                // Try to set the parameter with NotAssignedValue
                                if (TrySetParameter(element, paramItem.ParameterName, notAssignedValue, config.OverwriteExisting))
                                {
                                    parametersAssigned++;

                                    // Track statistics - count as N/A fills instead of skips
                                    if (paramItem.ApplicableMode == FillMode.RoomName)
                                    {
                                        summary.RoomNameParametersFilled++;
                                        summary.RoomNameParametersFilledWithNA++;
                                    }
                                    else if (paramItem.ApplicableMode == FillMode.RoomNumber)
                                    {
                                        summary.RoomNumberParametersFilled++;
                                        summary.RoomNumberParametersFilledWithNA++;
                                    }
                                }
                            }

                            if (parametersAssigned > 0)
                            {
                                summary.ElementsUpdated++;
                                summary.ElementsFilledWithNA++;
                            }
                            else
                            {
                                // Determine skip reason for logging purposes
                                var point = GetElementPoint(element);
                                if (point == null)
                                {
                                    summary.SkippedNoLocation++;
                                    TrackSkippedElement(summary, "NoLocation", element.Id.IntegerValue);
                                }
                                else
                                {
                                    summary.SkippedNoRoomFound++;
                                    TrackSkippedElement(summary, "NoRoomFound", element.Id.IntegerValue);
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    _logger.Info($"Transaction committed: Room parameter fill completed");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during room parameter fill operation", ex);
                    transaction.RollBack();
                    throw;
                }
            }

            summary.UniqueRoomsFound = uniqueRooms.Count;
            summary.ProcessingDuration = stopwatch.Elapsed;

            _logger.Info($"Room fill complete in {summary.ProcessingDuration.TotalSeconds:F2} seconds: " +
                        $"{summary.ElementsUpdated} elements updated, {summary.UniqueRoomsFound} unique rooms");

            return summary;
        }

        /// <summary>
        /// Collects elements from the document based on selected categories
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="selectedCategories">Categories to collect</param>
        /// <param name="previewSummary">Preview summary to populate with empty categories</param>
        /// <returns>Filtered elements</returns>
        private IList<Element> CollectElements(
            Document document,
            IList<BuiltInCategory> selectedCategories,
            RoomFillPreviewSummary previewSummary)
        {
            var allElements = new List<Element>();

            // Use category filter for each selected category
            foreach (var category in selectedCategories)
            {
                var collector = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .OfCategory(category);

                var categoryElements = collector.ToElements();

                if (!categoryElements.Any())
                {
                    var categoryName = category.ToString().Replace("OST_", "");
                    previewSummary.CategoriesWithNoElements.Add(categoryName);
                    _logger.Debug($"No elements found for category: {categoryName}");
                }
                else
                {
                    allElements.AddRange(categoryElements);
                    _logger.Debug($"Found {categoryElements.Count} elements in category: {category}");
                }
            }

            return allElements;
        }

        /// <summary>
        /// Dictionary to track which room each wall is associated with
        /// Key: Wall Element Id, Value: Room
        /// </summary>
        private Dictionary<int, Room> _wallRoomAssociations = new Dictionary<int, Room>();

        /// <summary>
        /// Collects walls that are room boundaries and associates them with their adjacent rooms
        /// </summary>
        /// <param name="hostDocument">The host/target document containing walls</param>
        /// <param name="sourceDocument">The source document containing rooms (may be linked)</param>
        /// <param name="coordinateTransform">Transform from host to source document (for linked docs)</param>
        /// <param name="currentElements">Currently collected elements (to filter out duplicates)</param>
        /// <param name="previewSummary">Preview summary to populate with empty categories</param>
        /// <returns>List of room boundary walls</returns>
        private IList<Element> CollectRoomBoundaryWalls(
            Document hostDocument,
            Document sourceDocument,
            Transform coordinateTransform,
            IList<Element> currentElements,
            RoomFillPreviewSummary previewSummary)
        {
            var boundaryWalls = new List<Element>();
            var currentElementIds = new HashSet<int>(currentElements.Select(e => e.Id.IntegerValue));

            _wallRoomAssociations.Clear();

            bool usingLinkedDocument = !hostDocument.Equals(sourceDocument);

            try
            {
                // Collect all rooms in the source document (may be linked)
                var rooms = new FilteredElementCollector(sourceDocument)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .OfType<Room>()
                    .Where(r => r.Area > 0 && r.Location != null)
                    .ToList();

                if (usingLinkedDocument)
                {
                    _logger.Info($"Found {rooms.Count} rooms in linked document '{sourceDocument.Title}' for spatial matching");
                }
                else
                {
                    _logger.Info($"Found {rooms.Count} rooms in current document for boundary wall detection");
                }

                // Collect all walls from the host document for spatial matching
                IList<Element> hostWalls = null;
                if (usingLinkedDocument)
                {
                    hostWalls = new FilteredElementCollector(hostDocument)
                        .OfCategory(BuiltInCategory.OST_Walls)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .ToElements();
                    _logger.Info($"Collected {hostWalls.Count} walls from host document for spatial matching");
                }

                // Process each room
                foreach (var room in rooms)
                {
                    try
                    {
                        if (usingLinkedDocument)
                        {
                            // SPATIAL MATCHING for linked documents
                            // Get room bounding box and find host walls within it
                            var roomBbox = room.get_BoundingBox(null);
                            if (roomBbox != null && hostWalls != null)
                            {
                                // Transform bounding box to host document space
                                BoundingBoxXYZ hostBbox = TransformBoundingBoxToHostSpace(roomBbox, coordinateTransform);

                                // Find walls within this room's bounding box
                                var wallsInRoom = FindElementsInBoundingBox(hostWalls, hostBbox);

                                foreach (var wall in wallsInRoom)
                                {
                                    int wallId = wall.Id.IntegerValue;

                                    // Add to boundary walls list if not already processed
                                    if (!currentElementIds.Contains(wallId) && !boundaryWalls.Any(w => w.Id.IntegerValue == wallId))
                                    {
                                        boundaryWalls.Add(wall);
                                    }

                                    // Store wall-room association (prefer first room found)
                                    if (!_wallRoomAssociations.ContainsKey(wallId))
                                    {
                                        _wallRoomAssociations[wallId] = room;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // BOUNDARY SEGMENT approach for current document
                            // Get boundary segments for the room at all levels
                            var boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

                            foreach (var boundarySegmentList in boundarySegments)
                            {
                                foreach (var segment in boundarySegmentList)
                                {
                                    // Try to get the element associated with this boundary segment
                                    Element element = null;
                                    try
                                    {
                                        // Get the element through the ElementId property (Revit 2024+)
                                        #if REVIT_2024 || REVIT_2025 || REVIT_2026
                                        element = sourceDocument.GetElement(segment.ElementId);
                                        #else
                                        // For older Revit versions, try GetElement method if available
                                        try
                                        {
                                            element = segment.GetElement();
                                        }
                                        catch
                                        {
                                            // Fallback: try getting element through ElementId
                                            element = sourceDocument.GetElement(segment.ElementId);
                                        }
                                        #endif
                                    }
                                    catch
                                    {
                                        // If getting element fails, skip this segment
                                        continue;
                                    }

                                    if (element != null && element.Category != null)
                                    {
                                        // Check if the boundary element is a wall
                                        if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls)
                                        {
                                            int wallId = element.Id.IntegerValue;

                                            // Add to boundary walls list if not already processed
                                            if (!currentElementIds.Contains(wallId) && !boundaryWalls.Any(w => w.Id.IntegerValue == wallId))
                                            {
                                                boundaryWalls.Add(element);
                                            }

                                            // Store wall-room association (prefer first room found)
                                            if (!_wallRoomAssociations.ContainsKey(wallId))
                                            {
                                                _wallRoomAssociations[wallId] = room;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Error processing room {room.Id}: {ex.Message}");
                    }
                }

                _logger.Info($"Found {boundaryWalls.Count} unique room boundary walls");
            }
            catch (Exception ex)
            {
                _logger.Error("Error collecting room boundary walls", ex);
            }

            return boundaryWalls;
        }

        /// <summary>
        /// Transforms a bounding box from linked document space to host document space
        /// </summary>
        private BoundingBoxXYZ TransformBoundingBoxToHostSpace(BoundingBoxXYZ linkedBbox, Transform inverseTransform)
        {
            if (linkedBbox == null)
                return null;

            if (inverseTransform == null)
                return linkedBbox;

            try
            {
                // The inverseTransform converts from host to linked, so we need the inverse of that
                // to convert from linked to host
                var hostTransform = inverseTransform.Inverse;

                var hostBbox = new BoundingBoxXYZ
                {
                    Min = hostTransform.OfPoint(linkedBbox.Min),
                    Max = hostTransform.OfPoint(linkedBbox.Max),
                    Transform = Transform.Identity
                };

                // Ensure Min is actually the minimum and Max is the maximum
                if (hostBbox.Min.X > hostBbox.Max.X)
                {
                    var temp = hostBbox.Min.X;
                    hostBbox.Min = new XYZ(hostBbox.Max.X, hostBbox.Min.Y, hostBbox.Min.Z);
                    hostBbox.Max = new XYZ(temp, hostBbox.Max.Y, hostBbox.Max.Z);
                }
                if (hostBbox.Min.Y > hostBbox.Max.Y)
                {
                    var temp = hostBbox.Min.Y;
                    hostBbox.Min = new XYZ(hostBbox.Min.X, hostBbox.Max.Y, hostBbox.Min.Z);
                    hostBbox.Max = new XYZ(hostBbox.Max.X, temp, hostBbox.Max.Z);
                }
                if (hostBbox.Min.Z > hostBbox.Max.Z)
                {
                    var temp = hostBbox.Min.Z;
                    hostBbox.Min = new XYZ(hostBbox.Min.X, hostBbox.Min.Y, hostBbox.Max.Z);
                    hostBbox.Max = new XYZ(hostBbox.Max.X, hostBbox.Max.Y, temp);
                }

                return hostBbox;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error transforming bounding box: {ex.Message}");
                return linkedBbox;
            }
        }

        /// <summary>
        /// Finds elements that are within or intersect a bounding box
        /// </summary>
        private IList<Element> FindElementsInBoundingBox(IList<Element> elements, BoundingBoxXYZ bbox)
        {
            var result = new List<Element>();

            if (elements == null || bbox == null)
                return result;

            double tolerance = 0.0001;

            foreach (var element in elements)
            {
                try
                {
                    var elementBbox = element.get_BoundingBox(null);
                    if (elementBbox == null)
                        continue;

                    if (BoundingBoxesIntersect(elementBbox, bbox, tolerance))
                    {
                        result.Add(element);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error checking element {element.Id}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if two bounding boxes intersect
        /// </summary>
        private bool BoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2, double tolerance)
        {
            bool xIntersect = box1.Max.X >= box2.Min.X - tolerance && box1.Min.X <= box2.Max.X + tolerance;
            bool yIntersect = box1.Max.Y >= box2.Min.Y - tolerance && box1.Min.Y <= box2.Max.Y + tolerance;
            bool zIntersect = box1.Max.Z >= box2.Min.Z - tolerance && box1.Min.Z <= box2.Max.Z + tolerance;

            return xIntersect && yIntersect && zIntersect;
        }

        /// <summary>
        /// Gets the center point of an element for room detection
        /// </summary>
        /// <param name="element">Element to get point from</param>
        /// <returns>XYZ point if available, null otherwise</returns>
        private XYZ GetElementPoint(Element element)
        {
            if (element == null)
            {
                return null;
            }

            // Try LocationPoint first
            var locationPoint = element.Location as LocationPoint;
            if (locationPoint != null)
            {
                return locationPoint.Point;
            }

            // Try LocationCurve for elements like doors/windows
            var locationCurve = element.Location as LocationCurve;
            if (locationCurve != null)
            {
                var curve = locationCurve.Curve;
                if (curve != null)
                {
                    // Use the midpoint of the curve
                    return curve.Evaluate(0.5, true);
                }
            }

            // Fallback to bounding box center
            var bbox = element.get_BoundingBox(null);
            if (bbox != null)
            {
                var center = new XYZ(
                    (bbox.Min.X + bbox.Max.X) / 2,
                    (bbox.Min.Y + bbox.Max.Y) / 2,
                    (bbox.Min.Z + bbox.Max.Z) / 2
                );
                return center;
            }

            return null;
        }

        /// <summary>
        /// Determines what room value to fill based on the parameter's ApplicableMode
        /// </summary>
        /// <param name="applicableMode">The FillMode this parameter is mapped to</param>
        /// <param name="room">Room object</param>
        /// <returns>Value to fill, or null if parameter type is unknown</returns>
        private string GetRoomValueForParameter(FillMode applicableMode, Room room)
        {
            if (room == null)
            {
                return null;
            }

            // Use the parameter's ApplicableMode to determine what value to fill
            // This ensures that parameters mapped to Room Name mode get room names,
            // and parameters mapped to Room Number mode get room numbers.
            string result = null;
            switch (applicableMode)
            {
                case FillMode.RoomName:
                    result = room.Name;
                    _logger.Debug($"GetRoomValueForParameter: ApplicableMode=RoomName, returning room.Name='{result}'");
                    return result;

                case FillMode.RoomNumber:
                    result = room.Number;
                    _logger.Debug($"GetRoomValueForParameter: ApplicableMode=RoomNumber, returning room.Number='{result}'");
                    return result;

                default:
                    // Unknown mode - log warning and return null
                    _logger.Warn($"Unknown ApplicableMode '{applicableMode}' for room parameter fill");
                    return null;
            }
        }

        /// <summary>
        /// Checks if a parameter is a room name parameter
        /// </summary>
        private bool IsRoomNameParameter(string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var roomNameParams = new[] { "ACG-4D-RoomName", "Room Name", "RoomName", "RoomName" };
            return roomNameParams.Any(p => paramName.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
                   paramName.ToLower().Contains("roomname") ||
                   (paramName.ToLower().Contains("room") && paramName.ToLower().Contains("name") && !paramName.ToLower().Contains("number"));
        }

        /// <summary>
        /// Checks if a parameter is a room number parameter
        /// </summary>
        private bool IsRoomNumberParameter(string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var roomNumberParams = new[] { "ACG-4D-RoomNumber", "Room Number", "RoomNumber", "RoomNumber" };
            return roomNumberParams.Any(p => paramName.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
                   paramName.ToLower().Contains("roomnumber") ||
                   (paramName.ToLower().Contains("room") && paramName.ToLower().Contains("number"));
        }

        /// <summary>
        /// Checks if a parameter is a room reference parameter
        /// </summary>
        private bool IsRoomRefParameter(string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var roomRefParams = new[] { "ACG-4D-RoomRef", "Room Ref", "RoomRef", "Room Reference", "RoomReference" };
            return roomRefParams.Any(p => paramName.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
                   paramName.ToLower().Contains("roomref");
        }

        /// <summary>
        /// Safely sets a parameter value on an element
        /// </summary>
        /// <param name="element">Element to modify</param>
        /// <param name="parameterName">Name of parameter to set</param>
        /// <param name="value">Value to set</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>True if parameter was set, false otherwise</returns>
        private bool TrySetParameter(Element element, string parameterName, string value, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(parameterName) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parameter = element.LookupParameter(parameterName);

            if (parameter == null)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' not found");
                return false;
            }

            if (parameter.IsReadOnly)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' is read-only");
                return false;
            }

            // Check if value exists and overwrite is disabled
            if (!overwrite)
            {
                var currentValue = parameter.AsString();
                if (!string.IsNullOrWhiteSpace(currentValue))
                {
                    _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' has existing value, overwrite disabled");
                    return false;
                }
            }

            try
            {
                parameter.Set(value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Element {element.Id}: Failed to set parameter '{parameterName}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Tracks a skipped element by its reason
        /// </summary>
        private void TrackSkippedElement(RoomFillSummary summary, string reason, int elementId)
        {
            if (!summary.SkippedElementIds.ContainsKey(reason))
            {
                summary.SkippedElementIds[reason] = new List<int>();
            }
            summary.SkippedElementIds[reason].Add(elementId);
        }

        /// <summary>
        /// Gets the appropriate tolerance value for room detection based on enabled room modes.
        /// Returns the maximum tolerance if both room modes are enabled.
        /// </summary>
        /// <param name="config">Fill configuration containing mode settings</param>
        /// <returns>Tolerance value in feet (internal Revit units)</returns>
        private double GetRoomTolerance(FillConfiguration config)
        {
            if (config == null)
                return 0.0;

            double tolerance = 0.0;

            // Use RoomNameMode tolerance if enabled
            if (config.RoomNameMode?.IsEnabled == true && config.RoomNameMode.Tolerance > tolerance)
            {
                tolerance = config.RoomNameMode.Tolerance;
            }

            // Use RoomNumberMode tolerance if enabled and greater
            if (config.RoomNumberMode?.IsEnabled == true && config.RoomNumberMode.Tolerance > tolerance)
            {
                tolerance = config.RoomNumberMode.Tolerance;
            }

            return tolerance;
        }

        /// <summary>
        /// Gets the NotAssignedValue to use for elements that don't match the filter.
        /// Uses the value from the enabled mode, or defaults to "N/A".
        /// </summary>
        /// <param name="config">Fill configuration containing mode settings</param>
        /// <param name="hasRoomNameMode">Whether RoomName mode is enabled</param>
        /// <param name="hasRoomNumberMode">Whether RoomNumber mode is enabled</param>
        /// <returns>The value to use for non-matching elements</returns>
        private string GetNotAssignedValue(FillConfiguration config, bool hasRoomNameMode, bool hasRoomNumberMode)
        {
            // Prefer RoomNameMode's NotAssignedValue if enabled
            if (hasRoomNameMode && config.RoomNameMode != null)
            {
                return config.RoomNameMode.NotAssignedValue ?? "N/A";
            }

            // Fall back to RoomNumberMode's NotAssignedValue if enabled
            if (hasRoomNumberMode && config.RoomNumberMode != null)
            {
                return config.RoomNumberMode.NotAssignedValue ?? "N/A";
            }

            // Default to "N/A"
            return "N/A";
        }
    }
}
