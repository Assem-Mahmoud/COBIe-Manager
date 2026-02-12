using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Lightweight summary for preview mode (no document modification).
    /// </summary>
    public class PreviewSummary
    {
        /// <summary>
        /// Estimated elements within level band
        /// </summary>
        public int EstimatedElementsToProcess { get; set; }

        /// <summary>
        /// Estimated elements with assignable rooms
        /// </summary>
        public int EstimatedRoomAssignments { get; set; }

        /// <summary>
        /// Categories that have zero elements in model
        /// </summary>
        public List<string> CategoriesWithNoElements { get; set; }

        /// <summary>
        /// Configuration issues (e.g., level elevation invalid)
        /// </summary>
        public List<string> ValidationWarnings { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PreviewSummary()
        {
            CategoriesWithNoElements = new List<string>();
            ValidationWarnings = new List<string>();
        }

        /// <summary>
        /// Whether the preview has any validation warnings
        /// </summary>
        public bool HasValidationWarnings => ValidationWarnings != null && ValidationWarnings.Count > 0;

        /// <summary>
        /// Whether there are any elements to process
        /// </summary>
        public bool HasElementsToProcess => EstimatedElementsToProcess > 0;

        /// <summary>
        /// Whether there are categories with no elements
        /// </summary>
        public bool HasEmptyCategories => CategoriesWithNoElements != null && CategoriesWithNoElements.Count > 0;

        /// <summary>
        /// Creates a formatted preview summary for display
        /// </summary>
        /// <returns>Formatted preview summary string</returns>
        public string ToFormattedString()
        {
            var summary = $"Preview Summary:\n";
            summary += $"---------------\n";
            summary += $"Estimated Elements to Process: {EstimatedElementsToProcess}\n";
            summary += $"Estimated Room Assignments: {EstimatedRoomAssignments}\n";

            if (HasEmptyCategories)
            {
                summary += $"\nCategories with No Elements:\n";
                foreach (var category in CategoriesWithNoElements)
                {
                    summary += $"  - {category}\n";
                }
            }

            if (HasValidationWarnings)
            {
                summary += $"\nValidation Warnings:\n";
                foreach (var warning in ValidationWarnings)
                {
                    summary += $"  ⚠️ {warning}\n";
                }
            }

            return summary;
        }

        /// <summary>
        /// Adds a validation warning
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void AddValidationWarning(string warning)
        {
            if (ValidationWarnings == null)
            {
                ValidationWarnings = new List<string>();
            }
            ValidationWarnings.Add(warning);
        }

        /// <summary>
        /// Adds a category with no elements
        /// </summary>
        /// <param name="categoryName">Category name</param>
        public void AddEmptyCategory(string categoryName)
        {
            if (CategoriesWithNoElements == null)
            {
                CategoriesWithNoElements = new List<string>();
            }
            CategoriesWithNoElements.Add(categoryName);
        }

        /// <summary>
        /// Gets a brief status message
        /// </summary>
        /// <returns>Brief status message</returns>
        public string GetStatusMessage()
        {
            if (HasValidationWarnings)
            {
                return $"⚠️ Configuration has {ValidationWarnings.Count} warning(s)";
            }

            if (!HasElementsToProcess)
            {
                return "ℹ️ No elements found to process";
            }

            return $"✓ Ready to process {EstimatedElementsToProcess} element(s)";
        }
    }
}
