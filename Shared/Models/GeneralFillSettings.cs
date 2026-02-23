using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// General settings that apply to all fill modes.
    /// These settings are shared across the entire fill operation.
    /// </summary>
    public class GeneralFillSettings
    {
        /// <summary>
        /// Whether to overwrite existing parameter values
        /// </summary>
        public bool OverwriteExisting { get; set; }=true;

        /// <summary>
        /// Element categories to process
        /// </summary>
        public IList<BuiltInCategory> SelectedCategories { get; set; }

        /// <summary>
        /// Available categories with selection state
        /// </summary>
        public IList<CategoryItem> AvailableCategories { get; set; }

        /// <summary>
        /// Available parameters with selection state and mapping
        /// </summary>
        public IList<ParameterItem> AvailableParameters { get; set; }

        /// <summary>
        /// Creates a default general settings instance
        /// </summary>
        public static GeneralFillSettings CreateDefault()
        {
            return new GeneralFillSettings
            {
                SelectedCategories = new List<BuiltInCategory>(),
                AvailableCategories = new List<CategoryItem>(),
                AvailableParameters = new List<ParameterItem>(),
                OverwriteExisting = false
            };
        }

        /// <summary>
        /// Gets the list of selected categories
        /// </summary>
        public IList<BuiltInCategory> GetSelectedCategories()
        {
            if (AvailableCategories != null && AvailableCategories.Any())
            {
                return AvailableCategories.Where(c => c.IsSelected).Select(c => c.Category).ToList();
            }

            return SelectedCategories ?? new List<BuiltInCategory>();
        }

        /// <summary>
        /// Gets the list of selected parameters
        /// </summary>
        public IList<ParameterItem> GetSelectedParameters()
        {
            if (AvailableParameters == null)
            {
                return new List<ParameterItem>();
            }

            return AvailableParameters.Where(p => p.IsSelected).ToList();
        }

        /// <summary>
        /// Gets selected parameters filtered by their applicable mode
        /// </summary>
        /// <param name="mode">The fill mode to filter by</param>
        /// <returns>List of parameter names that match the specified mode</returns>
        public IList<string> GetParameterNamesByMode(FillMode mode)
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && p.ApplicableMode == mode)
                .Select(p => p.ParameterName)
                .ToList();
        }

        /// <summary>
        /// Checks if at least one category is selected
        /// </summary>
        public bool HasSelectedCategories()
        {
            return (SelectedCategories != null && SelectedCategories.Count > 0) ||
                   (AvailableCategories != null && AvailableCategories.Any(c => c.IsSelected));
        }

        /// <summary>
        /// Checks if there are any unmapped parameters (selected but not mapped to a mode)
        /// </summary>
        public bool HasUnmappedParameters()
        {
            return AvailableParameters != null && AvailableParameters.Any(p => p.IsSelected && !p.IsMapped);
        }

        /// <summary>
        /// Gets the list of unmapped parameter names
        /// </summary>
        public IList<string> GetUnmappedParameterNames()
        {
            if (AvailableParameters == null)
            {
                return new List<string>();
            }

            return AvailableParameters
                .Where(p => p.IsSelected && !p.IsMapped)
                .Select(p => p.ParameterName)
                .ToList();
        }
    }
}
