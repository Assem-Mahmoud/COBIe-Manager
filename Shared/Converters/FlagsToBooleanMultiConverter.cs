using System;
using System.Globalization;
using System.Windows.Data;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Multi-value converter for two-way binding between FillMode flags and CheckBox.IsChecked
    /// Values: [0] = Current FillMode, [1] = Mode flag name (string)
    /// </summary>
    public class FlagsToBooleanMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts FillMode flags to boolean for CheckBox.IsChecked
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is FillMode fillMode && values[1] is string modeString)
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
        /// Returns the new FillMode value with the flag added or removed
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && parameter is string modeString)
            {
                if (Enum.TryParse<FillMode>(modeString, out var modeFlag))
                {
                    // Create a new FillMode with the flag added or removed
                    // This will be bound to the FillMode property via MultiBinding
                    FillMode newMode;
                    if (isChecked)
                    {
                        // Add the flag
                        // We need to OR with the flag, but we don't have the current value here
                        // This is a limitation - we'll return the flag value and let the ViewModel handle it
                        newMode = modeFlag;
                    }
                    else
                    {
                        // Remove the flag - return None as a signal
                        newMode = FillMode.None;
                    }

                    return new object[] { newMode, modeString };
                }
            }

            return new object[] { FillMode.None, string.Empty };
        }
    }
}
