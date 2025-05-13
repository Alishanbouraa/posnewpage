using QuickTechPOS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a transaction that failed to complete and can be retried
    /// </summary>
    public class FailedTransaction
    {
        /// <summary>
        /// Unique identifier for the failed transaction
        /// </summary>
        [Key]
        public int FailedTransactionId { get; set; }

        /// <summary>
        /// Original transaction ID if it was partially created
        /// </summary>
        public int? OriginalTransactionId { get; set; }

        /// <summary>
        /// Date and time when the transaction was attempted
        /// </summary>
        public DateTime AttemptedAt { get; set; }

        /// <summary>
        /// Current state of the failed transaction
        /// </summary>
        [Column(TypeName = "nvarchar(50)")]
        public FailedTransactionState State { get; set; }

        /// <summary>
        /// Error message that occurred during the transaction
        /// </summary>
        [MaxLength(1000)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Detailed technical information about the error
        /// </summary>
        [MaxLength(4000)]
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Component where the failure occurred (e.g., "Inventory", "Payment", "Database")
        /// </summary>
        [MaxLength(100)]
        public string FailureComponent { get; set; }

        /// <summary>
        /// Number of retry attempts that have been made
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Last time a retry was attempted
        /// </summary>
        public DateTime? LastRetryAt { get; set; }

        /// <summary>
        /// The cashier who initiated the transaction
        /// </summary>
        public string CashierId { get; set; }

        /// <summary>
        /// Cashier name
        /// </summary>
        [MaxLength(100)]
        public string CashierName { get; set; }

        /// <summary>
        /// Customer ID if provided
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Customer name
        /// </summary>
        [MaxLength(200)]
        public string CustomerName { get; set; }

        /// <summary>
        /// Total amount of the transaction
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Amount paid by the customer
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// Type of transaction (e.g., Sale, Return)
        /// </summary>
        [Column(TypeName = "nvarchar(50)")]
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Method of payment (e.g., Cash, Credit Card)
        /// </summary>
        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Serialized cart items
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string SerializedCartItems { get; set; }

        /// <summary>
        /// Drawer ID where the transaction was attempted
        /// </summary>
        public int? DrawerId { get; set; }

        /// <summary>
        /// Navigation property to original transaction if it exists
        /// </summary>
        [ForeignKey("OriginalTransactionId")]
        public virtual Transaction OriginalTransaction { get; set; }

        // Cache for cart items to avoid repeated deserialization
        private List<CartItem> _cartItems;

        /// <summary>
        /// Gets cart items deserialized from JSON, with caching to prevent repeated deserialization
        /// </summary>
        [NotMapped]
        public List<CartItem> CartItems
        {
            get
            {
                // Return cached items if available
                if (_cartItems != null)
                    return _cartItems;

                if (string.IsNullOrEmpty(SerializedCartItems))
                    return _cartItems = new List<CartItem>();

                try
                {
                    // Create serializer options to handle object references
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        MaxDepth = 64 // Increase max depth if needed
                    };

                    // Use a simplified cart item class for deserialization
                    var simplifiedItems = JsonSerializer.Deserialize<List<SimpleCartItem>>(SerializedCartItems, options);

                    // Convert to real cart items
                    _cartItems = new List<CartItem>();
                    foreach (var item in simplifiedItems)
                    {
                        _cartItems.Add(new CartItem
                        {
                            Product = new Product
                            {
                                ProductId = item.ProductId,
                                Name = item.ProductName,
                                SalePrice = item.UnitPrice,
                                PurchasePrice = item.PurchasePrice
                            },
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Discount = item.Discount,
                            DiscountType = item.DiscountType,
                            IsBox = item.IsBox,
                            IsWholesale = item.IsWholesale
                        });
                    }

                    return _cartItems;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing cart items: {ex.Message}");
                    return _cartItems = new List<CartItem>();
                }
            }
        }

        /// <summary>
        /// Gets the formatted transaction date
        /// </summary>
        [NotMapped]
        public string FormattedAttemptDate => AttemptedAt.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// Gets a user-friendly description of the error
        /// </summary>
        [NotMapped]
        public string UserFriendlyError
        {
            get
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                    return "Unknown error";

                // Return first 100 chars of error message without technical details
                string message = ErrorMessage.Replace("Exception:", "").Trim();
                return message.Length > 100 ? message.Substring(0, 97) + "..." : message;
            }
        }

        /// <summary>
        /// Gets whether this transaction can be retried
        /// </summary>
        [NotMapped]
        public bool CanRetry => State == FailedTransactionState.Failed && RetryCount < 5;

        /// <summary>
        /// Sets the cart items by serializing to JSON
        /// </summary>
        /// <param name="items">The cart items to serialize</param>
        public void SetCartItems(List<CartItem> items)
        {
            try
            {
                // Convert real cart items to simplified version for serialization
                var simplifiedItems = new List<SimpleCartItem>();

                foreach (var item in items)
                {
                    simplifiedItems.Add(new SimpleCartItem
                    {
                        ProductId = item.Product?.ProductId ?? 0,
                        ProductName = item.Product?.Name ?? "Unknown Product",
                        UnitPrice = item.UnitPrice,
                        PurchasePrice = item.Product?.PurchasePrice ?? 0,
                        Quantity = item.Quantity,
                        Discount = item.Discount,
                        DiscountType = item.DiscountType,
                        IsBox = item.IsBox,
                        IsWholesale = item.IsWholesale
                    });
                }

                // Create serializer options
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = false
                };

                SerializedCartItems = JsonSerializer.Serialize(simplifiedItems, options);

                // Clear cached items to force regeneration next time
                _cartItems = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing cart items: {ex.Message}");
                SerializedCartItems = "[]";
                _cartItems = new List<CartItem>();
            }
        }
    }

    /// <summary>
    /// Simplified cart item for serialization/deserialization
    /// </summary>
    public class SimpleCartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Discount { get; set; }
        public int DiscountType { get; set; }
        public bool IsBox { get; set; }
        public bool IsWholesale { get; set; }
    }

    /// <summary>
    /// Represents the state of a failed transaction
    /// </summary>
    public enum FailedTransactionState
    {
        Failed = 0,
        Retrying = 1,
        Completed = 2,
        Cancelled = 3
    }
}