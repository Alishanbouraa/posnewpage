using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using System.Diagnostics;
using System.Linq;

namespace QuickTechPOS.Services
{
    public class ReceiptPrinterService
    {
        private readonly BusinessSettingsService _settingsService;

        public ReceiptPrinterService()
        {
            _settingsService = new BusinessSettingsService();
        }

        /// <summary>
        /// Generates a receipt for a transaction
        /// </summary>
        /// <param name="transaction">Transaction to generate receipt for</param>
        /// <param name="cartItems">Items to include in the receipt</param>
        /// <param name="exchangeRate">Currency exchange rate</param>
        /// <param name="useAlternativeCurrency">Whether to include alternative currency amount</param>
        /// <returns>Receipt content as string</returns>
        public async Task<string> GenerateTransactionReceiptAsync(Transaction transaction, List<CartItem> cartItems, decimal exchangeRate = 90000, bool useAlternativeCurrency = false)
        {
            var receipt = new StringBuilder();

            try
            {
                var companyName = await _settingsService.GetCompanyNameAsync();
                var address = await _settingsService.GetAddressAsync();
                var phone = await _settingsService.GetPhoneAsync();
                var currency = await _settingsService.GetCurrencyAsync();
                var footer1 = await _settingsService.GetReceiptFooter1Async();
                var footer2 = await _settingsService.GetReceiptFooter2Async();

                receipt.AppendLine("===================================");
                receipt.AppendLine("             RECEIPT              ");
                receipt.AppendLine("===================================");
                receipt.AppendLine();

                receipt.AppendLine($"          {companyName}          ");
                if (!string.IsNullOrEmpty(address))
                {
                    receipt.AppendLine($"      {address}       ");
                }
                if (!string.IsNullOrEmpty(phone))
                {
                    receipt.AppendLine($"         {phone}          ");
                }
                receipt.AppendLine();

                receipt.AppendLine($"Transaction #: {transaction.TransactionId}");
                receipt.AppendLine($"Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}");
                receipt.AppendLine($"Cashier: {transaction.CashierName}");
                // No customer name printed as requested
                receipt.AppendLine($"Payment Method: {transaction.PaymentMethod}");
                receipt.AppendLine();

                receipt.AppendLine("------------- ITEMS ---------------");
                receipt.AppendLine("Qty  Description            Price   Total");
                receipt.AppendLine("------------------------------------");

                foreach (var item in cartItems)
                {
                    string description = item.Product.Name;
                    if (description.Length > 20)
                    {
                        description = description.Substring(0, 17) + "...";
                    }

                    decimal itemPrice = item.UnitPrice;
                    decimal itemTotal = item.Total;

                    receipt.AppendLine($"{item.Quantity,-4:F2} {description,-20} {currency}{itemPrice,-6:F2} {currency}{itemTotal:F2}");
                }

                receipt.AppendLine("------------------------------------");

                receipt.AppendLine($"Subtotal:         {currency}{transaction.TotalAmount:F2}");

                if (useAlternativeCurrency)
                {
                    decimal alternativeAmount = transaction.TotalAmount * exchangeRate;
                    receipt.AppendLine($"Exchange Rate:     1 {currency} = {exchangeRate:F0} LBP");
                    receipt.AppendLine($"Total (LBP):       LBP {alternativeAmount:F0}");
                }

                receipt.AppendLine($"Amount Paid:       {currency}{transaction.PaidAmount:F2}");
                receipt.AppendLine($"Change:            {currency}{transaction.ChangeAmount:F2}");
                receipt.AppendLine();

                if (!string.IsNullOrEmpty(footer1))
                {
                    receipt.AppendLine(footer1);
                }
                if (!string.IsNullOrEmpty(footer2))
                {
                    receipt.AppendLine(footer2);
                }
                receipt.AppendLine();
                receipt.AppendLine("===================================");
                receipt.AppendLine("         Thank You!               ");
                receipt.AppendLine("===================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating receipt: {ex.Message}");
                receipt.AppendLine($"Error generating receipt: {ex.Message}");
            }

            return receipt.ToString();
        }

        /// <summary>
        /// Prints a receipt to the default printer or saves to file
        /// </summary>
        /// <param name="receiptContent">Receipt content to print</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PrintReceiptAsync(string receiptContent)
        {
            try
            {
                Console.WriteLine("Printing receipt:");
                Console.WriteLine(receiptContent);

                await Task.Delay(500);

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastReceipt.txt");
                await File.WriteAllTextAsync(filePath, receiptContent);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing receipt: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a report for a drawer
        /// </summary>
        /// <param name="drawer">Drawer to generate report for</param>
        /// <returns>Drawer report content as string</returns>
        public async Task<string> GenerateDrawerReportAsync(Drawer drawer)
        {
            try
            {
                var companyName = await _settingsService.GetCompanyNameAsync();
                var address = await _settingsService.GetAddressAsync();
                var phone = await _settingsService.GetPhoneAsync();

                var receipt = new StringBuilder();

                receipt.AppendLine("===================================");
                receipt.AppendLine("           DRAWER REPORT           ");
                receipt.AppendLine("===================================");
                receipt.AppendLine();

                receipt.AppendLine($"          {companyName}          ");
                if (!string.IsNullOrEmpty(address))
                {
                    receipt.AppendLine($"      {address}       ");
                }
                if (!string.IsNullOrEmpty(phone))
                {
                    receipt.AppendLine($"         {phone}          ");
                }
                receipt.AppendLine();

                receipt.AppendLine($"Report Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                receipt.AppendLine($"Drawer ID: {drawer.DrawerId}");
                receipt.AppendLine($"Status: {drawer.Status}");
                receipt.AppendLine();

                receipt.AppendLine($"Cashier: {drawer.CashierName}");
                receipt.AppendLine($"Opened At: {drawer.FormattedOpenedAt}");
                if (drawer.ClosedAt.HasValue)
                {
                    receipt.AppendLine($"Closed At: {drawer.FormattedClosedAt}");
                }

                if (!string.IsNullOrEmpty(drawer.Notes))
                {
                    receipt.AppendLine();
                    receipt.AppendLine("------------- NOTES ---------------");
                    receipt.AppendLine(drawer.Notes);
                }

                receipt.AppendLine();

                receipt.AppendLine("------------ BALANCES --------------");
                receipt.AppendLine($"Opening Balance:    ${drawer.OpeningBalance:F2}");
                receipt.AppendLine($"Closing Balance:    ${drawer.CurrentBalance:F2}");
                receipt.AppendLine();

                receipt.AppendLine("------------ SALES -----------------");
                receipt.AppendLine($"Daily Sales:        ${drawer.DailySales:F2}");
                receipt.AppendLine($"Total Sales:        ${drawer.TotalSales:F2}");
                receipt.AppendLine();

                receipt.AppendLine("------------ CASH FLOW ------------");
                receipt.AppendLine($"Cash Out:          ${drawer.CashOut:F2}");
                receipt.AppendLine();

                receipt.AppendLine("------------ SUMMARY --------------");
                receipt.AppendLine($"Net Sales:          ${drawer.NetSales:F2}");
                receipt.AppendLine($"Net Cash Flow:      ${drawer.NetCashFlow:F2}");
                receipt.AppendLine();

                receipt.AppendLine("===================================");
                receipt.AppendLine("         End of Report             ");
                receipt.AppendLine("===================================");

                return receipt.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating drawer report: {ex.Message}");
                return $"Error generating drawer report: {ex.Message}";
            }
        }

        /// <summary>
        /// Prints a transaction receipt using the Windows printing system
        /// </summary>
        /// <param name="transaction">Transaction data</param>
        /// <param name="cartItems">Items in the transaction</param>
        /// <param name="customerId">Customer ID</param>
        /// <param name="previousCustomerBalance">Previous customer balance</param>
        /// <param name="exchangeRate">Exchange rate for alternative currency</param>
        /// <returns>Status message indicating success or failure</returns>
        public async Task<string> PrintTransactionReceiptWpfAsync(Transaction transaction, List<CartItem> cartItems, int customerId = 0, decimal previousCustomerBalance = 0, decimal exchangeRate = 90000)
        {
            try
            {
                if (transaction == null)
                {
                    return "No transaction loaded to print.";
                }

                bool printCancelled = false;
                string statusMessage = "Preparing receipt...";

                try
                {
                    int transactionId = transaction.TransactionId;

                    // Get business settings
                    string companyName;
                    string address;
                    string phoneNumber;
                    string email;
                    string footerText1;
                    string footerText2;
                    string logoPath = null;

                    try
                    {
                        companyName = await _settingsService.GetCompanyNameAsync();
                        address = await _settingsService.GetAddressAsync();
                        phoneNumber = await _settingsService.GetPhoneAsync();
                        email = await _settingsService.GetEmailAsync();
                        footerText1 = await _settingsService.GetReceiptFooter1Async();
                        footerText2 = await _settingsService.GetReceiptFooter2Async();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving business settings: {ex.Message}");
                        companyName = "QuickTech POS";
                        address = "123 Main Street";
                        phoneNumber = "(555) 123-4567";
                        email = "info@quicktech.com";
                        footerText1 = "Thank you for your business!";
                        footerText2 = "See you next time";
                    }

                    // Check for printer availability
                    statusMessage = "Checking printer availability...";

                    try
                    {
                        bool printerAvailable = false;
                        await Task.Run(() => {
                            try
                            {
                                PrintServer printServer = new PrintServer();
                                PrintQueueCollection printQueues = printServer.GetPrintQueues();
                                printerAvailable = printQueues.Count() > 0;
                            }
                            catch
                            {
                                printerAvailable = false;
                            }
                        });

                        if (!printerAvailable)
                        {
                            MessageBox.Show(
                                "No printer available. Please connect a printer and try again.",
                                "Printer Required",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            printCancelled = true;
                            return "Printing cancelled - No printer available";
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking printer availability: {ex.Message}");
                        // Continue anyway, the print dialog will handle errors
                    }

                    // Show print dialog
                    statusMessage = "Opening print dialog...";

                    PrintDialog printDialog = new PrintDialog();
                    bool? dialogResult = false;

                    try
                    {
                        dialogResult = printDialog.ShowDialog();
                    }
                    catch (Exception dialogEx)
                    {
                        Debug.WriteLine($"Error showing print dialog: {dialogEx.Message}");
                        MessageBox.Show(
                            "Failed to open print dialog. Please check printer configuration.",
                            "Print Dialog Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        printCancelled = true;
                        return "Printing cancelled - Dialog error";
                    }

                    if (dialogResult != true)
                    {
                        printCancelled = true;
                        return "Printing cancelled by user";
                    }

                    // Prepare the document
                    statusMessage = "Preparing document...";

                    try
                    {
                        decimal totalAmount = cartItems.Sum(i => i.Total);
                        var flowDocument = CreateReceiptDocument(
                            printDialog,
                            transactionId,
                            companyName,
                            address,
                            phoneNumber,
                            email,
                            footerText1,
                            footerText2,
                            transaction,
                            cartItems,
                            totalAmount,
                            0, // No discount for now, can be added later
                            previousCustomerBalance,
                            exchangeRate,
                            logoPath);

                        // Print the document
                        statusMessage = "Printing...";

                        printDialog.PrintDocument(
                            ((IDocumentPaginatorSource)flowDocument).DocumentPaginator,
                            "Transaction Receipt");

                        return "Receipt printed successfully";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error preparing or printing document: {ex.Message}");
                        MessageBox.Show(
                            $"Error printing receipt: {ex.Message}",
                            "Print Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return "Print error - Receipt not printed";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in PrintReceipt: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unexpected error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a formatted receipt document for printing
        /// </summary>
        private FlowDocument CreateReceiptDocument(
            PrintDialog printDialog,
            int transactionId,
            string companyName,
            string address,
            string phoneNumber,
            string email,
            string footerText1,
            string footerText2,
            Transaction transaction,
            List<CartItem> cartItems,
            decimal totalAmount,
            decimal discountAmount,
            decimal previousCustomerBalance,
            decimal exchangeRate,
            string logoPath = null)
        {
            var flowDocument = new FlowDocument
            {
                PageWidth = printDialog.PrintableAreaWidth,
                ColumnWidth = printDialog.PrintableAreaWidth,
                FontFamily = new FontFamily("Segoe UI, Arial"),
                FontWeight = FontWeights.Normal,
                PagePadding = new Thickness(10, 0, 10, 0),
                TextAlignment = TextAlignment.Center,
                PageHeight = printDialog.PrintableAreaHeight
            };

            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };

            header.Inlines.Add(new Run(companyName)
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            });
            header.Inlines.Add(new LineBreak());

            try
            {
                if (!string.IsNullOrEmpty(logoPath))
                {
                    BitmapImage logo = new BitmapImage();
                    if (File.Exists(logoPath))
                    {
                        logo.BeginInit();
                        logo.CacheOption = BitmapCacheOption.OnLoad;
                        logo.UriSource = new Uri(logoPath);
                        logo.EndInit();
                        logo.Freeze();

                        Image logoImage = new Image
                        {
                            Source = logo,
                            Width = 150,
                            Height = 70,
                            Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 5, 0, 5)
                        };

                        header.Inlines.Add(new InlineUIContainer(logoImage));
                        header.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        LoadDefaultLogo(header);
                    }
                }
                else
                {
                    LoadDefaultLogo(header);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading logo image: {ex.Message}");
                LoadDefaultLogo(header);
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                header.Inlines.Add(new Run(address)
                {
                    FontSize = 12,
                    FontWeight = FontWeights.Normal
                });
                header.Inlines.Add(new LineBreak());
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                header.Inlines.Add(new Run(phoneNumber)
                {
                    FontSize = 12,
                    FontWeight = FontWeights.Normal
                });
                header.Inlines.Add(new LineBreak());
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                header.Inlines.Add(new Run(email)
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Normal
                });
            }

            flowDocument.Blocks.Add(header);

            // Transaction details
            var metaTable = new Table { FontSize = 11, CellSpacing = 0 };
            metaTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
            metaTable.Columns.Add(new TableColumn { Width = GridLength.Auto });
            metaTable.RowGroups.Add(new TableRowGroup());
            AddMetaRow(metaTable, "Transaction #:", transactionId.ToString());
            AddMetaRow(metaTable, "Date:", transaction.TransactionDate.ToString("MM/dd/yyyy hh:mm tt"));
            AddMetaRow(metaTable, "Cashier:", transaction.CashierName);
            AddMetaRow(metaTable, "Payment:", transaction.PaymentMethod);

            flowDocument.Blocks.Add(metaTable);
            flowDocument.Blocks.Add(CreateDivider());

            // Items table
            var itemsTable = new Table { FontSize = 11, CellSpacing = 0 };
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
            itemsTable.RowGroups.Add(new TableRowGroup());

            var headerRow = new TableRow { Background = Brushes.LightGray };
            headerRow.Cells.Add(CreateCell("Product", FontWeights.Bold, TextAlignment.Left));
            headerRow.Cells.Add(CreateCell("Qty", FontWeights.Bold, TextAlignment.Center));
            headerRow.Cells.Add(CreateCell("Price", FontWeights.Bold, TextAlignment.Right));
            headerRow.Cells.Add(CreateCell("Total", FontWeights.Bold, TextAlignment.Right));
            itemsTable.RowGroups[0].Rows.Add(headerRow);

            foreach (var cartItem in cartItems)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(cartItem.Product.Name, FontWeights.Normal, TextAlignment.Left));
                row.Cells.Add(CreateCell(cartItem.Quantity.ToString(), FontWeights.Normal, TextAlignment.Center));
                row.Cells.Add(CreateCell($"${cartItem.UnitPrice:N2}", FontWeights.Normal, TextAlignment.Right));
                row.Cells.Add(CreateCell($"${cartItem.Total:N2}", FontWeights.Normal, TextAlignment.Right));
                itemsTable.RowGroups[0].Rows.Add(row);
            }

            flowDocument.Blocks.Add(itemsTable);
            flowDocument.Blocks.Add(CreateDivider());

            // Totals section
            var totalsTable = new Table { FontSize = 12, CellSpacing = 0 };
            totalsTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            totalsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            totalsTable.RowGroups.Add(new TableRowGroup());

            // Simple subtotal for now
            AddTotalRow(totalsTable, "Subtotal:", $"${totalAmount:N2}");

            if (discountAmount > 0)
            {
                AddTotalRow(totalsTable, "Discount:", $"-${discountAmount:N2}");
            }

            // Final total
            decimal finalTotal = Math.Max(0, totalAmount - discountAmount);

            // Calculate in LBP
            decimal totalLBP = finalTotal * exchangeRate;

            var totalRow = new TableRow();
            totalRow.Cells.Add(new TableCell(new Paragraph(new Run("Total:"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left
            }));

            var totalCell = new TableCell();
            var totalParagraph = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };

            totalParagraph.Inlines.Add(new Run($"${finalTotal:N2}")
            {
                Foreground = Brushes.Black
            });

            // Add LBP amount if exchange rate is available
            if (exchangeRate > 0)
            {
                totalParagraph.Inlines.Add(new LineBreak());
                totalParagraph.Inlines.Add(new Run($"{totalLBP:N0} LBP")
                {
                    Foreground = Brushes.Black,
                    FontSize = 12
                });
            }

            totalCell.Blocks.Add(totalParagraph);
            totalRow.Cells.Add(totalCell);
            totalsTable.RowGroups[0].Rows.Add(totalRow);

            flowDocument.Blocks.Add(totalsTable);
            flowDocument.Blocks.Add(CreateDivider());

            // Footer
            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 3)
            };

            if (!string.IsNullOrWhiteSpace(footerText1))
            {
                footer.Inlines.Add(new Run(footerText1)
                {
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                });
                footer.Inlines.Add(new LineBreak());
            }

            if (!string.IsNullOrWhiteSpace(footerText2))
            {
                footer.Inlines.Add(new Run(footerText2)
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Normal
                });
            }

            flowDocument.Blocks.Add(footer);

            return flowDocument;
        }

        /// <summary>
        /// Attempts to load a default logo
        /// </summary>
        private void LoadDefaultLogo(Paragraph header)
        {
            try
            {
                // Try to load a default logo from application resources
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();

                // Check if the resource exists
                string resourcePath = "pack://application:,,,/Resources/Images/Logo.png";
                try
                {
                    var resourceInfo = Application.GetResourceStream(new Uri(resourcePath));
                    if (resourceInfo == null)
                    {
                        // If resource doesn't exist, don't add any logo
                        return;
                    }
                }
                catch
                {
                    // If there's an error, don't add any logo
                    return;
                }

                logo.UriSource = new Uri(resourcePath);
                logo.CacheOption = BitmapCacheOption.OnLoad;
                logo.EndInit();
                logo.Freeze();

                Image logoImage = new Image
                {
                    Source = logo,
                    Width = 150,
                    Height = 70,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                header.Inlines.Add(new InlineUIContainer(logoImage));
                header.Inlines.Add(new LineBreak());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading default logo image: {ex.Message}");
                // Just continue without a logo
            }
        }

        /// <summary>
        /// Creates a table cell with text
        /// </summary>
        private TableCell CreateCell(string text, FontWeight fontWeight = default, TextAlignment alignment = TextAlignment.Left)
        {
            var paragraph = new Paragraph(new Run(text ?? string.Empty))
            {
                FontWeight = fontWeight == default ? FontWeights.Normal : fontWeight,
                TextAlignment = alignment,
                Margin = new Thickness(2)
            };
            return new TableCell(paragraph);
        }

        /// <summary>
        /// Adds a metadata row to a table
        /// </summary>
        private void AddMetaRow(Table table, string label, string value)
        {
            if (table == null) return;

            var row = new TableRow();
            row.Cells.Add(CreateCell(label, FontWeights.Bold));
            row.Cells.Add(CreateCell(value ?? string.Empty, FontWeights.Normal));
            table.RowGroups[0].Rows.Add(row);
        }

        /// <summary>
        /// Adds a total row to a table
        /// </summary>
        private void AddTotalRow(Table table, string label, string value, bool isBold = false)
        {
            if (table == null) return;

            var row = new TableRow();
            var fontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
            row.Cells.Add(CreateCell(label, fontWeight, TextAlignment.Left));
            row.Cells.Add(CreateCell(value, fontWeight, TextAlignment.Right));
            table.RowGroups[0].Rows.Add(row);
        }

        /// <summary>
        /// Creates a horizontal divider
        /// </summary>
        private BlockUIContainer CreateDivider()
        {
            return new BlockUIContainer(new Border
            {
                Height = 1,
                Background = Brushes.Black,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }

        /// <summary>
        /// Prints a drawer report
        /// </summary>
        /// <param name="drawer">Drawer to print report for</param>
        /// <returns>Status message indicating success or failure</returns>
        public async Task<string> PrintDrawerReportAsync(Drawer drawer)
        {
            try
            {
                if (drawer == null)
                {
                    Console.WriteLine("Cannot print report: Drawer is null");
                    return "No drawer to print report for.";
                }

                // Generate report
                Console.WriteLine($"Generating report for drawer #{drawer.DrawerId}...");
                string reportContent = await GenerateDrawerReportAsync(drawer);
                Console.WriteLine("Report generated successfully");

                // First save the report to a file as backup
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"DrawerReport_{drawer.DrawerId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                await File.WriteAllTextAsync(filePath, reportContent);
                Console.WriteLine($"Report saved to file: {filePath}");

                // Try to print using WPF printing
                try
                {
                    // Create a FlowDocument for printing
                    var printResult = await PrintDrawerReportWpfAsync(drawer);
                    Console.WriteLine($"WPF Print result: {printResult}");

                    if (printResult.Contains("successful"))
                    {
                        return $"Drawer report #{drawer.DrawerId} printed successfully.";
                    }
                    else
                    {
                        return $"Note: {printResult}. Report saved to: {filePath}";
                    }
                }
                catch (Exception wpfEx)
                {
                    Console.WriteLine($"WPF printing error: {wpfEx.Message}");

                    // Fall back to direct printing if WPF fails
                    bool printed = await PrintReceiptAsync(reportContent);

                    if (printed)
                    {
                        return $"Drawer report #{drawer.DrawerId} printed successfully.";
                    }
                    else
                    {
                        return $"Could not print drawer report. A copy has been saved to: {filePath}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing drawer report: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return $"Error printing drawer report: {ex.Message}. Please check printer settings.";
            }
        }

        // Add this new method for WPF-based printing
        public async Task<string> PrintDrawerReportWpfAsync(Drawer drawer)
        {
            try
            {
                Console.WriteLine("Setting up WPF printing for drawer report");

                // Create print dialog
                var printDialog = new PrintDialog();

                // Let user choose whether to proceed with printing
                bool? dialogResult = printDialog.ShowDialog();
                if (dialogResult != true)
                {
                    return "Printing was cancelled by user.";
                }

                // Create a flowdocument for the report
                var document = new FlowDocument();
                document.PageWidth = printDialog.PrintableAreaWidth;
                document.PageHeight = printDialog.PrintableAreaHeight;
                document.FontFamily = new FontFamily("Consolas, Courier New, Courier");
                document.FontSize = 10;

                // Generate the report content
                string reportContent = await GenerateDrawerReportAsync(drawer);

                // Create paragraphs for the document
                var paragraph = new Paragraph(new Run(reportContent));
                document.Blocks.Add(paragraph);

                // Print the document
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Drawer Report #{drawer.DrawerId}");

                return "Drawer report printed successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WPF printing error: {ex.Message}");
                throw; // Let the caller handle this
            }
        }
    }
}