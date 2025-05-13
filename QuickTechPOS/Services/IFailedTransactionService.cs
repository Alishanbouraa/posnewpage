using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Interface for failed transaction service operations
    /// </summary>
    public interface IFailedTransactionService
    {
        /// <summary>
        /// Gets all failed transactions that can be retried
        /// </summary>
        Task<List<FailedTransaction>> GetFailedTransactionsAsync();

        /// <summary>
        /// Gets a failed transaction by ID
        /// </summary>
        Task<FailedTransaction> GetFailedTransactionByIdAsync(int failedTransactionId);

        /// <summary>
        /// Records a failed transaction for later retry
        /// </summary>
        Task<FailedTransaction> RecordFailedTransactionAsync(
            Transaction partialTransaction,
            List<CartItem> cartItems,
            Employee cashier,
            Exception error,
            string component);

        /// <summary>
        /// Attempts to retry a failed transaction
        /// </summary>
        Task<(bool Success, string Message, Transaction Transaction)> RetryTransactionAsync(int failedTransactionId);

        /// <summary>
        /// Cancels a failed transaction
        /// </summary>
        Task<bool> CancelFailedTransactionAsync(int failedTransactionId);

        /// <summary>
        /// Imports any local backup files into the database
        /// </summary>
        Task<int> ImportLocalBackupsAsync();
    }
}