using System;
using System.Globalization;
using System.Windows.Data;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts between FillMode flags and boolean for CheckBox binding
    /// Note: This converter requires the FillMode to be passed as a separate binding parameter
    /// for proper two-way binding. Use MultiBinding for full two-way support.
    /// </summary>
    public class FlagsToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts FillMode flags to boolean for CheckBox.IsChecked
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FillMode fillMode && parameter is string modeString)
            {
                if (Enum.TryParse<FillMode>(modeString, out var modeFlag))
                {
                    return (fillMode & modeFlag) != 0;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts boolean back to FillMode flags
        /// WARNING: This cannot work properly without access to the current FillMode value.
        /// Use commands in the ViewModel instead for changing FillMode.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This cannot work properly - we need the current FillMode to add/remove the flag
            // Return DoNothing to prevent two-way binding issues
            // The ViewModel should handle CheckBox changes via commands instead
            return Binding.DoNothing;
        }
    }
}
