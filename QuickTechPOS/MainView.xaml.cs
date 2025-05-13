using QuickTechPOS.Helpers;
using QuickTechPOS.ViewModels;
using QuickTechPOS.Views;
using System.Windows;
using System.Windows.Controls;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the main view
        /// </summary>
        /// <param name="viewModel">The view model for this view</param>
        public MainView(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Add language settings button event handler
            if (LanguageButton != null)
            {
                LanguageButton.Click += LanguageButton_Click;
            }
        }

        /// <summary>
        /// Sets the main content of the view
        /// </summary>
        /// <param name="content">The content to display</param>
        public void SetContent(UIElement content)
        {
            MainContent.Content = content;
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            var languageDialog = new LanguageSettingsDialog();
            languageDialog.Owner = Application.Current.MainWindow;
            languageDialog.ShowDialog();
        }
    }
}