using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using COBIeManager.Features.ParameterFiller.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for assigning rooms to elements based on various detection methods
    /// </summary>
    public interface IRoomAssignmentService
    {
        /// <summary>
        /// Gets room associated with an element using specified detection method
        /// </summary>
        /// <param name="element">Element to find room for</param>
        /// <param name="detectionMethod">Room detection method</param>
        /// <returns>Room object if found, null otherwise</returns>
        Room GetRoomForElement(
            Element element,
            RoomDetectionMethod detectionMethod);

        /// <summary>
        /// Assigns room parameters to an element
        /// </summary>
        /// <param name="element">Element to assign parameters to</param>
        /// <param name="room">Room to get information from</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <param name="roomNumberParam">Room number parameter name</param>
        /// <param name="roomNameParam">Room name parameter name</param>
        /// <param name="roomRefParam">Room reference parameter name</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>Number of parameters assigned (0-3)</returns>
        int AssignRoomParameters(
            Element element,
            Room room,
            IProcessingLogger logger,
            string roomNumberParam = "ACG-4D-RoomNumber",
            string roomNameParam = "ACG-4D-RoomName",
            string roomRefParam = "ACG-4D-RoomRef",
            bool overwrite = true);
    }
}
