using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converter that shows content only when the mode is Level and is selected.
    /// Used for Level mode-specific settings panels.
    /// </summary>
    public class ModeToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isSelected && values[1] is FillMode mode)
            {
                // Show only for Level mode when selected
                if (isSelected && mode == FillMode.Level)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
