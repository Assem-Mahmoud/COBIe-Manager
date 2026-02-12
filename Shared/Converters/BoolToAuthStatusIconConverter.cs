using System;
using System.Globalization;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters;

/// <summary>
/// Converts boolean authentication status to a MaterialDesign icon kind
/// </summary>
public class BoolToAuthStatusIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAuthenticated)
        {
            return isAuthenticated ? "CheckCircle" : "AlertCircle";
        }
        return "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
