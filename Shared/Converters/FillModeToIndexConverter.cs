using System;
using System.Globalization;
using System.Windows.Data;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Converters
{
    /// <summary>
    /// Converts between FillMode enum and ComboBox SelectedIndex
    /// Maps FillMode values to indices: Level(1)->0, RoomName(2)->1, RoomNumber(4)->2, Groups(8)->3
    /// </summary>
    public class FillModeToIndexConverter : IValueConverter
    {
        /// <summary>
        /// Converts FillMode to ComboBox SelectedIndex
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return -1;
            }

            // Handle both FillMode enum and int types
            int fillModeValue;
            if (value is FillMode fillMode)
            {
                fillModeValue = (int)fillMode;
            }
            else if (value is int intValue)
            {
                fillModeValue = intValue;
            }
            else
            {
                return -1;
            }

            return fillModeValue switch
            {
                1 => 0,  // Level
                2 => 1,  // RoomName
                4 => 2,  // RoomNumber
                8 => 3,  // Groups
                _ => -1  // Unmapped
            };
        }

        /// <summary>
        /// Converts ComboBox SelectedIndex back to FillMode
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FillMode.None;
            }

            if (value is int index)
            {
                return index switch
                {
                    0 => FillMode.Level,
                    1 => FillMode.RoomName,
                    2 => FillMode.RoomNumber,
                    3 => FillMode.Groups,
                    _ => FillMode.None
                };
            }

            return FillMode.None;
        }
    }
}
