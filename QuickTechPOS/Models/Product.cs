// File: QuickTechPOS/Models/Product.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a product in the inventory
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Unique identifier for the product
        /// </summary>
        [Key]
        public int ProductId { get; set; }

        /// <summary>
        /// Product barcode for scanning
        /// </summary>
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// Product name
        /// </summary>
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the product
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Category identifier
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Price at which the product was purchased
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Price at which the product is sold
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SalePrice { get; set; }

        /// <summary>
        /// Current available stock
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentStock { get; set; }

        /// <summary>
        /// Minimum stock level before reordering
        /// </summary>
        public int MinimumStock { get; set; }

        /// <summary>
        /// Supplier identifier
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Indicates if the product is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date and time when the product was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Product speed or performance indicator
        /// </summary>
        [MaxLength(50)]
        public string? Speed { get; set; }

        /// <summary>
        /// Date and time when the product was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Path to the product image
        /// </summary>
        [MaxLength(500)]
        public string? ImagePath { get; set; }

        /// <summary>
        /// Main stock location identifier
        /// </summary>
        public int? MainStockId { get; set; }

        /// <summary>
        /// Barcode image data
        /// </summary>
        public byte[]? BarcodeImage { get; set; }

        /// <summary>
        /// Box barcode for scanning boxes
        /// </summary>
        [MaxLength(50)]
        public string BoxBarcode { get; set; } = string.Empty;

        /// <summary>
        /// Number of boxes in inventory
        /// </summary>
        public int NumberOfBoxes { get; set; }

        /// <summary>
        /// Number of items per box
        /// </summary>
        public int ItemsPerBox { get; set; } = 1;

        /// <summary>
        /// Purchase price per box
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BoxPurchasePrice { get; set; }

        /// <summary>
        /// Sale price per box
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BoxSalePrice { get; set; }

        /// <summary>
        /// Minimum box stock level before reordering
        /// </summary>
        public int MinimumBoxStock { get; set; }

        /// <summary>
        /// Wholesale price for individual items
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal WholesalePrice { get; set; }

        /// <summary>
        /// Wholesale price per box
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BoxWholesalePrice { get; set; }

        /// <summary>
        /// Gets the formatted sale price with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedSalePrice => $"${SalePrice:F2}";

        /// <summary>
        /// Gets the formatted wholesale price with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedWholesalePrice => $"${WholesalePrice:F2}";

        /// <summary>
        /// Gets the formatted box wholesale price with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedBoxWholesalePrice => $"${BoxWholesalePrice:F2}";

        /// <summary>
        /// Gets the stock status description
        /// </summary>
        [NotMapped]
        public string StockStatus => CurrentStock <= 0 ? "Out of Stock" :
                                    CurrentStock < MinimumStock ? "Low Stock" : "In Stock";

        /// <summary>
        /// Gets the box stock status description
        /// </summary>
        [NotMapped]
        public string BoxStockStatus => NumberOfBoxes <= 0 ? "Out of Stock" :
                                        NumberOfBoxes < MinimumBoxStock ? "Low Stock" : "In Stock";

        /// <summary>
        /// Gets the total stock (individual items plus items in boxes)
        /// </summary>
        [NotMapped]
        public decimal TotalStockCount => CurrentStock + (NumberOfBoxes * ItemsPerBox);

        /// <summary>
        /// Gets the formatted box sale price with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedBoxSalePrice => $"${BoxSalePrice:F2}";
    }
}