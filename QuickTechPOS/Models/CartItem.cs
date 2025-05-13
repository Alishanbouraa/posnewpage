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
                    OnPropertyChanged(nameof(DiscountValue));
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

                    // When changing between box and individual item, update the price accordingly
                    if (Product != null)
                    {
                        try
                        {
                            UnitPrice = _isBox ?
                                (_isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice) :
                                (_isWholesale ? Product.WholesalePrice : Product.SalePrice);
                        }
                        catch
                        {
                            // Fallback if properties are inaccessible
                            if (_isBox)
                            {
                                UnitPrice = _isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice;
                            }
                            else
                            {
                                UnitPrice = _isWholesale ? Product.WholesalePrice : Product.SalePrice;
                            }
                        }
                    }

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

                    // When changing between retail and wholesale, update the price accordingly
                    if (Product != null)
                    {
                        try
                        {
                            UnitPrice = _isBox ?
                                (_isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice) :
                                (_isWholesale ? Product.WholesalePrice : Product.SalePrice);
                        }
                        catch
                        {
                            // Fallback if properties are inaccessible
                            if (_isBox)
                            {
                                UnitPrice = _isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice;
                            }
                            else
                            {
                                UnitPrice = _isWholesale ? Product.WholesalePrice : Product.SalePrice;
                            }
                        }
                    }

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
                try
                {
                    return DiscountType == 0 ? Discount : (Discount / CalculateSubtotal()) * 100;
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                if (DiscountType == 0)
                {
                    // Amount-based discount
                    decimal subtotal = CalculateSubtotal();
                    Discount = value > subtotal ? subtotal : value;
                }
                else
                {
                    // Percentage-based discount
                    decimal percentage = value > 100 ? 100 : value;
                    decimal subtotal = CalculateSubtotal();
                    Discount = (percentage / 100) * subtotal;
                }
                OnPropertyChanged();
                InvalidateCache();
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
        /// Invalidates cached calculations
        /// </summary>
        private void InvalidateCache()
        {
            _cachedSubtotal = null;
            _cachedTotal = null;
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
            return CalculateSubtotal() - Discount;
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