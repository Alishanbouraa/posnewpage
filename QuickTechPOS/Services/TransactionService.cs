// File: QuickTechPOS/Services/TransactionService.cs

using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ProductService _productService;
        private readonly DrawerService _drawerService;
        private readonly CustomerService _customerService;
        private readonly TransactionStateMachine _stateMachine;
        private readonly Func<FailedTransactionService> _failedTransactionServiceFactory;
        private FailedTransactionService _lazyFailedTransactionService;

        public TransactionService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _productService = new ProductService();
            _drawerService = new DrawerService();
            _customerService = new CustomerService();
            _stateMachine = new TransactionStateMachine(_productService, _drawerService, _customerService);
            _failedTransactionServiceFactory = () => new FailedTransactionService(this);
        }

        // Constructor with dependency injection for FailedTransactionService
        public TransactionService(FailedTransactionService failedTransactionService)
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _productService = new ProductService();
            _drawerService = new DrawerService();
            _customerService = new CustomerService();
            _stateMachine = new TransactionStateMachine(_productService, _drawerService, _customerService);
            _lazyFailedTransactionService = failedTransactionService;
            _failedTransactionServiceFactory = null;
        }

        // Lazy loading property for FailedTransactionService
        private FailedTransactionService FailedTransactionService
        {
            get
            {
                if (_lazyFailedTransactionService == null && _failedTransactionServiceFactory != null)
                {
                    _lazyFailedTransactionService = _failedTransactionServiceFactory();
                }
                return _lazyFailedTransactionService;
            }
        }

        public async Task<Transaction> CreateTransactionAsync(
            List<CartItem> items,
            decimal paidAmount,
            Employee cashier,
            string paymentMethod = "Cash",
            string customerName = "Walk-in Customer",
            int customerId = 0)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting transaction creation at: {DateTime.Now}");

            // Get the current drawer
            var drawer = await _drawerService.GetOpenDrawerAsync(cashier.EmployeeId.ToString());
            if (drawer == null)
            {
                string errorMessage = "No open drawer found for this cashier";
                Console.WriteLine(errorMessage);
                throw new Exception(errorMessage);
            }

            // Calculate if we need to add to customer debt
            decimal totalAmount = items.Sum(item => item.Total);
            bool addToCustomerDebt = paidAmount < totalAmount && customerId > 0 && customerName != "Walk-in Customer";
            decimal amountToDebt = addToCustomerDebt ? totalAmount - paidAmount : 0;

            // Prepare transaction context
            var context = new TransactionStateMachine.TransactionContext
            {
                CartItems = items,
                Cashier = cashier,
                Drawer = drawer,
                CustomerId = customerId,
                CustomerName = customerName,
                PaymentMethod = paymentMethod,
                PaidAmount = paidAmount,
                AddToCustomerDebt = addToCustomerDebt,
                AmountToDebt = amountToDebt
            };

            try
            {
                // Execute the transaction state machine
                var result = await _stateMachine.ExecuteAsync(context);

                if (result.Success)
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Transaction completed successfully in {stopwatch.ElapsedMilliseconds}ms");
                    return result.Transaction;
                }
                else
                {
                    Console.WriteLine($"ERROR in CreateTransactionAsync: {result.ErrorMessage}");

                    // Record failed transaction for later retry
                    try
                    {
                        await FailedTransactionService.RecordFailedTransactionAsync(
                            context.Transaction,
                            items,
                            cashier,
                            context.LastException ?? new Exception(result.ErrorMessage),
                            context.FailureComponent ?? "Unknown");
                    }
                    catch (Exception recordEx)
                    {
                        Console.WriteLine($"Error recording failed transaction: {recordEx.Message}");
                    }

                    throw new Exception($"Error during checkout: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // This catches any unexpected errors not handled by the state machine
                Console.WriteLine($"Unexpected error in CreateTransactionAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                // Determine which component failed if not already set
                string failureComponent = context.FailureComponent ?? "Unknown";
                if (ex.Message.Contains("inventory") || ex.Message.Contains("stock"))
                {
                    failureComponent = "Inventory";
                }
                else if (ex.Message.Contains("drawer"))
                {
                    failureComponent = "Drawer";
                }
                else if (ex is DbUpdateException)
                {
                    failureComponent = "Database";
                }

                // Record the failed transaction for later retry
                try
                {
                    await FailedTransactionService.RecordFailedTransactionAsync(
                        context.Transaction,
                        items,
                        cashier,
                        ex,
                        failureComponent);
                }
                catch (Exception recordEx)
                {
                    Console.WriteLine($"Error recording failed transaction: {recordEx.Message}");
                }

                throw new Exception($"Error during checkout: {ex.Message}. Inner exception: {ex.InnerException?.Message}", ex);
            }
        }

        public async Task<Transaction> GetTransactionWithDetailsAsync(int transactionId)
        {
            try
            {
                Console.WriteLine($"Retrieving transaction #{transactionId} with details...");

                // First, get the transaction entity
                var transaction = await _dbContext.Transactions.FindAsync(transactionId);

                if (transaction == null)
                {
                    Console.WriteLine($"Transaction #{transactionId} not found");
                    return null;
                }

                Console.WriteLine($"Transaction found. Status: {transaction.Status}, Type: {transaction.TransactionType}");

                // Ensure the Status property is correctly handled
                if (transaction.Status == null)
                {
                    // Handle null status
                    Console.WriteLine("Transaction has null status, setting to Completed");
                    transaction.Status = TransactionStatus.Completed;
                }

                // Get associated transaction details
                var details = await _dbContext.TransactionDetails
                    .Where(d => d.TransactionId == transactionId)
                    .ToListAsync();

                Console.WriteLine($"Found {details.Count} detail records for transaction #{transactionId}");

                // Assign the details to the transaction
                transaction.Details = details;

                return transaction;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTransactionWithDetailsAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction, List<TransactionDetail> details)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting transaction update at: {DateTime.Now}");

            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingTransaction = await _dbContext.Transactions.FindAsync(transaction.TransactionId);
                if (existingTransaction == null)
                {
                    Console.WriteLine($"Transaction #{transaction.TransactionId} not found for update");
                    return false;
                }

                existingTransaction.CustomerId = transaction.CustomerId;
                existingTransaction.CustomerName = transaction.CustomerName;
                existingTransaction.TotalAmount = transaction.TotalAmount;
                existingTransaction.Status = transaction.Status;

                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Updated transaction #{transaction.TransactionId}");

                var existingDetails = await _dbContext.TransactionDetails
                    .Where(d => d.TransactionId == transaction.TransactionId)
                    .ToListAsync();

                _dbContext.TransactionDetails.RemoveRange(existingDetails);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Removed {existingDetails.Count} existing transaction details");

                foreach (var detail in details)
                {
                    _dbContext.TransactionDetails.Add(detail);
                    Console.WriteLine($"Added transaction detail for product: {detail.ProductId}, Qty: {detail.Quantity}, Total: {detail.Total:C2}");
                }

                await _dbContext.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                stopwatch.Stop();
                Console.WriteLine($"Transaction update completed successfully in {stopwatch.ElapsedMilliseconds}ms");

                return true;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                Console.WriteLine($"ERROR in UpdateTransactionAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return false;
            }
        }

        public async Task<int?> GetNextTransactionIdAsync(int currentTransactionId)
        {
            try
            {
                // Validate input
                if (currentTransactionId <= 0)
                {
                    Console.WriteLine("GetNextTransactionIdAsync called with invalid ID: " + currentTransactionId);
                    return null;
                }

                Console.WriteLine($"Looking for next transaction after ID: {currentTransactionId}");

                // Find transaction with ID greater than current, ordered by ID (ascending)
                var nextTransaction = await _dbContext.Transactions
                    .Where(t => t.TransactionId > currentTransactionId)
                    .OrderBy(t => t.TransactionId)
                    .FirstOrDefaultAsync();

                if (nextTransaction != null)
                {
                    Console.WriteLine($"Found next transaction ID: {nextTransaction.TransactionId}");
                    return nextTransaction.TransactionId;
                }
                else
                {
                    Console.WriteLine("No next transaction found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetNextTransactionIdAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        public async Task<int?> GetPreviousTransactionIdAsync(int currentTransactionId)
        {
            try
            {
                // Validate input
                if (currentTransactionId <= 0)
                {
                    Console.WriteLine("GetPreviousTransactionIdAsync called with invalid ID: " + currentTransactionId);
                    return null;
                }

                Console.WriteLine($"Looking for previous transaction before ID: {currentTransactionId}");

                // Find transaction with ID less than current, ordered by ID (descending)
                var previousTransaction = await _dbContext.Transactions
                    .Where(t => t.TransactionId < currentTransactionId)
                    .OrderByDescending(t => t.TransactionId)
                    .FirstOrDefaultAsync();

                if (previousTransaction != null)
                {
                    Console.WriteLine($"Found previous transaction ID: {previousTransaction.TransactionId}");
                    return previousTransaction.TransactionId;
                }
                else
                {
                    Console.WriteLine("No previous transaction found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPreviousTransactionIdAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
    }
}