using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using QuickTechPOS.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for CloseDrawerDialog.xaml
    /// </summary>
    public partial class CloseDrawerDialog : Window
    {
        private readonly CloseDrawerViewModel _viewModel;

        public CloseDrawerDialog(Drawer drawer)
        {
            try
            {
                Console.WriteLine("Initializing CloseDrawerDialog");
                InitializeComponent();

                // Apply current flow direction
                this.FlowDirection = LanguageManager.CurrentFlowDirection;

                if (drawer == null)
                {
                    Console.WriteLine("ERROR: Drawer is null in CloseDrawerDialog constructor");
                    string errorMessage = TryFindResource("ErrorDrawerNull") as string ?? "Cannot close drawer: No drawer information provided.";
                    string errorTitle = TryFindResource("ErrorTitle") as string ?? "Error";

                    MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    this.DialogResult = false;
                    this.Close();
                    return;
                }

                Console.WriteLine($"Creating view model for drawer #{drawer.DrawerId}, Status: {drawer.Status}");
                _viewModel = new CloseDrawerViewModel(drawer);
                DataContext = _viewModel;

                // Watch for property changes with enhanced logging
                _viewModel.PropertyChanged += (sender, e) =>
                {
                    Console.WriteLine($"ViewModel property changed: {e.PropertyName}");

                    if (e.PropertyName == nameof(CloseDrawerViewModel.DialogResult) &&
                        _viewModel.DialogResult.HasValue)
                    {
                        Console.WriteLine($"DialogResult changed to: {_viewModel.DialogResult}");

                        // Make sure we save the result before closing
                        this.DialogResult = _viewModel.DialogResult;
                        Console.WriteLine($"Window.DialogResult set to: {this.DialogResult}");

                        // Only close if the operation was successful
                        if (_viewModel.DialogResult == true)
                        {
                            Console.WriteLine("DialogResult is true, preparing to close dialog");

                            try
                            {
                                // Ensure all UI updates are processed
                                Application.Current.Dispatcher.Invoke(() => {
                                    Console.WriteLine("UI synchronization before close");
                                });

                                // Delay closing briefly to allow UI to update
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                    try
                                    {
                                        Console.WriteLine("Executing dialog close");
                                        this.Close();
                                        Console.WriteLine("Dialog closed successfully");
                                    }
                                    catch (Exception closeEx)
                                    {
                                        Console.WriteLine($"Error closing dialog: {closeEx.Message}");
                                    }
                                }), DispatcherPriority.Normal);
                            }
                            catch (Exception dispatcherEx)
                            {
                                Console.WriteLine($"Error in dispatcher operations: {dispatcherEx.Message}");
                                // Try direct close if dispatcher fails
                                try
                                {
                                    this.Close();
                                }
                                catch (Exception directCloseEx)
                                {
                                    Console.WriteLine($"Direct close failed: {directCloseEx.Message}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("DialogResult is false, not closing dialog");
                        }
                    }
                };

                Console.WriteLine("CloseDrawerDialog initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CloseDrawerDialog constructor: {ex.Message}");
                string errorMessage = TryFindResource("DialogInitError") as string ?? $"Error initializing Close Drawer dialog: {ex.Message}";
                string errorTitle = TryFindResource("DialogErrorTitle") as string ?? "Dialog Error";

                MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }
        private void CloseDialog(bool success)
        {
            try
            {
                // Set the dialog result
                this.DialogResult = success;

                // Add a small delay before closing to ensure UI updates
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(200);
                timer.Tick += (s, e) => {
                    timer.Stop();
                    this.Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing dialog: {ex.Message}");
                this.Close();
            }
        }
        /// <summary>
        /// Gets the closed drawer after dialog completion
        /// </summary>
        public Drawer ClosedDrawer
        {
            get
            {
                if (_viewModel != null)
                {
                    Console.WriteLine($"ClosedDrawer property accessed, drawer status: {_viewModel.Drawer?.Status ?? "null"}");
                    return _viewModel.Drawer;
                }
                Console.WriteLine("ViewModel is null, returning null from ClosedDrawer property");
                return null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Console.WriteLine("CloseDrawerDialog.OnClosed called");
            base.OnClosed(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}