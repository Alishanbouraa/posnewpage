// File: QuickTechPOS/Models/Transaction.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuickTechPOS.Models.Enums;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a sales transaction
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Unique identifier for the transaction
        /// </summary>
        [Key]
        public int TransactionId { get; set; }

        /// <summary>
        /// Customer identifier
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Customer name
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Total transaction amount
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Amount paid by the customer
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// Date and time of the transaction
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Type of transaction (e.g., Sale, Return)
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Status of the transaction (e.g., Completed, Pending)
        /// </summary>
        [Column(TypeName = "nvarchar(50)")]
        public TransactionStatus Status { get; set; }

        // Add a string property to help with conversion if needed
        [NotMapped]
        public string StatusString
        {
            get => Status.ToString();
            set
            {
                if (Enum.TryParse<TransactionStatus>(value, true, out var result))
                {
                    Status = result;
                }
                else
                {
                    Status = TransactionStatus.Completed; // Default value
                }
            }
        }

        /// <summary>
        /// Method of payment (e.g., Cash, Credit Card)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Cashier/employee identifier
        /// </summary>
        public string CashierId { get; set; } = string.Empty;

        /// <summary>
        /// Cashier/employee name
        /// </summary>
        public string CashierName { get; set; } = string.Empty;

        /// <summary>
        /// Cashier/employee role
        /// </summary>
        public string CashierRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of transaction details
        /// </summary>
        [NotMapped]
        public virtual ICollection<TransactionDetail> Details { get; set; } = new List<TransactionDetail>();

        /// <summary>
        /// Gets the change amount to be returned to the customer
        /// </summary>
        [NotMapped]
        public decimal ChangeAmount => PaidAmount - TotalAmount;

        /// <summary>
        /// Gets the formatted transaction date
        /// </summary>
        [NotMapped]
        public string FormattedDate => TransactionDate.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// Gets the transaction type as a string
        /// </summary>
        [NotMapped]
        public string TransactionTypeString => TransactionType.ToString();
    }
}