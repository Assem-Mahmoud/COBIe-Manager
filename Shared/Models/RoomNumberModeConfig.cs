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

        /// <summary>
        /// Tolerance to extend the room bounding box for detection in internal units (feet).
        /// UI input is in millimeters and converted to feet automatically.
        /// This helps detect elements that are hosted in walls (like electrical sockets)
        /// or elements just outside room boundaries.
        /// Default: 150mm (~0.5 feet) - typical wall thickness allowance
        /// </summary>
        public double Tolerance { get; set; } = 0.5;

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
