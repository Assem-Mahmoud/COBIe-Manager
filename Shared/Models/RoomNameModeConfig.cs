namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Room Name parameter filling.
    /// </summary>
    public class RoomNameModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.RoomName;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Room Name";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill with room names";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "Home";

        // Future room-specific settings can be added here:
        // - Search radius for finding rooms
        // - Room boundary options
        // - Phase selection
        // - etc.

        /// <summary>
        /// Validates the configuration for Room Name mode
        /// </summary>
        public override bool IsValid()
        {
            // Room Name mode doesn't require any specific settings currently
            return true;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            // No validation errors for Room Name mode currently
            return null;
        }
    }
}
