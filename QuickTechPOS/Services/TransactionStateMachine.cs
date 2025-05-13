// File: QuickTechPOS/Services/TransactionStateMachine.cs

using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Implements a state machine for transaction processing to ensure atomicity and reliability
    /// </summary>
    public class TransactionStateMachine
    {
        /// <summary>
        /// Defines the possible states of a transaction
        /// </summary>
        public enum TransactionState
        {
            Created,
            ValidatingData,
            RecordingTransaction,
            UpdatingInventory,
            UpdatingDrawer,
            UpdatedCustomerBalance,
            Completed,
            Failed
        }

        /// <summary>
        /// Represents a transition in the transaction state machine
        /// </summary>
        private class StateTransition
        {
            public TransactionState FromState { get; }
            public TransactionState ToState { get; }
            public Func<TransactionContext, Task<bool>> Action { get; }

            public StateTransition(TransactionState fromState, TransactionState toState, Func<TransactionContext, Task<bool>> action)
            {
                FromState = fromState;
                ToState = toState;
                Action = action;
            }
        }

        /// <summary>
        /// Context holding all data needed for transaction processing
        /// </summary>
        public class TransactionContext
        {
            public Transaction Transaction { get; set; }
            public List<CartItem> CartItems { get; set; }
            public Employee Cashier { get; set; }
            public Drawer Drawer { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public string PaymentMethod { get; set; }
            public decimal PaidAmount { get; set; }
            public bool AddToCustomerDebt { get; set; }
            public decimal AmountToDebt { get; set; }
            public string ErrorMessage { get; set; }
            public string FailureComponent { get; set; }
            public TransactionState CurrentState { get; set; } = TransactionState.Created;
            public Exception LastException { get; set; }
            public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

            // Transaction log for audit and recovery
            public List<string> TransactionLog { get; } = new List<string>();

            public void LogTransition(TransactionState fromState, TransactionState toState, string message)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {fromState} -> {toState}: {message}";
                TransactionLog.Add(logEntry);
                Console.WriteLine(logEntry);
            }
        }

        private readonly List<StateTransition> _transitions = new List<StateTransition>();
        private readonly ProductService _productService;
        private readonly DrawerService _drawerService;
        private readonly CustomerService _customerService;
        private readonly DatabaseContext _dbContext;

        public TransactionStateMachine(ProductService productService, DrawerService drawerService, CustomerService customerService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _drawerService = drawerService ?? throw new ArgumentNullException(nameof(drawerService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);

            // Define the state transitions
            _transitions.Add(new StateTransition(TransactionState.Created, TransactionState.ValidatingData, ValidateTransactionDataAsync));
            _transitions.Add(new StateTransition(TransactionState.ValidatingData, TransactionState.RecordingTransaction, RecordTransactionAsync));
            _transitions.Add(new StateTransition(TransactionState.RecordingTransaction, TransactionState.UpdatingInventory, UpdateInventoryAsync));
            _transitions.Add(new StateTransition(TransactionState.UpdatingInventory, TransactionState.UpdatingDrawer, UpdateDrawerAsync));
            _transitions.Add(new StateTransition(TransactionState.UpdatingDrawer, TransactionState.UpdatedCustomerBalance, UpdateCustomerBalanceAsync));
            _transitions.Add(new StateTransition(TransactionState.UpdatedCustomerBalance, TransactionState.Completed, CompleteTransactionAsync));
        }

        /// <summary>
        /// Executes the transaction state machine to process a transaction
        /// </summary>
        /// <param name="context">The transaction context containing all necessary data</param>
        /// <returns>A tuple containing success status, error message if any, and the completed transaction if successful</returns>
        public async Task<(bool Success, string ErrorMessage, Transaction Transaction)> ExecuteAsync(TransactionContext context)
        {
            // Initial validation
            if (context == null)
                return (false, "Transaction context cannot be null", null);

            if (context.CartItems == null || context.CartItems.Count == 0)
                return (false, "Cart items cannot be empty", null);

            if (context.Cashier == null)
                return (false, "Cashier information is required", null);

            // Begin transaction processing
            context.LogTransition(TransactionState.Created, TransactionState.Created, "Starting transaction processing");

            // Database transaction for atomicity
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                while (context.CurrentState != TransactionState.Completed &&
                       context.CurrentState != TransactionState.Failed)
                {
                    // Find the appropriate transition from the current state
                    var transition = _transitions.Find(t => t.FromState == context.CurrentState);

                    if (transition == null)
                    {
                        context.ErrorMessage = $"No transition defined from state {context.CurrentState}";
                        context.CurrentState = TransactionState.Failed;
                        context.LogTransition(context.CurrentState, TransactionState.Failed, context.ErrorMessage);
                        break;
                    }

                    // Execute the transition action
                    TransactionState previousState = context.CurrentState;
                    bool success = false;

                    try
                    {
                        success = await transition.Action(context);
                    }
                    catch (Exception ex)
                    {
                        context.LastException = ex;
                        context.ErrorMessage = $"Error during {context.CurrentState}: {ex.Message}";
                        context.CurrentState = TransactionState.Failed;
                        context.LogTransition(previousState, TransactionState.Failed, context.ErrorMessage);
                        break;
                    }

                    if (success)
                    {
                        // Transition to the next state
                        TransactionState nextState = transition.ToState;
                        context.LogTransition(context.CurrentState, nextState, "Transition successful");
                        context.CurrentState = nextState;
                    }
                    else
                    {
                        // Transition failed, move to Failed state
                        context.CurrentState = TransactionState.Failed;
                        context.LogTransition(previousState, TransactionState.Failed, context.ErrorMessage ?? "Transition failed");
                        break;
                    }
                }

                // If we reached Completed state, commit the transaction
                if (context.CurrentState == TransactionState.Completed)
                {
                    await dbTransaction.CommitAsync();
                    return (true, null, context.Transaction);
                }
                else
                {
                    // Otherwise rollback
                    await dbTransaction.RollbackAsync();
                    return (false, context.ErrorMessage, null);
                }
            }
            catch (Exception ex)
            {
                // Unexpected error
                await dbTransaction.RollbackAsync();
                string errorMessage = $"Unexpected error during transaction processing: {ex.Message}";
                context.LogTransition(context.CurrentState, TransactionState.Failed, errorMessage);
                return (false, errorMessage, null);
            }
        }

        #region State Transition Actions

        private async Task<bool> ValidateTransactionDataAsync(TransactionContext context)
        {
            try
            {
                // Validate drawer
                if (context.Drawer == null)
                {
                    context.ErrorMessage = "No open drawer found";
                    context.FailureComponent = "Drawer";
                    return false;
                }

                if (context.Drawer.Status != "Open")
                {
                    context.ErrorMessage = "Drawer is not open";
                    context.FailureComponent = "Drawer";
                    return false;
                }

                // Validate cart items
                foreach (var item in context.CartItems)
                {
                    if (item.Product == null || item.Product.ProductId <= 0)
                    {
                        context.ErrorMessage = "Invalid product in cart";
                        context.FailureComponent = "Inventory";
                        return false;
                    }

                    // Validate stock availability
                    var product = await _productService.GetProductByIdAsync(item.Product.ProductId);
                    if (product == null)
                    {
                        context.ErrorMessage = $"Product with ID {item.Product.ProductId} not found";
                        context.FailureComponent = "Inventory";
                        return false;
                    }

                    if (item.IsBox)
                    {
                        // Check box stock
                        if (product.NumberOfBoxes < Math.Floor(item.Quantity))
                        {
                            context.ErrorMessage = $"Insufficient box stock for {product.Name}. Available: {product.NumberOfBoxes}, Required: {Math.Floor(item.Quantity)}";
                            context.FailureComponent = "Inventory";
                            return false;
                        }
                    }
                    else
                    {
                        // Check individual item stock
                        if (product.CurrentStock < item.Quantity)
                        {
                            context.ErrorMessage = $"Insufficient stock for {product.Name}. Available: {product.CurrentStock}, Required: {item.Quantity}";
                            context.FailureComponent = "Inventory";
                            return false;
                        }
                    }
                }

                // Validate payment
                if (context.PaidAmount < 0)
                {
                    context.ErrorMessage = "Payment amount cannot be negative";
                    context.FailureComponent = "Payment";
                    return false;
                }

                decimal totalAmount = 0;
                foreach (var item in context.CartItems)
                {
                    totalAmount += item.Total;
                }

                if (context.AddToCustomerDebt && context.AmountToDebt > 0)
                {
                    // Check if we can add debt to this customer
                    if (context.CustomerId <= 0 || context.CustomerName == "Walk-in Customer")
                    {
                        context.ErrorMessage = "Cannot add debt to a walk-in customer";
                        context.FailureComponent = "Customer";
                        return false;
                    }

                    var customer = await _customerService.GetByIdAsync(context.CustomerId);
                    if (customer == null)
                    {
                        context.ErrorMessage = $"Customer with ID {context.CustomerId} not found";
                        context.FailureComponent = "Customer";
                        return false;
                    }
                }
                else
                {
                    // If not adding to debt, ensure payment covers the total
                    if (context.PaidAmount < totalAmount)
                    {
                        context.ErrorMessage = "Payment amount is less than total amount";
                        context.FailureComponent = "Payment";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error validating transaction data: {ex.Message}";
                context.FailureComponent = "Validation";
                context.LastException = ex;
                return false;
            }
        }

        private async Task<bool> RecordTransactionAsync(TransactionContext context)
        {
            try
            {
                decimal totalAmount = 0;
                foreach (var item in context.CartItems)
                {
                    totalAmount += item.Total;
                }

                var transaction = new Transaction
                {
                    CustomerId = context.CustomerId > 0 ? context.CustomerId : null,
                    CustomerName = context.CustomerName ?? "Walk-in Customer",
                    TotalAmount = totalAmount,
                    PaidAmount = context.PaidAmount,
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.Sale,
                    Status = TransactionStatus.Completed,
                    PaymentMethod = context.PaymentMethod ?? "Cash",
                    CashierId = context.Cashier.EmployeeId.ToString(),
                    CashierName = context.Cashier.FullName ?? "Unknown",
                    CashierRole = context.Cashier.Role ?? "Cashier"
                };

                _dbContext.Transactions.Add(transaction);
                await _dbContext.SaveChangesAsync();

                // Record transaction details
                foreach (var item in context.CartItems)
                {
                    var detail = new TransactionDetail
                    {
                        TransactionId = transaction.TransactionId,
                        ProductId = item.Product.ProductId,
                        Quantity = item.Quantity > 0 ? item.Quantity : 1,
                        UnitPrice = item.UnitPrice >= 0 ? item.UnitPrice : 0,
                        PurchasePrice = item.Product.PurchasePrice >= 0 ? item.Product.PurchasePrice : 0,
                        Discount = item.Discount >= 0 ? item.Discount : 0,
                        Total = item.Total >= 0 ? item.Total : (item.Quantity * item.UnitPrice)
                    };

                    _dbContext.TransactionDetails.Add(detail);
                }

                await _dbContext.SaveChangesAsync();

                // Store the transaction in the context for later use
                context.Transaction = transaction;
                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error recording transaction: {ex.Message}";
                context.FailureComponent = "Database";
                context.LastException = ex;
                return false;
            }
        }

        private async Task<bool> UpdateInventoryAsync(TransactionContext context)
        {
            try
            {
                foreach (var item in context.CartItems)
                {
                    try
                    {
                        if (item.IsBox)
                        {
                            // Update box inventory
                            bool boxStockUpdated = await _productService.UpdateBoxStockAsync(item.Product.ProductId, item.Quantity);
                            if (!boxStockUpdated)
                            {
                                context.ErrorMessage = $"Failed to update box stock for product {item.Product.Name}";
                                context.FailureComponent = "Inventory";
                                return false;
                            }
                        }
                        else
                        {
                            // Update individual item inventory
                            bool stockUpdated = await _productService.UpdateStockAsync(item.Product.ProductId, item.Quantity);
                            if (!stockUpdated)
                            {
                                context.ErrorMessage = $"Failed to update stock for product {item.Product.Name}";
                                context.FailureComponent = "Inventory";
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        context.ErrorMessage = $"Error updating inventory for product {item.Product.Name}: {ex.Message}";
                        context.FailureComponent = "Inventory";
                        context.LastException = ex;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error updating inventory: {ex.Message}";
                context.FailureComponent = "Inventory";
                context.LastException = ex;
                return false;
            }
        }

        private async Task<bool> UpdateDrawerAsync(TransactionContext context)
        {
            try
            {
                if (context.Drawer == null || context.Drawer.Status != "Open")
                {
                    context.ErrorMessage = "No open drawer found for updating";
                    context.FailureComponent = "Drawer";
                    return false;
                }

                decimal totalAmount = context.Transaction.TotalAmount;

                // Create drawer transaction
                var drawerTransaction = new DrawerTransaction
                {
                    DrawerId = context.Drawer.DrawerId,
                    Timestamp = DateTime.Now,
                    Type = "Cash Sale",
                    Amount = totalAmount,
                    Balance = context.Drawer.CurrentBalance + totalAmount,
                    ActionType = "Sale",
                    Description = $"Sale Transaction #{context.Transaction.TransactionId}",
                    TransactionReference = context.Transaction.TransactionId.ToString(),
                    IsVoided = false,
                    PaymentMethod = context.Transaction.PaymentMethod
                };

                _dbContext.DrawerTransactions.Add(drawerTransaction);
                await _dbContext.SaveChangesAsync();

                // Update drawer with transaction amount - receives the updated drawer
                Drawer updatedDrawer;
                try
                {
                    updatedDrawer = await _drawerService.UpdateDrawerTransactionsAsync(
                        context.Drawer.DrawerId,
                        totalAmount, // Sales amount
                        0,          // No expenses
                        0           // No supplier payments
                    );
                }
                catch (Exception ex)
                {
                    context.ErrorMessage = $"Failed to update drawer with transaction amount: {ex.Message}";
                    context.FailureComponent = "Drawer";
                    return false;
                }

                if (updatedDrawer == null)
                {
                    context.ErrorMessage = "Failed to update drawer with transaction amount - returned null";
                    context.FailureComponent = "Drawer";
                    return false;
                }

                // Update the drawer in the context with the updated drawer
                context.Drawer = updatedDrawer;
                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error updating drawer: {ex.Message}";
                context.FailureComponent = "Drawer";
                context.LastException = ex;
                return false;
            }
        }

        private async Task<bool> UpdateCustomerBalanceAsync(TransactionContext context)
        {
            try
            {
                // If adding to customer debt is not required, skip this step
                if (!context.AddToCustomerDebt || context.AmountToDebt <= 0 ||
                    context.CustomerId <= 0 || context.CustomerName == "Walk-in Customer")
                {
                    return true;
                }

                // Update customer balance
                bool balanceUpdated = await _customerService.UpdateCustomerBalanceAsync(
                    context.CustomerId, context.AmountToDebt);

                if (!balanceUpdated)
                {
                    context.ErrorMessage = "Failed to update customer balance";
                    context.FailureComponent = "Customer";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error updating customer balance: {ex.Message}";
                context.FailureComponent = "Customer";
                context.LastException = ex;
                return false;
            }
        }

        private async Task<bool> CompleteTransactionAsync(TransactionContext context)
        {
            try
            {
                // This is a final state where we can perform any last operations
                // such as logging or notifications

                // Log the completion
                context.LogTransition(TransactionState.UpdatedCustomerBalance, TransactionState.Completed,
                    $"Transaction #{context.Transaction.TransactionId} completed successfully");

                return true;
            }
            catch (Exception ex)
            {
                context.ErrorMessage = $"Error during transaction completion: {ex.Message}";
                context.FailureComponent = "Completion";
                context.LastException = ex;
                return false;
            }
        }

        #endregion
    }
}