// File: QuickTechPOS/Helpers/DifferenceToTextColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a numerical difference to a text color
    /// </summary>
    public class DifferenceToTextColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a difference value to a text color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal difference)
            {
                if (difference > 0)
                {
                    // Positive difference (excess) - green
                    return new SolidColorBrush(Color.FromRgb(46, 204, 113));
                }
                else if (difference < 0)
                {
                    // Negative difference (shortage) - red
                    return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                }
            }

            // No difference - dark gray
            return new SolidColorBrush(Color.FromRgb(44, 62, 80));
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