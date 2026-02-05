using Autodesk.Revit.UI;
using COBIeManager.Features.CobieParameters.ViewModels;

namespace COBIeManager.Features.CobieParameters.Views;

/// <summary>
/// Interaction logic for CobieParametersWindow.xaml
/// </summary>
public partial class CobieParametersWindow
{
    /// <summary>
    /// Gets the ViewModel - MUST be initialized in constructor AFTER InitializeComponent()
    /// </summary>
    public CobieParametersViewModel ViewModel { get; }

    public CobieParametersWindow()
    {
        InitializeComponent();
        // IMPORTANT: Initialize ViewModel AFTER InitializeComponent() to ensure ServiceLocator is ready
        // Pass null initially - it will be set via SetUiDocument before any operations
        ViewModel = new CobieParametersViewModel(null);
        DataContext = ViewModel;
    }

    /// <summary>
    /// Set the UI document for this window
    /// </summary>
    public void SetUiDocument(UIDocument uiDoc)
    {
        // Update the ViewModel with the actual UI document
        ViewModel.SetUiDocument(uiDoc);
    }
}
