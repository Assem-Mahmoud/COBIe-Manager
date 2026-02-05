using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for handling unit conversions between different measurement systems.
    /// Provides utilities for converting between imperial, metric, and Revit internal units.
    /// </summary>
    public interface IUnitConversionService
    {
        /// <summary>
        /// Converts a value from one unit to another.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="fromUnit">The source unit (e.g., "Millimeters", "Inches")</param>
        /// <param name="toUnit">The target unit</param>
        /// <returns>The converted value</returns>
        double Convert(double value, string fromUnit, string toUnit);

        /// <summary>
        /// Converts a value from the specified unit to Revit internal units (feet).
        /// </summary>
        double ToInternalUnits(double value, string fromUnit);

        /// <summary>
        /// Converts a value from Revit internal units (feet) to the specified unit.
        /// </summary>
        double FromInternalUnits(double value, string toUnit);

        /// <summary>
        /// Gets the conversion factor between two units.
        /// </summary>
        double GetConversionFactor(string fromUnit, string toUnit);

        /// <summary>
        /// Gets all supported unit types.
        /// </summary>
        List<string> GetSupportedUnits();

        /// <summary>
        /// Gets the symbol for a unit (e.g., "mm" for millimeters).
        /// </summary>
        string GetUnitSymbol(string unitName);
    }
}
