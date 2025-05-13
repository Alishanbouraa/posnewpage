// File: QuickTechPOS/Helpers/PositiveIntToVisibilityConverter.cs

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a positive integer value to a Visibility
    /// </summary>
    public class PositiveIntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts an integer to a Visibility (visible when > 0)
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}