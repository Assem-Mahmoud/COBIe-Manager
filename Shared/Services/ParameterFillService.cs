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
        private readonly IScopeBoxAssignmentService _scopeBoxAssignmentService;
        private readonly IZoneAssignmentService _zoneAssignmentService;

        public ParameterFillService(
            ILogger logger,
            ILevelAssignmentService levelAssignmentService,
            IRoomAssignmentService roomAssignmentService,
            IBoxIdFillService boxIdFillService,
            IRoomFillService roomFillService,
            IScopeBoxAssignmentService scopeBoxAssignmentService,
            IZoneAssignmentService zoneAssignmentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _levelAssignmentService = levelAssignmentService ?? throw new ArgumentNullException(nameof(levelAssignmentService));
            _roomAssignmentService = roomAssignmentService ?? throw new ArgumentNullException(nameof(roomAssignmentService));
            _boxIdFillService = boxIdFillService ?? throw new ArgumentNullException(nameof(boxIdFillService));
            _roomFillService = roomFillService ?? throw new ArgumentNullException(nameof(roomFillService));
            _scopeBoxAssignmentService = scopeBoxAssignmentService ?? throw new ArgumentNullException(nameof(scopeBoxAssignmentService));
            _zoneAssignmentService = zoneAssignmentService ?? throw new ArgumentNullException(nameof(zoneAssignmentService));
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
            bool hasScopeBoxMode = (config.FillMode & FillMode.ScopeBox) != 0;
            bool hasZoneMode = (config.FillMode & FillMode.Zone) != 0;

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

            // Process scope box preview if mode is selected and parameters are mapped
            if (hasScopeBoxMode && config.GetScopeBoxModeParameters().Count > 0)
            {
                var scopeBoxPreview = _scopeBoxAssignmentService.PreviewFill(document, config);
                summary.EstimatedElementsToProcess += scopeBoxPreview.ElementsFound;

                if (scopeBoxPreview.Errors > 0)
                {
                    foreach (var error in scopeBoxPreview.ErrorMessages)
                    {
                        summary.AddValidationWarning(error);
                    }
                }

                _logger.Info($"ScopeBox mode preview: {scopeBoxPreview.ElementsFound} elements in scope box, {scopeBoxPreview.ParametersFilled} parameters to fill");
            }

            // Process zone preview if mode is selected and parameters are mapped
            if (hasZoneMode && config.GetZoneModeParameters().Count > 0)
            {
                var zonePreview = _zoneAssignmentService.PreviewFill(document, config);
                summary.EstimatedElementsToProcess += zonePreview.ElementsFound;

                if (zonePreview.Errors > 0)
                {
                    foreach (var error in zonePreview.ErrorMessages)
                    {
                        summary.AddValidationWarning(error);
                    }
                }

                _logger.Info($"Zone mode preview: {zonePreview.ElementsFound} elements in zone, {zonePreview.ParametersFilled} parameters to fill");
            }

            // Process level preview if mode is selected and parameters are mapped
            if (hasLevelMode && config.GetLevelModeParameters().Count > 0)
            {
                var elements = CollectElements(document, config, summary);

                if (elements.Any())
                {
                    _logger.Info($"Previewing {elements.Count} elements for level mode");

                    // Sort selected levels by elevation
                    var sortedLevels = config.LevelMode.SelectedLevels
                        .OrderBy(l => l.Elevation)
                        .ToList();

                    if (sortedLevels.Count < 2)
                    {
                        summary.AddValidationWarning("At least 2 levels must be selected for level mode");
                        return summary;
                    }

                    int inBandCount = 0;
                    int inBandByCenterCount = 0;
                    int nearestLevelCount = 0;
                    int belowLowestCount = 0;
                    int aboveHighestCount = 0;

                    foreach (var element in elements)
                    {
                        // Check if element category is excluded
                        bool isExcludedCategory = false;
                        if (element.Category != null && config.LevelMode.ExcludedCategories != null)
                        {
                            var categoryId = (BuiltInCategory)element.Category.Id.IntegerValue;
                            isExcludedCategory = config.LevelMode.ExcludedCategories.Contains(categoryId);
                        }

                        Level assignedLevel = null;

                        if (isExcludedCategory)
                        {
                            // Use nearest level logic across all selected levels
                            assignedLevel = _levelAssignmentService.FindNearestLevelAmong(
                                element,
                                sortedLevels);
                            if (assignedLevel != null)
                            {
                                nearestLevelCount++;
                            }
                        }
                        else
                        {
                            // Process through consecutive ranges
                            assignedLevel = ProcessLevelRanges(element, sortedLevels);

                            if (assignedLevel != null)
                            {
                                // Check if it's an edge case (below lowest or above highest)
                                if (assignedLevel.Id == sortedLevels.First().Id)
                                {
                                    var position = _levelAssignmentService.GetElementPositionInBand(
                                        element,
                                        sortedLevels[0],
                                        sortedLevels[1]);

                                    if (position == LevelBandPosition.BelowBand)
                                    {
                                        belowLowestCount++;
                                    }
                                    else if (position == LevelBandPosition.InBand)
                                    {
                                        inBandCount++;
                                    }
                                    else if (position == LevelBandPosition.InBandByCenter)
                                    {
                                        inBandByCenterCount++;
                                    }
                                }
                                else if (assignedLevel.Id == sortedLevels.Last().Id)
                                {
                                    var position = _levelAssignmentService.GetElementPositionInBand(
                                        element,
                                        sortedLevels[sortedLevels.Count - 2],
                                        sortedLevels[sortedLevels.Count - 1]);

                                    if (position == LevelBandPosition.AboveBand)
                                    {
                                        aboveHighestCount++;
                                    }
                                    else if (position == LevelBandPosition.InBand)
                                    {
                                        inBandCount++;
                                    }
                                    else if (position == LevelBandPosition.InBandByCenter)
                                    {
                                        inBandByCenterCount++;
                                    }
                                }
                                else
                                {
                                    // In a middle range
                                    inBandCount++;
                                }
                            }
                        }
                    }

                    summary.EstimatedElementsToProcess += inBandCount + inBandByCenterCount + nearestLevelCount + belowLowestCount + aboveHighestCount;
                    _logger.Info($"Level mode preview: {inBandCount} fully in band, {inBandByCenterCount} by center, {nearestLevelCount} nearest level, {belowLowestCount} below lowest, {aboveHighestCount} above highest");
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
            bool hasScopeBoxMode = (config.FillMode & FillMode.ScopeBox) != 0;
            bool hasZoneMode = (config.FillMode & FillMode.Zone) != 0;

            _logger.Info($"Processing modes - Level: {hasLevelMode}, RoomName: {hasRoomNameMode}, RoomNumber: {hasRoomNumberMode}, Groups: {hasGroupsMode}, ScopeBox: {hasScopeBoxMode}, Zone: {hasZoneMode}");

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

            // Process scope box fill if mode is selected and parameters are mapped
            if (hasScopeBoxMode && config.GetScopeBoxModeParameters().Count > 0)
            {
                _logger.Info("Processing scope box fill");
                var scopeBoxSummary = _scopeBoxAssignmentService.ExecuteFill(document, config, progressAction);
                finalSummary.ScopeBoxFillSummary = scopeBoxSummary;
                _logger.Info($"Scope box fill complete: {scopeBoxSummary.ParametersFilled} parameters filled, {scopeBoxSummary.ElementsFound} elements in scope box");
            }

            // Process zone fill if mode is selected and parameters are mapped
            if (hasZoneMode && config.GetZoneModeParameters().Count > 0)
            {
                _logger.Info("Processing zone fill");
                var zoneSummary = _zoneAssignmentService.ExecuteFill(document, config, progressAction);
                finalSummary.ZoneFillSummary = zoneSummary;
                _logger.Info($"Zone fill complete: {zoneSummary.ParametersFilled} parameters filled, {zoneSummary.ElementsFound} elements in zone");
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

                    // Sort selected levels by elevation
                    var sortedLevels = config.LevelMode.SelectedLevels
                        .OrderBy(l => l.Elevation)
                        .ToList();

                    if (sortedLevels.Count < 2)
                    {
                        _logger.Error("At least 2 levels must be selected for level mode");
                        throw new InvalidOperationException("At least 2 levels must be selected for level mode");
                    }

                    _logger.Info($"Processing {sortedLevels.Count} levels from elevation {sortedLevels.First().Elevation} to {sortedLevels.Last().Elevation}");

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

                                // Check if element category is excluded
                                bool isExcludedCategory = false;
                                if (element.Category != null && config.LevelMode.ExcludedCategories != null)
                                {
                                    var categoryId = (BuiltInCategory)element.Category.Id.IntegerValue;
                                    isExcludedCategory = config.LevelMode.ExcludedCategories.Contains(categoryId);
                                }

                                Level assignedLevel = null;

                                if (isExcludedCategory)
                                {
                                    // Use nearest level logic across all selected levels
                                    assignedLevel = _levelAssignmentService.FindNearestLevelAmong(
                                        element,
                                        sortedLevels);

                                    if (assignedLevel != null)
                                    {
                                        // Get custom name if configured
                                        string levelNameToUse = GetLevelName(assignedLevel, config.LevelMode);

                                        foreach (var paramName in selectedParameters)
                                        {
                                            var result = _levelAssignmentService.AssignLevelParameter(
                                                element,
                                                levelNameToUse,
                                                processingLogger,
                                                paramName,
                                                config.OverwriteExisting);

                                            if (result.Success)
                                            {
                                                parametersFilled++;
                                            }
                                        }
                                        totalSucessElements.Add(element.Id);

                                        processingLogger.LogSuccess(element.Id, element.Category?.Name,
                                            $"Filled {selectedParameters.Count} level parameters (nearest level: {assignedLevel.Name})");
                                    }
                                    else
                                    {
                                        processingLogger.LogSkip(element.Id, element.Category?.Name, SkipReasons.NoNearestLevel);
                                    }
                                }
                                else
                                {
                                    // Process through consecutive ranges
                                    assignedLevel = ProcessLevelRanges(element, sortedLevels);

                                    if (assignedLevel != null)
                                    {
                                        // Get custom name if configured
                                        string levelNameToUse = GetLevelName(assignedLevel, config.LevelMode);

                                        // Fill all selected parameters with the level name
                                        foreach (var paramName in selectedParameters)
                                        {
                                            var result = _levelAssignmentService.AssignLevelParameter(
                                                element,
                                                levelNameToUse,
                                                processingLogger,
                                                paramName,
                                                config.OverwriteExisting);

                                            if (result.Success)
                                            {
                                                parametersFilled++;
                                            }
                                        }

                                        totalSucessElements.Add(element.Id);

                                        // Determine if this is an edge case
                                        string logMessage = assignedLevel.Id == sortedLevels.First().Id
                                            ? $"Filled {selectedParameters.Count} level parameters (lowest level)"
                                            : assignedLevel.Id == sortedLevels.Last().Id
                                                ? $"Filled {selectedParameters.Count} level parameters (highest level)"
                                                : $"Filled {selectedParameters.Count} level parameters";

                                        processingLogger.LogSuccess(element.Id, element.Category?.Name, logMessage);
                                    }
                                    else
                                    {
                                        processingLogger.LogSkip(element.Id, element.Category?.Name, SkipReasons.OutsideAllRanges);
                                    }
                                }
                            }                           
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

        /// <summary>
        /// Processes an element through consecutive level ranges to determine which level it should be assigned to.
        /// Elements in range [Ln, Ln+1] are assigned to Ln (the lower level).
        /// Elements below the lowest level are assigned to the lowest level.
        /// Elements above the highest level are assigned to the highest level.
        /// </summary>
        /// <param name="element">The element to process</param>
        /// <param name="sortedLevels">Levels sorted by elevation (ascending)</param>
        /// <returns>The level to assign the element to, or null if cannot determine</returns>
        private Level ProcessLevelRanges(Element element, IList<Level> sortedLevels)
        {
            if (element == null || sortedLevels == null || sortedLevels.Count < 2)
                return null;

            // Check each consecutive range
            for (int i = 0; i < sortedLevels.Count - 1; i++)
            {
                var lowerLevel = sortedLevels[i];
                var upperLevel = sortedLevels[i + 1];

                var position = _levelAssignmentService.GetElementPositionInBand(
                    element,
                    lowerLevel,
                    upperLevel);

                if (position == LevelBandPosition.InBand || position == LevelBandPosition.InBandByCenter)
                {
                    // Element is in this range, assign to lower level
                    return lowerLevel;
                }
                else if (position == LevelBandPosition.BelowBand && i == 0)
                {
                    // Element is below the lowest level
                    return sortedLevels.First();
                }
            }

            // If we get here, element is above the highest level
            return sortedLevels.Last();
        }

        /// <summary>
        /// Gets the level name to use, considering custom level names if configured.
        /// </summary>
        /// <param name="level">The level to get the name for</param>
        /// <param name="config">The level mode configuration</param>
        /// <returns>The level name to use</returns>
        private string GetLevelName(Level level, LevelModeConfig config)
        {
            if (level == null || config == null)
                return string.Empty;

            // Check if there's a custom name configured for this level
            if (config.CustomLevelNames != null && config.CustomLevelNames.TryGetValue(level.Id, out var customName))
            {
                if (!string.IsNullOrWhiteSpace(customName))
                    return customName;
            }

            // Fall back to Revit level name
            return level.Name;
        }
    }
}
