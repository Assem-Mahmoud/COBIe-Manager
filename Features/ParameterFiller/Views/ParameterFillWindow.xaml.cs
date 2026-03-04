using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.ViewModels;

namespace COBIeManager.Features.ParameterFiller.Views
{
    /// <summary>
    /// Code-behind for the Parameter Fill window
    /// </summary>
    public partial class ParameterFillWindow : Window
    {
        /// <summary>
        /// The ViewModel for this window - initialized AFTER InitializeComponent()
        /// </summary>
        public ParameterFillViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ParameterFillWindow
        /// </summary>
        /// <param name="uiDoc">The Revit UI document</param>
        public ParameterFillWindow(Autodesk.Revit.UI.UIDocument uiDoc)
        {
            // IMPORTANT: InitializeComponent() must be called first
            InitializeComponent();

            // CRITICAL: Initialize ViewModel AFTER InitializeComponent()
            // This ensures ServiceLocator is ready and all XAML bindings are set up
            ViewModel = new ParameterFillViewModel(uiDoc);

            // Set the DataContext to the ViewModel
            // This enables all XAML bindings to work
            DataContext = ViewModel;
        }

        /// <summary>
        /// Event handler for scope box checkbox checked event
        /// </summary>
        private void ScopeBoxCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Element scopeBoxElement)
            {
                ViewModel.Config.ScopeBoxMode.AddScopeBox(scopeBoxElement.Id);
            }
        }

        /// <summary>
        /// Event handler for scope box checkbox unchecked event
        /// </summary>
        private void ScopeBoxCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Element scopeBoxElement)
            {
                ViewModel.Config.ScopeBoxMode.RemoveScopeBox(scopeBoxElement.Id);
            }
        }

        /// <summary>
        /// Event handler for level checkbox checked event
        /// </summary>
        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Level level)
            {
                ViewModel.Config.LevelMode.SelectedLevelIds.Add(level.Id);
                ViewModel.Config.LevelMode.SelectedLevels.Add(level);
            }
        }

        /// <summary>
        /// Event handler for level checkbox unchecked event
        /// </summary>
        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Level level)
            {
                ViewModel.Config.LevelMode.SelectedLevelIds.Remove(level.Id);
                ViewModel.Config.LevelMode.SelectedLevels.Remove(level);
            }
        }

        /// <summary>
        /// Event handler for zone checkbox checked event
        /// </summary>
        private void ZoneCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Element zoneElement)
            {
                ViewModel.Config.ZoneMode.AddZone(zoneElement.Id);
            }
        }

        /// <summary>
        /// Event handler for zone checkbox unchecked event
        /// </summary>
        private void ZoneCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is Element zoneElement)
            {
                ViewModel.Config.ZoneMode.RemoveZone(zoneElement.Id);
            }
        }

        /// <summary>
        /// Event handler for custom zone name TextBox LostFocus event
        /// </summary>
        private void CustomZoneNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is Element zone)
            {
                var customName = textBox.Text.Trim();
                ViewModel.SetCustomZoneName(zone, customName);
            }
        }

        /// <summary>
        /// Event handler for custom scope box name TextBox LostFocus event
        /// </summary>
        private void CustomScopeBoxNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is Element scopeBox)
            {
                var customName = textBox.Text.Trim();
                ViewModel.SetCustomScopeBoxName(scopeBox, customName);
            }
        }

        /// <summary>
        /// Event handler for custom level name TextBox LostFocus event
        /// </summary>
        private void CustomLevelNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is Level level)
            {
                var customName = textBox.Text.Trim();
                ViewModel.SetCustomLevelName(level, customName);
            }
        }
    }
}
