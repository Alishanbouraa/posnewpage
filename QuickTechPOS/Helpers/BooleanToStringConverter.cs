// File: QuickTechPOS/Helpers/BooleanToStringConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickTechPOS
{
    /// <summary>
    /// Converts a boolean value to one of two string values
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a string based on the parameter
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;

            // If parameter is provided, use it to determine the text values
            if (parameter is string textParams)
            {
                string[] texts = textParams.Split('|');
                if (texts.Length == 2)
                {
                    return boolValue ? texts[0] : texts[1];
                }
                // If only one value provided, use it for true, empty for false
                return boolValue ? textParams : string.Empty;
            }

            // Default if no parameter provided
            return boolValue ? "(!)" : string.Empty;
        }

        /// <summary>
        /// Convert back from a string to a boolean (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}