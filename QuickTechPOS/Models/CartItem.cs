using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents an item in the shopping cart
    /// </summary>
    public class CartItem : INotifyPropertyChanged
    {
        private Product _product;
        private decimal _quantity;
        private decimal _unitPrice;
        private decimal _discount;
        private int _discountType = 0;
        private bool _isBox = false;
        private bool _isWholesale = false;
        private decimal? _cachedSubtotal;
        private decimal? _cachedTotal;
        private decimal? _cachedDiscountValue; // Added cache for discount value
        private bool _isUpdatingProperties = false; // Flag to prevent circular updates

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Associated product
        /// </summary>
        public Product Product
        {
            get => _product;
            set
            {
                if (_product != value)
                {
                    _product = value;
                    OnPropertyChanged();
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Quantity of the product
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Unit price of the product
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Discount amount for this item
        /// </summary>
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    OnPropertyChanged();
                    _cachedDiscountValue = null; // Invalidate discount value cache
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Discount type: 0 = Amount, 1 = Percentage
        /// </summary>
        [NotMapped]
        public int DiscountType
        {
            get => _discountType;
            set
            {
                if (_discountType != value)
                {
                    _discountType = value;
                    OnPropertyChanged();
                    _cachedDiscountValue = null; // Invalidate discount value cache
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Indicates whether this cart item represents a box instead of individual items
        /// </summary>
        [NotMapped]
        public bool IsBox
        {
            get => _isBox;
            set
            {
                if (_isBox != value)
                {
                    _isBox = value;
                    UpdatePrice(); // Use separate method to update price
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Indicates whether this cart item uses wholesale pricing
        /// </summary>
        [NotMapped]
        public bool IsWholesale
        {
            get => _isWholesale;
            set
            {
                if (_isWholesale != value)
                {
                    _isWholesale = value;
                    UpdatePrice(); // Use separate method to update price
                    OnPropertyChanged();
                    InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Gets a display name that indicates if this is a box
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (Product == null)
                    return "Unknown Product";

                try
                {
                    return IsBox ? $"BOX-{Product.Name}" : Product.Name;
                }
                catch
                {
                    // Fallback if properties are inaccessible
                    return IsBox ? "BOX-Product" : "Product";
                }
            }
        }

        /// <summary>
        /// Discount value (amount or percentage)
        /// </summary>
        [NotMapped]
        public decimal DiscountValue
        {
            get
            {
                // Use cached value if available
                if (_cachedDiscountValue.HasValue)
                    return _cachedDiscountValue.Value;

                try
                {
                    // Calculate only when needed
                    decimal result = DiscountType == 0
                        ? Discount
                        : CalculateDiscountPercentage();

                    _cachedDiscountValue = result;
                    return result;
                }
                catch
                {
                    _cachedDiscountValue = 0;
                    return 0;
                }
            }
            set
            {
                // Prevent circular updates
                if (_isUpdatingProperties)
                    return;

                _isUpdatingProperties = true;

                try
                {
                    if (DiscountType == 0)
                    {
                        // Amount-based discount
                        decimal subtotal = CalculateSubtotal();
                        Discount = value > subtotal ? subtotal : value;
                    }
                    else
                    {
                        // Percentage-based discount - limit to 100%
                        decimal percentage = value > 100 ? 100 : value;
                        decimal subtotal = CalculateSubtotal();

                        // Calculate the new discount amount based on percentage
                        decimal newDiscount = (percentage / 100) * subtotal;

                        // Directly update fields to avoid triggering additional property changed events
                        _discount = newDiscount;
                        _cachedDiscountValue = percentage;

                        // Manually notify that Discount has changed
                        OnPropertyChanged(nameof(Discount));
                    }

                    OnPropertyChanged();
                    InvalidateCache();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting DiscountValue: {ex.Message}");
                }
                finally
                {
                    _isUpdatingProperties = false;
                }
            }
        }

        /// <summary>
        /// Gets the subtotal for this item (Quantity * UnitPrice)
        /// </summary>
        [NotMapped]
        public decimal Subtotal
        {
            get
            {
                if (!_cachedSubtotal.HasValue)
                {
                    _cachedSubtotal = CalculateSubtotal();
                }
                return _cachedSubtotal.Value;
            }
        }

        /// <summary>
        /// Gets the total amount for this item (Subtotal - Discount)
        /// </summary>
        [NotMapped]
        public decimal Total
        {
            get
            {
                if (!_cachedTotal.HasValue)
                {
                    _cachedTotal = CalculateTotal();
                }
                return _cachedTotal.Value;
            }
        }

        /// <summary>
        /// Gets the number of individual items this cart item represents
        /// </summary>
        [NotMapped]
        public decimal TotalItemQuantity
        {
            get
            {
                try
                {
                    return IsBox && Product != null ? Quantity * Product.ItemsPerBox : Quantity;
                }
                catch
                {
                    return Quantity;
                }
            }
        }

        /// <summary>
        /// Calculates the discount percentage based on current values
        /// </summary>
        private decimal CalculateDiscountPercentage()
        {
            decimal subtotal = CalculateSubtotal();
            if (subtotal <= 0)
                return 0;

            return (Discount / subtotal) * 100;
        }

        /// <summary>
        /// Updates the price based on IsBox and IsWholesale properties
        /// </summary>
        private void UpdatePrice()
        {
            if (Product == null)
                return;

            try
            {
                decimal newPrice;

                if (_isBox)
                {
                    newPrice = _isWholesale
                        ? Product.BoxWholesalePrice
                        : Product.BoxSalePrice;
                }
                else
                {
                    newPrice = _isWholesale
                        ? Product.WholesalePrice
                        : Product.SalePrice;
                }

                // Only update if the price changes
                if (Math.Abs(_unitPrice - newPrice) > 0.001m)
                {
                    _unitPrice = newPrice;
                    OnPropertyChanged(nameof(UnitPrice));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating price: {ex.Message}");
            }
        }

        /// <summary>
        /// Invalidates cached calculations
        /// </summary>
        private void InvalidateCache()
        {
            if (_isUpdatingProperties)
                return;

            _cachedSubtotal = null;
            _cachedTotal = null;

            // Notify that dependent properties have changed
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalItemQuantity));
        }

        /// <summary>
        /// Calculates the subtotal directly
        /// </summary>
        private decimal CalculateSubtotal()
        {
            return Quantity * UnitPrice;
        }

        /// <summary>
        /// Calculates the total directly
        /// </summary>
        private decimal CalculateTotal()
        {
            decimal subtotal = CalculateSubtotal();
            return Math.Max(0, subtotal - Discount);
        }

        /// <summary>
        /// Raises property changed notifications
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}