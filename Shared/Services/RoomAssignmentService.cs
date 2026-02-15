using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for assigning rooms to elements based on various detection methods
    /// </summary>
    public class RoomAssignmentService : IRoomAssignmentService
    {
        private readonly ILogger _logger;

        public RoomAssignmentService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets room associated with an element using specified detection method
        /// </summary>
        /// <param name="element">Element to find room for</param>
        /// <param name="detectionMethod">Room detection method</param>
        /// <returns>Room object if found, null otherwise</returns>
        public Room GetRoomForElement(Element element, RoomDetectionMethod detectionMethod)
        {
            if (element == null)
            {
                _logger.Warn("Cannot find room for null element");
                return null;
            }

            var document = element.Document;

            // Try direct Room property first (most reliable)
            if (detectionMethod == RoomDetectionMethod.DirectRoomProperty ||
                detectionMethod == RoomDetectionMethod.PointInRoom)
            {
                if (element is FamilyInstance familyInstance)
                {
                    var room = familyInstance.Room;
                    if (room != null)
                    {
                        _logger.Debug($"Element {element.Id}: Room found via Room property - '{room.Number}: {room.Name}'");
                        return room;
                    }
                }
            }

            // Try FromRoom/ToRoom for doors
            if (element is FamilyInstance doorInstance &&
                doorInstance.Category != null &&
                doorInstance.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
            {
                var fromRoom = doorInstance.FromRoom;
                if (fromRoom != null)
                {
                    _logger.Debug($"Element {element.Id}: Room found via FromRoom property - '{fromRoom.Number}: {fromRoom.Name}'");
                    return fromRoom;
                }

                var toRoom = doorInstance.ToRoom;
                if (toRoom != null)
                {
                    _logger.Debug($"Element {element.Id}: Room found via ToRoom property - '{toRoom.Number}: {toRoom.Name}'");
                    return toRoom;
                }
            }

            // Fallback to point-in-room detection
            if (detectionMethod == RoomDetectionMethod.PointInRoom)
            {
                var point = GetElementPoint(element);
                if (point != null)
                {
               
                    var room = document.GetRoomAtPoint(point);
                    if (room != null)
                    {
                        _logger.Debug($"Element {element.Id}: Room found via GetRoomAtPoint - '{room.Number}: {room.Name}'");
                        return room;
                    }
                    else
                    {
                        _logger.Debug($"Element {element.Id}: No room found at point ({point.X}, {point.Y}, {point.Z})");
                    }
                }
                else
                {
                    _logger.Debug($"Element {element.Id}: No location point available for room detection");
                }
            }

            _logger.Debug($"Element {element.Id}: No room found using method '{detectionMethod}'");
            return null;
        }

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
        public int AssignRoomParameters(
            Element element,
            Room room,
            IProcessingLogger logger,
            string roomNumberParam = "ACG-4D-RoomNumber",
            string roomNameParam = "ACG-4D-RoomName",
            string roomRefParam = "ACG-4D-RoomRef",
            bool overwrite = true)
        {
            if (element == null || room == null)
            {
                return 0;
            }

            int parametersAssigned = 0;

            // Assign room number
            if (TrySetParameter(element, roomNumberParam, room.Number, overwrite))
            {
                parametersAssigned++;
            }

            // Assign room name
            if (TrySetParameter(element, roomNameParam, room.Name, overwrite))
            {
                parametersAssigned++;
            }

            // Assign room reference (combined number and name)
            var roomRef = $"{room.Number}: {room.Name}";
            if (TrySetParameter(element, roomRefParam, roomRef, overwrite))
            {
                parametersAssigned++;
            }

            // Log the overall room parameter assignment
            if (parametersAssigned > 0)
            {
                logger.LogRoomParameterFilled(element.Id, element.Category?.Name, roomRef);
            }

            return parametersAssigned;
        }

        /// <summary>
        /// Gets the center point of an element for room detection
        /// </summary>
        /// <param name="element">Element to get point from</param>
        /// <returns>XYZ point if available, null otherwise</returns>
        private XYZ GetElementPoint(Element element)
        {
            if (element == null)
            {
                return null;
            }

            // Try LocationPoint first
            var locationPoint = element.Location as LocationPoint;
            if (locationPoint != null)
            {
                return locationPoint.Point;
            }

            // Try LocationCurve for elements like doors/windows
            var locationCurve = element.Location as LocationCurve;
            if (locationCurve != null)
            {
                var curve = locationCurve.Curve;
                if (curve != null)
                {
                    // Use the midpoint of the curve
                    return curve.Evaluate(0.5, true);
                }
            }

            // Fallback to bounding box center
            var bbox = element.get_BoundingBox(null);
            if (bbox != null)
            {
                var center = new XYZ(
                    (bbox.Min.X + bbox.Max.X) / 2,
                    (bbox.Min.Y + bbox.Max.Y) / 2,
                    (bbox.Min.Z + bbox.Max.Z) / 2
                );
                return center;
            }

            return null;
        }

        /// <summary>
        /// Safely sets a parameter value on an element
        /// </summary>
        /// <param name="element">Element to modify</param>
        /// <param name="parameterName">Name of parameter to set</param>
        /// <param name="value">Value to set</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>True if parameter was set, false otherwise</returns>
        private bool TrySetParameter(Element element, string parameterName, string value, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            var parameter = element.LookupParameter(parameterName);

            if (parameter == null)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' not found");
                return false;
            }

            if (parameter.IsReadOnly)
            {
                _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' is read-only");
                return false;
            }

            // Check if value exists and overwrite is disabled
            if (!overwrite)
            {
                var currentValue = parameter.AsString();
                if (!string.IsNullOrWhiteSpace(currentValue))
                {
                    _logger.Debug($"Element {element.Id}: Parameter '{parameterName}' has existing value, overwrite disabled");
                    return false;
                }
            }

            try
            {
                parameter.Set(value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Element {element.Id}: Failed to set parameter '{parameterName}'", ex);
                return false;
            }
        }
    }
}
