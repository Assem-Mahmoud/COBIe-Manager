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
        /// Assigns elements to a level range based on bounding box intersection
        /// </summary>
        /// <param name="elements">Elements to process</param>
        /// <param name="baseLevel">Bottom level of the range</param>
        /// <param name="topLevel">Top level of the range</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <returns>Assignment results for all processed elements</returns>
        LevelAssignmentResult AssignElementsToLevelRange(
            IEnumerable<Element> elements,
            Level baseLevel,
            Level topLevel,
            IProcessingLogger logger);

        /// <summary>
        /// Gets the position of an element relative to a level band
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <param name="baseLevel">Bottom level</param>
        /// <param name="topLevel">Top level</param>
        /// <returns>Position relative to the level band</returns>
        LevelBandPosition GetElementPositionInBand(
            Element element,
            Level baseLevel,
            Level topLevel);
    }
}