// File: QuickTechPOS/Helpers/DifferenceToColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a numerical difference to a background color
    /// </summary>
    public class DifferenceToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a difference value to a background color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal difference)
            {
                if (difference > 0)
                {
                    // Positive difference (excess) - light green
                    return new SolidColorBrush(Color.FromRgb(213, 245, 227));
                }
                else if (difference < 0)
                {
                    // Negative difference (shortage) - light red
                    return new SolidColorBrush(Color.FromRgb(250, 219, 216));
                }
            }

            // No difference - light gray
            return new SolidColorBrush(Color.FromRgb(236, 240, 241));
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