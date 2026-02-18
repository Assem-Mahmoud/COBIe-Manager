using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for orchestrating parameter fill operations with multi-select fill modes
    /// </summary>
    public class ParameterFillService : IParameterFillService
    {
        private readonly ILogger _logger;
        private readonly ILevelAssignmentService _levelAssignmentService;
        private readonly IRoomAssignmentService _roomAssignmentService;
        private readonly IBoxIdFillService _boxIdFillService;
        private readonly IRoomFillService _roomFillService;

        public ParameterFillService(
            ILogger logger,
            ILevelAssignmentService levelAssignmentService,
            IRoomAssignmentService roomAssignmentService,
            IBoxIdFillService boxIdFillService,
            IRoomFillService roomFillService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _levelAssignmentService = levelAssignmentService ?? throw new ArgumentNullException(nameof(levelAssignmentService));
            _roomAssignmentService = roomAssignmentService ?? throw new ArgumentNullException(nameof(roomAssignmentService));
            _boxIdFillService = boxIdFillService ?? throw new ArgumentNullException(nameof(boxIdFillService));
            _roomFillService = roomFillService ?? throw new ArgumentNullException(nameof(roomFillService));
        }

        /// <summary>
        /// Analyzes elements and returns preview summary without modifying document
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <returns>Preview summary with estimated counts</returns>
        public PreviewSummary PreviewFill(Document document, FillConfiguration config)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _logger.Info($"Starting preview analysis (FillMode: {config.FillMode})");

            var summary = new PreviewSummary();

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                summary.AddValidationWarning(error ?? "Invalid configuration");
                _logger.Warn($"Preview configuration invalid: {error}");
                return summary;
            }

            // Check which modes are selected using bitwise operations
            bool hasLevelMode = (config.FillMode & FillMode.Level) != 0;
            bool hasRoomNameMode = (config.FillMode & FillMode.RoomName) != 0;
            bool hasRoomNumberMode = (config.FillMode & FillMode.RoomNumber) != 0;
            bool hasGroupsMode = (config.FillMode & FillMode.Groups) != 0;

            // Process room name preview if mode is selected and parameters are mapped
            if (hasRoomNameMode && config.GetRoomNameModeParameters().Count > 0)
            {
                var roomPreview = _roomFillService.PreviewFill(document, config);
                summary.EstimatedElementsToProcess += roomPreview.EstimatedElementsToProcess;
                summary.EstimatedRoomAssignments += roomPreview.EstimatedRoomsFound;

                foreach (var warning in roomPreview.ValidationWarnings)
                {
                    summary.AddValidationWarning(warning);
                }

                foreach (var category in roomPreview.CategoriesWithNoElements)
                {
                    summary.AddEmptyCategory(category);
                }

                _logger.Info($"RoomName mode preview: {roomPreview.EstimatedRoomsFound} elements with rooms");
            }

            // Process room number preview if mode is selected and parameters are mapped
            if (hasRoomNumberMode && config.GetRoomNumberModeParameters().Count > 0)
            {
                var roomPreview = _roomFillService.PreviewFill(document, config);
                summary.EstimatedElementsToProcess += roomPreview.EstimatedElementsToProcess;
                summary.EstimatedRoomAssignments += roomPreview.EstimatedRoomsFound;

                foreach (var warning in roomPreview.ValidationWarnings)
                {
                    summary.AddValidationWarning(warning);
                }

                foreach (var category in roomPreview.CategoriesWithNoElements)
                {
                    summary.AddEmptyCategory(category);
                }

                _logger.Info($"RoomNumber mode preview: {roomPreview.EstimatedRoomsFound} elements with rooms");
            }

            // Process groups preview if mode is selected and parameters are mapped
            if (hasGroupsMode && config.GetGroupModeParameters().Count > 0)
            {
                // Group mode preview would go here
                _logger.Info("Groups mode preview: Box ID fill preview not yet implemented");
            }

            // Process level preview if mode is selected and parameters are mapped
            if (hasLevelMode && config.GetLevelModeParameters().Count > 0)
            {
                var elements = CollectElements(document, config, summary);

                if (elements.Any())
                {
                    _logger.Info($"Previewing {elements.Count} elements for level mode");

                    int inBandCount = 0;
                    foreach (var element in elements)
                    {
                        if (_levelAssignmentService.IsElementInLevelBand(element, config.BaseLevel, config.TopLevel))
                        {
                            inBandCount++;
                        }
                    }

                    summary.EstimatedElementsToProcess += inBandCount;
                    _logger.Info($"Level mode preview: {inBandCount} elements in level band");
                }
            }

            _logger.Info($"Preview complete: {summary.EstimatedElementsToProcess} elements to process");

            return summary;
        }

        /// <summary>
        /// Executes parameter fill operation with progress reporting
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <param name="progressAction">Progress callback</param>
        /// <returns>Processing summary with actual results</returns>
        public ProcessingSummary ExecuteFill(
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

            _logger.Info($"Starting parameter fill operation (FillMode: {config.FillMode})");

            var stopwatch = Stopwatch.StartNew();
            var processingLogger = new ProcessingLogger(_logger);
            var finalSummary = processingLogger.GetSummary();

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                _logger.Error($"Configuration invalid: {error}");
                throw new InvalidOperationException($"Invalid configuration: {error}");
            }

            // Check which modes are selected using bitwise operations
            bool hasLevelMode = (config.FillMode & FillMode.Level) != 0;
            bool hasRoomNameMode = (config.FillMode & FillMode.RoomName) != 0;
            bool hasRoomNumberMode = (config.FillMode & FillMode.RoomNumber) != 0;
            bool hasGroupsMode = (config.FillMode & FillMode.Groups) != 0;

            _logger.Info($"Processing modes - Level: {hasLevelMode}, RoomName: {hasRoomNameMode}, RoomNumber: {hasRoomNumberMode}, Groups: {hasGroupsMode}");

            // Process room name fill if mode is selected and parameters are mapped
            if (hasRoomNameMode && config.GetRoomNameModeParameters().Count > 0)
            {
                _logger.Info("Processing room name fill");
                var roomFillSummary = _roomFillService.ExecuteFill(document, config, progressAction);
                finalSummary.RoomFillSummary = roomFillSummary;
                _logger.Info($"Room name fill complete: {roomFillSummary.ElementsUpdated} elements updated");
            }

            // Process room number fill if mode is selected and parameters are mapped
            if (hasRoomNumberMode && config.GetRoomNumberModeParameters().Count > 0)
            {
                _logger.Info("Processing room number fill");
                var roomFillSummary = _roomFillService.ExecuteFill(document, config, progressAction);
                if (finalSummary.RoomFillSummary == null)
                {
                    finalSummary.RoomFillSummary = roomFillSummary;
                }
                _logger.Info($"Room number fill complete: {roomFillSummary.ElementsUpdated} elements updated");
            }

            // Process groups fill if mode is selected and parameters are mapped
            if (hasGroupsMode && config.GetGroupModeParameters().Count > 0)
            {
                var selectedParams = config.GetGroupModeParameters();
                if (selectedParams.Count > 0)
                {
                    _logger.Info("Processing box ID fill");
                    var boxIdParameter = selectedParams[0];
                    var boxIdSummary = _boxIdFillService.ExecuteFill(
                        document,
                        boxIdParameter,
                        config.OverwriteExisting,
                        includeGroupElement: true,
                        progressAction);
                    finalSummary.BoxIdFillSummary = boxIdSummary;
                    _logger.Info($"Box ID fill complete: {boxIdSummary.MembersUpdated} members updated");
                }
            }

            // Process level fill if mode is selected and parameters are mapped
            if (hasLevelMode && config.GetLevelModeParameters().Count > 0)
            {
                var summary = new PreviewSummary();
                var elements = CollectElements(document, config, summary);

                if (elements.Any())
                {
                    _logger.Info($"Processing {elements.Count} elements for level mode");

                    var selectedParameters = config.GetLevelModeParameters();
                    _logger.Info($"Filling {selectedParameters.Count} level-mode parameters: {string.Join(", ", selectedParameters)}");

                    // Use transaction for parameter modification
                    using (var transaction = new Transaction(document, "Auto-Fill Level Parameters"))
                    {
                        transaction.Start();

                        try
                        {
                            int processedCount = 0;
                            int parametersFilled = 0;
                            var totalSucessElements = new List<ElementId>();

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

                                // Check if element is in level band
                                if (_levelAssignmentService.IsElementInLevelBand(element, config.BaseLevel, config.TopLevel))
                                {
                                    // Fill all selected parameters with the level name
                                    foreach (var paramName in selectedParameters)
                                    {
                                        var result = _levelAssignmentService.AssignLevelParameter(
                                            element,
                                            config.BaseLevel.Name,
                                            processingLogger,
                                            paramName,
                                            config.OverwriteExisting);

                                        if (result.Success)
                                        {
                                            parametersFilled++;
                                        }
                                    }

                                    totalSucessElements.Add(element.Id);

                                    // Log success for the first parameter (to avoid spamming logs)
                                    if (selectedParameters.Count > 0)
                                    {
                                        processingLogger.LogSuccess(element.Id, element.Category?.Name,
                                            $"Filled {selectedParameters.Count} level parameters");
                                    }
                                }
                                else
                                {
                                    // Element is outside level band
                                    var position = _levelAssignmentService.GetElementPositionInBand(element, config.BaseLevel, config.TopLevel);
                                    var skipReason = position == LevelBandPosition.BelowBand
                                        ? SkipReasons.BelowBand
                                        : SkipReasons.AboveBand;
                                    processingLogger.LogSkip(element.Id, element.Category?.Name, skipReason);
                                }
                            }

                            document.ActiveView.IsolateElementsTemporary(totalSucessElements);
                            transaction.Commit();
                            _logger.Info($"Level mode transaction committed: {parametersFilled} parameter assignments completed");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Error during level parameter fill operation", ex);
                            transaction.RollBack();
                            throw;
                        }
                    }

                    finalSummary.TotalElementsScanned = elements.Count;
                }
                else
                {
                    _logger.Warn("No elements found for level mode processing");
                }
            }

            finalSummary.ProcessingDuration = stopwatch.Elapsed;
            _logger.Info($"Parameter fill complete in {finalSummary.ProcessingDuration.TotalSeconds:F2} seconds");

            return finalSummary;
        }

        /// <summary>
        /// Collects elements from the document based on selected categories
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <param name="previewSummary">Preview summary to populate with empty categories</param>
        /// <returns>Filtered elements</returns>
        private IList<Element> CollectElements(
            Document document,
            FillConfiguration config,
            PreviewSummary previewSummary)
        {
            var allElements = new List<Element>();

            // Get selected categories using the new method
            var selectedCategories = config.GetSelectedCategories();

            if (selectedCategories.Count == 0)
            {
                _logger.Debug("No categories selected for processing");
                return allElements;
            }

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
                    previewSummary?.AddEmptyCategory(categoryName);
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
    }
}
