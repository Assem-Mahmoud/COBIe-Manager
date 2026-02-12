using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts an integer to Visibility.
    /// Zero or negative becomes Collapsed, positive values become Visible.
    /// </summary>
    public class IntegerToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
