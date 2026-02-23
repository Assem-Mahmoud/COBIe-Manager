namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Model Groups (Box ID) parameter filling.
    /// </summary>
    public class GroupsModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.Groups;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Groups";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill with Model Group box IDs";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "Group";

        // Future group-specific settings can be added here:
        // - Group type filter
        // - Nested group handling options
        // - Box ID format options
        // - etc.

        /// <summary>
        /// Validates the configuration for Groups mode
        /// </summary>
        public override bool IsValid()
        {
            // Groups mode doesn't require any specific settings currently
            return true;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            // No validation errors for Groups mode currently
            return null;
        }
    }
}
