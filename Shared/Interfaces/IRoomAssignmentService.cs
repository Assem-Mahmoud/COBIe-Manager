using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for assigning rooms to elements based on various detection methods
    /// </summary>
    public interface IRoomAssignmentService
    {
        /// <summary>
        /// Assigns rooms to elements using the configured detection method
        /// </summary>
        /// <param name="elements">Elements to process</param>
        /// <param name="detectionMethod">Room detection method to use</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <returns>Room assignment results for all processed elements</returns>
        RoomAssignmentResult AssignRoomsToElements(
            IEnumerable<Element> elements,
            RoomDetectionMethod detectionMethod,
            IProcessingLogger logger);

        /// <summary>
        /// Gets the room associated with an element using the specified detection method
        /// </summary>
        /// <param name="element">Element to find room for</param>
        /// <param name="detectionMethod">Room detection method</param>
        /// <returns>Room object if found, null otherwise</returns>
        Room GetRoomForElement(
            Element element,
            RoomDetectionMethod detectionMethod);
    }
}