using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Result of room detection and parameter assignment for an element.
    /// </summary>
    public class RoomAssignmentResult
    {
        /// <summary>
        /// The detected room (null if not found)
        /// </summary>
        public Room Room { get; set; }

        /// <summary>
        /// How the room was detected
        /// </summary>
        public RoomDetectionMethod DetectionMethod { get; set; }

        /// <summary>
        /// Count of parameters set (0-3)
        /// </summary>
        public int ParametersAssigned { get; set; }

        /// <summary>
        /// The element's ID for logging
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// Whether a room was found
        /// </summary>
        public bool RoomFound => Room != null;

        /// <summary>
        /// Whether any parameters were assigned
        /// </summary>
        public bool HasAssignments => ParametersAssigned > 0;

        /// <summary>
        /// Gets the room number if room was found
        /// </summary>
        public string RoomNumber => Room?.Number;

        /// <summary>
        /// Gets the room name if room was found
        /// </summary>
        public string RoomName => Room?.Name;

        /// <summary>
        /// Creates a successful room assignment result
        /// </summary>
        /// <param name="room">The detected room</param>
        /// <param name="detectionMethod">Method used for detection</param>
        /// <param name="parametersAssigned">Number of parameters assigned</param>
        /// <param name="elementId">Element ID</param>
        /// <returns>Successful room assignment result</returns>
        public static RoomAssignmentResult CreateSuccess(
            Room room,
            RoomDetectionMethod detectionMethod,
            int parametersAssigned,
            int elementId)
        {
            return new RoomAssignmentResult
            {
                Room = room,
                DetectionMethod = detectionMethod,
                ParametersAssigned = parametersAssigned,
                ElementId = elementId
            };
        }

        /// <summary>
        /// Creates a result for when no room was found
        /// </summary>
        /// <param name="detectionMethod">Method attempted</param>
        /// <param name="elementId">Element ID</param>
        /// <returns>Room assignment result with no room</returns>
        public static RoomAssignmentResult CreateNoRoom(
            RoomDetectionMethod detectionMethod,
            int elementId)
        {
            return new RoomAssignmentResult
            {
                Room = null,
                DetectionMethod = detectionMethod,
                ParametersAssigned = 0,
                ElementId = elementId
            };
        }

        /// <summary>
        /// Gets a formatted description of the detection method
        /// </summary>
        /// <returns>Human-readable detection method description</returns>
        public string GetDetectionMethodDescription()
        {
            return DetectionMethod switch
            {
                RoomDetectionMethod.DirectRoomProperty => "Direct Room Property",
                RoomDetectionMethod.FromRoomProperty => "From Room Property",
                RoomDetectionMethod.ToRoomProperty => "To Room Property",
                RoomDetectionMethod.PointInRoom => "Point in Room",
                RoomDetectionMethod.NoLocation => "No Location",
                RoomDetectionMethod.NoRoomFound => "No Room Found",
                _ => "Unknown"
            };
        }
    }
}
