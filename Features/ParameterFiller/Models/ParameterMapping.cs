namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Maps logical parameter names to actual Revit parameter names.
    /// </summary>
    public class ParameterMapping
    {
        /// <summary>
        /// Parameter name for level assignment
        /// </summary>
        public string LevelParameter { get; set; } = "ACG-4D-Level";

        /// <summary>
        /// Parameter name for room number
        /// </summary>
        public string RoomNumberParameter { get; set; } = "ACG-4D-RoomNumber";

        /// <summary>
        /// Parameter name for room name
        /// </summary>
        public string RoomNameParameter { get; set; } = "ACG-4D-RoomName";

        /// <summary>
        /// Parameter name for combined room reference
        /// </summary>
        public string RoomRefParameter { get; set; } = "ACG-4D-RoomRef";

        /// <summary>
        /// Parameter name for ACG Box ID from groups
        /// </summary>
        public string AcgBoxIdParameter { get; set; } = "ACG-BOX-ID";

        /// <summary>
        /// Validates that all parameter names are non-empty
        /// </summary>
        /// <returns>True if all parameter names are valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(LevelParameter))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RoomNumberParameter))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RoomNameParameter))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RoomRefParameter))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(AcgBoxIdParameter))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a custom parameter mapping with specified parameter names
        /// </summary>
        /// <param name="levelParameter">Level parameter name</param>
        /// <param name="roomNumberParameter">Room number parameter name</param>
        /// <param name="roomNameParameter">Room name parameter name</param>
        /// <param name="roomRefParameter">Room reference parameter name</param>
        /// <returns>Custom parameter mapping</returns>
        public static ParameterMapping CreateCustom(
            string levelParameter,
            string roomNumberParameter,
            string roomNameParameter,
            string roomRefParameter,
            string acgBoxIdParameter = null)
        {
            return new ParameterMapping
            {
                LevelParameter = levelParameter,
                RoomNumberParameter = roomNumberParameter,
                RoomNameParameter = roomNameParameter,
                RoomRefParameter = roomRefParameter,
                AcgBoxIdParameter = acgBoxIdParameter ?? "ACG-BOX-ID"
            };
        }
    }
}
