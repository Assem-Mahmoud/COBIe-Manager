using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for assigning elements to levels based on bounding box intersection
    /// </summary>
    public class LevelAssignmentService : ILevelAssignmentService
    {
        private readonly ILogger _logger;

        public LevelAssignmentService(ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Assigns elements to a level range based on bounding box intersection
        /// </summary>
        /// <param name="elements">Elements to process</param>
        /// <param name="baseLevel">Bottom level of range</param>
        /// <param name="topLevel">Top level of range</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <returns>Assignment results for all processed elements</returns>
        public LevelAssignmentResult AssignElementsToLevelRange(
            IEnumerable<Element> elements,
            Level baseLevel,
            Level topLevel,
            IProcessingLogger logger)
        {
            var result = new LevelAssignmentResult();
            var levelName = topLevel.Name;

            _logger.Info($"Starting level assignment for {elements.Count()} elements to level '{levelName}'");
            _logger.Debug($"Base Level: {baseLevel.Name} (Elevation: {baseLevel.Elevation})");
            _logger.Debug($"Top Level: {topLevel.Name} (Elevation: {topLevel.Elevation})");

            foreach (var element in elements)
            {
                var position = GetElementPositionInBand(element, baseLevel, topLevel);

                switch (position)
                {
                    case LevelBandPosition.InBand:
                        // Element is within the level band
                        var assignResult = AssignLevelParameter(element, levelName, logger);
                        if (assignResult.Success)
                        {
                            result.ElementsAssigned++;
                            logger.LogSuccess(element.Id, element.Category?.Name, $"Assigned to level '{levelName}'");
                        }
                        else if (assignResult.Skipped)
                        {
                            result.ElementsSkippedNoBoundingBox++;
                            logger.LogSkip(element.Id, element.Category?.Name, assignResult.SkipReason);
                        }
                        else
                        {
                            result.ElementsFailed++;
                            logger.LogSkip(element.Id, element.Category?.Name, assignResult.SkipReason ?? SkipReasons.FailedToAssignParameter);
                        }
                        break;

                    case LevelBandPosition.NoBoundingBox:
                        result.ElementsSkippedNoBoundingBox++;
                        logger.LogSkip(element.Id, element.Category?.Name, SkipReasons.NoBoundingBox);
                        break;

                    default:
                        // Element is above or below the band
                        result.ElementsSkippedOutsideBand++;
                        var positionReason = position == LevelBandPosition.BelowBand
                            ? SkipReasons.BelowBand
                            : SkipReasons.AboveBand;
                        logger.LogSkip(element.Id, element.Category?.Name, positionReason);
                        break;
                }
            }

            result.TotalElementsProcessed = result.ElementsAssigned + result.ElementsFailed +
                                        result.ElementsSkippedNoBoundingBox + result.ElementsSkippedOutsideBand;

            _logger.Info($"Level assignment complete: {result.ElementsAssigned} assigned, {result.ElementsFailed} failed, " +
                        $"{result.ElementsSkippedNoBoundingBox + result.ElementsSkippedOutsideBand} skipped");

            return result;
        }

        /// <summary>
        /// Gets the position of an element relative to a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level</param>
        /// <param name="topLevel">Top level</param>
        /// <returns>Position relative to level band</returns>
        public LevelBandPosition GetElementPositionInBand(
            Element element,
            Level baseLevel,
            Level topLevel)
        {
            if (element == null)
            {
                return LevelBandPosition.NoBoundingBox;
            }

            var bbox = element.get_BoundingBox(null);

            if (bbox == null)
            {
                _logger.Debug($"Element {element.Id} has no bounding box");
                return LevelBandPosition.NoBoundingBox;
            }

            var baseElevation = baseLevel.Elevation;
            var topElevation = topLevel.Elevation;

            // Check if element is completely below the band
            if (bbox.Max.Z <= baseElevation)
            {
                return LevelBandPosition.BelowBand;
            }

            // Check if element is completely above the band
            if (bbox.Min.Z >= topElevation)
            {
                return LevelBandPosition.AboveBand;
            }

            // Element intersects with the band
            return LevelBandPosition.InBand;
        }

        /// <summary>
        /// Safely assigns a level parameter value to an element
        /// </summary>
        /// <param name="element">Element to assign level to</param>
        /// <param name="levelName">Level name to assign</param>
        /// <param name="parameterName">Parameter name (default: "ACG-4D-Level")</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>ParameterAssignmentResult indicating success, skip, or failure with reason</returns>
        public ParameterAssignmentResult AssignLevelParameter(
            Element element,
            string levelName,
            IProcessingLogger logger,
            string parameterName = "ACG-4D-Level",
            bool overwrite = true)
        {
            if (element == null)
            {
                _logger.Warn("Cannot assign level parameter to null element");
                return ParameterAssignmentResult.CreateFailure(-1, "Element is null");
            }

            if (string.IsNullOrWhiteSpace(levelName))
            {
                _logger.Warn("Level name is null or empty");
                return ParameterAssignmentResult.CreateFailure(element.Id.IntegerValue, "Level name is null or empty");
            }

            var parameter = element.LookupParameter(parameterName);

            if (parameter == null)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' not found");
                return ParameterAssignmentResult.CreateSkipped(element.Id.IntegerValue, SkipReasons.ParameterMissing);
            }

            if (parameter.IsReadOnly)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' is read-only");
                return ParameterAssignmentResult.CreateSkipped(element.Id.IntegerValue, SkipReasons.ParameterReadOnly);
            }

            // Check if value exists and overwrite is disabled
            if (!overwrite)
            {
                var currentValue = parameter.AsString();
                if (!string.IsNullOrWhiteSpace(currentValue))
                {
                    _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' has existing value '{currentValue}', overwrite disabled");
                    return ParameterAssignmentResult.CreateSkipped(element.Id.IntegerValue, SkipReasons.ExistingValueNoOverwrite);
                }
            }

            try
            {
                parameter.Set(levelName);
                _logger.Debug($"Element {element.Id}: Successfully assigned level '{levelName}'");
                return ParameterAssignmentResult.CreateSuccess(element.Id.IntegerValue);
            }
            catch (System.Exception ex)
            {
                _logger.Error($"Element {element.Id}: Failed to assign level parameter '{parameterName}'", ex);
                return ParameterAssignmentResult.CreateFailure(element.Id.IntegerValue, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an element is within a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level of band</param>
        /// <param name="topLevel">Top level of band</param>
        /// <returns>True if element intersects the level band</returns>
        public bool IsElementInLevelBand(Element element, Level baseLevel, Level topLevel)
        {
            return GetElementPositionInBand(element, baseLevel, topLevel) == LevelBandPosition.InBand;
        }
    }

    /// <summary>
    /// Result of level assignment operation
    /// </summary>
    public class LevelAssignmentResult
    {
        public int TotalElementsProcessed { get; set; }
        public int ElementsAssigned { get; set; }
        public int ElementsFailed { get; set; }
        public int ElementsSkippedNoBoundingBox { get; set; }
        public int ElementsSkippedOutsideBand { get; set; }
    }
}
