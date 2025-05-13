using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Printing;
using System.IO;

namespace QuickTechPOS.ViewModels
{
    /// <summary>
    /// View model for the close drawer dialog
    /// </summary>
    public class CloseDrawerViewModel : BaseViewModel
    {
        private readonly DrawerService _drawerService;

        private Drawer _drawer;
        private decimal _closingBalance;
        private decimal _calculatedDifference;
        private string _errorMessage;
        private bool _isProcessing;
        private string _closingNotes;
        private bool _printReportAfterClosing = true;

        /// <summary>
        /// Gets the current drawer
        /// </summary>
        public Drawer Drawer
        {
            get => _drawer;
            private set => SetProperty(ref _drawer, value);
        }

        /// <summary>
        /// Gets or sets the closing balance
        /// </summary>
        public decimal ClosingBalance
        {
            get => _closingBalance;
            set
            {
                if (SetProperty(ref _closingBalance, value))
                {
                    CalculatedDifference = ClosingBalance - (Drawer?.CurrentBalance ?? 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to print the drawer report after closing
        /// </summary>
        public bool PrintReportAfterClosing
        {
            get => _printReportAfterClosing;
            set => SetProperty(ref _printReportAfterClosing, value);
        }

        /// <summary>
        /// Gets the calculated difference between the system balance and the entered closing balance
        /// </summary>
        public decimal CalculatedDifference
        {
            get => _calculatedDifference;
            private set => SetProperty(ref _calculatedDifference, value);
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
        /// Gets or sets the closing notes
        /// </summary>
        public string ClosingNotes
        {
            get => _closingNotes;
            set => SetProperty(ref _closingNotes, value);
        }

        /// <summary>
        /// Command to close the drawer
        /// </summary>
        public ICommand CloseDrawerCommand { get; }

        /// <summary>
        /// Command to cancel the operation
        /// </summary>
        public ICommand CancelCommand { get; }
        public ICommand PrintReportCommand { get; }

        /// <summary>
        /// Initializes a new instance of the close drawer view model
        /// </summary>
        /// <param name="drawer">The drawer to close</param>
        public CloseDrawerViewModel(Drawer drawer)
        {
            try
            {
                Console.WriteLine("Initializing CloseDrawerViewModel");

                // Create a new service instance
                _drawerService = new DrawerService();

                // Null check for drawer
                if (drawer == null)
                {
                    Console.WriteLine("ERROR: Drawer is null in CloseDrawerViewModel constructor");
                    throw new ArgumentNullException(nameof(drawer));
                }

                Console.WriteLine($"Drawer ID: {drawer.DrawerId}, Status: {drawer.Status}, CurrentBalance: {drawer.CurrentBalance}");

                Drawer = drawer;

                // Initialize with the system's current balance
                ClosingBalance = drawer.CurrentBalance;
                CalculatedDifference = 0;
                ErrorMessage = string.Empty;
                IsProcessing = false;
                ClosingNotes = string.Empty;
                PrintReportCommand = new RelayCommand(async param => await PrintDrawerReportAsync(Drawer), param => Drawer != null);
                CloseDrawerCommand = new RelayCommand(async param => await CloseDrawerAsync(), CanCloseDrawer);
                CancelCommand = new RelayCommand(param => Cancel());

                Console.WriteLine("CloseDrawerViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CloseDrawerViewModel constructor: {ex.Message}");
                MessageBox.Show($"Error initializing Close Drawer: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ErrorMessage = $"Initialization error: {ex.Message}";
            }
        }

        /// <summary>
        /// Determines if the drawer can be closed
        /// </summary>
        private bool CanCloseDrawer(object parameter)
        {
            bool canExecute = !IsProcessing && ClosingBalance >= 0 && Drawer != null && Drawer.Status == "Open";
            Console.WriteLine($"CanCloseDrawer evaluation: {canExecute}");
            return canExecute;
        }

        /// <summary>
        /// Closes the drawer with the specified closing balance
        /// </summary>
        private async Task CloseDrawerAsync()
        {
            Console.WriteLine("CloseDrawerAsync method started");
            try
            {
                // Show in-progress UI state
                IsProcessing = true;
                ErrorMessage = string.Empty;

                // Validate input
                if (ClosingBalance < 0)
                {
                    ErrorMessage = "Closing balance cannot be negative.";
                    Console.WriteLine("Validation error: Closing balance cannot be negative");
                    IsProcessing = false;
                    return;
                }

                Console.WriteLine($"Attempting to close drawer #{Drawer.DrawerId} with balance ${ClosingBalance:F2}");
                Console.WriteLine($"Current drawer status: {Drawer.Status}");
                Console.WriteLine($"PrintReportAfterClosing is set to: {PrintReportAfterClosing}");

                // Double check drawer status
                if (Drawer.Status != "Open")
                {
                    ErrorMessage = "Cannot close this drawer because it's not in 'Open' status.";
                    Console.WriteLine($"Cannot close drawer because status is: {Drawer.Status}");
                    IsProcessing = false;
                    return;
                }

                try
                {
                    // Close the drawer
                    var updatedDrawer = await _drawerService.CloseDrawerAsync(Drawer.DrawerId, ClosingBalance, ClosingNotes);

                    if (updatedDrawer != null)
                    {
                        // Update our local drawer object with the updated values from the database
                        Console.WriteLine($"Drawer closed successfully. Updated status: {updatedDrawer.Status}");
                        Drawer = updatedDrawer;

                        // Verify status change
                        if (Drawer.Status != "Closed")
                        {
                            Console.WriteLine($"WARNING: Drawer status is not 'Closed' after operation. Current status: {Drawer.Status}");
                        }

                        // Print the drawer report if requested
                        if (PrintReportAfterClosing)
                        {
                            await PrintDrawerReportAsync(updatedDrawer);
                        }

                        // Set DialogResult to true to indicate success and close the dialog
                        DialogResult = true;
                        OnPropertyChanged(nameof(DialogResult));
                        Console.WriteLine("DialogResult set to true - dialog should close now");
                    }
                    else
                    {
                        ErrorMessage = "Failed to close drawer. Please try again.";
                        Console.WriteLine("Close drawer operation failed: drawer service returned null");
                        DialogResult = false;
                        OnPropertyChanged(nameof(DialogResult));
                    }
                }
                catch (Exception serviceEx)
                {
                    Console.WriteLine($"DrawerService.CloseDrawerAsync error: {serviceEx.Message}");
                    if (serviceEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {serviceEx.InnerException.Message}");
                    }

                    ErrorMessage = $"Error closing drawer: {serviceEx.Message}";
                    MessageBox.Show($"Failed to close drawer: {serviceEx.Message}", "Drawer Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    OnPropertyChanged(nameof(DialogResult));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error closing drawer: {ex.Message}";
                Console.WriteLine($"Exception in CloseDrawerAsync: {ex.Message}");
                DialogResult = false;
                OnPropertyChanged(nameof(DialogResult));
            }
            finally
            {
                IsProcessing = false;
                CommandManager.InvalidateRequerySuggested();
                Console.WriteLine("CloseDrawerAsync method completed");
            }
        }

        // Separate method for printing to better handle exceptions
        private async Task PrintDrawerReportAsync(Drawer drawer)
        {
            try
            {
                Console.WriteLine("Printing drawer report...");

                // Create a new instance of the receipt printer service
                var receiptPrinterService = new ReceiptPrinterService();

                // Print the drawer report
                string printResult = await receiptPrinterService.PrintDrawerReportAsync(drawer);

                Console.WriteLine($"Print result: {printResult}");

                // Show success message if needed
                if (printResult.Contains("successful"))
                {
                    Console.WriteLine("Drawer report printed successfully");
                }
                else
                {
                    // Show a message but don't fail the drawer closing
                    Console.WriteLine($"Warning: Print operation result: {printResult}");
                    MessageBox.Show($"Note: {printResult}", "Print Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Printing.PrintQueueException pqEx)
            {
                Console.WriteLine($"PrintQueue error: {pqEx.Message}");
                MessageBox.Show("Couldn't access printer. Please check your printer setup and try printing manually.",
                    "Printer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing drawer report: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                // Don't fail the drawer closing, just show a message
                MessageBox.Show($"Could not print drawer report: {ex.Message}. " +
                    "The drawer has been closed successfully.",
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Cancels the operation
        /// </summary>
        private void Cancel()
        {
            Console.WriteLine("Cancel method called");
            DialogResult = false;
            OnPropertyChanged(nameof(DialogResult));
        }

        /// <summary>
        /// Gets or sets the dialog result
        /// </summary>
        public bool? DialogResult { get; set; }
    }
}