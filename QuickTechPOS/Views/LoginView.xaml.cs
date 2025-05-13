using QuickTechPOS.Helpers;
using QuickTechPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickTechPOS.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        private readonly LoginViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the login view
        /// </summary>
        /// <param name="viewModel">The view model for this view</param>
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Handle password changes
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

            // Initialize language selector
            InitializeLanguageSelector();

            // Set focus to username field
            Loaded += (s, e) =>
            {
                if (string.IsNullOrEmpty(_viewModel.Username))
                {
                    Dispatcher.BeginInvoke(new System.Action(() => Keyboard.Focus(UsernameTextBox)));
                }
                else
                {
                    Dispatcher.BeginInvoke(new System.Action(() => Keyboard.Focus(PasswordBox)));
                }
            };
        }

        /// <summary>
        /// Updates the password in the view model when the password box text changes
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void InitializeLanguageSelector()
        {
            if (LanguageSelector != null)
            {
                // Set languages
                LanguageSelector.ItemsSource = LanguageManager.AvailableLanguages;

                // Select current language
                var currentLanguage = LanguageManager.GetCurrentLanguage();
                LanguageSelector.SelectedItem = currentLanguage;
            }
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageSelector.SelectedItem is LanguageOption selectedLanguage)
            {
                string selectedCode = selectedLanguage.Code;
                if (selectedCode != LanguageManager.CurrentLanguageCode)
                {
                    LanguageManager.ChangeLanguage(selectedCode);
                }
            }
        }
    }
}