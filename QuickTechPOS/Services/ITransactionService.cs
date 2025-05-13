// File: QuickTechPOS/Services/ITransactionService.cs

using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Interface for transaction service operations
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Creates a new transaction using a state machine for reliable processing
        /// </summary>
        /// <param name="items">Cart items to be purchased</param>
        /// <param name="paidAmount">Amount paid by the customer</param>
        /// <param name="cashier">Employee processing the transaction</param>
        /// <param name="paymentMethod">Method of payment (default: Cash)</param>
        /// <param name="customerName">Name of the customer (default: Walk-in Customer)</param>
        /// <param name="customerId">ID of the customer (default: 0 for walk-in)</param>
        /// <returns>The completed transaction if successful</returns>
        Task<Transaction> CreateTransactionAsync(
            List<CartItem> items,
            decimal paidAmount,
            Employee cashier,
            string paymentMethod = "Cash",
            string customerName = "Walk-in Customer",
            int customerId = 0);

        /// <summary>
        /// Gets a transaction with its details
        /// </summary>
        /// <param name="transactionId">ID of the transaction to retrieve</param>
        /// <returns>The transaction with details if found, otherwise null</returns>
        Task<Transaction> GetTransactionWithDetailsAsync(int transactionId);

        /// <summary>
        /// Updates an existing transaction
        /// </summary>
        /// <param name="transaction">Transaction with updated information</param>
        /// <param name="details">Updated transaction details</param>
        /// <returns>True if update successful, otherwise false</returns>
        Task<bool> UpdateTransactionAsync(Transaction transaction, List<TransactionDetail> details);

        /// <summary>
        /// Gets the next transaction ID
        /// </summary>
        /// <param name="currentTransactionId">Current transaction ID</param>
        /// <returns>The next transaction ID if found, otherwise null</returns>
        Task<int?> GetNextTransactionIdAsync(int currentTransactionId);

        /// <summary>
        /// Gets the previous transaction ID
        /// </summary>
        /// <param name="currentTransactionId">Current transaction ID</param>
        /// <returns>The previous transaction ID if found, otherwise null</returns>
        Task<int?> GetPreviousTransactionIdAsync(int currentTransactionId);
    }
}