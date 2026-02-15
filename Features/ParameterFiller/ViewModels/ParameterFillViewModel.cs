using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        // UI Collections
        public ObservableCollection<Level> Levels { get; }

        // Computed properties
        public bool HasPreview => PreviewSummary != null;
        public bool CanExecutePreview => Config != null && Config.IsValid() && !IsProcessing;
        public bool CanExecuteFill => Config != null && Config.IsValid() && !IsProcessing;
        public bool HasValidationWarning => PreviewSummary != null && PreviewSummary.HasValidationWarnings;
        public string PreviewStatusMessage => PreviewSummary?.GetStatusMessage() ?? "Run preview to see estimated counts";

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

            // Load levels from document
            LoadLevels();
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
        /// Executes the preview command
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecutePreview))]
        private void Preview()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Running preview...";

                // Automatically include all relevant categories FIRST
                UpdateConfigWithAllCategories();

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
        /// Executes the fill command
        /// </summary>
        [RelayCommand]
        private void Fill()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Filling parameters...";

                // Automatically include all relevant categories FIRST
                UpdateConfigWithAllCategories();

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

        /// <summary>
        /// Updates the configuration with all valid model categories
        /// </summary>
        private void UpdateConfigWithAllCategories()
        {
            if (Config == null) return;
            if (_uiDoc == null || _uiDoc.Document == null) return;

            try
            {
                var allCategories = new System.Collections.Generic.List<BuiltInCategory>
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
                    BuiltInCategory.OST_LightingDevices
                };

                Config.SelectedCategories = allCategories;
            }
            catch (Exception ex)
            {
                // Just log to status if finding categories fails, but don't crash
                 StatusMessage = $"Warning: Failed to auto-detect categories: {ex.Message}";
            }
        }
    }
}
