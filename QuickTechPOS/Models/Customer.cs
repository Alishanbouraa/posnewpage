// File: QuickTechPOS/Models/Customer.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a customer in the system
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Unique identifier for the customer
        /// </summary>
        [Key]
        public int CustomerId { get; set; }

        /// <summary>
        /// Customer name
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Customer phone number
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        /// <summary>
        /// Customer email address
        /// </summary>
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Customer address
        /// </summary>
        [MaxLength(500)]
        public string Address { get; set; }

        /// <summary>
        /// Indicates if the customer is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date and time when the customer was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Date and time when the customer was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Customer balance
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Gets the formatted balance with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedBalance => $"${Balance:F2}";
    }
}