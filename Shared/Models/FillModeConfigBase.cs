using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Base class for fill mode specific configurations.
    /// Each fill mode (Level, RoomName, RoomNumber, Groups) has its own configuration.
    /// </summary>
    public abstract class FillModeConfigBase
    {
        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public abstract FillMode Mode { get; }

        /// <summary>
        /// Whether this fill mode is enabled/selected by the user
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The value to use for elements that don't match the filter criteria.
        /// Default is "N/A" to indicate that the element is not associated with any room, level, group, etc.
        /// </summary>
        public string NotAssignedValue { get; set; } = "N/A";

        /// <summary>
        /// Validates the configuration for this fill mode
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public abstract bool IsValid();

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        /// <returns>Validation error message or null if valid</returns>
        public abstract string GetValidationError();

        /// <summary>
        /// Gets the display name for this fill mode
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Gets the description for this fill mode
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the icon kind for this fill mode (for Material Design icons)
        /// </summary>
        public abstract string IconKind { get; }
    }
}
