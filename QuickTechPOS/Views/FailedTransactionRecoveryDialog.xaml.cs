using QuickTechPOS.ViewModels;
using System.Windows;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for FailedTransactionRecoveryDialog.xaml
    /// </summary>
    public partial class FailedTransactionRecoveryDialog : Window
    {
        private readonly FailedTransactionRecoveryViewModel _viewModel;

        public FailedTransactionRecoveryDialog()
        {
            InitializeComponent();
            _viewModel = new FailedTransactionRecoveryViewModel();
            DataContext = _viewModel;

            // Subscribe to view model events
            _viewModel.RequestClose += (s, e) => DialogResult = e.DialogResult;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}