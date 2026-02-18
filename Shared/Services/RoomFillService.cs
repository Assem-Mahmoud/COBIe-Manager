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

        public RoomFillService(
            ILogger logger,
            IRoomAssignmentService roomAssignmentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roomAssignmentService = roomAssignmentService ?? throw new ArgumentNullException(nameof(roomAssignmentService));
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

            // Collect elements
            var elements = CollectElements(document, selectedCategories, summary);
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
                var room = _roomAssignmentService.GetRoomForElement(element, RoomDetectionMethod.PointInRoom);
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
            IList<string> selectedParameters;
            string fillType;

            bool hasRoomNameMode = (config.FillMode & FillMode.RoomName) != 0;
            bool hasRoomNumberMode = (config.FillMode & FillMode.RoomNumber) != 0;

            // Prioritize RoomName mode if set, otherwise RoomNumber
            if (hasRoomNameMode)
            {
                selectedParameters = config.GetRoomNameModeParameters();
                fillType = "room name";
            }
            else if (hasRoomNumberMode)
            {
                selectedParameters = config.GetRoomNumberModeParameters();
                fillType = "room number";
            }
            else
            {
                // Fallback - use all room parameters
                selectedParameters = config.GetRoomModeParameters();
                fillType = "room";
            }

            if (selectedParameters.Count == 0)
            {
                _logger.Warn("No parameters selected for room fill");
                return summary;
            }

            _logger.Info($"Filling {selectedParameters.Count} {fillType} parameters: {string.Join(", ", selectedParameters)}");

            // Collect elements
            var previewSummary = new RoomFillPreviewSummary();
            var elements = CollectElements(document, selectedCategories, previewSummary);

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
                        var room = _roomAssignmentService.GetRoomForElement(element, RoomDetectionMethod.PointInRoom);

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
                            foreach (var paramName in selectedParameters)
                            {
                                // Determine what value to fill based on FillMode and parameter name
                                string valueToFill = GetRoomValueForParameter(paramName, room, config.FillMode);

                                if (valueToFill != null)
                                {
                                    // Try to set the parameter
                                    if (TrySetParameter(element, paramName, valueToFill, config.OverwriteExisting))
                                    {
                                        parametersAssigned++;

                                        // Track statistics based on FillMode using bitwise operations
                                        // Note: hasRoomNameMode and hasRoomNumberMode are declared earlier in the method

                                        if (hasRoomNameMode)
                                        {
                                            summary.RoomNameParametersFilled++;
                                        }
                                        else if (hasRoomNumberMode)
                                        {
                                            summary.RoomNumberParametersFilled++;
                                        }
                                        else
                                        {
                                            // Fallback - use parameter type detection
                                            if (IsRoomNameParameter(paramName))
                                            {
                                                summary.RoomNameParametersFilled++;
                                            }
                                            else if (IsRoomNumberParameter(paramName))
                                            {
                                                summary.RoomNumberParametersFilled++;
                                            }
                                            else if (IsRoomRefParameter(paramName))
                                            {
                                                summary.RoomRefParametersFilled++;
                                            }
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
                            // No room found - determine skip reason
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
        /// Determines what room value to fill based on parameter name and FillMode
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="room">Room object</param>
        /// <param name="fillMode">The FillMode being used (flags-based)</param>
        /// <returns>Value to fill, or null if parameter type is unknown</returns>
        private string GetRoomValueForParameter(string paramName, Room room, Shared.Models.FillMode fillMode)
        {
            if (room == null || string.IsNullOrWhiteSpace(paramName))
            {
                return null;
            }

            // Check which room mode is selected using bitwise operations
            bool hasRoomNameMode = (fillMode & FillMode.RoomName) != 0;
            bool hasRoomNumberMode = (fillMode & FillMode.RoomNumber) != 0;

            // For new explicit modes, always return the corresponding value
            if (hasRoomNameMode)
            {
                return room.Name;
            }

            if (hasRoomNumberMode)
            {
                return room.Number;
            }

            // Fallback: use parameter name detection for legacy support
            // Check if parameter is for room name
            if (IsRoomNameParameter(paramName))
            {
                return room.Name;
            }

            // Check if parameter is for room number
            if (IsRoomNumberParameter(paramName))
            {
                return room.Number;
            }

            // Check if parameter is for room reference (combined)
            if (IsRoomRefParameter(paramName))
            {
                return $"{room.Number}: {room.Name}";
            }

            // Default: try to determine from parameter name
            var lowerParam = paramName.ToLower();
            if (lowerParam.Contains("name") && !lowerParam.Contains("number"))
            {
                return room.Name;
            }
            if (lowerParam.Contains("number") || lowerParam.Contains("num"))
            {
                return room.Number;
            }

            // Fallback to room name as default
            return room.Name;
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
    }
}
