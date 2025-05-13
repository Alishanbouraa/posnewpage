using QuickTechPOS.Helpers;
using QuickTechPOS.ViewModels;
using System.Windows;

namespace QuickTechPOS.Views
{
    public partial class OpenDrawerDialog : Window
    {
        private readonly OpenDrawerViewModel _viewModel;

        public OpenDrawerDialog(OpenDrawerViewModel viewModel)
        {
            InitializeComponent();

            // Apply current flow direction
            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            _viewModel = viewModel;
            DataContext = _viewModel;

            // Close the dialog when the view model sets the DialogResult
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(OpenDrawerViewModel.DialogResult) &&
                    _viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = _viewModel.DialogResult;

                    // If dialog was successful, close it immediately
                    if (_viewModel.DialogResult == true)
                    {
                        this.Close();
                    }
                }
            };

            // Focus on the opening balance field when the dialog loads
            this.Loaded += (s, e) =>
            {
                OpeningBalanceTextBox.Focus();
                OpeningBalanceTextBox.SelectAll();
            };
        }
    }
}