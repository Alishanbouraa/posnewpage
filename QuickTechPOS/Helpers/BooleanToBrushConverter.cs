// File: QuickTechPOS/Helpers/BooleanToBrushConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a boolean value to a brush color
    /// </summary>
    public class BooleanToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a SolidColorBrush
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;

            // If parameter is provided, use it to determine the colors
            if (parameter is string colorParams)
            {
                string[] colors = colorParams.Split(':');
                if (colors.Length == 2)
                {
                    string colorWhenTrue = colors[0];
                    string colorWhenFalse = colors[1];

                    try
                    {
                        Color color = boolValue ?
                            (Color)ColorConverter.ConvertFromString(colorWhenTrue) :
                            (Color)ColorConverter.ConvertFromString(colorWhenFalse);

                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        // Fall back to default if color parsing fails
                    }
                }
            }

            // Default colors if no parameter provided or parsing fails
            return boolValue ?
                new SolidColorBrush(Color.FromRgb(216, 27, 96)) : // #D81B60 when true
                new SolidColorBrush(Color.FromRgb(245, 158, 11)); // #F59E0B when false
        }

        /// <summary>
        /// Converts back from a brush to a boolean (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}