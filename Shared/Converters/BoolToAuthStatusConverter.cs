using System;
using System.Globalization;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters;

/// <summary>
/// Converts boolean to authentication status text
/// </summary>
public class BoolToAuthStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAuthenticated)
        {
            return isAuthenticated ? "Authenticated" : "Not Authenticated";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
