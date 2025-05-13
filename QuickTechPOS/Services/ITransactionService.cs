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
        /// Creates a new transaction
        /// </summary>
        Task<Transaction> CreateTransactionAsync(
            List<CartItem> items,
            decimal paidAmount,
            Employee cashier,
            string paymentMethod,
            string customerName,
            int customerId);

        /// <summary>
        /// Gets a transaction with its details
        /// </summary>
        Task<Transaction> GetTransactionWithDetailsAsync(int transactionId);

        /// <summary>
        /// Updates an existing transaction
        /// </summary>
        Task<bool> UpdateTransactionAsync(Transaction transaction, List<TransactionDetail> details);

        /// <summary>
        /// Gets the next transaction ID
        /// </summary>
        Task<int?> GetNextTransactionIdAsync(int currentTransactionId);

        /// <summary>
        /// Gets the previous transaction ID
        /// </summary>
        Task<int?> GetPreviousTransactionIdAsync(int currentTransactionId);
    }
}