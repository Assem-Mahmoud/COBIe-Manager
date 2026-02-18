using CommunityToolkit.Mvvm.ComponentModel;
using COBIeManager.Shared.Models;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Wrapper class for FillMode with selection state for UI binding
    /// </summary>
    public partial class FillModeItem : ObservableObject
    {
        /// <summary>
        /// Display name for the fill mode
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Description of what this mode does
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The FillMode enum value
        /// </summary>
        public FillMode Mode { get; }

        /// <summary>
        /// Icon kind for MaterialDesign icon
        /// </summary>
        public string IconKind { get; }

        /// <summary>
        /// Whether this mode is selected for filling
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Number of parameters mapped to this mode
        /// </summary>
        [ObservableProperty]
        private int _mappedParameterCount;

        /// <summary>
        /// Whether this mode has any mapped parameters
        /// </summary>
        public bool HasMappedParameters => MappedParameterCount > 0;

        /// <summary>
        /// Creates a new FillModeItem
        /// </summary>
        public FillModeItem(string displayName, string description, FillMode mode, string iconKind, bool isSelected = false)
        {
            DisplayName = displayName;
            Description = description;
            Mode = mode;
            IconKind = iconKind;
            IsSelected = isSelected;
            MappedParameterCount = 0;
        }

        /// <summary>
        /// Called when MappedParameterCount changes to update HasMappedParameters
        /// </summary>
        partial void OnMappedParameterCountChanged(int value)
        {
            OnPropertyChanged(nameof(HasMappedParameters));
        }
    }
}
