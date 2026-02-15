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
    /// Service for orchestrating parameter fill operations
    /// </summary>
    public class ParameterFillService : IParameterFillService
    {
        private readonly ILogger _logger;
        private readonly ILevelAssignmentService _levelAssignmentService;
        private readonly IRoomAssignmentService _roomAssignmentService;

        public ParameterFillService(
            ILogger logger,
            ILevelAssignmentService levelAssignmentService,
            IRoomAssignmentService roomAssignmentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _levelAssignmentService = levelAssignmentService ?? throw new ArgumentNullException(nameof(levelAssignmentService));
            _roomAssignmentService = roomAssignmentService ?? throw new ArgumentNullException(nameof(roomAssignmentService));
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

            _logger.Info("Starting preview analysis");

            var summary = new PreviewSummary();

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                summary.AddValidationWarning(error ?? "Invalid configuration");
                _logger.Warn($"Preview configuration invalid: {error}");
                return summary;
            }

            // Collect elements by category
            var elements = CollectElements(document, config, summary);

            if (!elements.Any())
            {
                _logger.Info("No elements found for preview");
                return summary;
            }

            _logger.Info($"Previewing {elements.Count} elements");

            // Count elements in level band
            int inBandCount = 0;
            int roomAssignableCount = 0;
            int processedCount = 0;

            foreach (var element in elements)
            {
                processedCount++;

                // Report progress every 100 elements
                if (processedCount % 100 == 0)
                {
                    _logger.Debug($"Preview progress: {processedCount}/{elements.Count} elements analyzed");
                }

                // Check if element is in level band
                if (_levelAssignmentService.IsElementInLevelBand(element, config.BaseLevel, config.TopLevel))
                {
                    inBandCount++;

                    // Check if room can be assigned (only for MVP - User Story 1)
                    // Room assignment is User Story 2, so we skip for now
                }
            }

            summary.EstimatedElementsToProcess = inBandCount;
            summary.EstimatedRoomAssignments = 0; // Will be implemented in User Story 2

            _logger.Info($"Preview complete: {inBandCount} elements in level band");

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

            _logger.Info("Starting parameter fill operation");

            var stopwatch = Stopwatch.StartNew();
            var processingLogger = new ProcessingLogger(_logger);

            // Validate configuration
            if (!config.IsValid())
            {
                var error = config.GetValidationError();
                _logger.Error($"Configuration invalid: {error}");
                throw new InvalidOperationException($"Invalid configuration: {error}");
            }

            // Collect elements by category
            var summary = new PreviewSummary();
            var elements = CollectElements(document, config, summary);

            if (!elements.Any())
            {
                _logger.Warn("No elements found to process");
                var emptySummary = processingLogger.GetSummary();
                emptySummary.ProcessingDuration = stopwatch.Elapsed;
                return emptySummary;
            }

            _logger.Info($"Processing {elements.Count} elements");

            // Use transaction for parameter modification
            using (var transaction = new Transaction(document, "Auto-Fill Parameters"))
            {
                transaction.Start();

                try
                {
                    int processedCount = 0;
                    int levelParametersFilled = 0;
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
                            // Assign level parameter
                            var result = _levelAssignmentService.AssignLevelParameter(
                                element,
                                config.BaseLevel.Name,
                                processingLogger,
                                config.ParameterMapping.LevelParameter,
                                config.OverwriteExisting);

                            totalSucessElements.Add(element.Id);

                            if (result.Success)
                            {
                                levelParametersFilled++;
                                processingLogger.LogSuccess(element.Id, element.Category?.Name, result.SkipReason);
                            }
                            else if (result.Skipped)
                            {
                                processingLogger.LogSkip(element.Id, element.Category?.Name, result.SkipReason);
                            }
                            else
                            {
                                processingLogger.LogError(element.Id, element.Category?.Name, result.SkipReason ?? "Failed to assign level parameter");
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
                    _logger.Info($"Transaction committed: {levelParametersFilled} level parameters filled");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during parameter fill operation", ex);
                    transaction.RollBack();
                    throw;
                }
            }

            var finalSummary = processingLogger.GetSummary();
            finalSummary.ProcessingDuration = stopwatch.Elapsed;
            finalSummary.TotalElementsScanned = elements.Count;

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

            //foreach (var category in config.SelectedCategories)
            //{
            //    var collector = new FilteredElementCollector(document)                    
            //        .WhereElementIsNotElementType();

            //    var categoryElements = collector.ToElements();

            //    if (!categoryElements.Any())
            //    {
            //        var categoryName = category.ToString().Replace("OST_", "");
            //        previewSummary?.AddEmptyCategory(categoryName);
            //        _logger.Debug($"No elements found for category: {categoryName}");
            //    }
            //    else
            //    {
            //        allElements.AddRange(categoryElements);
            //        _logger.Debug($"Found {categoryElements.Count} elements in category: {category}");
            //    }
            //}
            var collector = new FilteredElementCollector(document)
    .WhereElementIsNotElementType()
    .WhereElementIsViewIndependent(); // removes view-specific items

            // Exclude non-model categories
            allElements= collector
                .Where(e =>
                    e.Category != null &&
                    e.Category.CategoryType == CategoryType.Model &&
                    !e.Category.IsTagCategory &&
                    e.CanHaveTypeAssigned()) // optional safety
                .ToList();

            return allElements;
        }
    }
}
