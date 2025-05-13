using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Service for managing failed transactions
    /// </summary>
    public class FailedTransactionService : IFailedTransactionService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ITransactionService _transactionService;
        private readonly DrawerService _drawerService;

        /// <summary>
        /// Initializes a new instance of the failed transaction service with dependency injection
        /// </summary>
        public FailedTransactionService(ITransactionService transactionService)
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _transactionService = transactionService;
            _drawerService = new DrawerService();
        }

        /// <summary>
        /// Constructor to break circular dependency when created from TransactionService
        /// </summary>
        public FailedTransactionService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _transactionService = null; // Will be set later if needed
            _drawerService = new DrawerService();
        }

        /// <summary>
        /// Gets all failed transactions that can be retried
        /// </summary>
        public async Task<List<FailedTransaction>> GetFailedTransactionsAsync()
        {
            try
            {
                return await _dbContext.FailedTransactions
                    .Where(ft => ft.State == FailedTransactionState.Failed)
                    .OrderByDescending(ft => ft.AttemptedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting failed transactions: {ex.Message}");
                return new List<FailedTransaction>();
            }
        }

        /// <summary>
        /// Gets a failed transaction by ID
        /// </summary>
        public async Task<FailedTransaction> GetFailedTransactionByIdAsync(int failedTransactionId)
        {
            try
            {
                return await _dbContext.FailedTransactions.FindAsync(failedTransactionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting failed transaction: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Records a failed transaction for later retry
        /// </summary>
        public async Task<FailedTransaction> RecordFailedTransactionAsync(
            Transaction partialTransaction,
            List<CartItem> cartItems,
            Employee cashier,
            Exception error,
            string component)
        {
            try
            {
                // First, ensure current drawer is available
                var drawer = await _drawerService.GetOpenDrawerAsync(cashier.EmployeeId.ToString());

                // Create the failed transaction
                var failedTransaction = new FailedTransaction
                {
                    OriginalTransactionId = partialTransaction?.TransactionId,
                    AttemptedAt = DateTime.Now,
                    State = FailedTransactionState.Failed,
                    ErrorMessage = error.Message,
                    ErrorDetails = $"{error.GetType().Name}: {error.Message}\n{error.StackTrace}",
                    FailureComponent = component,
                    RetryCount = 0,
                    CashierId = cashier.EmployeeId.ToString(),
                    CashierName = cashier.FullName ?? cashier.Username,
                    CustomerId = partialTransaction?.CustomerId,
                    CustomerName = partialTransaction?.CustomerName ?? "Walk-in Customer",
                    TotalAmount = partialTransaction?.TotalAmount ?? cartItems.Sum(i => i.Total),
                    PaidAmount = partialTransaction?.PaidAmount ?? 0,
                    TransactionType = partialTransaction?.TransactionType ?? TransactionType.Sale,
                    PaymentMethod = partialTransaction?.PaymentMethod ?? "Cash",
                    DrawerId = drawer?.DrawerId
                };

                // Serialize cart items
                failedTransaction.SetCartItems(cartItems);

                // Save to database
                _dbContext.FailedTransactions.Add(failedTransaction);
                await _dbContext.SaveChangesAsync();

                return failedTransaction;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording failed transaction: {ex.Message}");
                // In case of failure here, we'll try to use local storage as backup
                await SaveToLocalBackupAsync(cartItems, error, cashier);
                return null;
            }
        }

        /// <summary>
        /// Attempts to retry a failed transaction
        /// </summary>
        public async Task<(bool Success, string Message, Transaction Transaction)> RetryTransactionAsync(int failedTransactionId)
        {
            // Verify we have a transaction service
            if (_transactionService == null)
            {
                return (false, "Transaction service not available for retry.", null);
            }

            var failedTransaction = await _dbContext.FailedTransactions.FindAsync(failedTransactionId);
            if (failedTransaction == null)
            {
                return (false, "Failed transaction not found.", null);
            }

            if (failedTransaction.State != FailedTransactionState.Failed)
            {
                return (false, $"Cannot retry transaction in {failedTransaction.State} state.", null);
            }

            // Mark as retrying
            failedTransaction.State = FailedTransactionState.Retrying;
            failedTransaction.RetryCount++;
            failedTransaction.LastRetryAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            try
            {
                // Get current employee
                var employeeService = new AuthenticationService();
                var employee = employeeService.CurrentEmployee;
                if (employee == null)
                {
                    // Use original cashier info if current employee not available
                    employee = new Employee
                    {
                        EmployeeId = int.Parse(failedTransaction.CashierId),
                        FirstName = failedTransaction.CashierName.Split(' ')[0],
                        LastName = failedTransaction.CashierName.Contains(" ") ?
                            failedTransaction.CashierName.Substring(failedTransaction.CashierName.IndexOf(' ') + 1) :
                            string.Empty
                    };
                }

                // Verify drawer is open
                var drawer = await _drawerService.GetOpenDrawerAsync(employee.EmployeeId.ToString());
                if (drawer == null)
                {
                    failedTransaction.State = FailedTransactionState.Failed;
                    failedTransaction.ErrorMessage = "No open drawer available.";
                    await _dbContext.SaveChangesAsync();
                    return (false, "No open drawer available. Please open a drawer first.", null);
                }

                // Get cart items
                var cartItems = failedTransaction.CartItems;
                if (cartItems.Count == 0)
                {
                    failedTransaction.State = FailedTransactionState.Failed;
                    failedTransaction.ErrorMessage = "No cart items found in the failed transaction.";
                    await _dbContext.SaveChangesAsync();
                    return (false, "No cart items found in the failed transaction.", null);
                }

                // Retry the transaction
                var transaction = await _transactionService.CreateTransactionAsync(
                    failedTransaction.CartItems,
                    failedTransaction.PaidAmount,
                    employee,
                    failedTransaction.PaymentMethod,
                    failedTransaction.CustomerName,
                    failedTransaction.CustomerId ?? 0
                );

                // Mark as completed if successful
                failedTransaction.State = FailedTransactionState.Completed;
                failedTransaction.OriginalTransactionId = transaction.TransactionId;
                await _dbContext.SaveChangesAsync();

                return (true, "Transaction completed successfully.", transaction);
            }
            catch (Exception ex)
            {
                // Update error information
                failedTransaction.State = FailedTransactionState.Failed;
                failedTransaction.ErrorMessage = ex.Message;
                failedTransaction.ErrorDetails = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                failedTransaction.LastRetryAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                return (false, $"Failed to retry transaction: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Cancels a failed transaction
        /// </summary>
        public async Task<bool> CancelFailedTransactionAsync(int failedTransactionId)
        {
            try
            {
                var failedTransaction = await _dbContext.FailedTransactions.FindAsync(failedTransactionId);
                if (failedTransaction == null)
                {
                    return false;
                }

                failedTransaction.State = FailedTransactionState.Cancelled;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling failed transaction: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Backup method to save failed transaction data to local storage if database fails
        /// </summary>
        private async Task SaveToLocalBackupAsync(List<CartItem> cartItems, Exception error, Employee cashier)
        {
            try
            {
                var backupData = new
                {
                    Timestamp = DateTime.Now,
                    CashierId = cashier?.EmployeeId.ToString(),
                    CashierName = cashier?.FullName,
                    CartItems = cartItems,
                    TotalAmount = cartItems.Sum(i => i.Total),
                    ErrorMessage = error.Message,
                    ErrorDetails = $"{error.GetType().Name}: {error.Message}\n{error.StackTrace}"
                };

                string json = JsonSerializer.Serialize(backupData);
                string fileName = $"FailedTransaction_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuickTechPOS", "FailedTransactions", fileName);

                // Ensure directory exists
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));

                // Write to file
                await System.IO.File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"Failed transaction backed up to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backing up failed transaction: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports any local backup files into the database
        /// </summary>
        public async Task<int> ImportLocalBackupsAsync()
        {
            try
            {
                string backupDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuickTechPOS", "FailedTransactions");

                if (!System.IO.Directory.Exists(backupDir))
                    return 0;

                int importedCount = 0;
                foreach (var file in System.IO.Directory.GetFiles(backupDir, "FailedTransaction_*.json"))
                {
                    try
                    {
                        string json = await System.IO.File.ReadAllTextAsync(file);
                        var backupData = JsonSerializer.Deserialize<dynamic>(json);

                        // Create a failed transaction from the backup data
                        var failedTransaction = new FailedTransaction
                        {
                            AttemptedAt = backupData.GetProperty("Timestamp").GetDateTime(),
                            State = FailedTransactionState.Failed,
                            ErrorMessage = backupData.GetProperty("ErrorMessage").GetString(),
                            ErrorDetails = backupData.GetProperty("ErrorDetails").GetString(),
                            FailureComponent = "Unknown (Backup)",
                            RetryCount = 0,
                            CashierId = backupData.GetProperty("CashierId").GetString(),
                            CashierName = backupData.GetProperty("CashierName").GetString(),
                            CustomerName = "Walk-in Customer",
                            TotalAmount = backupData.GetProperty("TotalAmount").GetDecimal(),
                            PaidAmount = 0,
                            TransactionType = TransactionType.Sale,
                            PaymentMethod = "Cash"
                        };

                        // Set cart items from backup
                        var cartItemsJson = backupData.GetProperty("CartItems").GetRawText();
                        failedTransaction.SerializedCartItems = cartItemsJson;

                        // Save to database
                        _dbContext.FailedTransactions.Add(failedTransaction);
                        await _dbContext.SaveChangesAsync();

                        // Move file to processed folder
                        string processedDir = System.IO.Path.Combine(backupDir, "Processed");
                        System.IO.Directory.CreateDirectory(processedDir);
                        string destFile = System.IO.Path.Combine(processedDir, System.IO.Path.GetFileName(file));
                        System.IO.File.Move(file, destFile, true);

                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error importing backup file {file}: {ex.Message}");
                    }
                }

                return importedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing local backups: {ex.Message}");
                return 0;
            }
        }
    }
}