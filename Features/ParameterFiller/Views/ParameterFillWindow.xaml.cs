using System.Windows;
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
    }
}
