using QuickTechPOS.Helpers;
using QuickTechPOS.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace QuickTechPOS.ViewModels
{
    /// <summary>
    /// View model for the login screen
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;
        private readonly NavigationService _navigationService;

        private string _username;
        private string _password;
        private string _errorMessage;
        private bool _isLoading;

        /// <summary>
        /// Gets or sets the username for login
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// Gets or sets the password for login
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// Gets or sets the error message to display
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets or sets whether the view is in a loading state
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Command to execute the login action
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// Initializes a new instance of the login view model
        /// </summary>
        /// <param name="navigationService">Service for navigating between views</param>
        /// <param name="authService">Service for authentication</param>
        public LoginViewModel(NavigationService navigationService, AuthenticationService authService = null)
        {
            _authService = authService ?? new AuthenticationService();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            LoginCommand = new RelayCommand(async param => await LoginAsync(), CanLogin);
        }

        /// <summary>
        /// Determines if login can be executed
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if login can be executed, otherwise false</returns>
        private bool CanLogin(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        /// <summary>
        /// Executes the login process
        /// </summary>
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                Console.WriteLine($"Attempting login with username: {Username}");
                var success = await _authService.AuthenticateAsync(Username, Password);

                if (success)
                {
                    Console.WriteLine("Login successful!");
                    // Navigate to main view
                    _navigationService.NavigateTo("MainView");
                }
                else
                {
                    Console.WriteLine("Login failed: Invalid credentials");
                    ErrorMessage = "Invalid username or password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.GetType().Name} - {ex.Message}");
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}