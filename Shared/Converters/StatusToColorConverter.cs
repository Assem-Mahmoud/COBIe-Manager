using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts status strings to background colors for visual feedback.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "success":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "error":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    case "skipped":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    case "pending":
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
