using System.Windows;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Features.ParameterFiller.ViewModels;

namespace COBIeManager.Features.ParameterFiller.Views
{
    /// <summary>
    /// Interaction logic for ParameterMappingWindow.xaml
    /// </summary>
    public partial class ParameterMappingWindow : Window
    {
        public ParameterMappingWindow(System.Collections.Generic.IEnumerable<ParameterItem> selectedParameters)
        {
            InitializeComponent();
            ViewModel = new ParameterMappingViewModel(selectedParameters);
            DataContext = ViewModel;
        }

        public ParameterMappingViewModel ViewModel { get; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsValid)
            {
                // Parameters are already marked as mapped when the dialog opened
                // Just close with success to save the mode selections
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
