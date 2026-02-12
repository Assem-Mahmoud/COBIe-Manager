using Autodesk.Revit.DB;
using COBIeManager.Shared.Interfaces;
using System.Collections.Generic;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// User configuration for the parameter fill operation.
    /// </summary>
    public class FillConfiguration
    {
        /// <summary>
        /// The lower level defining the vertical band bottom
        /// </summary>
        public Level BaseLevel { get; set; }

        /// <summary>
        /// The upper level defining the vertical band top
        /// </summary>
        public Level TopLevel { get; set; }

        /// <summary>
        /// Element categories to process
        /// </summary>
        public IList<BuiltInCategory> SelectedCategories { get; set; }

        /// <summary>
        /// Whether to overwrite existing parameter values
        /// </summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>
        /// Mapping of logical parameter names to actual Revit parameter names
        /// </summary>
        public ParameterMapping ParameterMapping { get; set; }

        /// <summary>
        /// Creates a default configuration with standard settings
        /// </summary>
        public static FillConfiguration CreateDefault()
        {
            return new FillConfiguration
            {
                SelectedCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_MechanicalEquipment,
                    BuiltInCategory.OST_GenericModel
                },
                OverwriteExisting = false,
                ParameterMapping = new ParameterMapping()
            };
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (BaseLevel == null)
            {
                return false;
            }

            if (TopLevel == null)
            {
                return false;
            }

            if (SelectedCategories == null || SelectedCategories.Count == 0)
            {
                return false;
            }

            if (ParameterMapping == null)
            {
                return false;
            }

            // Validate that base level is below top level
            if (BaseLevel.Elevation >= TopLevel.Elevation)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        /// <returns>Validation error message or null if valid</returns>
        public string GetValidationError()
        {
            if (BaseLevel == null)
            {
                return "Base level must be selected";
            }

            if (TopLevel == null)
            {
                return "Top level must be selected";
            }

            if (SelectedCategories == null || SelectedCategories.Count == 0)
            {
                return "At least one category must be selected";
            }

            if (ParameterMapping == null)
            {
                return "Parameter mapping must be configured";
            }

            if (BaseLevel.Elevation >= TopLevel.Elevation)
            {
                return "Top level must be above base level";
            }

            return null;
        }
    }
}
