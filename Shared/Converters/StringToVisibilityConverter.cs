using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts a string to Visibility.
    /// Empty/null string becomes Collapsed, non-empty becomes Visible.
    /// Use parameter "Inverse" to reverse the logic.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = true;

            if (value is string str)
            {
                isEmpty = string.IsNullOrWhiteSpace(str);
            }

            // Check for inverse parameter
            bool inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (inverse)
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
