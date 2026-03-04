using System.Collections.Generic;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for assigning elements to levels based on bounding box intersection
    /// </summary>
    public interface ILevelAssignmentService
    {
        /// <summary>
        /// Gets the position of an element relative to a level band.
        /// Uses a two-tier check:
        /// 1. First checks if element is COMPLETELY INSIDE the level band (with tolerance applied)
        /// 2. If not, checks if element's CENTER POINT is within the level band (without tolerance)
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level</param>
        /// <param name="topLevel">Top level</param>
        /// <param name="baseTolerance">Tolerance below base level in project units for complete containment check (default: 0)</param>
        /// <param name="topTolerance">Tolerance above top level in project units for complete containment check (default: 0)</param>
        /// <returns>Position relative to level band (InBand, InBandByCenter, BelowBand, AboveBand, or NoBoundingBox)</returns>
        LevelBandPosition GetElementPositionInBand(
            Element element,
            Level baseLevel,
            Level topLevel,
            double baseTolerance = 0.0,
            double topTolerance = 0.0);

        /// <summary>
        /// Checks if an element is within a level band.
        /// Returns true if element is completely inside the band OR if its center point is within the band.
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level of band</param>
        /// <param name="topLevel">Top level of band</param>
        /// <param name="baseTolerance">Tolerance below base level in project units (default: 0)</param>
        /// <param name="topTolerance">Tolerance above top level in project units (default: 0)</param>
        /// <returns>True if element is within the level band (fully inside or by center)</returns>
        bool IsElementInLevelBand(
            Element element,
            Level baseLevel,
            Level topLevel,
            double baseTolerance = 0.0,
            double topTolerance = 0.0);

        /// <summary>
        /// Assigns level parameter to a single element
        /// </summary>
        /// <param name="element">Element to assign level to</param>
        /// <param name="levelName">Level name to assign</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <param name="parameterName">Parameter name (default: "ACG-4D-Level")</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>ParameterAssignmentResult indicating success, skip, or failure with reason</returns>
        ParameterAssignmentResult AssignLevelParameter(
            Element element,
            string levelName,
            IProcessingLogger logger,
            string parameterName = "ACG-4D-Level",
            bool overwrite = true);

        /// <summary>
        /// Finds the nearest level between two levels (base and top).
        /// For excluded categories, determines which of the two levels is nearest to the element.
        /// </summary>
        /// <param name="element">Element to find nearest level for</param>
        /// <param name="baseLevel">Base level</param>
        /// <param name="topLevel">Top level</param>
        /// <returns>Nearest level (base or top) or null if element has no bounding box</returns>
        Level FindNearestLevelBetween(Element element, Level baseLevel, Level topLevel);

        /// <summary>
        /// Finds the nearest level among a list of levels.
        /// For excluded categories, determines which level from the list is nearest to the element.
        /// </summary>
        /// <param name="element">Element to find nearest level for</param>
        /// <param name="levels">List of levels to search (must contain at least 2)</param>
        /// <returns>Nearest level from the list or null if element has no bounding box or list is empty</returns>
        Level FindNearestLevelAmong(Element element, IList<Level> levels);
    }
}
