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
        /// Gets the position of an element relative to a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level</param>
        /// <param name="topLevel">Top level</param>
        /// <param name="baseTolerance">Tolerance below base level in project units (default: 0)</param>
        /// <param name="topTolerance">Tolerance above top level in project units (default: 0)</param>
        /// <returns>Position relative to level band</returns>
        LevelBandPosition GetElementPositionInBand(
            Element element,
            Level baseLevel,
            Level topLevel,
            double baseTolerance = 0.0,
            double topTolerance = 0.0);

        /// <summary>
        /// Checks if an element is within a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level of band</param>
        /// <param name="topLevel">Top level of band</param>
        /// <param name="baseTolerance">Tolerance below base level in project units (default: 0)</param>
        /// <param name="topTolerance">Tolerance above top level in project units (default: 0)</param>
        /// <returns>True if element intersects the level band</returns>
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
    }
}
