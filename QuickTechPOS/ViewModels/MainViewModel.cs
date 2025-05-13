using QuickTechPOS.Helpers;
using QuickTechPOS.Services;
using System;
using System.Windows.Input;

namespace QuickTechPOS.ViewModels
{
    /// <summary>
    /// View model for the main application screen
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;
        private readonly NavigationService _navigationService;

        private string _welcomeMessage;

        /// <summary>
        /// Gets or sets the welcome message
        /// </summary>
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        /// <summary>
        /// Command to log out of the application
        /// </summary>
        public ICommand LogoutCommand { get; }

        /// <summary>
        /// Initializes a new instance of the main view model
        /// </summary>
        /// <param name="authService">Service for authentication</param>
        /// <param name="navigationService">Service for navigating between views</param>
        public MainViewModel(AuthenticationService authService, NavigationService navigationService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Set welcome message based on current user
            WelcomeMessage = $"Welcome, {_authService.CurrentEmployee?.FullName ?? _authService.CurrentEmployee?.Username ?? "User"}!";

            // Initialize commands
            LogoutCommand = new RelayCommand(Logout);
        }

        /// <summary>
        /// Logs out the current user and returns to the login view
        /// </summary>
        private void Logout(object parameter)
        {
            _authService.Logout();
            _navigationService.NavigateTo("LoginView");
        }
    }
}