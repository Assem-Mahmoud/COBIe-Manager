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
        public bool CanExecuteFill => Config != null && HasSelectedFillModes && !IsProcessing;
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
            AvailableCategories = new ObservableCollection<CategoryItem>();
            AvailableParameters = new ObservableCollection<ParameterItem>();
            AvailableFillModes = new ObservableCollection<FillModeItem>
            {
                new FillModeItem("Level", "Fill level-based parameters", FillMode.Level, "Layers", isSelected: false),
                new FillModeItem("Room Name", "Fill with room names", FillMode.RoomName, "Home", isSelected: false),
                new FillModeItem("Room Number", "Fill with room numbers", FillMode.RoomNumber, "Numeric", isSelected: false),
                new FillModeItem("Groups", "Fill with Model Group box IDs", FillMode.Groups, "Group", isSelected: false)
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

                // Set default levels if available
                if (Levels.Count >= 2)
                {
                    Config.BaseLevel = Levels[0];
                    Config.TopLevel = Levels[1];
                }

                StatusMessage = $"Loaded {Levels.Count} levels";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading levels: {ex.Message}";
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

                // Define common model categories to include
                var modelCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_MechanicalEquipment,
                    BuiltInCategory.OST_GenericModel,
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Roofs,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_PlumbingFixtures,
                    BuiltInCategory.OST_ElectricalEquipment,
                    BuiltInCategory.OST_LightingFixtures,
                    BuiltInCategory.OST_SpecialityEquipment,
                    BuiltInCategory.OST_Casework,
                    BuiltInCategory.OST_DuctCurves,
                    BuiltInCategory.OST_DuctFitting,
                    BuiltInCategory.OST_DuctTerminal,
                    BuiltInCategory.OST_DuctAccessory,
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_PipeFitting,
                    BuiltInCategory.OST_PipeAccessory,
                    BuiltInCategory.OST_CableTray,
                    BuiltInCategory.OST_CableTrayFitting,
                    BuiltInCategory.OST_Conduit,
                    BuiltInCategory.OST_ConduitFitting,
                    BuiltInCategory.OST_Sprinklers,
                    BuiltInCategory.OST_FireAlarmDevices,
                    BuiltInCategory.OST_CommunicationDevices,
                    BuiltInCategory.OST_DataDevices,
                    BuiltInCategory.OST_SecurityDevices,
                    BuiltInCategory.OST_NurseCallDevices,
                    BuiltInCategory.OST_LightingDevices,
                    BuiltInCategory.OST_Stairs,
                    BuiltInCategory.OST_Ramps,
                    BuiltInCategory.OST_Railings,
                    BuiltInCategory.OST_CurtainWallPanels,
                    BuiltInCategory.OST_MechanicalEquipment,
                    BuiltInCategory.OST_ElectricalFixtures,
                    BuiltInCategory.OST_CommunicationDevices,
                    BuiltInCategory.OST_DataDevices,
                    BuiltInCategory.OST_FireProtection,
                    BuiltInCategory.OST_SecurityDevices,
                    BuiltInCategory.OST_NurseCallDevices,
                    BuiltInCategory.OST_TelephoneDevices,
                    BuiltInCategory.OST_SpecialityEquipment
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

                Config.AvailableCategories = AvailableCategories.ToList();

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

                Config.AvailableParameters = AvailableParameters.ToList();
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

            Config.SelectedCategories = AvailableCategories
                .Where(c => c.IsSelected)
                .Select(c => c.Category)
                .ToList();

            Config.AvailableCategories = AvailableCategories.ToList();
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
                    return;
                }

                if (_uiDoc?.Document == null)
                {
                    StatusMessage = "No document available";
                    IsProcessing = false;
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
            }
        }

        /// <summary>
        /// Synchronizes selected FillModeItems to Config.FillMode before execution
        /// </summary>
        private void SyncSelectedFillModesToConfig()
        {
            if (Config == null || AvailableFillModes == null) return;

            FillMode selectedModes = FillMode.None;
            foreach (var modeItem in AvailableFillModes.Where(m => m.IsSelected))
            {
                selectedModes |= modeItem.Mode;
            }

            Config.FillMode = selectedModes;
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
                    return;
                }

                if (_uiDoc?.Document == null)
                {
                    StatusMessage = "No document available";
                    IsProcessing = false;
                    return;
                }

                // Run fill operation via ParameterFillService with progress callback
                var summary = _parameterFillService.ExecuteFill(
                    _uiDoc.Document,
                    Config,
                    (current, message) =>
                    {
                        StatusMessage = message;
                    });

                // Display results
                var resultMessage = summary.ToFormattedString();
                System.Windows.MessageBox.Show(
                    resultMessage,
                    "Parameter Fill Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                StatusMessage = $"Filled {summary.LevelParametersFilled} level parameters " +
                              $"in {summary.ProcessingDuration.TotalSeconds:F2} seconds";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fill failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to fill parameters:\n\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
