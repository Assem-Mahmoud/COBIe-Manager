using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts config summary strings to background colors for visual feedback.
    /// </summary>
    public class ConfigSummaryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string config)
            {
                // If no configuration ("-"), return gray
                if (string.IsNullOrEmpty(config) || config == "-")
                {
                    return new SolidColorBrush(Color.FromRgb(189, 189, 189)); // Light Gray
                }

                // If has any configuration (C, F, W, CF, CW, FW, CFW), return blue/teal
                return new SolidColorBrush(Color.FromRgb(0, 150, 136)); // Teal
            }
            return new SolidColorBrush(Color.FromRgb(189, 189, 189)); // Light Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
