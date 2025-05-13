using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace QuickTechPOS.Views
{
    public partial class RestoreCartDialog : Window
    {
        public HeldCart SelectedCart { get; private set; }

        public RestoreCartDialog(List<HeldCart> heldCarts)
        {
            InitializeComponent();

            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            HeldCartsListView.ItemsSource = heldCarts;

            if (heldCarts.Count > 0)
            {
                HeldCartsListView.SelectedIndex = 0;
            }
        }

        private void HeldCartsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HeldCartsListView.SelectedItem is HeldCart selectedCart)
            {
                SelectedCart = selectedCart;
                DialogResult = true;
                Close();
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (HeldCartsListView.SelectedItem is HeldCart selectedCart)
            {
                SelectedCart = selectedCart;
                DialogResult = true;
                Close();
            }
            else
            {
                string message = TryFindResource("PleaseSelectCart") as string ?? "Please select a cart to restore.";
                string title = TryFindResource("InfoTitle") as string ?? "Information";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}