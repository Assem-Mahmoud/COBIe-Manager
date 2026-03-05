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
        /// Gets all levels from the specified document
        /// </summary>
        /// <param name="document">The Revit document to get levels from</param>
        /// <returns>List of levels sorted by elevation</returns>
        public IList<Level> GetLevels(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            try
            {
                var levelCollector = new FilteredElementCollector(document)
                    .OfClass(typeof(Level));

                var levels = levelCollector
                    .OrderBy(l => ((Level)l).Elevation)
                    .Cast<Level>()
                    .ToList();

                _logger.Info($"Found {levels.Count} levels in document");
                return levels;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting levels: {ex.Message}");
                return new List<Level>();
            }
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

                    case LevelBandPosition.InBandByCenter:
                        // Element's center is within the level band
                        var centerAssignResult = AssignLevelParameter(element, levelName, logger);
                        if (centerAssignResult.Success)
                        {
                            result.ElementsAssigned++;
                            logger.LogSuccess(element.Id, element.Category?.Name, $"Assigned to level '{levelName}' ({SkipReasons.InBandByCenter})");
                        }
                        else if (centerAssignResult.Skipped)
                        {
                            result.ElementsSkippedNoBoundingBox++;
                            logger.LogSkip(element.Id, element.Category?.Name, centerAssignResult.SkipReason);
                        }
                        else
                        {
                            result.ElementsFailed++;
                            logger.LogSkip(element.Id, element.Category?.Name, centerAssignResult.SkipReason ?? SkipReasons.FailedToAssignParameter);
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
        /// Gets the position of an element relative to a level band.
        /// Uses a two-tier check:
        /// 1. First checks if element is COMPLETELY INSIDE the level band
        /// 2. If not, checks if element's CENTER POINT is within the level band (for elements straddling boundaries)
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level</param>
        /// <param name="topLevel">Top level</param>
        /// <param name="baseTolerance">Tolerance to extend below base level (project units) - not used for center check</param>
        /// <param name="topTolerance">Tolerance to extend above top level (project units) - not used for center check</param>
        /// <returns>Position relative to level band</returns>
        public LevelBandPosition GetElementPositionInBand(
            Element element,
            Level baseLevel,
            Level topLevel,
            double baseTolerance = 0.0,
            double topTolerance = 0.0)
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

            // Extend the band with tolerance for complete containment check
            var adjustedBase = baseElevation - baseTolerance;
            var adjustedTop = topElevation + topTolerance;

            // First check: Is element COMPLETELY INSIDE the extended range?
            if (bbox.Min.Z >= adjustedBase && bbox.Max.Z <= adjustedTop)
            {
                return LevelBandPosition.InBand;
            }

            // Second check: Is element's CENTER within the level band (no tolerance)?
            // This handles elements that straddle the boundary but whose center is in range
            double centerZ = (bbox.Min.Z + bbox.Max.Z) / 2.0;
            if (centerZ >= baseElevation && centerZ <= topElevation)
            {
                return LevelBandPosition.InBandByCenter;
            }

            // Determine position based on minimum point
            if (bbox.Min.Z < adjustedBase)
            {
                return LevelBandPosition.BelowBand;
            }

            // Must be above band
            return LevelBandPosition.AboveBand;
        }

        /// <summary>
        /// Checks if an element is within a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level of band</param>
        /// <param name="topLevel">Top level of band</param>
        /// <param name="baseTolerance">Tolerance to extend below base level (project units)</param>
        /// <param name="topTolerance">Tolerance to extend above top level (project units)</param>
        /// <returns>True if element is completely inside or has center within the level band</returns>
        public bool IsElementInLevelBand(
            Element element,
            Level baseLevel,
            Level topLevel,
            double baseTolerance = 0.0,
            double topTolerance = 0.0)
        {
            var position = GetElementPositionInBand(element, baseLevel, topLevel, baseTolerance, topTolerance);
            return position == LevelBandPosition.InBand || position == LevelBandPosition.InBandByCenter;
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
        /// <returns>True if element is completely inside or has center within the level band</returns>
        public bool IsElementInLevelBand(Element element, Level baseLevel, Level topLevel)
        {
            var position = GetElementPositionInBand(element, baseLevel, topLevel);
            return position == LevelBandPosition.InBand || position == LevelBandPosition.InBandByCenter;
        }

        /// <summary>
        /// Finds the nearest level between two levels (base and top).
        /// For excluded categories, determines which of the two levels is nearest to the element.
        /// </summary>
        /// <param name="element">Element to find nearest level for</param>
        /// <param name="baseLevel">Base level</param>
        /// <param name="topLevel">Top level</param>
        /// <returns>Nearest level (base or top) or null if element has no bounding box</returns>
        public Level FindNearestLevelBetween(Element element, Level baseLevel, Level topLevel)
        {
            if (element == null || baseLevel == null || topLevel == null)
            {
                return null;
            }

            var bbox = element.get_BoundingBox(null);
            if (bbox == null)
            {
                return null;
            }

            // Use the middle of the element's bounding box
            var elementElevation = (bbox.Min.Z + bbox.Max.Z) / 2.0;

            // Calculate distances to base and top levels
            var distanceToBase = System.Math.Abs(baseLevel.Elevation - elementElevation);
            var distanceToTop = System.Math.Abs(topLevel.Elevation - elementElevation);

            // Return the nearer level (typically top for elements between levels)
            return distanceToTop <= distanceToBase ? topLevel : baseLevel;
        }

        /// <summary>
        /// Finds the nearest level among a list of levels.
        /// For excluded categories, determines which level from the list is nearest to the element.
        /// </summary>
        /// <param name="element">Element to find nearest level for</param>
        /// <param name="levels">List of levels to search (must contain at least 2)</param>
        /// <returns>Nearest level from the list or null if element has no bounding box or list is empty</returns>
        public Level FindNearestLevelAmong(Element element, IList<Level> levels)
        {
            if (element == null || levels == null)
            {
                return null;
            }

            var bbox = element.get_BoundingBox(null);
            if (bbox == null)
            {
                return null;
            }

            // Use the middle of the element's bounding box
            var elementElevation = (bbox.Min.Z + bbox.Max.Z) / 2.0;

            // Find the nearest level
            Level nearestLevel = null;
            double minDistance = double.MaxValue;

            foreach (var level in levels)
            {
                var distance = System.Math.Abs(level.Elevation - elementElevation);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestLevel = level;
                }
            }

            return nearestLevel;
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
