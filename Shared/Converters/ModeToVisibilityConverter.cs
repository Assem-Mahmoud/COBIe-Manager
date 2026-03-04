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
    public class LevelOnlyVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isSelected && values[1] is FillMode mode)
            {
                // Show ONLY for Level mode when selected
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

    /// <summary>
    /// Converter that shows content only when the mode is Zone and is selected.
    /// Used for Zone mode-specific settings panels.
    /// </summary>
    public class ZoneOnlyVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isSelected && values[1] is FillMode mode)
            {
                // Show ONLY for Zone mode when selected
                if (isSelected && mode == FillMode.Zone)
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

    /// <summary>
    /// Converter that shows content only when the mode is Level or Zone and is selected.
    /// Used for Level and Zone mode-specific settings panels.
    /// NOTE: This legacy converter shows both Level and Zone - prefer using LevelOnlyVisibilityConverter
    /// or ZoneOnlyVisibilityConverter for mode-specific panels.
    /// </summary>
    [Obsolete("Use LevelOnlyVisibilityConverter or ZoneOnlyVisibilityConverter instead")]
    public class ModeToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isSelected && values[1] is FillMode mode)
            {
                // Show for Level or Zone mode when selected
                if (isSelected && (mode == FillMode.Level || mode == FillMode.Zone))
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
