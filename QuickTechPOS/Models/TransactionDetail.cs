using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a detail line item in a transaction
    /// </summary>
    public class TransactionDetail
    {
        /// <summary>
        /// Unique identifier for the transaction detail
        /// </summary>
        [Key]
        public int TransactionDetailId { get; set; }

        /// <summary>
        /// Transaction identifier
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Product identifier
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of the product
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit price of the product
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Purchase price of the product
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Discount amount for this line item
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Discount { get; set; }

        /// <summary>
        /// Total amount for this line item
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }

        /// <summary>
        /// Navigation property to the associated transaction
        /// </summary>
        [ForeignKey("TransactionId")]
        public virtual Transaction Transaction { get; set; }

        /// <summary>
        /// Navigation property to the associated product
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}