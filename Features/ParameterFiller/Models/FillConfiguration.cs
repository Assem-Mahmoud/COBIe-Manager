using Autodesk.Revit.DB;
using COBIeManager.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// User configuration for the parameter fill operation.
    /// </summary>
    public class FillConfiguration
    {
        /// <summary>
        /// The lower level defining the vertical band bottom
        /// </summary>
        public Level BaseLevel { get; set; }

        /// <summary>
        /// The upper level defining the vertical band top
        /// </summary>
        public Level TopLevel { get; set; }

        /// <summary>
        /// Element categories to process (legacy property for backward compatibility)
        /// </summary>
        [System.Obsolete("Use AvailableCategories instead")]
        public IList<BuiltInCategory> SelectedCategories { get; set; }

        /// <summary>
        /// Available categories with selection state
        /// </summary>
        public IList<CategoryItem> AvailableCategories { get; set; }

        /// <summary>
        /// Available parameters with selection state
        /// </summary>
        public IList<ParameterItem> AvailableParameters { get; set; }

        /// <summary>
        /// Whether to overwrite existing parameter values
        /// </summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>
        /// Fill operation mode(s) - can be multiple using flags
        /// </summary>
        public FillMode FillMode { get; set; } = Shared.Models.FillMode.None;

        /// <summary>
        /// Creates a default configuration with standard settings
        /// </summary>
        public static FillConfiguration CreateDefault()
        {
            return new FillConfiguration
            {
                SelectedCategories = new List<BuiltInCategory>(),
                AvailableCategories = new List<CategoryItem>(),
                AvailableParameters = new List<ParameterItem>(),
                OverwriteExisting = false,
                FillMode = Shared.Models.FillMode.None
            };
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Check for unmapped parameters (selected but not mapped to any mode)
            if (HasUnmappedParameters())
            {
                return false;
            }

            // No mode selected
            if (FillMode == Shared.Models.FillMode.None)
            {
                return false;
            }

            // Helper to check if categories are selected
            bool HasCategories() => (SelectedCategories != null && SelectedCategories.Count > 0) ||
                                    (AvailableCategories != null && AvailableCategories.Any(c => c.IsSelected));

            bool hasLevelMode = (FillMode & Shared.Models.FillMode.Level) != 0;

            // Must have at least one category selected
            if (!HasCategories())
            {
                return false;
            }

            // If Level mode is selected, validate level requirements (but NOT parameter count)
            if (hasLevelMode)
            {
                if (BaseLevel == null || TopLevel == null)
                {
                    return false;
                }

                if (BaseLevel.Elevation >= TopLevel.Elevation)
                {
                    return false;
                }
            }

            // At least one selected mode must have parameters mapped
            // Modes without parameters will be skipped during execution
            bool hasAnyMappedParameters = GetLevelModeParameters().Count > 0 ||
                                          GetRoomNameModeParameters().Count > 0 ||
                                          GetRoomNumberModeParameters().Count > 0 ||
                                          GetGroupModeParameters().Count > 0;

            return hasAnyMappedParameters;
        }

        /// <summary>
        /// Checks if at least one parameter is selected
        /// </summary>
        private bool HasAtLeastOneSelectedParameter()
        {
            return AvailableParameters != null && AvailableParameters.Any(p => p.IsSelected);
        }

        /// <summary>
        /// Checks if all selected parameters have been mapped to a fill mode
        /// </summary>
        public bool HasUnmappedParameters()
        {
            return AvailableParameters != null && AvailableParameters.Any(p => p.IsSelected && !p.IsMapped);
        }

        /// <summary>
        /// Gets the list of unmapped parameter names
        /// </summary>
        public IList<string> GetUnmappedParameterNames()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && !p.IsMapped)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets the list of selected categories
        /// </summary>
        public IList<BuiltInCategory> GetSelectedCategories()
        {
            // If AvailableCategories is used, return selected ones
            if (AvailableCategories != null && AvailableCategories.Any())
            {
                return AvailableCategories.Where(c => c.IsSelected).Select(c => c.Category).ToList();
            }

            // Otherwise, return the legacy SelectedCategories
            return SelectedCategories ?? new List<BuiltInCategory>();
        }

        /// <summary>
        /// Gets the list of selected parameters
        /// </summary>
        public IList<ParameterItem> GetSelectedParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<ParameterItem>();
            }

            return AvailableParameters.Where(p => p.IsSelected).ToList();
        }

        /// <summary>
        /// Gets the list of selected parameter names
        /// </summary>
        public IList<string> GetSelectedParameterNames()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters.Where(p => p.IsSelected).Select(p => p.ParameterName).ToList();
        }

        /// <summary>
        /// Gets selected parameters filtered by their applicable mode
        /// </summary>
        /// <param name="mode">The fill mode to filter by</param>
        /// <returns>List of parameter names that match the specified mode</returns>
        public IList<string> GetParameterNamesByMode(Shared.Models.FillMode mode)
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == mode)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters applicable for level-based filling
        /// </summary>
        /// <returns>List of parameter names for level mode</returns>
        public IList<string> GetLevelModeParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == Shared.Models.FillMode.Level)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters applicable for room name filling
        /// </summary>
        /// <returns>List of parameter names for room name mode</returns>
        public IList<string> GetRoomNameModeParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == Shared.Models.FillMode.RoomName)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters applicable for room number filling
        /// </summary>
        /// <returns>List of parameter names for room number mode</returns>
        public IList<string> GetRoomNumberModeParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == Shared.Models.FillMode.RoomNumber)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters applicable for group-based filling
        /// </summary>
        /// <returns>List of parameter names for group mode</returns>
        public IList<string> GetGroupModeParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == Shared.Models.FillMode.Groups)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters applicable for room-based filling (all room-related modes)
        /// </summary>
        /// <returns>List of parameter names for room mode</returns>
        public IList<string> GetRoomModeParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected &&
                           (p.ApplicableMode == Shared.Models.FillMode.RoomName ||
                            p.ApplicableMode == Shared.Models.FillMode.RoomNumber))
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Checks if a specific parameter name is selected
        /// </summary>
        public bool IsParameterSelected(string parameterName)
        {
            if (AvailableParameters == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            return AvailableParameters.Any(p =>
                p.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase) && p.IsSelected);
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        /// <returns>Validation error message or null if valid</returns>
        public string GetValidationError()
        {
            // Check for unmapped parameters first
            var unmappedParams = GetUnmappedParameterNames();
            if (unmappedParams.Count > 0)
            {
                return $"The following parameters must be mapped to a fill mode: {string.Join(", ", unmappedParams)}";
            }

            // No mode selected
            if (FillMode == Shared.Models.FillMode.None)
            {
                return "At least one fill mode must be selected";
            }

            // Helper to check if categories are selected
            bool HasCategories() => (SelectedCategories != null && SelectedCategories.Count > 0) ||
                                    (AvailableCategories != null && AvailableCategories.Any(c => c.IsSelected));

            bool hasLevelMode = (FillMode & Shared.Models.FillMode.Level) != 0;

            // Must have at least one category selected
            if (!HasCategories())
            {
                return "At least one category must be selected";
            }

            // If Level mode is selected, validate level requirements (but NOT parameter count)
            if (hasLevelMode)
            {
                if (BaseLevel == null)
                {
                    return "Base level must be selected when Level mode is enabled";
                }

                if (TopLevel == null)
                {
                    return "Top level must be selected when Level mode is enabled";
                }

                if (BaseLevel.Elevation >= TopLevel.Elevation)
                {
                    return "Top level must be above base level";
                }
            }

            // At least one selected mode must have parameters mapped
            // Modes without parameters will be skipped during execution
            bool hasAnyMappedParameters = GetLevelModeParameters().Count > 0 ||
                                          GetRoomNameModeParameters().Count > 0 ||
                                          GetRoomNumberModeParameters().Count > 0 ||
                                          GetGroupModeParameters().Count > 0;

            if (!hasAnyMappedParameters)
            {
                return "At least one parameter must be mapped to a fill mode";
            }

            return null;
        }
    }
}
