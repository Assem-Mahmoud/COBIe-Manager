using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using WpfVisibility = System.Windows.Visibility;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// MultiBinding converter that converts "contains element id" boolean to Visibility.
    /// Returns Visible if the element ID is in the list, Collapsed otherwise.
    /// </summary>
    public class ContainsElementIdToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is IList<ElementId> elementIds && values[1] is ElementId id)
            {
                return elementIds.Contains(id) ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            }
            return WpfVisibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiBinding converter that checks if a level has a custom name (not empty).
    /// Returns Visibility.Collapsed when custom name exists, Visible when empty.
    /// </summary>
    public class LevelCustomNameIsEmptyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId levelId)
            {
                if (customNames.TryGetValue(levelId, out var customName) && !string.IsNullOrWhiteSpace(customName))
                {
                    return WpfVisibility.Collapsed;
                }
            }
            return WpfVisibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiBinding converter for one-way binding of custom level names.
    /// Returns the custom name for a level from the dictionary, or empty string.
    /// </summary>
    public class LevelCustomNameBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId levelId)
            {
                if (customNames.TryGetValue(levelId, out var customName))
                {
                    return customName ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not used - updates are handled through LostFocus event
            return new object[] { null, null };
        }
    }

    /// <summary>
    /// MultiBinding converter that checks if a zone has a custom name (not empty).
    /// Returns Visibility.Collapsed when custom name exists, Visible when empty.
    /// </summary>
    public class ZoneCustomNameIsEmptyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId zoneId)
            {
                if (customNames.TryGetValue(zoneId, out var customName) && !string.IsNullOrWhiteSpace(customName))
                {
                    return WpfVisibility.Collapsed;
                }
            }
            return WpfVisibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiBinding converter for one-way binding of custom zone names.
    /// Returns the custom name for a zone from the dictionary, or empty string.
    /// </summary>
    public class ZoneCustomNameBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId zoneId)
            {
                if (customNames.TryGetValue(zoneId, out var customName))
                {
                    return customName ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not used - updates are handled through LostFocus event
            return new object[] { null, null };
        }
    }

    /// <summary>
    /// MultiBinding converter that checks if a scope box has a custom name (not empty).
    /// Returns Visibility.Collapsed when custom name exists, Visible when empty.
    /// </summary>
    public class ScopeBoxCustomNameIsEmptyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId scopeBoxId)
            {
                if (customNames.TryGetValue(scopeBoxId, out var customName) && !string.IsNullOrWhiteSpace(customName))
                {
                    return WpfVisibility.Collapsed;
                }
            }
            return WpfVisibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiBinding converter for one-way binding of custom scope box names.
    /// Returns the custom name for a scope box from the dictionary, or empty string.
    /// </summary>
    public class ScopeBoxCustomNameBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<ElementId, string> customNames && values[1] is ElementId scopeBoxId)
            {
                if (customNames.TryGetValue(scopeBoxId, out var customName))
                {
                    return customName ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not used - updates are handled through LostFocus event
            return new object[] { null, null };
        }
    }

    /// <summary>
    /// Converter for LevelIsSelected - simpler approach using data binding
    /// This will be handled through a different mechanism
    /// </summary>
    public class LevelIsSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This is handled by ContainsElementIdConverter instead
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
