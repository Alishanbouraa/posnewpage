using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    public class TransactionService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ProductService _productService;

        public TransactionService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _productService = new ProductService();
        }

        // File: QuickTechPOS/Services/TransactionService.cs - Update the CreateTransactionAsync method

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

            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                decimal totalAmount = items.Sum(item => item.Total);
                Console.WriteLine($"Calculated total amount: {totalAmount:C2} from {items.Count} items");

                var newTransaction = new Transaction
                {
                    CustomerId = customerId == 0 ? null : customerId,
                    CustomerName = customerName ?? "Walk-in Customer",
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.Sale,
                    Status = TransactionStatus.Completed,
                    PaymentMethod = paymentMethod ?? "Cash",
                    CashierId = cashier.EmployeeId.ToString(),
                    CashierName = cashier.FullName ?? "Unknown",
                    CashierRole = cashier.Role ?? "Cashier"
                };

                _dbContext.Transactions.Add(newTransaction);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Created transaction with ID: {newTransaction.TransactionId}");

                foreach (var item in items)
                {
                    if (item.Product == null || item.Product.ProductId <= 0)
                    {
                        Console.WriteLine("Warning: Skipping invalid product in transaction detail");
                        continue;
                    }

                    var detail = new TransactionDetail
                    {
                        TransactionId = newTransaction.TransactionId,
                        ProductId = item.Product.ProductId,
                        Quantity = item.Quantity > 0 ? item.Quantity : 1,
                        UnitPrice = item.UnitPrice >= 0 ? item.UnitPrice : 0,
                        PurchasePrice = item.Product.PurchasePrice >= 0 ? item.Product.PurchasePrice : 0,
                        Discount = item.Discount >= 0 ? item.Discount : 0,
                        Total = item.Total >= 0 ? item.Total : (item.Quantity * item.UnitPrice)
                    };

                    _dbContext.TransactionDetails.Add(detail);
                    Console.WriteLine($"Added transaction detail for product: {item.Product.Name}, Qty: {item.Quantity}, Total: {item.Total:C2}");

                    try
                    {
                        // Check if the item is a box and update inventory accordingly
                        if (item.IsBox)
                        {
                            // If it's a box, update box inventory
                            bool boxStockUpdated = await _productService.UpdateBoxStockAsync(item.Product.ProductId, item.Quantity);
                            if (!boxStockUpdated)
                            {
                                Console.WriteLine($"Warning: Failed to update box stock for product {item.Product.ProductId}");
                            }
                            else
                            {
                                Console.WriteLine($"Updated box inventory for product {item.Product.ProductId}, reduced {item.Quantity} boxes");
                            }
                        }
                        else
                        {
                            // If it's an individual item, update regular stock
                            bool stockUpdated = await _productService.UpdateStockAsync(item.Product.ProductId, item.Quantity);
                            if (!stockUpdated)
                            {
                                Console.WriteLine($"Warning: Failed to update stock for product {item.Product.ProductId}");
                            }
                        }
                    }
                    catch (Exception stockEx)
                    {
                        Console.WriteLine($"Stock update error for product {item.Product.ProductId}: {stockEx.Message}");
                    }
                }

                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"Database update error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {dbEx.InnerException.Message}");
                    }

                    var validationErrors = dbEx.Entries
                        .SelectMany(entry => entry.Entity.GetType().GetProperties())
                        .Where(property => property.GetCustomAttributes(typeof(RequiredAttribute), false).Any())
                        .Select(property => property.Name);

                    if (validationErrors.Any())
                    {
                        Console.WriteLine($"Validation errors on properties: {string.Join(", ", validationErrors)}");
                    }

                    throw;
                }

                try
                {
                    var drawerService = new DrawerService();
                    var openDrawer = await drawerService.GetOpenDrawerAsync(cashier.EmployeeId.ToString());

                    if (openDrawer != null)
                    {
                        var drawerTransaction = new DrawerTransaction
                        {
                            DrawerId = openDrawer.DrawerId,
                            Timestamp = DateTime.Now,
                            Type = "Cash Sale",
                            Amount = totalAmount,
                            Balance = openDrawer.CurrentBalance + totalAmount,
                            ActionType = "Sale",
                            Description = $"Sale Transaction #{newTransaction.TransactionId}",
                            TransactionReference = newTransaction.TransactionId.ToString(),
                            IsVoided = false,
                            PaymentMethod = paymentMethod
                        };

                        _dbContext.DrawerTransactions.Add(drawerTransaction);
                        await _dbContext.SaveChangesAsync();

                        Console.WriteLine($"Created drawer transaction for sale: {drawerTransaction.TransactionId}");
                    }
                }
                catch (Exception drawerEx)
                {
                    Console.WriteLine($"Error creating drawer transaction: {drawerEx.Message}");
                }

                await dbTransaction.CommitAsync();

                stopwatch.Stop();
                Console.WriteLine($"Transaction completed successfully in {stopwatch.ElapsedMilliseconds}ms");

                return newTransaction;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                Console.WriteLine($"ERROR in CreateTransactionAsync: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
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