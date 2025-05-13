// File: QuickTechPOS/Views/AddCustomerDialog.xaml.cs

using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.Services;
using System;
using System.Windows;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for AddCustomerDialog.xaml
    /// </summary>
    public partial class AddCustomerDialog : Window
    {
        private readonly CustomerService _customerService;

        /// <summary>
        /// Gets the newly added customer
        /// </summary>
        public Customer Customer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the add customer dialog
        /// </summary>
        public AddCustomerDialog()
        {
            InitializeComponent();

            // Apply current flow direction
            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            _customerService = new CustomerService();
        }

        /// <summary>
        /// Handles the save button click event
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    string validationError = TryFindResource("ValidationError") as string ?? "Validation Error";
                    string nameRequired = TryFindResource("CustomerNameRequired") as string ?? "Customer name is required.";

                    MessageBox.Show(nameRequired, validationError, MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    string validationError = TryFindResource("ValidationError") as string ?? "Validation Error";
                    string phoneRequired = TryFindResource("CustomerPhoneRequired") as string ?? "Phone number is required.";

                    MessageBox.Show(phoneRequired, validationError, MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneTextBox.Focus();
                    return;
                }

                var customer = new Customer
                {
                    Name = NameTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    Address = AddressTextBox.Text.Trim(),
                    Balance = 0,
                    IsActive = true
                };

                // Save customer to database
                Customer = await _customerService.AddCustomerAsync(customer);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                string errorTitle = TryFindResource("ErrorTitle") as string ?? "Error";
                string errorMsg = TryFindResource("ErrorAddingCustomer") as string ?? "Error adding customer:";

                MessageBox.Show($"{errorMsg} {ex.Message}", errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the cancel button click event
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}