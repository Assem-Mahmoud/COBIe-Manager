using System;
using System.Globalization;
using System.Windows.Data;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts an enum value to boolean for RadioButton binding.
    /// Returns true if the enum value matches the converter parameter.
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            // If parameter is already the same type as value, compare directly
            if (value.GetType() == parameter.GetType())
            {
                return value.Equals(parameter);
            }

            // If parameter is a string, parse it to the enum type for comparison
            string paramString = parameter as string;
            if (paramString != null && value.GetType().IsEnum)
            {
                try
                {
                    var parsedEnum = Enum.Parse(value.GetType(), paramString);
                    return value.Equals(parsedEnum);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // When RadioButton is checked (true), return the parameter parsed to the target enum type
            if (value is bool boolValue && boolValue && parameter != null && targetType.IsEnum)
            {
                // If parameter is already the correct type, return it
                if (parameter.GetType() == targetType)
                {
                    return parameter;
                }

                // If parameter is a string, parse it to the enum type
                string paramString = parameter as string;
                if (paramString != null)
                {
                    try
                    {
                        return Enum.Parse(targetType, paramString);
                    }
                    catch
                    {
                        return Binding.DoNothing;
                    }
                }
            }

            // Return Binding.DoNothing to avoid updating when unchecking
            return Binding.DoNothing;
        }
    }
}
