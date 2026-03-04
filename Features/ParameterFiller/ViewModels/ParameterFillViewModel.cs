using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.DependencyInjection;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Models;
using COBIeManager.Shared.Services;

namespace COBIeManager.Features.ParameterFiller.ViewModels
{
    /// <summary>
    /// ViewModel for the Parameter Fill window
    /// </summary>
    public partial class ParameterFillViewModel : ObservableObject
    {
        private readonly IParameterFillService _parameterFillService;
        private readonly Autodesk.Revit.UI.UIDocument _uiDoc;

        [ObservableProperty]
        private FillConfiguration _config;

        [ObservableProperty]
        private PreviewSummary _previewSummary;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _categorySearchText = string.Empty;

        [ObservableProperty]
        private string _parameterSearchText = string.Empty;

        // UI Collections
        public ObservableCollection<Level> Levels { get; }
        public ObservableCollection<Element> ScopeBoxes { get; }
        public ObservableCollection<Element> Zones { get; }
        public ObservableCollection<CategoryItem> AvailableCategories { get; }
        public ObservableCollection<ParameterItem> AvailableParameters { get; }
        public ObservableCollection<FillModeItem> AvailableFillModes { get; }

        // Filtered collections for search
        public ICollectionView FilteredCategories { get; }
        public ICollectionView FilteredParameters { get; }

        // Computed properties
        public bool HasPreview => PreviewSummary != null;
        public bool HasUnmappedParameters => AvailableParameters?.Any(p => p.IsSelected && !p.IsMapped) ?? false;
        public bool HasSelectedFillModes => AvailableFillModes?.Any(m => m.IsSelected) ?? false;
        public bool CanExecutePreview => Config != null && HasSelectedFillModes && !IsProcessing;
        public bool CanExecuteFill => Config != null && HasSelectedFillModes && !IsProcessing && !HasUnmappedParameters;
        public bool HasValidationWarning => PreviewSummary != null && PreviewSummary.HasValidationWarnings;
        public string PreviewStatusMessage => PreviewSummary?.GetStatusMessage() ?? "Run preview to see estimated counts";

        // Category selection counts
        public int SelectedCategoryCount => AvailableCategories?.Count(c => c.IsSelected) ?? 0;
        public int TotalCategoryCount => AvailableCategories?.Count ?? 0;

        // Parameter selection counts
        public int SelectedParameterCount => AvailableParameters?.Count(p => p.IsSelected) ?? 0;
        public int TotalParameterCount => AvailableParameters?.Count ?? 0;

        // FillMode selection counts
        public int SelectedFillModeCount => AvailableFillModes?.Count(m => m.IsSelected) ?? 0;
        public int TotalFillModeCount => AvailableFillModes?.Count ?? 0;

        public ParameterFillViewModel(Autodesk.Revit.UI.UIDocument uiDoc)
        {
            if (uiDoc == null)
                throw new ArgumentNullException(nameof(uiDoc));

            _uiDoc = uiDoc;

            // Get services from ServiceLocator
            _parameterFillService = ServiceLocator.GetService<IParameterFillService>()
                ?? throw new InvalidOperationException("IParameterFillService not registered in DI container");

            // Initialize default configuration
            Config = FillConfiguration.CreateDefault();

            // Initialize collections
            Levels = new ObservableCollection<Level>();
            ScopeBoxes = new ObservableCollection<Element>();
            Zones = new ObservableCollection<Element>();
            AvailableCategories = new ObservableCollection<CategoryItem>();
            AvailableParameters = new ObservableCollection<ParameterItem>();

            // Create FillModeItems linked to their corresponding config objects
            AvailableFillModes = new ObservableCollection<FillModeItem>
            {
                new FillModeItem("Level", "Fill level-based parameters", FillMode.Level, "Layers", isSelected: false)
                {
                    Config = Config.LevelMode
                },
                new FillModeItem("Room Name", "Fill with room names", FillMode.RoomName, "AlphabeticalVariant", isSelected: false)
                {
                    Config = Config.RoomNameMode
                },
                new FillModeItem("Room Number", "Fill with room numbers", FillMode.RoomNumber, "Numeric", isSelected: false)
                {
                    Config = Config.RoomNumberMode
                },
                new FillModeItem("Groups", "Fill with Model Group box IDs", FillMode.Groups, "Group", isSelected: false)
                {
                    Config = Config.GroupsMode
                },
                new FillModeItem("Building", "Fill with scope box names", FillMode.ScopeBox, "Home", isSelected: false)
                {
                    Config = Config.ScopeBoxMode
                },
                new FillModeItem("Zone", "Fill with zone names", FillMode.Zone, "CheckboxBlankCircleOutline", isSelected: false)
                {
                    Config = Config.ZoneMode
                }
            };

            // Wire up property change handlers for FillModeItems
            foreach (var modeItem in AvailableFillModes)
            {
                modeItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FillModeItem.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedFillModeCount));
                        OnPropertyChanged(nameof(HasSelectedFillModes));
                        OnPropertyChanged(nameof(CanExecutePreview));
                        OnPropertyChanged(nameof(CanExecuteFill));
                    }
                    else if (e.PropertyName == nameof(FillModeItem.MappedParameterCount))
                    {
                        OnPropertyChanged(nameof(HasSelectedFillModes));
                        OnPropertyChanged(nameof(CanExecutePreview));
                        OnPropertyChanged(nameof(CanExecuteFill));
                    }
                };
            }

            // Initialize filtered collections with search
            FilteredCategories = new CollectionViewSource { Source = AvailableCategories }.View;
            FilteredCategories.Filter = FilterCategories;

            FilteredParameters = new CollectionViewSource { Source = AvailableParameters }.View;
            FilteredParameters.Filter = FilterParameters;

            // Load data from document
            LoadLevels();
            LoadScopeBoxes();
            LoadZones();
            LoadCategories();
            LoadParameters();
        }

        /// <summary>
        /// Called when Config property changes
        /// </summary>
        partial void OnConfigChanged(FillConfiguration value)
        {
            if (value != null)
            {
                // Reset preview when config changes
                PreviewSummary = new PreviewSummary();

                // Update status message based on validation
                if (!value.IsValid())
                {
                    var error = value.GetValidationError();
                    StatusMessage = $"Configuration error: {error}";
                }
                else
                {
                    StatusMessage = "Ready to preview";
                }

                OnPropertyChanged(nameof(CanExecutePreview));
                OnPropertyChanged(nameof(CanExecuteFill));
                OnPropertyChanged(nameof(HasValidationWarning));
            }
        }

        /// <summary>
        /// Called when PreviewSummary property changes
        /// </summary>
        partial void OnPreviewSummaryChanged(PreviewSummary value)
        {
            OnPropertyChanged(nameof(HasPreview));
            OnPropertyChanged(nameof(HasValidationWarning));
            OnPropertyChanged(nameof(PreviewStatusMessage));
        }

        /// <summary>
        /// Called when IsProcessing property changes
        /// </summary>
        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanExecutePreview));
            OnPropertyChanged(nameof(CanExecuteFill));
        }

        /// <summary>
        /// Called when CategorySearchText property changes
        /// </summary>
        partial void OnCategorySearchTextChanged(string value)
        {
            FilteredCategories?.Refresh();
        }

        /// <summary>
        /// Called when ParameterSearchText property changes
        /// </summary>
        partial void OnParameterSearchTextChanged(string value)
        {
            FilteredParameters?.Refresh();
        }

        /// <summary>
        /// Filter method for categories
        /// </summary>
        private bool FilterCategories(object obj)
        {
            if (obj is CategoryItem category)
            {
                if (string.IsNullOrWhiteSpace(CategorySearchText))
                    return true;

                return category.DisplayName.IndexOf(CategorySearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        /// <summary>
        /// Filter method for parameters
        /// </summary>
        private bool FilterParameters(object obj)
        {
            if (obj is ParameterItem parameter)
            {
                if (string.IsNullOrWhiteSpace(ParameterSearchText))
                    return true;

                return parameter.DisplayName.IndexOf(ParameterSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        /// <summary>
        /// Loads levels from the Revit document
        /// </summary>
        private void LoadLevels()
        {
            if (_uiDoc == null || _uiDoc.Document == null)
            {
                StatusMessage = "No active document";
                return;
            }

            try
            {
                var doc = _uiDoc.Document;
                var levelCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level));

                Levels.Clear();
                foreach (var level in levelCollector
                    .OrderBy(l => ((Level)l).Elevation)
                    .Cast<Level>())
                {
                    Levels.Add(level);
                }

                // Restore selected levels from config
                // First, save the previously selected IDs before clearing
                var previouslySelectedIds = Config.LevelMode.SelectedLevelIds.ToList();
                Config.LevelMode.SelectedLevels.Clear();
                Config.LevelMode.SelectedLevelIds.Clear();
                foreach (var level in Levels.Where(l => previouslySelectedIds.Contains(l.Id)))
                {
                    Config.LevelMode.SelectedLevels.Add(level);
                    Config.LevelMode.SelectedLevelIds.Add(level.Id);
                }

                // If no levels were previously selected and we have at least 2, select the first 2 by default
                if (Config.LevelMode.SelectedLevels.Count == 0 && Levels.Count >= 2)
                {
                    Config.LevelMode.SelectedLevels.Add(Levels[0]);
                    Config.LevelMode.SelectedLevelIds.Add(Levels[0].Id);
                    Config.LevelMode.SelectedLevels.Add(Levels[1]);
                    Config.LevelMode.SelectedLevelIds.Add(Levels[1].Id);
                }

                StatusMessage = $"Loaded {Levels.Count} levels";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading levels: {ex.Message}";
            }
        }

        /// <summary>
        /// Toggles the selection of a level
        /// </summary>
        public void ToggleLevel(Level level)
        {
            if (level == null || Config?.LevelMode == null)
                return;

            var levelId = level.Id;

            if (Config.LevelMode.SelectedLevelIds.Contains(levelId))
            {
                Config.LevelMode.SelectedLevelIds.Remove(levelId);
                Config.LevelMode.SelectedLevels.Remove(level);
            }
            else
            {
                Config.LevelMode.SelectedLevelIds.Add(levelId);
                Config.LevelMode.SelectedLevels.Add(level);
            }

            // Notify that the collections have changed
            OnPropertyChanged(nameof(Config.LevelMode.SelectedLevelIds));
            OnPropertyChanged(nameof(Config.LevelMode.SelectedLevels));

            StatusMessage = $"Selected {Config.LevelMode.SelectedLevelIds.Count} level(s)";
        }

        /// <summary>
        /// Command for toggling level selection
        /// </summary>
        public IRelayCommand<Level> ToggleLevelCommand => new RelayCommand<Level>(ToggleLevel);

        /// <summary>
        /// Checks if a level is selected
        /// </summary>
        public bool IsLevelSelected(Level level)
        {
            if (level == null || Config?.LevelMode == null)
                return false;

            return Config.LevelMode.SelectedLevelIds.Contains(level.Id);
        }

        /// <summary>
        /// Sets a custom name for a level
        /// </summary>
        /// <param name="level">The level to set the custom name for</param>
        /// <param name="customName">The custom name (empty or null to remove)</param>
        public void SetCustomLevelName(Level level, string customName)
        {
            if (level == null || Config?.LevelMode == null)
                return;

            if (string.IsNullOrWhiteSpace(customName))
            {
                // Remove custom name if empty
                if (Config.LevelMode.CustomLevelNames.ContainsKey(level.Id))
                {
                    Config.LevelMode.CustomLevelNames.Remove(level.Id);
                }
            }
            else
            {
                // Set or update custom name
                Config.LevelMode.CustomLevelNames[level.Id] = customName.Trim();
            }

            StatusMessage = $"Custom name {(string.IsNullOrWhiteSpace(customName) ? "removed" : "set")} for {level.Name}";
        }

        /// <summary>
        /// Gets the custom name for a level
        /// </summary>
        /// <param name="level">The level to get the custom name for</param>
        /// <returns>Custom name if set, otherwise null</returns>
        public string GetCustomLevelName(Level level)
        {
            if (level == null || Config?.LevelMode == null)
                return null;

            if (Config.LevelMode.CustomLevelNames.TryGetValue(level.Id, out var customName))
            {
                return customName;
            }

            return null;
        }

        /// <summary>
        /// Sets a custom name for a zone (scope box)
        /// </summary>
        /// <param name="zone">The zone element to set the custom name for</param>
        /// <param name="customName">The custom name (empty or null to remove)</param>
        public void SetCustomZoneName(Element zone, string customName)
        {
            if (zone == null || Config?.ZoneMode == null)
                return;

            if (string.IsNullOrWhiteSpace(customName))
            {
                // Remove custom name if empty
                if (Config.ZoneMode.CustomZoneNames.ContainsKey(zone.Id))
                {
                    Config.ZoneMode.CustomZoneNames.Remove(zone.Id);
                }
            }
            else
            {
                // Set or update custom name
                Config.ZoneMode.CustomZoneNames[zone.Id] = customName.Trim();
            }

            StatusMessage = $"Custom name {(string.IsNullOrWhiteSpace(customName) ? "removed" : "set")} for {zone.Name}";
        }

        /// <summary>
        /// Gets the custom name for a zone
        /// </summary>
        /// <param name="zone">The zone element to get the custom name for</param>
        /// <returns>Custom name if set, otherwise null</returns>
        public string GetCustomZoneName(Element zone)
        {
            if (zone == null || Config?.ZoneMode == null)
                return null;

            if (Config.ZoneMode.CustomZoneNames.TryGetValue(zone.Id, out var customName))
            {
                return customName;
            }

            return null;
        }

        /// <summary>
        /// Sets a custom name for a scope box
        /// </summary>
        /// <param name="scopeBox">The scope box element to set the custom name for</param>
        /// <param name="customName">The custom name (empty or null to remove)</param>
        public void SetCustomScopeBoxName(Element scopeBox, string customName)
        {
            if (scopeBox == null || Config?.ScopeBoxMode == null)
                return;

            if (string.IsNullOrWhiteSpace(customName))
            {
                // Remove custom name if empty
                if (Config.ScopeBoxMode.CustomScopeBoxNames.ContainsKey(scopeBox.Id))
                {
                    Config.ScopeBoxMode.CustomScopeBoxNames.Remove(scopeBox.Id);
                }
            }
            else
            {
                // Set or update custom name
                Config.ScopeBoxMode.CustomScopeBoxNames[scopeBox.Id] = customName.Trim();
            }

            StatusMessage = $"Custom name {(string.IsNullOrWhiteSpace(customName) ? "removed" : "set")} for {scopeBox.Name}";
        }

        /// <summary>
        /// Gets the custom name for a scope box
        /// </summary>
        /// <param name="scopeBox">The scope box element to get the custom name for</param>
        /// <returns>Custom name if set, otherwise null</returns>
        public string GetCustomScopeBoxName(Element scopeBox)
        {
            if (scopeBox == null || Config?.ScopeBoxMode == null)
                return null;

            if (Config.ScopeBoxMode.CustomScopeBoxNames.TryGetValue(scopeBox.Id, out var customName))
            {
                return customName;
            }

            return null;
        }

        /// <summary>
        /// Loads scope boxes from the Revit document
        /// </summary>
        private void LoadScopeBoxes()
        {
            if (_uiDoc == null || _uiDoc.Document == null)
            {
                StatusMessage = "No active document";
                return;
            }

            try
            {
                var doc = _uiDoc.Document;

                // Get scope boxes (category: OST_VolumeOfInterest)
                var scopeBoxCollector = new FilteredElementCollector(doc)
                    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                ScopeBoxes.Clear();
                var scopeBoxElements = scopeBoxCollector
                    .OrderBy(sb => sb.Name)
                    .ToList();

                // Populate scope boxes and restore selections from config
                foreach (var scopeBox in scopeBoxElements)
                {
                    ScopeBoxes.Add(scopeBox);
                }

                // Restore selected scope boxes from config
                // First, save the previously selected IDs before clearing
                var previouslySelectedIds = Config.ScopeBoxMode.SelectedScopeBoxIds.ToList();
                Config.ScopeBoxMode.SelectedScopeBoxes.Clear();
                Config.ScopeBoxMode.SelectedScopeBoxIds.Clear();
                foreach (var scopeBox in ScopeBoxes.Where(sb => previouslySelectedIds.Contains(sb.Id)))
                {
                    Config.ScopeBoxMode.SelectedScopeBoxes.Add(scopeBox);
                    Config.ScopeBoxMode.SelectedScopeBoxIds.Add(scopeBox.Id);
                }

                StatusMessage = $"Loaded {ScopeBoxes.Count} scope boxes";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading scope boxes: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads zones (scope boxes) from the Revit document
        /// </summary>
        private void LoadZones()
        {
            if (_uiDoc == null || _uiDoc.Document == null)
            {
                StatusMessage = "No active document";
                return;
            }

            try
            {
                var doc = _uiDoc.Document;

                // Get zones (scope boxes - category: OST_VolumeOfInterest)
                var zoneCollector = new FilteredElementCollector(doc)
                    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                Zones.Clear();
                var zoneElements = zoneCollector
                    .OrderBy(z => z.Name)
                    .ToList();

                // Populate zones and restore selections from config
                foreach (var zone in zoneElements)
                {
                    Zones.Add(zone);
                }

                // Restore selected zones from config
                // First, save the previously selected IDs before clearing
                var previouslySelectedIds = Config.ZoneMode.SelectedZoneIds.ToList();
                Config.ZoneMode.SelectedZones.Clear();
                Config.ZoneMode.SelectedZoneIds.Clear();
                foreach (var zone in Zones.Where(z => previouslySelectedIds.Contains(z.Id)))
                {
                    Config.ZoneMode.SelectedZones.Add(zone);
                    Config.ZoneMode.SelectedZoneIds.Add(zone.Id);
                }

                StatusMessage = $"Loaded {Zones.Count} zones";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading zones: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads available model categories from the Revit document
        /// </summary>
        private void LoadCategories()
        {
            if (_uiDoc == null || _uiDoc.Document == null)
            {
                StatusMessage = "No active document";
                return;
            }

            try
            {
                var doc = _uiDoc.Document;
                AvailableCategories.Clear();

                // Comprehensive list of Revit model categories (valid BuiltInCategory enum values)
                var modelCategories = new List<BuiltInCategory>
                {
                    // === Architectural ===
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Roofs,
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_CurtainWallPanels,
                    BuiltInCategory.OST_CurtainWallMullions,

                    // === Stairs, Ramps, Railings ===
                    BuiltInCategory.OST_Stairs,
                    BuiltInCategory.OST_Ramps,
                    BuiltInCategory.OST_Railings,

                    // === Structural ===
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_StructuralFoundation,
                    BuiltInCategory.OST_StructuralTruss,

                    // === Rebar & Reinforcement ===
                    BuiltInCategory.OST_Rebar,
                    BuiltInCategory.OST_FabricReinforcement,
                    BuiltInCategory.OST_FabricAreas,

                    // === MEP - Mechanical ===
                    BuiltInCategory.OST_MechanicalEquipment,
                    BuiltInCategory.OST_DuctCurves,
                    BuiltInCategory.OST_DuctFitting,
                    BuiltInCategory.OST_DuctTerminal,
                    BuiltInCategory.OST_DuctAccessory,
                    BuiltInCategory.OST_DuctInsulations,
                    BuiltInCategory.OST_DuctLinings,

                    // === MEP - Plumbing ===
                    BuiltInCategory.OST_PlumbingFixtures,
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_PipeFitting,
                    BuiltInCategory.OST_PipeAccessory,
                    BuiltInCategory.OST_PipeInsulations,
                    BuiltInCategory.OST_FlexPipeCurves,

                    // === MEP - Electrical ===
                    BuiltInCategory.OST_ElectricalEquipment,
                    BuiltInCategory.OST_ElectricalFixtures,
                    BuiltInCategory.OST_LightingFixtures,
                    BuiltInCategory.OST_LightingDevices,
                    BuiltInCategory.OST_ElectricalCircuit,

                    // === MEP - Cable Tray ===
                    BuiltInCategory.OST_CableTray,
                    BuiltInCategory.OST_CableTrayFitting,

                    // === MEP - Conduit ===
                    BuiltInCategory.OST_Conduit,
                    BuiltInCategory.OST_ConduitFitting,

                    // === MEP - Fire Protection ===
                    BuiltInCategory.OST_Sprinklers,
                    BuiltInCategory.OST_FireAlarmDevices,

                    // === MEP - Communication ===
                    BuiltInCategory.OST_CommunicationDevices,
                    BuiltInCategory.OST_DataDevices,
                    BuiltInCategory.OST_TelephoneDevices,
                    BuiltInCategory.OST_NurseCallDevices,
                    BuiltInCategory.OST_SecurityDevices,

                    // === Speciality ===
                    BuiltInCategory.OST_SpecialityEquipment,

                    // === Furniture & Casework ===
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_FurnitureSystems,
                    BuiltInCategory.OST_Casework,

                    // === Generic ===
                    BuiltInCategory.OST_GenericModel,

                    // === Site ===
                    BuiltInCategory.OST_Topography,
                    BuiltInCategory.OST_Site,
                    BuiltInCategory.OST_Parking,

                    // === Mass ===
                    BuiltInCategory.OST_Mass,

                    // === Parts ===
                    BuiltInCategory.OST_Parts,
                    BuiltInCategory.OST_Assemblies,

                    // === Divided Surfaces ===
                    BuiltInCategory.OST_DividedSurface,

                    // === Rooms ===
                    BuiltInCategory.OST_Rooms,

                    // === Areas ===
                    BuiltInCategory.OST_Areas,

                    // === Spaces ===
                    BuiltInCategory.OST_MEPSpaces,

                    // === Shaft Openings ===
                    BuiltInCategory.OST_ShaftOpening,

                    // === Grids ===
                    BuiltInCategory.OST_Grids,

                    // === Levels ===
                    BuiltInCategory.OST_Levels,

                    // === Trusses ===
                    BuiltInCategory.OST_Truss,

                    // === Detail Items ===
                    BuiltInCategory.OST_DetailComponents,

                    // === Fascia & Gutters ===
                    BuiltInCategory.OST_Fascia,

                    // === Roof Soffit ===
                    BuiltInCategory.OST_RoofSoffit
                };

                foreach (var category in modelCategories)
                {
                    try
                    {
                        // Get the category name
                        var cat = Category.GetCategory(doc, category);
                        if (cat == null) continue;

                        // Count elements in this category
                        var elementCount = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .WhereElementIsViewIndependent()
                            .OfCategory(category)
                            .GetElementCount();

                        var categoryItem = new CategoryItem(
                            category,
                            cat.Name,
                            elementCount,
                            isSelected: elementCount > 0); // Auto-select if elements exist

                        // Subscribe to property changes to update counts
                        categoryItem.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(CategoryItem.IsSelected))
                            {
                                OnPropertyChanged(nameof(SelectedCategoryCount));
                            }
                        };

                        AvailableCategories.Add(categoryItem);
                    }
                    catch
                    {
                        // Skip categories that don't exist in this document
                        continue;
                    }
                }

                // Sort by name and add to config
                var sortedCategories = AvailableCategories.OrderBy(c => c.DisplayName).ToList();
                AvailableCategories.Clear();
                foreach (var cat in sortedCategories)
                {
                    AvailableCategories.Add(cat);
                }

                Config.General.AvailableCategories = AvailableCategories.ToList();

                OnPropertyChanged(nameof(SelectedCategoryCount));
                OnPropertyChanged(nameof(TotalCategoryCount));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading categories: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads available parameters from the Revit document
        /// </summary>
        private void LoadParameters()
        {
            if (_uiDoc == null || _uiDoc.Document == null)
            {
                StatusMessage = "No active document";
                return;
            }

            try
            {
                AvailableParameters.Clear();
                var doc = _uiDoc.Document;

                // Use a HashSet to track unique parameter names
                var foundParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // First, get all shared parameters from the project
                try
                {
                    var sharedParams = new FilteredElementCollector(doc)
                        .OfClass(typeof(SharedParameterElement))
                        .Cast<SharedParameterElement>();

                    foreach (var sharedParam in sharedParams)
                    {
                        foundParameterNames.Add(sharedParam.Name);
                    }
                }
                catch
                {
                    // Ignore errors collecting shared parameters
                }

                // Second, get parameters from sample elements to find project parameters
                // Try multiple element types to get comprehensive coverage
                var elementTypes = new[]
                {
                    typeof(Wall),
                    typeof(FamilyInstance),
                    typeof(Floor),
                    typeof(Ceiling)
                };

                foreach (var elemType in elementTypes)
                {
                    try
                    {
                        var sampleElement = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .OfClass(elemType)
                            .ToElements()
                            .FirstOrDefault();

                        if (sampleElement != null)
                        {
                            var parameters = sampleElement.Parameters;
                            foreach (Parameter param in parameters)
                            {
                                try
                                {
                                    var paramName = param.Definition.Name;
                                    foundParameterNames.Add(paramName);
                                }
                                catch
                                {
                                    // Skip parameters that can't be accessed
                                    continue;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next element type if this one fails
                        continue;
                    }
                }

                // Create ParameterItems for all found parameters
                foreach (var paramName in foundParameterNames)
                {
                    var parameterItem = new ParameterItem(
                        paramName,      // Display name
                        paramName,      // Parameter name
                        $"Project parameter: {paramName}",  // Description
                        isSelected: false);

                    // Subscribe to property changes to update counts and validation
                    parameterItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ParameterItem.IsSelected))
                        {
                            OnPropertyChanged(nameof(SelectedParameterCount));
                            OnPropertyChanged(nameof(HasUnmappedParameters));
                            OnPropertyChanged(nameof(CanExecutePreview));
                            OnPropertyChanged(nameof(CanExecuteFill));
                        }
                        else if (e.PropertyName == nameof(ParameterItem.IsMapped))
                        {
                            OnPropertyChanged(nameof(HasUnmappedParameters));
                            OnPropertyChanged(nameof(CanExecutePreview));
                            OnPropertyChanged(nameof(CanExecuteFill));
                        }
                    };

                    AvailableParameters.Add(parameterItem);
                }

                // Sort by parameter name
                var sortedParams = AvailableParameters.OrderBy(p => p.DisplayName).ToList();
                AvailableParameters.Clear();
                foreach (var param in sortedParams)
                {
                    AvailableParameters.Add(param);
                }

                Config.General.AvailableParameters = AvailableParameters.ToList();
                OnPropertyChanged(nameof(SelectedParameterCount));
                OnPropertyChanged(nameof(TotalParameterCount));
                OnPropertyChanged(nameof(HasUnmappedParameters));
                StatusMessage = $"Loaded {AvailableParameters.Count} parameters from project";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading parameters: {ex.Message}";
            }
        }

        /// <summary>
        /// Selects all categories
        /// </summary>
        [RelayCommand]
        private void SelectAllCategories()
        {
            foreach (var category in AvailableCategories)
            {
                category.IsSelected = true;
            }

            OnPropertyChanged(nameof(SelectedCategoryCount));
            UpdateConfigWithSelectedCategories();
        }

        /// <summary>
        /// Deselects all categories
        /// </summary>
        [RelayCommand]
        private void DeselectAllCategories()
        {
            foreach (var category in AvailableCategories)
            {
                category.IsSelected = false;
            }

            OnPropertyChanged(nameof(SelectedCategoryCount));
            UpdateConfigWithSelectedCategories();
        }

        /// <summary>
        /// Updates the configuration with selected categories
        /// </summary>
        private void UpdateConfigWithSelectedCategories()
        {
            if (Config == null || AvailableCategories == null) return;

            Config.General.SelectedCategories = AvailableCategories
                .Where(c => c.IsSelected)
                .Select(c => c.Category)
                .ToList();

            Config.General.AvailableCategories = AvailableCategories.ToList();
        }

        /// <summary>
        /// Selects all parameters
        /// </summary>
        [RelayCommand]
        private void SelectAllParameters()
        {
            foreach (var parameter in AvailableParameters)
            {
                parameter.IsSelected = true;
            }

            OnPropertyChanged(nameof(SelectedParameterCount));
            OnPropertyChanged(nameof(HasUnmappedParameters));
            OnPropertyChanged(nameof(CanExecutePreview));
            OnPropertyChanged(nameof(CanExecuteFill));
        }

        /// <summary>
        /// Deselects all parameters
        /// </summary>
        [RelayCommand]
        private void DeselectAllParameters()
        {
            foreach (var parameter in AvailableParameters)
            {
                parameter.IsSelected = false;
            }

            OnPropertyChanged(nameof(SelectedParameterCount));
            OnPropertyChanged(nameof(HasUnmappedParameters));
            OnPropertyChanged(nameof(CanExecutePreview));
            OnPropertyChanged(nameof(CanExecuteFill));
        }

        /// <summary>
        /// Toggles the selection of a scope box
        /// </summary>
        [RelayCommand]
        private void ToggleScopeBox(Autodesk.Revit.DB.Element scopeBoxElement)
        {
            if (scopeBoxElement == null || Config?.ScopeBoxMode == null)
                return;

            var scopeBoxId = scopeBoxElement.Id;

            if (Config.ScopeBoxMode.IsScopeBoxSelected(scopeBoxId))
            {
                Config.ScopeBoxMode.RemoveScopeBox(scopeBoxId);
            }
            else
            {
                Config.ScopeBoxMode.AddScopeBox(scopeBoxId);
            }

            // Update the SelectedScopeBoxes collection
            Config.ScopeBoxMode.SelectedScopeBoxes.Clear();
            foreach (var scopeBox in ScopeBoxes.Where(sb => Config.ScopeBoxMode.SelectedScopeBoxIds.Contains(sb.Id)))
            {
                Config.ScopeBoxMode.SelectedScopeBoxes.Add(scopeBox);
            }

            // Notify that the collection has changed
            OnPropertyChanged(nameof(Config.ScopeBoxMode.SelectedScopeBoxIds));

            StatusMessage = $"Selected {Config.ScopeBoxMode.SelectedScopeBoxIds.Count} scope box(es)";
        }

        /// <summary>
        /// Toggles the selection of a zone
        /// </summary>
        [RelayCommand]
        private void ToggleZone(Autodesk.Revit.DB.Element zoneElement)
        {
            if (zoneElement == null || Config?.ZoneMode == null)
                return;

            var zoneId = zoneElement.Id;

            if (Config.ZoneMode.IsZoneSelected(zoneId))
            {
                Config.ZoneMode.RemoveZone(zoneId);
            }
            else
            {
                Config.ZoneMode.AddZone(zoneId);
            }

            // Update the SelectedZones collection
            Config.ZoneMode.SelectedZones.Clear();
            foreach (var zone in Zones.Where(z => Config.ZoneMode.SelectedZoneIds.Contains(z.Id)))
            {
                Config.ZoneMode.SelectedZones.Add(zone);
            }

            // Notify that the collection has changed
            OnPropertyChanged(nameof(Config.ZoneMode.SelectedZoneIds));

            StatusMessage = $"Selected {Config.ZoneMode.SelectedZoneIds.Count} zone(s)";
        }

        /// <summary>
        /// Opens the parameter mapping dialog
        /// </summary>
        [RelayCommand]
        private void OpenMappingDialog()
        {
            // Check if AvailableParameters is null
            if (AvailableParameters == null)
            {
                StatusMessage = "No parameters available";
                return;
            }

            // Get only selected parameters for mapping
            var selectedParams = AvailableParameters.Where(p => p.IsSelected).ToList();

            if (!selectedParams.Any())
            {
                StatusMessage = "Please select at least one parameter to map";
                return;
            }

            try
            {
                StatusMessage = "Opening mapping dialog...";

                var mappingWindow = new Views.ParameterMappingWindow(selectedParams);
           

                var result = mappingWindow.ShowDialog();

                if (result == true)
                {
                    // Update mapped parameter counts for each FillMode
                    UpdateFillModeMappedCounts();

                    // Update validation state
                    OnPropertyChanged(nameof(HasUnmappedParameters));
                    OnPropertyChanged(nameof(CanExecutePreview));
                    OnPropertyChanged(nameof(CanExecuteFill));
                    StatusMessage = $"Mapped {selectedParams.Count} parameters successfully";
                }
                else
                {
                    StatusMessage = "Mapping cancelled";
                }
            }
            catch (Exception ex)
            {
                // Log full exception details for debugging
                System.Diagnostics.Debug.WriteLine($"Error opening mapping dialog: {ex}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                StatusMessage = $"Error opening mapping dialog: {ex.Message}";
                if (ex.InnerException != null)
                {
                    StatusMessage += $" | Inner: {ex.InnerException.Message}";
                }

                // Show a message box with the full error
                System.Windows.MessageBox.Show(
                    $"Error opening mapping dialog:\n\n{ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack:\n{ex.StackTrace}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Executes the preview command
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecutePreview))]
        private void Preview()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Running preview...";

                // Show loading overlay
                LoadingOverlayService.Show("Running Preview", "Analyzing elements and parameters...");

                // Update configuration with selected categories and parameters
                UpdateConfigWithSelectedCategories();

                // Sync selected FillModeItems to Config.FillMode
                SyncSelectedFillModesToConfig();

                // Now check validity
                if (Config == null || !Config.IsValid())
                {
                    StatusMessage = "Please fix configuration errors before previewing";
                    if (Config != null && !Config.IsValid())
                    {
                        StatusMessage = $"Configuration error: {Config.GetValidationError()}";
                    }
                    IsProcessing = false;
                    LoadingOverlayService.Hide();
                    return;
                }

                if (_uiDoc?.Document == null)
                {
                    StatusMessage = "No document available";
                    IsProcessing = false;
                    LoadingOverlayService.Hide();
                    return;
                }

                // Run preview via ParameterFillService
                var summary = _parameterFillService.PreviewFill(_uiDoc.Document, Config);

                PreviewSummary = summary;

                if (summary.HasValidationWarnings)
                {
                    StatusMessage = $"Preview complete with {summary.ValidationWarnings.Count} warning(s)";
                }
                else if (summary.HasElementsToProcess)
                {
                    StatusMessage = $"Preview complete: {summary.EstimatedElementsToProcess} elements to process";
                }
                else
                {
                    StatusMessage = "Preview complete: No elements found to process";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Preview failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                LoadingOverlayService.Hide();
            }
        }

        /// <summary>
        /// Synchronizes selected FillModeItems to Config mode-specific configs before execution
        /// </summary>
        private void SyncSelectedFillModesToConfig()
        {
            if (Config == null || AvailableFillModes == null) return;

            foreach (var modeItem in AvailableFillModes)
            {
                switch (modeItem.Mode)
                {
                    case FillMode.Level:
                        Config.LevelMode.IsEnabled = modeItem.IsSelected;
                        break;
                    case FillMode.RoomName:
                        Config.RoomNameMode.IsEnabled = modeItem.IsSelected;
                        break;
                    case FillMode.RoomNumber:
                        Config.RoomNumberMode.IsEnabled = modeItem.IsSelected;
                        break;
                    case FillMode.Groups:
                        Config.GroupsMode.IsEnabled = modeItem.IsSelected;
                        break;
                    case FillMode.ScopeBox:
                        Config.ScopeBoxMode.IsEnabled = modeItem.IsSelected;
                        break;
                    case FillMode.Zone:
                        Config.ZoneMode.IsEnabled = modeItem.IsSelected;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates mapped parameter counts for each FillMode
        /// </summary>
        private void UpdateFillModeMappedCounts()
        {
            if (AvailableFillModes == null || AvailableParameters == null) return;

            foreach (var modeItem in AvailableFillModes)
            {
                var count = AvailableParameters.Count(p => p.IsSelected && p.ApplicableMode == modeItem.Mode);
                modeItem.MappedParameterCount = count;
            }
        }

        /// <summary>
        /// Executes the fill command
        /// </summary>
        [RelayCommand]
        private void Fill()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Filling parameters...";

                // Show loading overlay
                LoadingOverlayService.Show("Applying Parameters", "Starting fill operation...");

                // Update configuration with selected categories and parameters
                UpdateConfigWithSelectedCategories();

                // Sync selected FillModeItems to Config.FillMode
                SyncSelectedFillModesToConfig();

                // Check for unmapped parameters
                if (HasUnmappedParameters)
                {
                    var unmapped = AvailableParameters.Where(p => p.IsSelected && !p.IsMapped)
                        .Select(p => p.DisplayName).ToList();
                    StatusMessage = $"Please map all selected parameters first. Unmapped: {string.Join(", ", unmapped)}";
                    IsProcessing = false;
                    LoadingOverlayService.Hide();
                    return;
                }

                // Now check validity
                if (Config == null || !Config.IsValid())
                {
                    StatusMessage = "Please fix configuration errors before applying";
                    if (Config != null && !Config.IsValid())
                    {
                        StatusMessage = $"Configuration error: {Config.GetValidationError()}";
                    }
                    IsProcessing = false;
                    LoadingOverlayService.Hide();
                    return;
                }

                if (_uiDoc?.Document == null)
                {
                    StatusMessage = "No document available";
                    IsProcessing = false;
                    LoadingOverlayService.Hide();
                    return;
                }

                // Run fill operation via ParameterFillService with progress callback
                var summary = _parameterFillService.ExecuteFill(
                    _uiDoc.Document,
                    Config,
                    (current, message) =>
                    {
                        StatusMessage = message;
                        LoadingOverlayService.UpdateMessage("Applying Parameters", message);
                    });

                // Display results
                var resultMessage = summary.ToFormattedString();
               

                StatusMessage = $"Filled {summary.LevelParametersFilled} level parameters " +
                              $"in {summary.ProcessingDuration.TotalSeconds:F2} seconds";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fill failed: {ex.Message}";               
            }
            finally
            {
                IsProcessing = false;
                LoadingOverlayService.Hide();
            }
        }
    }
}
