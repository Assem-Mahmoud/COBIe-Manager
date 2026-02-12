using COBIeManager.Features.CobieParameters.ViewModels;

namespace COBIeManager.Features.CobieParameters.Views;

/// <summary>
/// Interaction logic for ParameterCreationResultWindow.xaml
/// </summary>
public partial class ParameterCreationResultWindow : System.Windows.Window
{
    public ParameterCreationResultWindow()
    {
        InitializeComponent();
        ViewModel = new ParameterCreationResultViewModel();
        DataContext = ViewModel;
    }

    public ParameterCreationResultViewModel ViewModel { get; }

    private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }
}
