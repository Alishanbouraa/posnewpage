using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a boolean value to a Visibility value (inverted logic - true becomes Collapsed)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a Visibility (true to Collapsed, false to Visible)
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        /// <summary>
        /// Converts a Visibility to a boolean (Collapsed to true, Visible to false)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }

            return false;
        }
    }
}