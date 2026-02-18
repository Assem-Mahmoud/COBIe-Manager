using CommunityToolkit.Mvvm.ComponentModel;
using COBIeManager.Shared.Models;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Wrapper class for parameter items with selection state for UI binding
    /// </summary>
    public partial class ParameterItem : ObservableObject
    {
        /// <summary>
        /// Display name for the parameter
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Parameter name in Revit
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Description of what this parameter does
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether this parameter is selected for filling
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// The fill mode this parameter is assigned to
        /// </summary>
        [ObservableProperty]
        private FillMode _applicableMode = FillMode.None;

        /// <summary>
        /// Whether this parameter has been mapped to a mode by the user
        /// </summary>
        [ObservableProperty]
        private bool _isMapped = false;

        /// <summary>
        /// Creates a new parameter item
        /// </summary>
        /// <param name="displayName">Display name (typically the parameter name)</param>
        /// <param name="parameterName">Revit parameter name</param>
        /// <param name="description">Description</param>
        /// <param name="isSelected">Initial selection state</param>
        public ParameterItem(
            string displayName,
            string parameterName,
            string description,
            bool isSelected = false)
        {
            DisplayName = displayName;
            ParameterName = parameterName;
            Description = description;
            IsSelected = isSelected;
        }

        /// <summary>
        /// Called when ApplicableMode property changes
        /// </summary>
        partial void OnApplicableModeChanged(FillMode value)
        {
            // Automatically mark as mapped when a mode is explicitly set
            IsMapped = true;
        }
    }
}
