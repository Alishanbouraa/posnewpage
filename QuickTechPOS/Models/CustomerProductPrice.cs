// File: QuickTechPOS/Models/CustomerProductPrice.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a special product price for a specific customer
    /// </summary>
    public class CustomerProductPrice
    {
        /// <summary>
        /// Unique identifier for the customer product price
        /// </summary>
        [Key]
        public int CustomerProductPriceId { get; set; }

        /// <summary>
        /// Customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Product identifier
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Special price for this customer-product combination
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Navigation property to the associated customer
        /// </summary>
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        /// <summary>
        /// Navigation property to the associated product
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}