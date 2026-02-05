using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for handling unit conversions between different measurement systems.
    /// Provides utilities for converting between imperial, metric, and Revit internal units.
    /// Revit's internal unit is feet.
    /// </summary>
    public class UnitConversionService : IUnitConversionService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, double> _conversionFactors;

        public UnitConversionService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conversionFactors = InitializeConversionFactors();
        }

        /// <summary>
        /// Converts a value from one unit to another.
        /// </summary>
        public double Convert(double value, string fromUnit, string toUnit)
        {
            try
            {
                if (string.IsNullOrEmpty(fromUnit)) throw new ArgumentException("From unit cannot be empty", nameof(fromUnit));
                if (string.IsNullOrEmpty(toUnit)) throw new ArgumentException("To unit cannot be empty", nameof(toUnit));

                if (fromUnit == toUnit)
                {
                    _logger.Debug($"Conversion from {fromUnit} to {toUnit} - same unit, no conversion needed");
                    return value;
                }

                var key = $"{fromUnit}To{toUnit}";

                if (_conversionFactors.TryGetValue(key, out var factor))
                {
                    var result = value * factor;
                    _logger.Debug($"Converted {value} {fromUnit} to {result} {toUnit}");
                    return result;
                }

                _logger.Warn($"Conversion from {fromUnit} to {toUnit} not found, returning original value");
                return value;
            }
            catch (Exception ex)
            {
                _logger.Error($"Conversion failed from {fromUnit} to {toUnit}", ex);
                return value;
            }
        }

        /// <summary>
        /// Converts a value from the specified unit to Revit internal units (feet).
        /// </summary>
        public double ToInternalUnits(double value, string fromUnit)
        {
            try
            {
                if (string.IsNullOrEmpty(fromUnit))
                    throw new ArgumentException("From unit cannot be empty", nameof(fromUnit));

                var result = Convert(value, fromUnit, "Feet");
                _logger.Debug($"Converted {value} {fromUnit} to {result} feet (internal units)");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to convert {fromUnit} to internal units", ex);
                return value;
            }
        }

        /// <summary>
        /// Converts a value from Revit internal units (feet) to the specified unit.
        /// </summary>
        public double FromInternalUnits(double value, string toUnit)
        {
            try
            {
                if (string.IsNullOrEmpty(toUnit))
                    throw new ArgumentException("To unit cannot be empty", nameof(toUnit));

                var result = Convert(value, "Feet", toUnit);
                _logger.Debug($"Converted {value} feet (internal units) to {result} {toUnit}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to convert from internal units to {toUnit}", ex);
                return value;
            }
        }

        /// <summary>
        /// Gets the conversion factor between two units.
        /// </summary>
        public double GetConversionFactor(string fromUnit, string toUnit)
        {
            try
            {
                if (string.IsNullOrEmpty(fromUnit)) throw new ArgumentException("From unit cannot be empty", nameof(fromUnit));
                if (string.IsNullOrEmpty(toUnit)) throw new ArgumentException("To unit cannot be empty", nameof(toUnit));

                var key = $"{fromUnit}To{toUnit}";

                if (_conversionFactors.TryGetValue(key, out var factor))
                {
                    _logger.Debug($"Conversion factor from {fromUnit} to {toUnit}: {factor}");
                    return factor;
                }

                _logger.Warn($"Conversion factor not found for {fromUnit} to {toUnit}");
                return 1.0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get conversion factor", ex);
                return 1.0;
            }
        }

        /// <summary>
        /// Gets all supported unit types.
        /// </summary>
        public List<string> GetSupportedUnits()
        {
            return new List<string>
            {
                "Feet",
                "Inches",
                "Millimeters",
                "Centimeters",
                "Meters",
                "Yards"
            };
        }

        /// <summary>
        /// Gets the symbol for a unit (e.g., "mm" for millimeters).
        /// </summary>
        public string GetUnitSymbol(string unitName)
        {
            return unitName switch
            {
                "Feet" => "ft",
                "Inches" => "in",
                "Millimeters" => "mm",
                "Centimeters" => "cm",
                "Meters" => "m",
                "Yards" => "yd",
                _ => unitName
            };
        }

        /// <summary>
        /// Initializes conversion factors between common units.
        /// Base unit: Feet (Revit internal unit)
        /// </summary>
        private Dictionary<string, double> InitializeConversionFactors()
        {
            var factors = new Dictionary<string, double>
            {
                // From Feet
                { "FeetToInches", 12.0 },
                { "FeetToMillimeters", 304.8 },
                { "FeetToCentimeters", 30.48 },
                { "FeetToMeters", 0.3048 },
                { "FeetToYards", 0.333333 },

                // To Feet
                { "InchesToFeet", 0.0833333 },
                { "MillimetersToFeet", 0.00328084 },
                { "CentimetersToFeet", 0.0328084 },
                { "MetersToFeet", 3.28084 },
                { "YardsToFeet", 3.0 },

                // Between other units (commonly used)
                { "MillimetersToInches", 0.0393701 },
                { "InchesToMillimeters", 25.4 },
                { "MillimetersToCentimeters", 0.1 },
                { "CentimetersToMillimeters", 10.0 },
                { "MetersToMillimeters", 1000.0 },
                { "MillimetersToMeters", 0.001 },
                { "MetersToCentimeters", 100.0 },
                { "CentimetersToMeters", 0.01 },
                { "InchesToCentimeters", 2.54 },
                { "CentimetersToInches", 0.393701 },

                // Same unit conversions
                { "FeetToFeet", 1.0 },
                { "InchesToInches", 1.0 },
                { "MillimetersToMillimeters", 1.0 },
                { "CentimetersToCentimeters", 1.0 },
                { "MetersToMeters", 1.0 },
                { "YardsToYards", 1.0 },
            };

            _logger.Info($"Initialized {factors.Count} unit conversion factors");
            return factors;
        }
    }
}
