using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Services;
using QuickTechPOS.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickTechPOS.Views
{
    public partial class TransactionView : UserControl
    {
        private readonly TransactionViewModel _viewModel;

        public TransactionView(TransactionViewModel viewModel)
        {
            InitializeComponent();

            // Apply current flow direction
            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(TransactionViewModel.IsDrawerOpen) ||
                    e.PropertyName == nameof(TransactionViewModel.CurrentDrawer))
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
            };

            Loaded += async (sender, e) => {
                await _viewModel.RefreshDrawerStatusAsync();
            };
        }

        private void BarcodeSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchBarcodeCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _viewModel.UpdateNameQuery(comboBox.Text);
                comboBox.IsDropDownOpen = !string.IsNullOrWhiteSpace(comboBox.Text);
            }
        }

        private void ProductSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Product product)
            {
                _viewModel.AddSelectedProductToCart(product);
                comboBox.Text = string.Empty;
                comboBox.SelectedItem = null;
            }
        }

        private void CustomerSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _viewModel.UpdateCustomerQuery(comboBox.Text);
                comboBox.IsDropDownOpen = !string.IsNullOrWhiteSpace(comboBox.Text);
            }
        }

        private void CustomerSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Customer customer)
            {
                _viewModel.SetSelectedCustomer(customer);
                _viewModel.CustomerQuery = customer.Name;
            }
        }

        private void CustomerSearch_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                comboBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.CaretIndex = textBox.Text.Length;
                    textBox.SelectionLength = 0;
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void CustomerItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Customer customer)
            {
                _viewModel.SetSelectedCustomerAndFillSearch(customer);
                e.Handled = true;
            }
        }

        private void Quantity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is CartItem cartItem)
            {
                _viewModel.UpdateCartItemQuantity(cartItem);
            }
        }

        private void Discount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is CartItem cartItem)
            {
                _viewModel.UpdateCartItemDiscount(cartItem);
            }
        }

        private void DiscountType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is CartItem cartItem)
            {
                _viewModel.UpdateCartItemDiscount(cartItem);
            }
        }
    }
}