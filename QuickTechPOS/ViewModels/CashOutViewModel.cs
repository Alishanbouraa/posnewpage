using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickTechPOS.ViewModels
{
    /// <summary>
    /// View model for the cash out dialog
    /// </summary>
    public class CashOutViewModel : BaseViewModel
    {
        private readonly DrawerService _drawerService;
        private readonly Drawer _drawer;

        private decimal _cashOutAmount;
        private string _notes;
        private string _errorMessage;
        private bool _isProcessing;

        /// <summary>
        /// Gets or sets the cash out amount
        /// </summary>
        public decimal CashOutAmount
        {
            get => _cashOutAmount;
            set => SetProperty(ref _cashOutAmount, value);
        }

        /// <summary>
        /// Gets or sets the notes for this cash out operation
        /// </summary>
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets or sets whether the view is in a processing state
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        /// <summary>
        /// Gets the current drawer
        /// </summary>
        public Drawer Drawer => _drawer;

        /// <summary>
        /// Command to execute the cash out operation
        /// </summary>
        public ICommand ExecuteCashOutCommand { get; }

        /// <summary>
        /// Command to cancel the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the cash out view model
        /// </summary>
        /// <param name="drawer">The drawer to take cash from</param>
        public CashOutViewModel(Drawer drawer)
        {
            _drawerService = new DrawerService();
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));

            CashOutAmount = 0;
            Notes = string.Empty;
            ErrorMessage = string.Empty;
            IsProcessing = false;

            ExecuteCashOutCommand = new RelayCommand(async param => await ExecuteCashOutAsync(), CanExecuteCashOut);
            CancelCommand = new RelayCommand(param => Cancel());
        }

        /// <summary>
        /// Determines if the cash out operation can be executed
        /// </summary>
        private bool CanExecuteCashOut(object parameter)
        {
            return !IsProcessing
                && CashOutAmount > 0
                && CashOutAmount <= _drawer.CurrentBalance
                && !string.IsNullOrWhiteSpace(Notes);
        }

        /// <summary>
        /// Executes the cash out operation
        /// </summary>
        private async Task ExecuteCashOutAsync()
        {
            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;

                if (CashOutAmount <= 0)
                {
                    ErrorMessage = "Cash out amount must be greater than zero.";
                    return;
                }

                if (CashOutAmount > _drawer.CurrentBalance)
                {
                    ErrorMessage = "Cash out amount cannot exceed the current drawer balance.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Notes))
                {
                    ErrorMessage = "Please provide a reason for this cash out operation.";
                    return;
                }

                // Log before cash out attempt
                Console.WriteLine($"[CashOutViewModel] Executing cash out: Amount=${CashOutAmount:F2}, DrawerID={_drawer.DrawerId}");
                Console.WriteLine($"Drawer before cash out: Balance=${_drawer.CurrentBalance:F2}, CashOut=${_drawer.CashOut:F2}");

                // Use the drawer service to perform the cash out operation
                var updatedDrawer = await _drawerService.PerformCashOutAsync(_drawer.DrawerId, CashOutAmount, Notes);

                if (updatedDrawer != null)
                {
                    // Copy updated values to our drawer object
                    _drawer.CashOut = updatedDrawer.CashOut;
                    _drawer.CurrentBalance = updatedDrawer.CurrentBalance;
                    _drawer.NetCashFlow = updatedDrawer.NetCashFlow;
                    _drawer.Notes = updatedDrawer.Notes;
                    _drawer.LastUpdated = updatedDrawer.LastUpdated;

                    Console.WriteLine($"[CashOutViewModel] Cash out operation completed successfully.");
                    Console.WriteLine($"Drawer after: Balance=${_drawer.CurrentBalance:F2}, CashOut=${_drawer.CashOut:F2}");

                    DialogResult = true;
                    OnPropertyChanged(nameof(DialogResult));
                }
                else
                {
                    Console.WriteLine("[CashOutViewModel] Cash out operation failed: drawer service returned null");
                    ErrorMessage = "Cash out operation failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashOutViewModel] Cash out operation failed with exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                ErrorMessage = $"Error performing cash out: {ex.Message}";
                DialogResult = false;
                OnPropertyChanged(nameof(DialogResult));
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Cancels the operation
        /// </summary>
        private void Cancel()
        {
            DialogResult = false;
            OnPropertyChanged(nameof(DialogResult));
        }

        /// <summary>
        /// Gets or sets the dialog result
        /// </summary>
        public bool? DialogResult { get; set; }
    }
}