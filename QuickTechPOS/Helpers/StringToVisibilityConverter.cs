// File: QuickTechPOS/Helpers/StringToVisibilityConverter.cs

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a string value to a Visibility (visible when not empty)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a string to a Visibility (visible when not empty)
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue) ? Visibility.Visible : Visibility.Collapsed;
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