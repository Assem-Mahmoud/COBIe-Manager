using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;

namespace COBIeManager.Features.ParameterFiller.ViewModels
{
    /// <summary>
    /// ViewModel for the Parameter Mapping dialog
    /// </summary>
    public partial class ParameterMappingViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ParameterItem> _selectedParameters;

        [ObservableProperty]
        private bool _hasUnmappedParameters;

        public int UnmappedCount => SelectedParameters?.Count(p => !p.IsMapped) ?? 0;
        public int MappedCount => SelectedParameters?.Count(p => p.IsMapped) ?? 0;
        public int TotalCount => SelectedParameters?.Count ?? 0;

        public ParameterMappingViewModel(System.Collections.Generic.IEnumerable<ParameterItem> selectedParameters)
        {
            SelectedParameters = new ObservableCollection<ParameterItem>(selectedParameters);

            // Subscribe to property changes BEFORE updating initial state
            foreach (var param in SelectedParameters)
            {
                param.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ParameterItem.IsMapped) ||
                        e.PropertyName == nameof(ParameterItem.ApplicableMode))
                    {
                        UpdateHasUnmappedParameters();
                        OnPropertyChanged(nameof(UnmappedCount));
                        OnPropertyChanged(nameof(MappedCount));
                        OnPropertyChanged(nameof(IsValid));
                    }
                };
            }

            // Mark all parameters as mapped initially since they're being opened for mapping
            // The user will set their modes, and the dialog will save those selections
            foreach (var param in SelectedParameters)
            {
                param.IsMapped = true;
            }

            // After initializing, update the unmapped state (should be all false now)
            UpdateHasUnmappedParameters();
        }

        partial void OnSelectedParametersChanged(ObservableCollection<ParameterItem> value)
        {
            UpdateHasUnmappedParameters();
        }

        private void UpdateHasUnmappedParameters()
        {
            HasUnmappedParameters = SelectedParameters?.Any(p => !p.IsMapped) ?? false;
        }

        /// <summary>
        /// Sets the mode for all unmapped parameters to Level mode
        /// </summary>
        [RelayCommand]
        private void SetAllUnmappedToLevel()
        {
            foreach (var param in SelectedParameters.Where(p => !p.IsMapped))
            {
                param.ApplicableMode = FillMode.Level;
                param.IsMapped = true;
            }
        }

        /// <summary>
        /// Sets the mode for all unmapped parameters to Room Name mode
        /// </summary>
        [RelayCommand]
        private void SetAllUnmappedToRoomName()
        {
            foreach (var param in SelectedParameters.Where(p => !p.IsMapped))
            {
                param.ApplicableMode = FillMode.RoomName;
                param.IsMapped = true;
            }
        }

        /// <summary>
        /// Sets the mode for all unmapped parameters to Room Number mode
        /// </summary>
        [RelayCommand]
        private void SetAllUnmappedToRoomNumber()
        {
            foreach (var param in SelectedParameters.Where(p => !p.IsMapped))
            {
                param.ApplicableMode = FillMode.RoomNumber;
                param.IsMapped = true;
            }
        }

        /// <summary>
        /// Sets the mode for all unmapped parameters to Groups mode
        /// </summary>
        [RelayCommand]
        private void SetAllUnmappedToGroups()
        {
            foreach (var param in SelectedParameters.Where(p => !p.IsMapped))
            {
                param.ApplicableMode = FillMode.Groups;
                param.IsMapped = true;
            }
        }

        /// <summary>
        /// Validates that all selected parameters have been mapped
        /// </summary>
        public bool IsValid => !HasUnmappedParameters;

        public string GetValidationError()
        {
            var unmapped = SelectedParameters.Where(p => !p.IsMapped).Select(p => p.DisplayName).ToList();
            if (unmapped.Count == 0) return null;
            return $"The following parameters are not mapped: {string.Join(", ", unmapped)}";
        }
    }
}
