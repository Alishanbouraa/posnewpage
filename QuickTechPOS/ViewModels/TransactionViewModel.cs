using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using QuickTechPOS.Services;
using QuickTechPOS.Views;
using System.Windows;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace QuickTechPOS.ViewModels
{
    public class TransactionViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly TransactionService _transactionService;
        private readonly CustomerService _customerService;
        private readonly AuthenticationService _authService;
        private readonly DrawerService _drawerService;
        private readonly ReceiptPrinterService _receiptPrinterService;
        private readonly BusinessSettingsService _businessSettingsService;
        private bool _wholesaleMode;
        private string _lastSearchQuery;
        private System.Timers.Timer _searchTimer;
        private System.Timers.Timer _customerSearchTimer;
        private string _barcodeQuery;
        private string _nameQuery;
        private string _customerQuery;
        private decimal _totalAmount;
        private ObservableCollection<HeldCart> _heldCarts;
        private int _nextCartId = 1;
        private string _customerName;
        private int _customerId;
        private string _statusMessage;
        private bool _isProcessing;
        private CartItem _selectedCartItem;
        private Product _selectedProduct;
        private Customer _selectedCustomer;
        private ObservableCollection<Product> _searchedProducts;
        private ObservableCollection<Customer> _searchedCustomers;
        private ObservableCollection<CartItem> _cartItems;
        private readonly CustomerProductPriceService _customerPriceService;
        private readonly Customer _walkInCustomer;
        private string _transactionLookupId;
        private Transaction _loadedTransaction;
        private bool _isTransactionLoaded;
        private bool _isEditMode;
        private bool _canNavigateNext;
        private bool _canNavigatePrevious;
        private decimal _paidAmount;
        private bool _addToCustomerDebt;
        private decimal _amountToDebt;
        private Drawer _currentDrawer;
        private bool _useExchangeRate;
        public bool CanCheckout => CartItems.Count > 0 && IsDrawerOpen;
        private int _pendingTransactionCount;
        private decimal _exchangeRate;
        private decimal _exchangeAmount;

        public string BarcodeQuery
        {
            get => _barcodeQuery;
            set => SetProperty(ref _barcodeQuery, value);
        }

        public Drawer CurrentDrawer
        {
            get => _currentDrawer;
            private set => SetProperty(ref _currentDrawer, value);
        }

        public string NameQuery
        {
            get => _nameQuery;
            set => SetProperty(ref _nameQuery, value);
        }
        public string DrawerStatusToolTip
        {
            get => IsDrawerOpen ? "Process payment and complete the transaction" : "Drawer must be open to complete a sale";
        }
        public ObservableCollection<HeldCart> HeldCarts
        {
            get => _heldCarts;
            set => SetProperty(ref _heldCarts, value);
        }
        public string CustomerQuery
        {
            get => _customerQuery;
            set => SetProperty(ref _customerQuery, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public int CustomerId
        {
            get => _customerId;
            set => SetProperty(ref _customerId, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public CartItem SelectedCartItem
        {
            get => _selectedCartItem;
            set
            {
                if (SetProperty(ref _selectedCartItem, value))
                {
                    OnPropertyChanged(nameof(CanRemoveItem));
                }
            }
        }
        public int PendingTransactionCount
        {
            get => _pendingTransactionCount;
            set => SetProperty(ref _pendingTransactionCount, value);
        }
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value) && value != null)
                {
                    CustomerId = value.CustomerId;
                    CustomerName = value.Name;
                }
            }
        }

        public ObservableCollection<Product> SearchedProducts
        {
            get => _searchedProducts;
            set => SetProperty(ref _searchedProducts, value);
        }


        public ObservableCollection<Customer> SearchedCustomers
        {
            get => _searchedCustomers;
            set => SetProperty(ref _searchedCustomers, value);
        }

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        public string TransactionLookupId
        {
            get => _transactionLookupId;
            set => SetProperty(ref _transactionLookupId, value);
        }

        public Transaction LoadedTransaction
        {
            get => _loadedTransaction;
            set => SetProperty(ref _loadedTransaction, value);
        }

        public bool IsTransactionLoaded
        {
            get => _isTransactionLoaded;
            set => SetProperty(ref _isTransactionLoaded, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool CanNavigateNext
        {
            get => _canNavigateNext;
            set => SetProperty(ref _canNavigateNext, value);
        }

        public bool CanNavigatePrevious
        {
            get => _canNavigatePrevious;
            set => SetProperty(ref _canNavigatePrevious, value);
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (SetProperty(ref _paidAmount, value))
                {
                    CalculateAmountToDebt();
                }
            }
        }

        public bool AddToCustomerDebt
        {
            get => _addToCustomerDebt;
            set
            {
                if (SetProperty(ref _addToCustomerDebt, value))
                {
                    CalculateAmountToDebt();
                }
            }
        }

        public decimal AmountToDebt
        {
            get => _amountToDebt;
            private set => SetProperty(ref _amountToDebt, value);
        }

        public bool UseExchangeRate
        {
            get => _useExchangeRate;
            set
            {
                if (SetProperty(ref _useExchangeRate, value))
                {
                    CalculateExchangeAmount();
                }
            }
        }

        public decimal ExchangeRate
        {
            get => _exchangeRate;
            set
            {
                if (SetProperty(ref _exchangeRate, value))
                {
                    CalculateExchangeAmount();
                }
            }
        }

        public decimal ExchangeAmount
        {
            get => _exchangeAmount;
            private set => SetProperty(ref _exchangeAmount, value);
        }
        public bool HasPendingTransactions => PendingTransactionCount > 0;

        public bool CanRemoveItem => SelectedCartItem != null;

        public bool IsDrawerOpen
        {
            get => CurrentDrawer != null && CurrentDrawer.Status == "Open";
        }
        public ICommand SearchBarcodeCommand { get; }
        public ICommand SearchNameCommand { get; }
        public ICommand SelectCustomerCommand { get; }
        public ICommand SearchCustomersCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand AddToCartAsBoxCommand { get; }
        public ICommand AddToCartAsWholesaleCommand { get; }
        public ICommand AddToCartAsWholesaleBoxCommand { get; }
        public ICommand HoldCartCommand { get; }
        public ICommand RestoreCartCommand { get; }
        public ICommand AddCustomerCommand { get; }
        public ICommand LookupTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand OpenRecoveryDialogCommand { get; }
        public ICommand PrintDrawerReportCommand { get; }
        public ICommand SaveTransactionCommand { get; }
        public ICommand PrintReceiptCommand { get; }
        public ICommand NextTransactionCommand { get; }
        public ICommand PreviousTransactionCommand { get; }
        public ICommand CashOutCommand { get; }
        public ICommand OpenDrawerCommand { get; }
        public ICommand CloseDrawerCommand { get; }

        public TransactionViewModel(AuthenticationService authService, Customer walkInCustomer = null)
        {
            _productService = new ProductService();
            _transactionService = new TransactionService();
            _customerService = new CustomerService();
            _customerPriceService = new CustomerProductPriceService();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _walkInCustomer = walkInCustomer;
            _drawerService = new DrawerService();
            _receiptPrinterService = new ReceiptPrinterService();
            _businessSettingsService = new BusinessSettingsService();
            _exchangeRate = 90000; // Default value until loaded from DB
            LogoutCommand = new RelayCommand(param => Logout());
            NextTransactionCommand = new RelayCommand(
            async param => await NavigateToNextTransactionAsync(),
            param => IsTransactionLoaded && CanNavigateNext);
            HeldCarts = new ObservableCollection<HeldCart>();
            HoldCartCommand = new RelayCommand(param => HoldCurrentCart(), param => CanHoldCart());
            RestoreCartCommand = new RelayCommand(param => RestoreHeldCart(), param => CanRestoreCart());
            PreviousTransactionCommand = new RelayCommand(
            async param => await NavigateToPreviousTransactionAsync(),
            param => IsTransactionLoaded && CanNavigatePrevious);
            SearchedProducts = new ObservableCollection<Product>();
            SearchedCustomers = new ObservableCollection<Customer>();
            CartItems = new ObservableCollection<CartItem>();

            CustomerName = "Walk-in Customer";
            if (_walkInCustomer != null)
            {
                CustomerId = _walkInCustomer.CustomerId;
                Console.WriteLine($"Using walk-in customer with ID: {CustomerId}");
            }
            else
            {
                CustomerId = 0;
                Console.WriteLine("No walk-in customer provided. Customer ID set to 0.");
            }

            _searchTimer = new System.Timers.Timer(300);
            _searchTimer.Elapsed += OnSearchTimerElapsed;
            _searchTimer.AutoReset = false;

            _customerSearchTimer = new System.Timers.Timer(300);
            _customerSearchTimer.Elapsed += OnCustomerSearchTimerElapsed;
            _customerSearchTimer.AutoReset = false;

            PaidAmount = 0;
            AddToCustomerDebt = false;
            AmountToDebt = 0;

            // Initialize commands
            PrintDrawerReportCommand = new RelayCommand(async param => await PrintDrawerReportAsync(), param => IsDrawerOpen);
            CashOutCommand = new RelayCommand(async param => await ShowCashOutDialogAsync(), param => IsDrawerOpen);
            SearchBarcodeCommand = new RelayCommand(async param => await SearchByBarcodeAsync());
            SearchNameCommand = new RelayCommand(async param => await SearchByNameAsync());
            SearchCustomersCommand = new RelayCommand(async param => await SearchCustomersAsync());
            AddToCartCommand = new RelayCommand(param => AddToCart(param as Product));
            RemoveFromCartCommand = new RelayCommand(param => RemoveFromCart(param as CartItem), param => param != null);
            ClearCartCommand = new RelayCommand(param => ClearCart());
            OpenRecoveryDialogCommand = new RelayCommand(param => OpenRecoveryDialog());
            CheckForFailedTransactionsAsync().ConfigureAwait(false);
            CheckoutCommand = new RelayCommand(async param => await CheckoutAsync(),
    param => CanCheckout && IsDrawerOpen);

            AddToCartAsBoxCommand = new RelayCommand(param => AddToCart(param as Product, true, false));
            AddToCartAsWholesaleCommand = new RelayCommand(param => AddToCart(param as Product, false, true));
            AddToCartAsWholesaleBoxCommand = new RelayCommand(param => AddToCart(param as Product, true, true));


            PrintDrawerReportCommand = new RelayCommand(async param => await PrintDrawerReportAsync(), param => IsDrawerOpen);
            CheckoutCommand = new RelayCommand(async param => await CheckoutAsync(), param => CanCheckout);
            AddCustomerCommand = new RelayCommand(param => OpenAddCustomerDialog());
            SelectCustomerCommand = new RelayCommand(param => OpenCustomerSelectionDialog());
            OpenDrawerCommand = new RelayCommand(async param => await OpenDrawerDialogAsync());
            CloseDrawerCommand = new RelayCommand(async param => await CloseDrawerDialogAsync(), param => IsDrawerOpen);
            PrintReceiptCommand = new RelayCommand(param => PrintReceipt(), param => IsTransactionLoaded);
            LookupTransactionCommand = new RelayCommand(async param =>
            {
                if (param != null)
                {
                    TransactionLookupId = param.ToString();
                }
                await LookupTransactionAsync();
            });

            EditTransactionCommand = new RelayCommand(param => EnterEditMode(), param => IsTransactionLoaded && !IsEditMode);
            SaveTransactionCommand = new RelayCommand(async param => await SaveTransactionChangesAsync(), param => IsTransactionLoaded && IsEditMode);
          

            NextTransactionCommand = new RelayCommand(async param => await NavigateToNextTransactionAsync(), param => CanNavigateNext);
            PreviousTransactionCommand = new RelayCommand(async param => await NavigateToPreviousTransactionAsync(), param => CanNavigatePrevious);

            LoadInitialProductsAsync();
            LoadInitialCustomersAsync();
            GetCurrentDrawerAsync().ConfigureAwait(false);
            LoadExchangeRateAsync();
        }
        private async Task CheckForFailedTransactionsAsync()
        {
            try
            {
                var failedTransactionService = new FailedTransactionService();
                var transactions = await failedTransactionService.GetFailedTransactionsAsync();

                PendingTransactionCount = transactions.Count;
                OnPropertyChanged(nameof(HasPendingTransactions));

                if (PendingTransactionCount > 0)
                {
                    StatusMessage = $"There are {PendingTransactionCount} failed transactions that need attention.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for failed transactions: {ex.Message}");
            }
        }

        private void OpenRecoveryDialog()
        {
            try
            {
                var dialog = new Views.FailedTransactionRecoveryDialog();
                dialog.Owner = Application.Current.MainWindow;
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh transaction counts after recovery
                    CheckForFailedTransactionsAsync().ConfigureAwait(false);

                    // Refresh drawer status
                    RefreshDrawerStatusAsync().ConfigureAwait(false);

                    StatusMessage = "Transaction recovery completed successfully.";
                }
                else
                {
                    // Just refresh the counts
                    CheckForFailedTransactionsAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening recovery dialog: {ex.Message}";
                Console.WriteLine($"Error opening recovery dialog: {ex.Message}");
            }
        }

        private async Task LoadExchangeRateAsync()
        {
            try
            {
                ExchangeRate = await _businessSettingsService.GetExchangeRateAsync();
                Console.WriteLine($"Loaded exchange rate: {ExchangeRate}");
                // Always enable exchange rate display by default
                UseExchangeRate = true;
                CalculateExchangeAmount();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading exchange rate: {ex.Message}");
            }
        }
        private bool CanHoldCart()
        {
            return CartItems.Count > 0 && HeldCarts.Count < 10;
        }

        private void HoldCurrentCart()
        {
            try
            {
                if (CartItems.Count == 0)
                {
                    StatusMessage = "Cannot hold an empty cart.";
                    return;
                }

                if (HeldCarts.Count >= 10)
                {
                    StatusMessage = "Maximum number of held carts (10) reached. Please restore or complete a held cart first.";
                    return;
                }

                var heldCart = new HeldCart
                {
                    Id = _nextCartId++,
                    CreatedAt = DateTime.Now,
                    CustomerId = CustomerId,
                    CustomerName = CustomerName,
                    TotalAmount = TotalAmount,
                    Items = new List<CartItem>()
                };

                foreach (var item in CartItems)
                {
                    var clonedItem = new CartItem
                    {
                        Product = item.Product,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Discount = item.Discount,
                        DiscountType = item.DiscountType,
                        IsBox = item.IsBox,
                        IsWholesale = item.IsWholesale
                    };
                    heldCart.Items.Add(clonedItem);
                }

                HeldCarts.Add(heldCart);

                CartItems.Clear();
                UpdateTotals();

                if (CustomerId > 0 && CustomerName != "Walk-in Customer")
                {
                    if (_walkInCustomer != null)
                    {
                        CustomerId = _walkInCustomer.CustomerId;
                    }
                    else
                    {
                        CustomerId = 0;
                    }

                    CustomerName = "Walk-in Customer";
                    SelectedCustomer = null;
                    OnPropertyChanged(nameof(CustomerName));
                    OnPropertyChanged(nameof(CustomerId));
                }

                StatusMessage = $"Cart #{heldCart.Id} held successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error holding cart: {ex.Message}";
                Console.WriteLine($"Error holding cart: {ex}");
            }
        }

        private bool CanRestoreCart()
        {
            return HeldCarts.Count > 0;
        }

        private void RestoreHeldCart()
        {
            try
            {
                if (HeldCarts.Count == 0)
                {
                    StatusMessage = "No held carts available to restore.";
                    return;
                }

                if (CartItems.Count > 0)
                {
                    var result = MessageBox.Show(
                        "Current cart contains items. Would you like to hold it before restoring another cart?",
                        "Hold Current Cart?",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        HoldCurrentCart();

                        if (CartItems.Count > 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        CartItems.Clear();
                        UpdateTotals();
                    }
                }

                var dialog = new RestoreCartDialog(HeldCarts.ToList());
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true && dialog.SelectedCart != null)
                {
                    var selectedCart = dialog.SelectedCart;

                    CartItems.Clear();
                    foreach (var item in selectedCart.Items)
                    {
                        CartItems.Add(item);
                    }

                    CustomerId = selectedCart.CustomerId;
                    CustomerName = selectedCart.CustomerName;

                    if (selectedCart.CustomerId > 0)
                    {
                        try
                        {
                            var customer = _customerService.GetByIdAsync(selectedCart.CustomerId).Result;
                            if (customer != null)
                            {
                                SelectedCustomer = customer;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading customer details: {ex.Message}");
                        }
                    }
                    else
                    {
                        SelectedCustomer = null;
                    }

                    OnPropertyChanged(nameof(CustomerName));
                    OnPropertyChanged(nameof(CustomerId));
                    OnPropertyChanged(nameof(SelectedCustomer));

                    HeldCarts.Remove(selectedCart);

                    UpdateTotals();

                    StatusMessage = $"Cart #{selectedCart.Id} restored successfully.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error restoring cart: {ex.Message}";
                Console.WriteLine($"Error restoring cart: {ex}");
            }
        }
        private async Task UpdateStockAfterSaleAsync(IEnumerable<CartItem> items)
        {
            foreach (var item in items)
            {
                if (item.Product == null)
                    continue;

                try
                {
                    Console.WriteLine($"Updating stock for {item.Product.Name} (ID: {item.Product.ProductId})");
                    Console.WriteLine($"Before update - Current stock: {item.Product.CurrentStock}, Boxes: {item.Product.NumberOfBoxes}");
                    Console.WriteLine($"Sale details - Is Box: {item.IsBox}, Quantity: {item.Quantity}");

                    if (item.IsBox)
                    {
                        // For box sales, update box inventory AND individual items
                        bool boxStockUpdated = await _productService.UpdateBoxStockAsync(item.Product.ProductId, item.Quantity);

                        if (!boxStockUpdated)
                        {
                            Console.WriteLine($"Warning: Failed to update box stock for product {item.Product.ProductId}");
                        }
                        else
                        {
                            Console.WriteLine($"Successfully updated box stock for product {item.Product.ProductId}");
                        }
                    }
                    else
                    {
                        // For individual item sales, deduct from CurrentStock
                        bool stockUpdated = await _productService.UpdateStockAsync(item.Product.ProductId, item.Quantity);

                        if (!stockUpdated)
                        {
                            Console.WriteLine($"Warning: Failed to update stock for product {item.Product.ProductId}");
                        }
                        else
                        {
                            Console.WriteLine($"Successfully updated individual stock for product {item.Product.ProductId}");
                        }
                    }

                    // Refresh the product to show updated quantities
                    var updatedProduct = await _productService.GetProductByIdAsync(item.Product.ProductId);
                    if (updatedProduct != null)
                    {
                        Console.WriteLine($"After update - Current stock: {updatedProduct.CurrentStock}, Boxes: {updatedProduct.NumberOfBoxes}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating stock for product {item.Product.Name}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
        }
        private void CalculateExchangeAmount()
        {
            // Always calculate exchange amount if we have a valid exchange rate
            if (ExchangeRate > 0)
            {
                ExchangeAmount = TotalAmount * ExchangeRate;
                Console.WriteLine($"Calculated exchange amount: {TotalAmount} USD × {ExchangeRate} = {ExchangeAmount} LBP");
            }
            else
            {
                ExchangeAmount = 0;
            }
        }

        private void CalculateAmountToDebt()
        {
            if (AddToCustomerDebt && PaidAmount < TotalAmount)
            {
                AmountToDebt = TotalAmount - PaidAmount;
            }
            else
            {
                AmountToDebt = 0;
            }
        }
        public bool WholesaleMode
        {
            get => _wholesaleMode;
            set
            {
                if (SetProperty(ref _wholesaleMode, value))
                {
                    // When wholesale mode changes, update all existing cart items
                    UpdateCartWholesaleMode();

                    // Update the status message
                    StatusMessage = value ?
                        "Switched to Wholesale mode. All items will use wholesale pricing." :
                        "Switched to Retail mode. All items will use regular pricing.";
                }
            }
        }

        private void UpdateCartWholesaleMode()
        {
            foreach (var item in CartItems)
            {
                // Only update if the current wholesale setting doesn't match the global setting
                if (item.IsWholesale != WholesaleMode)
                {
                    item.IsWholesale = WholesaleMode;
                    // Note: The IsWholesale setter in CartItem already updates the price
                }
            }

            // Recalculate totals after updating all items
            UpdateTotals();
        }
        private async Task GetCurrentDrawerAsync()
        {
            try
            {
                if (_authService.CurrentEmployee == null)
                {
                    CurrentDrawer = null;
                    OnPropertyChanged(nameof(IsDrawerOpen));
                    return;
                }

                string cashierId = _authService.CurrentEmployee.EmployeeId.ToString();
                var drawer = await _drawerService.GetOpenDrawerAsync(cashierId);

                if (drawer != null && drawer.Notes == null)
                {
                    drawer.Notes = string.Empty;
                }

                CurrentDrawer = drawer;

                // Important: Notify UI that drawer state has changed
                OnPropertyChanged(nameof(IsDrawerOpen));

                // Force UI update through dispatcher for immediate refresh
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    CommandManager.InvalidateRequerySuggested();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current drawer: {ex.Message}");
                StatusMessage = $"Error retrieving drawer information: {ex.Message}";
            }
        }
        /// <summary>
        /// Converts a string transaction type to the corresponding enum value
        /// </summary>
        private TransactionType ConvertToTransactionType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return TransactionType.Sale;

            if (Enum.TryParse<TransactionType>(type, true, out var result))
                return result;

            switch (type.ToLower())
            {
                case "sale":
                    return TransactionType.Sale;
                case "return":
                    return TransactionType.Return;
                case "exchange":
                    return TransactionType.Exchange;
                case "void":
                    return TransactionType.Void;
                case "refund":
                    return TransactionType.Refund;
                default:
                    return TransactionType.Sale;
            }
        }

        /// <summary>
        /// Converts a string transaction status to the corresponding enum value
        /// </summary>
        private TransactionStatus ConvertToTransactionStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return TransactionStatus.Completed;

            if (Enum.TryParse<TransactionStatus>(status, true, out var result))
                return result;

            switch (status.ToLower())
            {
                case "pending":
                    return TransactionStatus.Pending;
                case "completed":
                    return TransactionStatus.Completed;
                case "cancelled":
                    return TransactionStatus.Cancelled;
                case "voided":
                    return TransactionStatus.Voided;
                case "refunded":
                    return TransactionStatus.Refunded;
                default:
                    return TransactionStatus.Completed;
            }
        }
        private async Task ShowCashOutDialogAsync()
        {
            try
            {
                await GetCurrentDrawerAsync();

                if (!IsDrawerOpen)
                {
                    StatusMessage = "No open drawer found.";
                    return;
                }

                var dialog = new CashOutDialog(CurrentDrawer);
                dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dialog.Owner = System.Windows.Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    // Explicit refresh after successful cash out
                    await Task.Delay(100); // Small delay to ensure DB transaction completes
                    await RefreshDrawerStatusAsync();

                    var updatedDrawer = dialog.UpdatedDrawer;
                    StatusMessage = $"Cash out operation completed successfully.";

                    // Force UI update
                    OnPropertyChanged(nameof(CurrentDrawer));
                    OnPropertyChanged(nameof(IsDrawerOpen));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during cash out: {ex.Message}";
                Console.WriteLine($"Cash out dialog error: {ex}");
            }
        }


        public async Task RefreshDrawerStatusAsync()
        {
            try
            {
                // Force refresh of drawer status from database with multiple retries
                if (_authService.CurrentEmployee == null)
                {
                    CurrentDrawer = null;
                    Console.WriteLine("RefreshDrawerStatusAsync: No employee logged in");
                    OnPropertyChanged(nameof(IsDrawerOpen));
                    return;
                }

                string cashierId = _authService.CurrentEmployee.EmployeeId.ToString();
                Console.WriteLine($"Refreshing drawer status for cashier ID: {cashierId}...");

                // Create a new DbContext to avoid any caching issues
                var freshDrawerService = new DrawerService();

                // Clear drawer first to make sure we don't show stale data
                CurrentDrawer = null;
                OnPropertyChanged(nameof(CurrentDrawer));
                OnPropertyChanged(nameof(IsDrawerOpen));

                // Get current open drawer from the database with a direct query
                bool success = false;
                Exception lastException = null;

                // Try up to 3 times with increasing delays
                for (int attempt = 1; attempt <= 3 && !success; attempt++)
                {
                    try
                    {
                        if (attempt > 1)
                        {
                            // Add a delay between retries that increases with each attempt
                            await Task.Delay(attempt * 200);
                            Console.WriteLine($"Retry attempt {attempt} for drawer status...");
                        }

                        var drawer = await freshDrawerService.GetOpenDrawerAsync(cashierId);

                        Console.WriteLine($"Drawer status refreshed. Found drawer: {drawer != null}, " +
                            $"Status: {drawer?.Status ?? "None"}, DrawerId: {drawer?.DrawerId.ToString() ?? "None"}");

                        // Update current drawer and notify UI
                        CurrentDrawer = drawer;
                        success = true;

                        // Explicitly force null check on drawer properties to avoid binding errors
                        if (CurrentDrawer != null && CurrentDrawer.Notes == null)
                        {
                            CurrentDrawer.Notes = string.Empty;
                        }
                    }
                    catch (Exception queryEx)
                    {
                        lastException = queryEx;
                        Console.WriteLine($"Error querying drawer (attempt {attempt}): {queryEx.Message}");
                        if (queryEx.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {queryEx.InnerException.Message}");
                        }
                    }
                }

                if (!success && lastException != null)
                {
                    Console.WriteLine($"All attempts to refresh drawer status failed: {lastException.Message}");
                    throw lastException;
                }

                // Ensure all drawer-related properties are updated
                OnPropertyChanged(nameof(CurrentDrawer));
                OnPropertyChanged(nameof(IsDrawerOpen));
                OnPropertyChanged(nameof(CanCheckout));
                OnPropertyChanged(nameof(DrawerStatusToolTip));

                // Force UI refresh through dispatcher with a slight delay to ensure DB transaction completes
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    CommandManager.InvalidateRequerySuggested();
                    await Task.Delay(50);
                    CommandManager.InvalidateRequerySuggested();
                });

                Console.WriteLine($"Drawer refresh complete. IsDrawerOpen = {IsDrawerOpen}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshDrawerStatusAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                StatusMessage = $"Error refreshing drawer status: {ex.Message}";
            }
        }
        private async Task OpenDrawerDialogAsync()
        {
            try
            {
                // First, completely refresh drawer status from database
                await RefreshDrawerStatusAsync();

                if (IsDrawerOpen)
                {
                    StatusMessage = $"Drawer already open with ID #{CurrentDrawer.DrawerId}";
                    return;
                }

                var viewModel = new OpenDrawerViewModel(_authService);
                var dialog = new OpenDrawerDialog(viewModel);

                dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.Topmost = true;

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    // Use multiple refresh attempts with delays to ensure we get the updated status
                    await Task.Delay(500); // Initial delay for DB to complete operation
                    await RefreshDrawerStatusAsync();

                    // Double-check with a second refresh after a short delay
                    await Task.Delay(200);
                    await RefreshDrawerStatusAsync();

                    StatusMessage = "Drawer opened successfully.";

                    // Force additional property updates and command reevaluation
                    Application.Current.Dispatcher.Invoke(() => {
                        OnPropertyChanged(nameof(IsDrawerOpen));
                        OnPropertyChanged(nameof(CurrentDrawer));
                        OnPropertyChanged(nameof(CanCheckout));
                        OnPropertyChanged(nameof(DrawerStatusToolTip));
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
                else
                {
                    StatusMessage = "Drawer opening was cancelled or failed.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening drawer: {ex.Message}";
                Console.WriteLine($"Open drawer dialog error: {ex}");

                // Even on error, try to refresh state
                try
                {
                    await RefreshDrawerStatusAsync();
                }
                catch { /* Ignore any errors during refresh */ }
            }
        }

        private async Task CloseDrawerDialogAsync()
        {
            try
            {
                Console.WriteLine("CloseDrawerDialogAsync started");

                // Explicitly refresh drawer data from database with our enhanced method
                Console.WriteLine("Refreshing drawer status from database");
                await RefreshDrawerStatusAsync();

                if (!IsDrawerOpen)
                {
                    StatusMessage = "No open drawer found.";
                    Console.WriteLine("Cannot close drawer: No open drawer found");
                    MessageBox.Show("No open drawer found. Please open a drawer first.",
                        "No Drawer", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Additional checks on drawer state
                if (CurrentDrawer == null)
                {
                    StatusMessage = "Drawer information is not available.";
                    Console.WriteLine("CurrentDrawer is null despite IsDrawerOpen being true");
                    return;
                }

                if (CurrentDrawer.Status != "Open")
                {
                    StatusMessage = $"Drawer status is '{CurrentDrawer.Status}', not 'Open'.";
                    Console.WriteLine($"Cannot close drawer with status: {CurrentDrawer.Status}");
                    return;
                }

                // Log the drawer we're about to close
                Console.WriteLine($"About to close drawer #{CurrentDrawer.DrawerId}, Current status: {CurrentDrawer.Status}");
                Console.WriteLine($"Current balance: ${CurrentDrawer.CurrentBalance:F2}");

                try
                {
                    // Store a local copy of the drawer ID before closing
                    int drawerIdBeforeClose = CurrentDrawer.DrawerId;

                    // Create and show the dialog
                    var dialog = new CloseDrawerDialog(CurrentDrawer);
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.Owner = Application.Current.MainWindow;
                    dialog.Topmost = true;  // Force dialog to stay on top

                    // Show the dialog
                    Console.WriteLine("Showing CloseDrawerDialog");
                    bool? result = dialog.ShowDialog();
                    Console.WriteLine($"Dialog ShowDialog returned result: {result}");

                    // Clear the current drawer first to avoid stale data
                    CurrentDrawer = null;
                    OnPropertyChanged(nameof(CurrentDrawer));
                    OnPropertyChanged(nameof(IsDrawerOpen));
                    CommandManager.InvalidateRequerySuggested();

                    // Give database operations time to complete
                    Console.WriteLine("Waiting for database operations to complete");
                    await Task.Delay(1000);

                    // Multiple refresh attempts with delay to ensure we get the updated status
                    for (int i = 0; i < 3; i++)
                    {
                        // Use our enhanced refresh method
                        Console.WriteLine($"Refreshing drawer status after dialog close (attempt {i + 1})");
                        await RefreshDrawerStatusAsync();

                        // Allow a slight delay between refresh attempts
                        if (i < 2) await Task.Delay(300);
                    }

                    // Force complete UI refresh
                    Application.Current.Dispatcher.Invoke(() => {
                        OnPropertyChanged(nameof(IsDrawerOpen));
                        OnPropertyChanged(nameof(CurrentDrawer));
                        OnPropertyChanged(nameof(CanCheckout));
                        OnPropertyChanged(nameof(DrawerStatusToolTip));
                        CommandManager.InvalidateRequerySuggested();
                    });

                    // If drawer was successfully closed
                    if (result == true)
                    {
                        StatusMessage = "Drawer closed successfully.";
                        Console.WriteLine("Drawer closed successfully according to dialog result");

                        // Directly check the drawer status in the database to be absolutely sure
                        var drawerService = new DrawerService();
                        var drawerDb = await drawerService.GetDrawerByIdAsync(drawerIdBeforeClose);

                        if (drawerDb != null)
                        {
                            Console.WriteLine($"Database check confirms drawer status is: {drawerDb.Status}");

                            if (drawerDb.Status != "Closed")
                            {
                                Console.WriteLine("WARNING: Database shows drawer is not actually closed");
                                // Try one more refresh
                                await RefreshDrawerStatusAsync();
                            }
                        }
                    }
                    else
                    {
                        StatusMessage = "Drawer closing was cancelled or unsuccessful.";
                        Console.WriteLine("Drawer closing was cancelled or unsuccessful");

                        // Ensure we have current data even if closing failed
                        await RefreshDrawerStatusAsync();
                    }
                }
                catch (Exception dialogEx)
                {
                    StatusMessage = $"Error showing close drawer dialog: {dialogEx.Message}";
                    Console.WriteLine($"Dialog creation/show error: {dialogEx.Message}");
                    MessageBox.Show($"Error showing close drawer dialog: {dialogEx.Message}",
                        "Dialog Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Attempt to refresh even on error
                    await RefreshDrawerStatusAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error closing drawer: {ex.Message}";
                Console.WriteLine($"Close drawer dialog error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Error closing drawer: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Final attempt to refresh on error
                try
                {
                    await RefreshDrawerStatusAsync();
                }
                catch { /* Ignore any errors during refresh */ }
            }
            finally
            {
                Console.WriteLine("CloseDrawerDialogAsync completed");
            }
        }
        private async void LoadInitialProductsAsync()
        {
            try
            {
                StatusMessage = "Loading products...";
                var products = await _productService.SearchByNameAsync("");
                SearchedProducts.Clear();

                foreach (var product in products)
                {
                    SearchedProducts.Add(product);
                }

                if (products.Count > 0)
                {
                    StatusMessage = "Ready.";
                }
                else
                {
                    StatusMessage = "No products available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Console.WriteLine($"Error loading initial products: {ex}");
            }
        }

        private void OpenCustomerSelectionDialog()
        {
            var customerDialog = new Views.CustomerSelectionDialog();
            if (customerDialog.ShowDialog() == true)
            {
                var selectedCustomer = customerDialog.SelectedCustomer;
                if (selectedCustomer != null)
                {
                    SetSelectedCustomer(selectedCustomer);
                    StatusMessage = $"Selected customer: {selectedCustomer.Name}";
                }
            }
        }

        private async void LoadInitialCustomersAsync()
        {
            try
            {
                var customers = await _customerService.SearchCustomersAsync("");
                SearchedCustomers.Clear();

                foreach (var customer in customers)
                {
                    SearchedCustomers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading customers: {ex.Message}";
                Console.WriteLine($"Error loading initial customers: {ex}");
            }
        }

        private async void OnSearchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await SearchByNameAsync();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in search timer: {ex}");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Search error: {ex.Message}";
                });
            }
        }

        private async void OnCustomerSearchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await SearchCustomersAsync();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in customer search timer: {ex}");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Customer search error: {ex.Message}";
                });
            }
        }

        public void StartSearchAsYouType()
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        public void UpdateNameQuery(string query)
        {
            NameQuery = query;

            if (string.IsNullOrWhiteSpace(query))
            {
                _lastSearchQuery = query;
                LoadInitialProductsAsync();
                return;
            }

            if (_lastSearchQuery == query)
                return;

            _lastSearchQuery = query;
            StartSearchAsYouType();
        }

        public void UpdateCustomerQuery(string query)
        {
            CustomerQuery = query;

            _customerSearchTimer.Stop();
            _customerSearchTimer.Start();
        }

        private async Task SearchByBarcodeAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BarcodeQuery))
                {
                    LoadInitialProductsAsync();
                    return;
                }

                StatusMessage = "Searching by barcode...";

                var searchResult = await _productService.FindByAnyBarcodeAsync(BarcodeQuery);

                if (searchResult != null)
                {
                    // When adding to cart, set isBox automatically based on barcode type
                    AddToCart(searchResult.Product, searchResult.IsBoxBarcode, false);
                    BarcodeQuery = string.Empty;
                    StatusMessage = searchResult.IsBoxBarcode ?
    $"Added BOX-{searchResult.Product.Name} to cart." :
    $"Added {searchResult.Product.Name} to cart.";
                }
                else
                {
                    StatusMessage = "No product found with this barcode.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Console.WriteLine($"Barcode search error: {ex}");
            }
        }

        private async Task SearchByNameAsync()
        {
            try
            {
                StatusMessage = "Searching...";

                if (string.IsNullOrWhiteSpace(NameQuery))
                {
                    var allProducts = await _productService.SearchByNameAsync("");

                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        SearchedProducts.Clear();
                        foreach (var product in allProducts)
                        {
                            SearchedProducts.Add(product);
                        }
                    });

                    StatusMessage = "Ready";
                    return;
                }

                var products = await _productService.SearchByNameAsync(NameQuery);

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    SearchedProducts.Clear();

                    if (products.Count > 0)
                    {
                        foreach (var product in products)
                        {
                            SearchedProducts.Add(product);
                        }
                        StatusMessage = $"Found {products.Count} products matching '{NameQuery}'";
                    }
                    else
                    {
                        StatusMessage = "No products found matching your search.";
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Console.WriteLine($"Search error: {ex}");
            }
        }

        private async Task SearchCustomersAsync()
        {
            try
            {
                var customers = await _customerService.SearchCustomersAsync(CustomerQuery);

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    SearchedCustomers.Clear();

                    foreach (var customer in customers)
                    {
                        SearchedCustomers.Add(customer);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching customers: {ex.Message}";
                Console.WriteLine($"Customer search error: {ex}");
            }
        }

        public async void SetSelectedCustomer(Customer customer)
        {
            if (customer == null)
                return;

            try
            {
                Console.WriteLine($"Setting selected customer: {customer.Name} (ID: {customer.CustomerId})");

                SelectedCustomer = customer;
                CustomerId = customer.CustomerId;
                CustomerName = customer.Name;

                OnPropertyChanged(nameof(SelectedCustomer));
                OnPropertyChanged(nameof(CustomerId));
                OnPropertyChanged(nameof(CustomerName));

                if (CartItems.Count > 0)
                {
                    await ApplyCustomerPricingToCartAsync();
                }

                StatusMessage = $"Selected customer: {customer.Name}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting customer: {ex.Message}");
                StatusMessage = $"Error selecting customer: {ex.Message}";
            }
        }

        private async Task ApplyCustomerPricingToCartAsync()
        {
            if (CustomerId <= 0 || CartItems.Count == 0)
                return;

            var customerPrices = await _customerPriceService.GetAllPricesForCustomerAsync(CustomerId);

            if (customerPrices.Count == 0)
                return;

            foreach (var item in CartItems)
            {
                if (customerPrices.TryGetValue(item.Product.ProductId, out decimal specialPrice))
                {
                    if (item.UnitPrice != specialPrice)
                    {
                        item.UnitPrice = specialPrice;
                        Console.WriteLine($"Updated price for {item.Product.Name}: ${specialPrice}");
                    }
                }
            }

            UpdateTotals();
            StatusMessage = "Updated prices based on customer-specific pricing.";
        }

        public void SetSelectedCustomerAndFillSearch(Customer customer)
        {
            if (customer == null)
                return;

            try
            {
                Console.WriteLine($"Setting selected customer: {customer.Name} (ID: {customer.CustomerId})");

                SelectedCustomer = customer;
                CustomerId = customer.CustomerId;
                CustomerName = customer.Name;
                CustomerQuery = customer.Name;

                OnPropertyChanged(nameof(SelectedCustomer));
                OnPropertyChanged(nameof(CustomerId));
                OnPropertyChanged(nameof(CustomerName));
                OnPropertyChanged(nameof(CustomerQuery));

                StatusMessage = $"Selected customer: {customer.Name}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting customer: {ex.Message}");
                StatusMessage = $"Error selecting customer: {ex.Message}";
            }
        }

        private async Task PrintDrawerReportAsync()
        {
            try
            {
                if (CurrentDrawer == null)
                {
                    StatusMessage = "No drawer available to print report.";
                    return;
                }

                IsProcessing = true;
                StatusMessage = $"Printing drawer report for drawer #{CurrentDrawer.DrawerId}...";

                // Use the receipt service to print the drawer report
                string result = await _receiptPrinterService.PrintDrawerReportAsync(CurrentDrawer);

                StatusMessage = result;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error printing drawer report: {ex.Message}";
                Console.WriteLine($"Drawer report print error: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        private void OpenAddCustomerDialog()
        {
            var newCustomerDialog = new Views.AddCustomerDialog();
            if (newCustomerDialog.ShowDialog() == true)
            {
                var newCustomer = newCustomerDialog.Customer;
                if (newCustomer != null)
                {
                    SetSelectedCustomer(newCustomer);
                    StatusMessage = $"Added new customer: {newCustomer.Name}";
                }
            }
        }

        public void AddSelectedProductToCart(Product product)
        {
            if (product == null)
                return;

            AddToCart(product);
            NameQuery = string.Empty;
        }

        private async void AddToCart(Product product, bool isBox = false, bool? isWholesale = null)
        {
            if (product == null)
                return;

            // Use the global WholesaleMode if isWholesale is not specifically provided
            bool useWholesale = isWholesale ?? WholesaleMode;

            // Determine the appropriate price based on the selection
            decimal unitPrice;
            if (isBox)
            {
                unitPrice = useWholesale ? product.BoxWholesalePrice : product.BoxSalePrice;
            }
            else
            {
                unitPrice = useWholesale ? product.WholesalePrice : product.SalePrice;
            }

            // Check for customer-specific pricing if not using wholesale pricing
            if (!useWholesale && CustomerId > 0)
            {
                var specialPrice = await _customerPriceService.GetCustomerProductPriceAsync(CustomerId, product.ProductId);
                if (specialPrice.HasValue)
                {
                    unitPrice = specialPrice.Value;
                    Console.WriteLine($"Applied customer-specific price for {product.Name}: ${unitPrice}");
                }
            }

            // Check if the product already exists in the cart with the same options
            var existingItemIndex = CartItems.ToList().FindIndex(i =>
                i.Product.ProductId == product.ProductId &&
                i.IsBox == isBox &&
                i.IsWholesale == useWholesale);

            if (existingItemIndex >= 0)
            {
                // Get the existing cart item
                var existingItem = CartItems[existingItemIndex];

                // Update the quantity
                existingItem.Quantity += 1;

                // Recalculate discount if it's a percentage-based discount
                if (existingItem.DiscountType == 1)
                {
                    // Store the current percentage
                    decimal percentage = existingItem.DiscountValue;
                    // Update the discount amount based on the new quantity
                    existingItem.DiscountValue = percentage;
                }

                // Force UI refresh by removing and re-adding the item
                var updatedItem = existingItem;
                CartItems.RemoveAt(existingItemIndex);
                CartItems.Insert(existingItemIndex, updatedItem);

                StatusMessage = $"Updated {(isBox ? $"BOX-{product.Name}" : product.Name)} quantity to {updatedItem.Quantity}";
            }
            else
            {
                // Product doesn't exist in the cart, add it as a new item
                var newItem = new CartItem
                {
                    Product = product,
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    Discount = 0,
                    DiscountType = 0,
                    IsBox = isBox,
                    IsWholesale = useWholesale
                };

                CartItems.Add(newItem);
                StatusMessage = $"Added {(isBox ? $"BOX-{product.Name}" : product.Name)} to cart.";
            }

            UpdateTotals();
        }
        public void UpdateCartItemQuantity(CartItem cartItem)
        {
            if (cartItem == null)
                return;

            if (cartItem.Quantity <= 0)
                cartItem.Quantity = 1;

            if (cartItem.DiscountType == 1)
            {
                decimal percentage = (cartItem.Discount / (cartItem.Quantity * cartItem.UnitPrice - cartItem.Discount)) * 100;
                cartItem.Discount = (percentage / 100) * (cartItem.Quantity * cartItem.UnitPrice);
            }

            UpdateTotals();
        }

        public void UpdateCartItemDiscount(CartItem cartItem)
        {
            if (cartItem == null)
                return;

            if (cartItem.DiscountType == 0 && cartItem.Discount > cartItem.Subtotal)
                cartItem.Discount = cartItem.Subtotal;

            if (cartItem.DiscountType == 1)
            {
                decimal percentage = cartItem.DiscountValue;
                if (percentage > 100)
                    cartItem.DiscountValue = 100;
            }

            UpdateTotals();
        }

        private void RemoveFromCart(CartItem cartItem = null)
        {
            var itemToRemove = cartItem ?? SelectedCartItem;

            if (itemToRemove == null)
                return;

            CartItems.Remove(itemToRemove);
            UpdateTotals();
            StatusMessage = "Item removed from cart.";

            if (SelectedCartItem == itemToRemove)
                SelectedCartItem = null;
        }

        private void ClearCart()
        {
            CartItems.Clear();
            UpdateTotals();
            StatusMessage = "Cart cleared.";

            LoadedTransaction = null;
            IsTransactionLoaded = false;
            IsEditMode = false;
        }

        private void UpdateTotals()
        {
            TotalAmount = CartItems.Sum(i => i.Total);

            if (PaidAmount <= 0 || PaidAmount > TotalAmount)
            {
                PaidAmount = TotalAmount;
            }

            // Always calculate exchange amount whenever totals change
            CalculateExchangeAmount();
            CalculateAmountToDebt();
            // Ensure ExchangeAmount property change is notified
            OnPropertyChanged(nameof(ExchangeAmount));
        }
        private async Task CheckoutAsync()
        {
            try
            {
                if (!IsDrawerOpen)
                {
                    StatusMessage = "Cannot checkout: Drawer is closed. Please open a drawer first.";
                    return;
                }

                if (CartItems.Count == 0)
                {
                    StatusMessage = "Cart is empty. Cannot checkout.";
                    return;
                }

                if (PaidAmount < 0)
                {
                    StatusMessage = "Payment amount cannot be negative.";
                    return;
                }

                if (AddToCustomerDebt && AmountToDebt > 0 && (CustomerId <= 0 || CustomerName == "Walk-in Customer"))
                {
                    StatusMessage = "Cannot add debt to a walk-in customer. Please select a registered customer.";
                    return;
                }

                IsProcessing = true;
                StatusMessage = "Processing transaction...";
                Console.WriteLine($"Starting checkout process at: {DateTime.Now}");
                Console.WriteLine($"Cart contains {CartItems.Count} items, total amount: {TotalAmount:C2}, paid amount: {PaidAmount:C2}");

                if (AddToCustomerDebt && AmountToDebt > 0)
                {
                    Console.WriteLine($"Adding {AmountToDebt:C2} to customer debt (Customer ID: {CustomerId})");
                }

                var currentEmployee = _authService.CurrentEmployee;
                if (currentEmployee == null)
                {
                    StatusMessage = "Error: No cashier is logged in.";
                    IsProcessing = false;
                    Console.WriteLine("Checkout failed: No cashier logged in");
                    return;
                }

                int customerIdForTransaction;
                string customerNameForTransaction;

                if (CustomerId <= 0 || CustomerName == "Walk-in Customer")
                {
                    if (_walkInCustomer != null)
                    {
                        customerIdForTransaction = _walkInCustomer.CustomerId;
                        customerNameForTransaction = _walkInCustomer.Name;
                        Console.WriteLine($"Using walk-in customer: ID={customerIdForTransaction}, Name={customerNameForTransaction}");
                    }
                    else
                    {
                        var dbService = new DatabaseService();
                        var walkInCustomer = await dbService.EnsureWalkInCustomerExistsAsync();

                        if (walkInCustomer != null)
                        {
                            customerIdForTransaction = walkInCustomer.CustomerId;
                            customerNameForTransaction = walkInCustomer.Name;
                            Console.WriteLine($"Retrieved walk-in customer: ID={customerIdForTransaction}, Name={customerNameForTransaction}");
                        }
                        else
                        {
                            StatusMessage = "Error: Cannot proceed without a valid customer ID. Please select a customer.";
                            IsProcessing = false;
                            Console.WriteLine("Checkout failed: No valid walk-in customer");
                            return;
                        }
                    }
                }
                else
                {
                    customerIdForTransaction = CustomerId;
                    customerNameForTransaction = CustomerName;
                    Console.WriteLine($"Using selected customer: ID={customerIdForTransaction}, Name={customerNameForTransaction}");
                }

                Console.WriteLine($"Cashier: {currentEmployee.FullName} (ID: {currentEmployee.EmployeeId})");
                Console.WriteLine($"Customer: {customerNameForTransaction} (ID: {customerIdForTransaction})");

                var transaction = await _transactionService.CreateTransactionAsync(
                    CartItems.ToList(),
                    PaidAmount,
                    currentEmployee,
                    "Cash",
                    customerNameForTransaction,
                    customerIdForTransaction
                );

                if (transaction == null || transaction.TransactionId <= 0)
                {
                    StatusMessage = "Error: Failed to create transaction record.";
                    IsProcessing = false;
                    Console.WriteLine("Checkout failed: Transaction record creation failed");
                    return;
                }

                Console.WriteLine($"Transaction #{transaction.TransactionId} created successfully");
                Console.WriteLine($"- Total Amount: {transaction.TotalAmount:C2}, Paid Amount: {transaction.PaidAmount:C2}");

                // Updated receipt printing using the new service method
                string receiptResult = await _receiptPrinterService.PrintTransactionReceiptWpfAsync(
                    transaction,
                    CartItems.ToList(),
                    customerIdForTransaction,
                    0, // No previous balance for new transactions
                    ExchangeRate);

                bool printed = !receiptResult.Contains("cancelled") && !receiptResult.Contains("error");
                Console.WriteLine(receiptResult);

                // After successful transaction, update the drawer
                try
                {
                    if (CurrentDrawer != null && CurrentDrawer.Status == "Open")
                    {
                        // Update the drawer with the transaction amount
                        await _drawerService.UpdateDrawerTransactionsAsync(
                            CurrentDrawer.DrawerId,
                            transaction.TotalAmount, // Sales amount
                            0, // No expenses
                            0  // No supplier payments
                        );

                        Console.WriteLine($"Updated drawer #{CurrentDrawer.DrawerId} with sales amount: {transaction.TotalAmount:C2}");

                        // Also refresh the drawer data
                        await GetCurrentDrawerAsync();
                    }
                    else
                    {
                        Console.WriteLine("Warning: No open drawer found to update for this transaction");
                    }
                }
                catch (Exception drawerEx)
                {
                    // Log the error but don't fail the transaction
                    Console.WriteLine($"Error updating drawer: {drawerEx.Message}");
                    StatusMessage += " (Warning: Drawer update failed)";
                }

                if (AddToCustomerDebt && AmountToDebt > 0 && customerIdForTransaction > 0 && CustomerName != "Walk-in Customer")
                {
                    var customerService = new CustomerService();
                    bool balanceUpdated = await customerService.UpdateCustomerBalanceAsync(customerIdForTransaction, AmountToDebt);

                    if (balanceUpdated)
                    {
                        Console.WriteLine($"Added {AmountToDebt:C2} to customer balance successfully");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Failed to update customer balance");
                        StatusMessage = $"Transaction completed but failed to update customer balance. Please check customer account.";
                    }
                }

                TransactionLookupId = transaction.TransactionId.ToString();

                CartItems.Clear();
                UpdateTotals();

                if (CustomerId > 0 && CustomerName != "Walk-in Customer")
                {
                    if (_walkInCustomer != null)
                    {
                        CustomerId = _walkInCustomer.CustomerId;
                    }
                    else
                    {
                        CustomerId = 0;
                    }

                    CustomerName = "Walk-in Customer";
                    SelectedCustomer = null;
                    OnPropertyChanged(nameof(CustomerName));
                    OnPropertyChanged(nameof(CustomerId));
                }

                PaidAmount = 0;
                AddToCustomerDebt = false;
                AmountToDebt = 0;
                UseExchangeRate = true;

                await LookupTransactionAsync();

                LoadInitialProductsAsync();

                StatusMessage = $"Transaction #{transaction.TransactionId} completed successfully.";
                if (printed)
                {
                    StatusMessage += " Receipt printed.";
                }
                if (AddToCustomerDebt && AmountToDebt > 0)
                {
                    StatusMessage += $" Added {AmountToDebt:C2} to customer balance.";
                }

                Console.WriteLine("Checkout process completed successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during checkout: {ex.Message}";
                if (ex.InnerException != null)
                {
                    StatusMessage += $" Inner exception: {ex.InnerException.Message}";
                }

                Console.WriteLine($"Checkout error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }
        private void Logout()
        {
            // You can either implement logout here
            // Or just delegate to the parent view
            Application.Current.MainWindow.Close();
        }
        private async Task LookupTransactionAsync()
        {
            IsEditMode = false;

            try
            {
                if (string.IsNullOrWhiteSpace(TransactionLookupId) || !int.TryParse(TransactionLookupId, out int transactionId) || transactionId <= 0)
                {
                    StatusMessage = "Please enter a valid transaction ID.";
                    LoadedTransaction = null;
                    IsTransactionLoaded = false;
                    CanNavigateNext = false;
                    CanNavigatePrevious = false;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                IsProcessing = true;
                StatusMessage = $"Looking up transaction #{transactionId}...";

                CartItems.Clear();

                Transaction transaction = null;
                try
                {
                    transaction = await _transactionService.GetTransactionWithDetailsAsync(transactionId);
                }
                catch (InvalidCastException ex)
                {
                    Console.WriteLine($"Type conversion error loading transaction: {ex.Message}");
                    // Use direct database access with manual conversion as a fallback
                    transaction = await LoadTransactionWithDirectQueryAsync(transactionId);
                }

                if (transaction == null)
                {
                    StatusMessage = $"Transaction #{transactionId} not found.";
                    LoadedTransaction = null;
                    IsTransactionLoaded = false;
                    CanNavigateNext = false;
                    CanNavigatePrevious = false;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                await LoadTransactionToCartAsync(transaction);

                LoadedTransaction = transaction;
                IsTransactionLoaded = true;

                if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
                {
                    CustomerId = transaction.CustomerId.Value;
                    CustomerName = transaction.CustomerName;

                    var customer = await _customerService.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        SelectedCustomer = customer;
                    }
                }
                else
                {
                    CustomerId = 0;
                    CustomerName = transaction.CustomerName ?? "Walk-in Customer";
                    SelectedCustomer = null;
                }

                await CheckNavigationAvailabilityAsync(transactionId);
                CommandManager.InvalidateRequerySuggested();

                CalculateExchangeAmount();
                OnPropertyChanged(nameof(ExchangeAmount));

                StatusMessage = $"Loaded transaction #{transactionId} ({transaction.TransactionTypeString}/{transaction.StatusString}) completed on {transaction.FormattedDate}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error looking up transaction: {ex.Message}";
                Console.WriteLine($"Transaction lookup error: {ex}");
                LoadedTransaction = null;
                IsTransactionLoaded = false;
                CanNavigateNext = false;
                CanNavigatePrevious = false;
                CommandManager.InvalidateRequerySuggested();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // Add this helper method to manually load a transaction with direct query
        private async Task<Transaction> LoadTransactionWithDirectQueryAsync(int transactionId)
        {
            try
            {
                Console.WriteLine($"Using direct query fallback for transaction #{transactionId}");

                // Create a new database context for this operation
                using var dbContext = new DatabaseContext(ConfigurationService.ConnectionString);

                // Use raw SQL to fetch the transaction
                var transaction = await dbContext.Transactions
                    .FromSqlRaw("SELECT * FROM Transactions WHERE TransactionId = {0}", transactionId)
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    Console.WriteLine($"Transaction #{transactionId} not found using direct query");
                    return null;
                }

                // Manually handle the enum conversions
                if (transaction.Status == default && !string.IsNullOrEmpty(transaction.StatusString))
                {
                    transaction.Status = Helpers.EnumConverter.StringToTransactionStatus(transaction.StatusString);
                    Console.WriteLine($"Converted status from string '{transaction.StatusString}' to enum '{transaction.Status}'");
                }

                if (transaction.TransactionType == default && !string.IsNullOrEmpty(transaction.TransactionTypeString))
                {
                    transaction.TransactionType = Helpers.EnumConverter.StringToTransactionType(transaction.TransactionTypeString);
                    Console.WriteLine($"Converted type from string '{transaction.TransactionTypeString}' to enum '{transaction.TransactionType}'");
                }

                // Load details
                var details = await dbContext.TransactionDetails
                    .Where(d => d.TransactionId == transactionId)
                    .ToListAsync();

                transaction.Details = details;
                Console.WriteLine($"Loaded {details.Count} details for transaction #{transactionId} using direct query");

                return transaction;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in direct query transaction loading: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
        private async Task LoadTransactionToCartAsync(Transaction transaction)
        {
            if (transaction == null || transaction.Details == null || !transaction.Details.Any())
                return;

            CartItems.Clear();

            foreach (var detail in transaction.Details)
            {
                var product = await GetProductForTransactionDetailAsync(detail);

                var cartItem = new CartItem
                {
                    Product = product,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Discount = detail.Discount
                };

                CartItems.Add(cartItem);
            }

            UpdateTotals();
        }

        private async Task<Product> GetProductForTransactionDetailAsync(TransactionDetail detail)
        {
            try
            {
                var dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
                var product = await dbContext.Products.FindAsync(detail.ProductId);

                if (product != null)
                    return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving product: {ex.Message}");
            }

            return new Product
            {
                ProductId = detail.ProductId,
                Name = "Product #" + detail.ProductId,
                SalePrice = detail.UnitPrice,
                PurchasePrice = detail.PurchasePrice
            };
        }

        private void EnterEditMode()
        {
            if (LoadedTransaction == null)
                return;

            IsEditMode = true;
            StatusMessage = $"Editing transaction #{LoadedTransaction.TransactionId}. Make changes and click Save Changes when done.";
        }

        private async Task SaveTransactionChangesAsync()
        {
            try
            {
                if (LoadedTransaction == null)
                    return;

                IsProcessing = true;
                StatusMessage = $"Saving changes to transaction #{LoadedTransaction.TransactionId}...";

                var updatedTransaction = new Transaction
                {
                    TransactionId = LoadedTransaction.TransactionId,
                    CustomerId = CustomerId > 0 ? CustomerId : null,
                    CustomerName = CustomerName,
                    TotalAmount = TotalAmount,
                    PaidAmount = LoadedTransaction.PaidAmount,
                    TransactionDate = LoadedTransaction.TransactionDate,
                    TransactionType = LoadedTransaction.TransactionType, // Keep the original type
                    Status = LoadedTransaction.Status, // Keep the original status
                    PaymentMethod = LoadedTransaction.PaymentMethod,
                    CashierId = LoadedTransaction.CashierId,
                    CashierName = LoadedTransaction.CashierName,
                    CashierRole = LoadedTransaction.CashierRole
                };

                var updatedDetails = CartItems.Select(item => new TransactionDetail
                {
                    TransactionId = LoadedTransaction.TransactionId,
                    ProductId = item.Product.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    PurchasePrice = item.Product.PurchasePrice,
                    Discount = item.Discount,
                    Total = item.Total
                }).ToList();

                bool success = await _transactionService.UpdateTransactionAsync(updatedTransaction, updatedDetails);

                if (success)
                {
                    StatusMessage = $"Transaction #{LoadedTransaction.TransactionId} updated successfully.";

                    LoadedTransaction = updatedTransaction;
                    LoadedTransaction.Details = updatedDetails;

                    IsEditMode = false;
                }
                else
                {
                    StatusMessage = $"Failed to update transaction #{LoadedTransaction.TransactionId}.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating transaction: {ex.Message}";
                Console.WriteLine($"Transaction update error: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }


        private async Task CheckNavigationAvailabilityAsync(int currentTransactionId)
        {
            try
            {
                // Check for next transaction
                var nextId = await _transactionService.GetNextTransactionIdAsync(currentTransactionId);
                CanNavigateNext = nextId.HasValue && nextId.Value > 0;

                // Check for previous transaction
                var prevId = await _transactionService.GetPreviousTransactionIdAsync(currentTransactionId);
                CanNavigatePrevious = prevId.HasValue && prevId.Value > 0;

                Console.WriteLine($"Navigation availability updated: Previous={CanNavigatePrevious}, Next={CanNavigateNext}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking navigation availability: {ex.Message}");
                CanNavigateNext = false;
                CanNavigatePrevious = false;
            }
        }

        private async void PrintReceipt()
        {
            try
            {
                if (LoadedTransaction == null)
                {
                    StatusMessage = "No transaction loaded to print.";
                    return;
                }

                StatusMessage = $"Printing receipt for transaction #{LoadedTransaction.TransactionId}...";
                IsProcessing = true;

                decimal previousBalance = 0;
                if (CustomerId > 0 && SelectedCustomer != null)
                {
                    try
                    {
                        var customer = await _customerService.GetByIdAsync(CustomerId);
                        if (customer != null)
                        {
                            previousBalance = customer.Balance;
                            // If this is a loaded transaction, subtract its total to avoid double counting
                            if (IsTransactionLoaded && !IsEditMode)
                            {
                                previousBalance -= TotalAmount;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving customer balance: {ex.Message}");
                    }
                }

                // Use the enhanced service method
                string result = await _receiptPrinterService.PrintTransactionReceiptWpfAsync(
                    LoadedTransaction,
                    CartItems.ToList(),
                    CustomerId,
                    previousBalance,
                    ExchangeRate);

                StatusMessage = result;
                IsProcessing = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error printing receipt: {ex.Message}";
                Console.WriteLine($"Print receipt error: {ex}");
                IsProcessing = false;
            }
        }
        private async Task NavigateToNextTransactionAsync()
        {
            try
            {
                if (!IsTransactionLoaded || LoadedTransaction == null)
                    return;

                int currentId = LoadedTransaction.TransactionId;
                var nextId = await _transactionService.GetNextTransactionIdAsync(currentId);

                if (nextId.HasValue && nextId.Value > 0)
                {
                    Console.WriteLine($"Navigating to next transaction: {nextId.Value}");
                    TransactionLookupId = nextId.Value.ToString();
                    await LookupTransactionAsync();
                }
                else
                {
                    StatusMessage = "No more transactions available.";
                    Console.WriteLine("No next transaction found");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to next transaction: {ex.Message}";
                Console.WriteLine($"Next transaction navigation error: {ex}");
            }
        }

        private async Task NavigateToPreviousTransactionAsync()
        {
            try
            {
                if (!IsTransactionLoaded || LoadedTransaction == null)
                    return;

                int currentId = LoadedTransaction.TransactionId;
                var prevId = await _transactionService.GetPreviousTransactionIdAsync(currentId);

                if (prevId.HasValue && prevId.Value > 0)
                {
                    Console.WriteLine($"Navigating to previous transaction: {prevId.Value}");
                    TransactionLookupId = prevId.Value.ToString();
                    await LookupTransactionAsync();
                }
                else
                {
                    StatusMessage = "No previous transactions available.";
                    Console.WriteLine("No previous transaction found");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to previous transaction: {ex.Message}";
                Console.WriteLine($"Previous transaction navigation error: {ex}");
            }
        }
    }
}