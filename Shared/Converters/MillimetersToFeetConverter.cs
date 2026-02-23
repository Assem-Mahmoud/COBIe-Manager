using System;
using System.Globalization;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts between millimeters (display) and feet (Revit internal units).
    /// Used for tolerance input fields where users enter values in mm.
    /// </summary>
    public class MillimetersToFeetConverter : IValueConverter
    {
        // Conversion factor: 1 foot = 304.8 millimeters
        private const double MillimetersPerFoot = 304.8;

        /// <summary>
        /// Converts from feet (source) to millimeters (display for UI)
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double feet)
            {
                // Convert feet to millimeters for display
                return feet * MillimetersPerFoot;
            }
            return 0.0;
        }

        /// <summary>
        /// Converts from millimeters (user input) to feet (internal storage)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double millimeters)
            {
                // Convert millimeters to feet for storage
                return millimeters / MillimetersPerFoot;
            }

            if (value is string strValue && double.TryParse(strValue, out double parsedMillimeters))
            {
                // Convert millimeters to feet for storage
                return parsedMillimeters / MillimetersPerFoot;
            }

            return 0.0;
        }
    }
}
