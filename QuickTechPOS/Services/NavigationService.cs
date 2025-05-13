using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Handles navigation between different views in the application
    /// </summary>
    public class NavigationService
    {
        private readonly Dictionary<string, Func<UserControl>> _viewFactories = new Dictionary<string, Func<UserControl>>();
        private readonly Frame _navigationFrame;

        /// <summary>
        /// Initializes a new instance of the navigation service
        /// </summary>
        /// <param name="frame">The frame to use for navigation</param>
        public NavigationService(Frame frame)
        {
            _navigationFrame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        /// <summary>
        /// Registers a view with the navigation service
        /// </summary>
        /// <param name="viewName">Name of the view</param>
        /// <param name="viewFactory">Factory function to create the view</param>
        public void RegisterView(string viewName, Func<UserControl> viewFactory)
        {
            _viewFactories[viewName] = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        /// <summary>
        /// Navigates to the specified view
        /// </summary>
        /// <param name="viewName">Name of the view to navigate to</param>
        public void NavigateTo(string viewName)
        {
            if (!_viewFactories.ContainsKey(viewName))
                throw new ArgumentException($"View {viewName} is not registered.", nameof(viewName));

            var view = _viewFactories[viewName]();
            _navigationFrame.Navigate(view);
        }
    }
}