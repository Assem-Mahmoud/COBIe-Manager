using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// User configuration for the parameter fill operation.
    /// This configuration separates general settings from mode-specific settings.
    /// </summary>
    public class FillConfiguration
    {
        /// <summary>
        /// General settings that apply to all fill modes
        /// </summary>
        public GeneralFillSettings General { get; set; }

        /// <summary>
        /// Configuration specific to Level-based filling
        /// </summary>
        public LevelModeConfig LevelMode { get; set; }

        /// <summary>
        /// Configuration specific to Room Name filling
        /// </summary>
        public RoomNameModeConfig RoomNameMode { get; set; }

        /// <summary>
        /// Configuration specific to Room Number filling
        /// </summary>
        public RoomNumberModeConfig RoomNumberMode { get; set; }

        /// <summary>
        /// Configuration specific to Groups (Box ID) filling
        /// </summary>
        public GroupsModeConfig GroupsMode { get; set; }

        /// <summary>
        /// Configuration specific to Scope Box-based filling
        /// </summary>
        public ScopeBoxModeConfig ScopeBoxMode { get; set; }

        /// <summary>
        /// Configuration specific to Zone-based filling
        /// </summary>
        public ZoneModeConfig ZoneMode { get; set; }

        // ========== LEGACY PROPERTIES FOR BACKWARD COMPATIBILITY ==========
        // These properties delegate to the new structure to maintain compatibility
        // with existing code that hasn't been migrated yet.

        /// <summary>
        /// Legacy property - maps to General.OverwriteExisting
        /// </summary>
        [Obsolete("Use General.OverwriteExisting instead")]
        public bool OverwriteExisting
        {
            get => General?.OverwriteExisting ?? false;
            set
            {
                if (General != null) General.OverwriteExisting = value;
            }
        }

        /// <summary>
        /// Legacy property - maps to General.SelectedCategories
        /// </summary>
        [Obsolete("Use General.SelectedCategories instead")]
        public IList<BuiltInCategory> SelectedCategories
        {
            get => General?.SelectedCategories;
            set
            {
                if (General != null) General.SelectedCategories = value;
            }
        }

        /// <summary>
        /// Legacy property - maps to General.AvailableCategories
        /// </summary>
        [Obsolete("Use General.AvailableCategories instead")]
        public IList<CategoryItem> AvailableCategories
        {
            get => General?.AvailableCategories;
            set
            {
                if (General != null) General.AvailableCategories = value;
            }
        }

        /// <summary>
        /// Legacy property - maps to General.AvailableParameters
        /// </summary>
        [Obsolete("Use General.AvailableParameters instead")]
        public IList<ParameterItem> AvailableParameters
        {
            get => General?.AvailableParameters;
            set
            {
                if (General != null) General.AvailableParameters = value;
            }
        }

        /// <summary>
        /// Legacy property - returns the lowest selected level for backward compatibility
        /// </summary>
        [Obsolete("Use LevelMode.SelectedLevels instead - multi-level selection is now supported")]
        public Level BaseLevel
        {
            get
            {
                if (LevelMode?.SelectedLevels == null || LevelMode.SelectedLevels.Count == 0)
                    return null;
                // Return the lowest level (sorted by elevation)
                return LevelMode.SelectedLevels.OrderBy(l => l.Elevation).FirstOrDefault();
            }
            set
            {
                // For backward compatibility, replace selected levels with just this one level
                if (LevelMode != null && value != null)
                {
                    LevelMode.SelectedLevelIds.Clear();
                    LevelMode.SelectedLevels.Clear();
                    LevelMode.SelectedLevelIds.Add(value.Id);
                    LevelMode.SelectedLevels.Add(value);
                }
            }
        }

        /// <summary>
        /// Legacy property - returns the highest selected level for backward compatibility
        /// </summary>
        [Obsolete("Use LevelMode.SelectedLevels instead - multi-level selection is now supported")]
        public Level TopLevel
        {
            get
            {
                if (LevelMode?.SelectedLevels == null || LevelMode.SelectedLevels.Count == 0)
                    return null;
                // Return the highest level (sorted by elevation)
                return LevelMode.SelectedLevels.OrderByDescending(l => l.Elevation).FirstOrDefault();
            }
            set
            {
                // For backward compatibility, add this level to selected levels
                if (LevelMode != null && value != null && !LevelMode.SelectedLevelIds.Contains(value.Id))
                {
                    LevelMode.SelectedLevelIds.Add(value.Id);
                    LevelMode.SelectedLevels.Add(value);
                }
            }
        }

        /// <summary>
        /// Legacy property - returns the combined flags of enabled modes
        /// </summary>
        [Obsolete("Use individual mode properties instead")]
        public FillMode FillMode
        {
            get
            {
                FillMode mode = FillMode.None;
                if (LevelMode?.IsEnabled == true) mode |= FillMode.Level;
                if (RoomNameMode?.IsEnabled == true) mode |= FillMode.RoomName;
                if (RoomNumberMode?.IsEnabled == true) mode |= FillMode.RoomNumber;
                if (GroupsMode?.IsEnabled == true) mode |= FillMode.Groups;
                if (ScopeBoxMode?.IsEnabled == true) mode |= FillMode.ScopeBox;
                if (ZoneMode?.IsEnabled == true) mode |= FillMode.Zone;
                return mode;
            }
            set
            {
                // Set enabled flags based on the FillMode value
                if (LevelMode != null) LevelMode.IsEnabled = (value & FillMode.Level) != 0;
                if (RoomNameMode != null) RoomNameMode.IsEnabled = (value & FillMode.RoomName) != 0;
                if (RoomNumberMode != null) RoomNumberMode.IsEnabled = (value & FillMode.RoomNumber) != 0;
                if (GroupsMode != null) GroupsMode.IsEnabled = (value & FillMode.Groups) != 0;
                if (ScopeBoxMode != null) ScopeBoxMode.IsEnabled = (value & FillMode.ScopeBox) != 0;
                if (ZoneMode != null) ZoneMode.IsEnabled = (value & FillMode.Zone) != 0;
            }
        }

        /// <summary>
        /// Creates a default configuration with standard settings
        /// </summary>
        public static FillConfiguration CreateDefault()
        {
            return new FillConfiguration
            {
                General = GeneralFillSettings.CreateDefault(),
                LevelMode = new LevelModeConfig
                {
                    IsEnabled = false,
                    ExcludedCategories = new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_StructuralFraming,  // Beams
                        BuiltInCategory.OST_Floors               // Slabs/Floors
                    }
                },
                RoomNameMode = new RoomNameModeConfig { IsEnabled = false },
                RoomNumberMode = new RoomNumberModeConfig { IsEnabled = false },
                GroupsMode = new GroupsModeConfig { IsEnabled = false },
                ScopeBoxMode = new ScopeBoxModeConfig { IsEnabled = false },
                ZoneMode = new ZoneModeConfig { IsEnabled = false }
            };
        }

        /// <summary>
        /// Gets all enabled fill mode configurations
        /// </summary>
        public IEnumerable<FillModeConfigBase> GetEnabledModes()
        {
            var modes = new List<FillModeConfigBase>();

            if (LevelMode?.IsEnabled == true) modes.Add(LevelMode);
            if (RoomNameMode?.IsEnabled == true) modes.Add(RoomNameMode);
            if (RoomNumberMode?.IsEnabled == true) modes.Add(RoomNumberMode);
            if (GroupsMode?.IsEnabled == true) modes.Add(GroupsMode);
            if (ScopeBoxMode?.IsEnabled == true) modes.Add(ScopeBoxMode);
            if (ZoneMode?.IsEnabled == true) modes.Add(ZoneMode);

            return modes;
        }

        /// <summary>
        /// Gets a specific mode configuration by its type
        /// </summary>
        public T GetModeConfig<T>() where T : FillModeConfigBase
        {
            return typeof(T) switch
            {
                var t when t == typeof(LevelModeConfig) => LevelMode as T,
                var t when t == typeof(RoomNameModeConfig) => RoomNameMode as T,
                var t when t == typeof(RoomNumberModeConfig) => RoomNumberMode as T,
                var t when t == typeof(GroupsModeConfig) => GroupsMode as T,
                var t when t == typeof(ScopeBoxModeConfig) => ScopeBoxMode as T,
                var t when t == typeof(ZoneModeConfig) => ZoneMode as T,
                _ => null
            };
        }

        /// <summary>
        /// Validates the entire configuration
        /// </summary>
        public bool IsValid()
        {
            // Check for unmapped parameters (selected but not mapped to any mode)
            if (General?.HasUnmappedParameters() == true)
            {
                return false;
            }

            // Must have at least one mode enabled
            if (!HasAnyEnabledMode())
            {
                return false;
            }

            // Must have at least one category selected
            if (General?.HasSelectedCategories() != true)
            {
                return false;
            }

            // Validate each enabled mode
            foreach (var mode in GetEnabledModes())
            {
                if (!mode.IsValid())
                {
                    return false;
                }
            }

            // At least one enabled mode must have parameters mapped
            bool hasAnyMappedParameters = GetLevelModeParameters().Count > 0 ||
                                          GetRoomNameModeParameters().Count > 0 ||
                                          GetRoomNumberModeParameters().Count > 0 ||
                                          GetGroupModeParameters().Count > 0 ||
                                          GetScopeBoxModeParameters().Count > 0 ||
                                          GetZoneModeParameters().Count > 0;

            return hasAnyMappedParameters;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public string GetValidationError()
        {
            // Check for unmapped parameters first
            if (General?.HasUnmappedParameters() == true)
            {
                var unmappedParams = General.GetUnmappedParameterNames();
                return $"The following parameters must be mapped to a fill mode: {string.Join(", ", unmappedParams)}";
            }

            // No mode selected
            if (!HasAnyEnabledMode())
            {
                return "At least one fill mode must be selected";
            }

            // Must have at least one category selected
            if (General?.HasSelectedCategories() != true)
            {
                return "At least one category must be selected";
            }

            // Validate each enabled mode
            foreach (var mode in GetEnabledModes())
            {
                var error = mode.GetValidationError();
                if (error != null)
                {
                    return error;
                }
            }

            // At least one enabled mode must have parameters mapped
            bool hasAnyMappedParameters = GetLevelModeParameters().Count > 0 ||
                                          GetRoomNameModeParameters().Count > 0 ||
                                          GetRoomNumberModeParameters().Count > 0 ||
                                          GetGroupModeParameters().Count > 0 ||
                                          GetScopeBoxModeParameters().Count > 0 ||
                                          GetZoneModeParameters().Count > 0;

            if (!hasAnyMappedParameters)
            {
                return "At least one parameter must be mapped to a fill mode";
            }

            return null;
        }

        /// <summary>
        /// Checks if at least one fill mode is enabled
        /// </summary>
        private bool HasAnyEnabledMode()
        {
            return LevelMode?.IsEnabled == true ||
                   RoomNameMode?.IsEnabled == true ||
                   RoomNumberMode?.IsEnabled == true ||
                   GroupsMode?.IsEnabled == true ||
                   ScopeBoxMode?.IsEnabled == true ||
                   ZoneMode?.IsEnabled == true;
        }

        // ========== LEGACY METHODS FOR BACKWARD COMPATIBILITY ==========

        /// <summary>
        /// Checks if all selected parameters have been mapped to a fill mode
        /// </summary>
        public bool HasUnmappedParameters()
        {
            return General?.HasUnmappedParameters() ?? false;
        }

        /// <summary>
        /// Gets the list of unmapped parameter names
        /// </summary>
        public IList<string> GetUnmappedParameterNames()
        {
            return General?.GetUnmappedParameterNames() ?? new List<string>();
        }

        /// <summary>
        /// Gets the list of selected categories
        /// </summary>
        public IList<BuiltInCategory> GetSelectedCategories()
        {
            return General?.GetSelectedCategories() ?? new List<BuiltInCategory>();
        }

        /// <summary>
        /// Gets the list of selected parameters
        /// </summary>
        public IList<ParameterItem> GetSelectedParameters()
        {
            return General?.GetSelectedParameters() ?? new List<ParameterItem>();
        }

        /// <summary>
        /// Gets the list of selected parameter names
        /// </summary>
        public IList<string> GetSelectedParameterNames()
        {
            if (General?.AvailableParameters == null)
            {
                return new List<string>();
            }

            return General.AvailableParameters
                .Where(p => p.IsSelected)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Gets selected parameters filtered by their applicable mode
        /// </summary>
        public IList<string> GetParameterNamesByMode(FillMode mode)
        {
            return General?.GetParameterNamesByMode(mode) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for level-based filling
        /// </summary>
        public IList<string> GetLevelModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.Level) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for room name filling
        /// </summary>
        public IList<string> GetRoomNameModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.RoomName) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for room number filling
        /// </summary>
        public IList<string> GetRoomNumberModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.RoomNumber) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for group-based filling
        /// </summary>
        public IList<string> GetGroupModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.Groups) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for scope box-based filling
        /// </summary>
        public IList<string> GetScopeBoxModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.ScopeBox) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for zone-based filling
        /// </summary>
        public IList<string> GetZoneModeParameters()
        {
            return General?.GetParameterNamesByMode(FillMode.Zone) ?? new List<string>();
        }

        /// <summary>
        /// Gets selected parameters applicable for room-based filling (all room-related modes)
        /// </summary>
        public IList<string> GetRoomModeParameters()
        {
            if (General?.AvailableParameters == null)
            {
                return new List<string>();
            }

            return General.AvailableParameters
                .Where(p => p.IsSelected &&
                           (p.ApplicableMode == FillMode.RoomName ||
                            p.ApplicableMode == FillMode.RoomNumber))
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Checks if a specific parameter name is selected
        /// </summary>
        public bool IsParameterSelected(string parameterName)
        {
            if (General?.AvailableParameters == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            return General.AvailableParameters.Any(p =>
                p.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase) && p.IsSelected);
        }
    }
}
