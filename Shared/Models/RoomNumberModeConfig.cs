namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Room Number parameter filling.
    /// </summary>
    public class RoomNumberModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.RoomNumber;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Room Number";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill with room numbers";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "Numeric";

        // Future room number-specific settings can be added here:
        // - Search radius for finding rooms
        // - Room boundary options
        // - Phase selection
        // - Number format options
        // - etc.

        /// <summary>
        /// Validates the configuration for Room Number mode
        /// </summary>
        public override bool IsValid()
        {
            // Room Number mode doesn't require any specific settings currently
            return true;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            // No validation errors for Room Number mode currently
            return null;
        }
    }
}
