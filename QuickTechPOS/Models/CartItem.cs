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
                    // When quantity changes, also notify that Subtotal and Total have changed
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
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
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
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
                    OnPropertyChanged(nameof(Total));
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
                        UnitPrice = _isBox ?
                            (_isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice) :
                            (_isWholesale ? Product.WholesalePrice : Product.SalePrice);
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
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
                        UnitPrice = _isBox ?
                            (_isWholesale ? Product.BoxWholesalePrice : Product.BoxSalePrice) :
                            (_isWholesale ? Product.WholesalePrice : Product.SalePrice);
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        /// <summary>
        /// Gets a display name that indicates if this is a box
        /// </summary>
        [NotMapped]
        public string DisplayName => IsBox ? $"BOX-{Product?.Name}" : Product?.Name;

        /// <summary>
        /// Discount value (amount or percentage)
        /// </summary>
        [NotMapped]
        public decimal DiscountValue
        {
            get => DiscountType == 0 ? Discount : (Discount / Subtotal) * 100;
            set
            {
                if (DiscountType == 0)
                {
                    // Amount-based discount
                    Discount = value > Subtotal ? Subtotal : value;
                }
                else
                {
                    // Percentage-based discount
                    decimal percentage = value > 100 ? 100 : value;
                    Discount = (percentage / 100) * Subtotal;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the subtotal for this item (Quantity * UnitPrice)
        /// </summary>
        [NotMapped]
        public decimal Subtotal => Quantity * UnitPrice;

        /// <summary>
        /// Gets the total amount for this item (Subtotal - Discount)
        /// </summary>
        [NotMapped]
        public decimal Total => Subtotal - Discount;

        /// <summary>
        /// Gets the number of individual items this cart item represents
        /// </summary>
        [NotMapped]
        public decimal TotalItemQuantity => IsBox ? Quantity * Product.ItemsPerBox : Quantity;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}