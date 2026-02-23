using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Collections.Generic;
using DataBinding = System.Windows.Data.Binding;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converter that checks if an ElementId is contained in a collection of ElementIds.
    /// Used for CheckBox.IsChecked binding in multi-selection scenarios.
    /// Supports both single-value (for ConverterParameter) and MultiBinding.
    /// </summary>
    public class ContainsElementIdConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList<ElementId> elementIds && parameter is ElementId id)
            {
                return elementIds.Contains(id);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used - we handle changes through event handlers
            return DataBinding.DoNothing;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is IList<ElementId> elementIds && values[1] is ElementId id)
            {
                return elementIds.Contains(id);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not used - we handle changes through event handlers
            return null;
        }
    }
}
