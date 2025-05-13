// File: QuickTechPOS/Models/DrawerTransaction.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a transaction that affects a cash drawer
    /// </summary>
    public class DrawerTransaction
    {
        /// <summary>
        /// Unique identifier for the drawer transaction
        /// </summary>
        [Key]
        public int TransactionId { get; set; }

        /// <summary>
        /// ID of the associated drawer
        /// </summary>
        public int DrawerId { get; set; }

        /// <summary>
        /// Date and time when the transaction occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Type of transaction (Cash Sale, Expense, Cash In, Cash Out, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Transaction amount
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Drawer balance after this transaction
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Additional notes about this transaction
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Action type (Add, Remove, Adjust, etc.)
        /// </summary>
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Description of the transaction
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Reference to associated transaction if applicable
        /// </summary>
        [MaxLength(50)]
        public string TransactionReference { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the transaction has been voided
        /// </summary>
        public bool IsVoided { get; set; }

        /// <summary>
        /// Payment method used (Cash, Card, etc.)
        /// </summary>
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to associated drawer
        /// </summary>
        [ForeignKey("DrawerId")]
        public virtual Drawer? Drawer { get; set; }

        /// <summary>
        /// Gets the formatted amount with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedAmount => $"${Amount:F2}";

        /// <summary>
        /// Gets the formatted balance with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedBalance => $"${Balance:F2}";

        /// <summary>
        /// Gets the formatted timestamp
        /// </summary>
        [NotMapped]
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// Calculates the new balance based on transaction type and amount
        /// </summary>
        /// <param name="currentBalance">The current balance</param>
        /// <param name="transactionType">The type of transaction</param>
        /// <param name="amount">The amount of the transaction</param>
        /// <returns>The new balance after applying the transaction</returns>
        public decimal GetNewBalance(decimal currentBalance, string transactionType, decimal amount)
        {
            switch (transactionType.ToLower())
            {
                case "expense":
                case "supplier payment":
                case "cash out":
                    return currentBalance - amount;
                case "open":
                    return amount;
                case "cash in":
                case "cash sale":
                    return currentBalance + amount;
                default:
                    return currentBalance;
            }
        }
    }
}