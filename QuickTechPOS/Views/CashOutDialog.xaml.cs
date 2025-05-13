using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.ViewModels;
using System.Windows;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for CashOutDialog.xaml
    /// </summary>
    public partial class CashOutDialog : Window
    {
        private readonly CashOutViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the cash out dialog
        /// </summary>
        /// <param name="drawer">The drawer to take cash from</param>
        public CashOutDialog(Drawer drawer)
        {
            InitializeComponent();

            // Apply current flow direction
            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            _viewModel = new CashOutViewModel(drawer);
            DataContext = _viewModel;

            // Close the dialog when the view model sets the DialogResult
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(CashOutViewModel.DialogResult) &&
                    _viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult;

                    // Set the result before closing to ensure the updated drawer is available
                    if (_viewModel.DialogResult == true)
                    {
                        // Log the updated drawer values without assigning to UpdatedDrawer
                        System.Console.WriteLine($"CashOutDialog: Updated drawer values before closing:");
                        System.Console.WriteLine($"  - DrawerID: {_viewModel.Drawer.DrawerId}");
                        System.Console.WriteLine($"  - CashOut: ${_viewModel.Drawer.CashOut:F2}");
                        System.Console.WriteLine($"  - CurrentBalance: ${_viewModel.Drawer.CurrentBalance:F2}");

                        // Ensure all UI updates are processed
                        System.Windows.Application.Current.Dispatcher.Invoke(() => { });

                        this.Close();
                    }
                }
            };
        }

        /// <summary>
        /// Gets the updated drawer after cash out
        /// </summary>
        public Drawer UpdatedDrawer => _viewModel.Drawer;
    }
}