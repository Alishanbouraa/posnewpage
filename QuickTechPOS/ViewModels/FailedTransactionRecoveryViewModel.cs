using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using QuickTechPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace QuickTechPOS.ViewModels
{
    /// <summary>
    /// View model for failed transaction recovery dialog
    /// </summary>
    public class FailedTransactionRecoveryViewModel : BaseViewModel
    {
        private readonly FailedTransactionService _failedTransactionService;
        private readonly ProductService _productService;
        private readonly DrawerService _drawerService;

        private ObservableCollection<FailedTransaction> _failedTransactions;
        private FailedTransaction _selectedFailedTransaction;
        private ObservableCollection<CartItem> _cartItems;
        private string _statusMessage;
        private string _resolutionSuggestion;
        private bool _isLoading;
        private bool _isRetrying;

        /// <summary>
        /// Gets or sets the collection of failed transactions
        /// </summary>
        public ObservableCollection<FailedTransaction> FailedTransactions
        {
            get => _failedTransactions;
            set => SetProperty(ref _failedTransactions, value);
        }

        /// <summary>
        /// Gets or sets the selected failed transaction
        /// </summary>
        public FailedTransaction SelectedFailedTransaction
        {
            get => _selectedFailedTransaction;
            set
            {
                if (SetProperty(ref _selectedFailedTransaction, value))
                {
                    LoadCartItems();
                    GenerateResolutionSuggestion();
                    OnPropertyChanged(nameof(IsTransactionSelected));
                    OnPropertyChanged(nameof(CanRetrySelected));
                }
            }
        }

        /// <summary>
        /// Gets or sets the cart items of the selected transaction
        /// </summary>
        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets the resolution suggestion
        /// </summary>
        public string ResolutionSuggestion
        {
            get => _resolutionSuggestion;
            set => SetProperty(ref _resolutionSuggestion, value);
        }

        /// <summary>
        /// Gets or sets whether the view is in a loading state
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Gets whether a transaction is selected
        /// </summary>
        public bool IsTransactionSelected => SelectedFailedTransaction != null;

        /// <summary>
        /// Gets whether the selected transaction can be retried
        /// </summary>
        public bool CanRetrySelected => SelectedFailedTransaction?.CanRetry ?? false;

        /// <summary>
        /// Command to refresh the transaction list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to retry a failed transaction
        /// </summary>
        public ICommand RetryCommand { get; }

        /// <summary>
        /// Command to cancel a failed transaction
        /// </summary>
        public ICommand CancelTransactionCommand { get; }

        /// <summary>
        /// Event that fires when the view model requests the window to close
        /// </summary>
        public event EventHandler<DialogResultEventArgs> RequestClose;

        /// <summary>
        /// Initializes a new instance of the failed transaction recovery view model
        /// </summary>
        public FailedTransactionRecoveryViewModel()
        {
            _failedTransactionService = new FailedTransactionService();
            _productService = new ProductService();
            _drawerService = new DrawerService();

            FailedTransactions = new ObservableCollection<FailedTransaction>();
            CartItems = new ObservableCollection<CartItem>();

            RefreshCommand = new RelayCommand(async param => await LoadFailedTransactionsAsync());
            RetryCommand = new RelayCommand(async param => await RetryTransactionAsync(), param => CanRetrySelected && !_isRetrying);
            CancelTransactionCommand = new RelayCommand(async param => await CancelTransactionAsync(), param => IsTransactionSelected && !_isRetrying);

            StatusMessage = "Loading failed transactions...";
            LoadFailedTransactionsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads failed transactions from the database
        /// </summary>
        private async Task LoadFailedTransactionsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading failed transactions...";

                // First try to import any local backups
                int importedCount = await _failedTransactionService.ImportLocalBackupsAsync();
                if (importedCount > 0)
                {
                    StatusMessage = $"Imported {importedCount} transaction(s) from local backups.";
                }

                // Then load all failed transactions
                var transactions = await _failedTransactionService.GetFailedTransactionsAsync();

                FailedTransactions.Clear();
                foreach (var transaction in transactions)
                {
                    FailedTransactions.Add(transaction);
                }

                if (FailedTransactions.Count > 0)
                {
                    StatusMessage = $"Loaded {FailedTransactions.Count} failed transaction(s).";
                }
                else
                {
                    StatusMessage = "No failed transactions found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading transactions: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads cart items from the selected transaction
        /// </summary>
        private void LoadCartItems()
        {
            CartItems.Clear();

            if (SelectedFailedTransaction == null)
                return;

            try
            {
                var items = SelectedFailedTransaction.CartItems;

                foreach (var item in items)
                {
                    // Ensure product data is loaded for display
                    if (item.Product == null || item.Product.ProductId <= 0)
                        continue;

                    CartItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading cart items: {ex.Message}";
            }
        }

        /// <summary>
        /// Generates a resolution suggestion based on the failure component
        /// </summary>
        private void GenerateResolutionSuggestion()
        {
            if (SelectedFailedTransaction == null)
            {
                ResolutionSuggestion = string.Empty;
                return;
            }

            switch (SelectedFailedTransaction.FailureComponent?.ToLower())
            {
                case "drawer":
                    ResolutionSuggestion = "This transaction failed because of a drawer-related issue. " +
                                          "Ensure an open cash drawer is available before retrying. " +
                                          "Check that the drawer has been properly opened and is accessible.";
                    break;
                case "inventory":
                    ResolutionSuggestion = "This transaction failed during inventory update. " +
                                          "Verify that all products in the cart have sufficient stock available. " +
                                          "Consider reducing quantities or removing products that are out of stock.";
                    break;
                case "database":
                    ResolutionSuggestion = "This transaction failed due to a database error. " +
                                          "Ensure the database connection is stable before retrying. " +
                                          "If the problem persists, contact your system administrator.";
                    break;
                default:
                    ResolutionSuggestion = "This transaction failed for an unknown reason. " +
                                          "Check that all systems are operational before retrying. " +
                                          "If the problem persists, contact your system administrator.";
                    break;
            }
        }

        /// <summary>
        /// Retries the selected failed transaction
        /// </summary>
        private async Task RetryTransactionAsync()
        {
            if (SelectedFailedTransaction == null || !CanRetrySelected)
                return;

            try
            {
                _isRetrying = true;
                StatusMessage = $"Retrying transaction #{SelectedFailedTransaction.FailedTransactionId}...";

                // Check if a drawer is open
                var authService = new AuthenticationService();
                if (authService.CurrentEmployee == null)
                {
                    StatusMessage = "No cashier is logged in. Please log in first.";
                    MessageBox.Show("No cashier is logged in. Please log in first.",
                        "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _isRetrying = false;
                    return;
                }

                string cashierId = authService.CurrentEmployee.EmployeeId.ToString();
                var drawer = await _drawerService.GetOpenDrawerAsync(cashierId);
                if (drawer == null)
                {
                    StatusMessage = "No open drawer found. Please open a drawer first.";
                    MessageBox.Show("This transaction requires an open drawer. Please open a drawer first.",
                        "Drawer Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _isRetrying = false;
                    return;
                }

                // Retry the transaction
                var result = await _failedTransactionService.RetryTransactionAsync(SelectedFailedTransaction.FailedTransactionId);

                if (result.Success)
                {
                    StatusMessage = $"Transaction #{result.Transaction.TransactionId} completed successfully.";
                    MessageBox.Show($"Transaction #{result.Transaction.TransactionId} completed successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload failed transactions
                    await LoadFailedTransactionsAsync();

                    // Request dialog close with success result
                    RequestClose?.Invoke(this, new DialogResultEventArgs(true));
                }
                else
                {
                    StatusMessage = $"Failed to retry transaction: {result.Message}";
                    MessageBox.Show($"Failed to retry transaction: {result.Message}",
                        "Retry Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Reload failed transactions to get updated error
                    await LoadFailedTransactionsAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error retrying transaction: {ex.Message}";
                MessageBox.Show($"Error retrying transaction: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRetrying = false;
            }
        }

        /// <summary>
        /// Cancels the selected failed transaction
        /// </summary>
        private async Task CancelTransactionAsync()
        {
            if (SelectedFailedTransaction == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to cancel this transaction? This action cannot be undone.",
                "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _isRetrying = true;
                StatusMessage = $"Cancelling transaction #{SelectedFailedTransaction.FailedTransactionId}...";

                bool success = await _failedTransactionService.CancelFailedTransactionAsync(SelectedFailedTransaction.FailedTransactionId);

                if (success)
                {
                    StatusMessage = $"Transaction #{SelectedFailedTransaction.FailedTransactionId} cancelled successfully.";

                    // Reload failed transactions
                    await LoadFailedTransactionsAsync();
                }
                else
                {
                    StatusMessage = $"Failed to cancel transaction.";
                    MessageBox.Show("Failed to cancel transaction. Please try again.",
                        "Cancel Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cancelling transaction: {ex.Message}";
                MessageBox.Show($"Error cancelling transaction: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRetrying = false;
            }
        }
    }

    /// <summary>
    /// Event arguments for dialog result events
    /// </summary>
    public class DialogResultEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the dialog result
        /// </summary>
        public bool DialogResult { get; }

        /// <summary>
        /// Initializes a new instance of the dialog result event arguments
        /// </summary>
        /// <param name="dialogResult">The dialog result</param>
        public DialogResultEventArgs(bool dialogResult)
        {
            DialogResult = dialogResult;
        }
    }
}