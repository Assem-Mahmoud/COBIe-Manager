using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Wrapper class for category items with selection state for UI binding
    /// </summary>
    public partial class CategoryItem : ObservableObject
    {
        /// <summary>
        /// The built-in category enum value
        /// </summary>
        public BuiltInCategory Category { get; }

        /// <summary>
        /// Display name for the category
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Number of elements found in this category
        /// </summary>
        public int ElementCount { get; set; }

        /// <summary>
        /// Whether this category is selected for processing
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Creates a new category item
        /// </summary>
        /// <param name="category">Built-in category enum</param>
        /// <param name="displayName">Display name</param>
        /// <param name="isSelected">Initial selection state</param>
        public CategoryItem(BuiltInCategory category, string displayName, bool isSelected = false)
        {
            Category = category;
            DisplayName = displayName;
            IsSelected = isSelected;
            ElementCount = 0;
        }

        /// <summary>
        /// Creates a category item with element count
        /// </summary>
        /// <param name="category">Built-in category enum</param>
        /// <param name="displayName">Display name</param>
        /// <param name="elementCount">Number of elements in category</param>
        /// <param name="isSelected">Initial selection state</param>
        public CategoryItem(BuiltInCategory category, string displayName, int elementCount, bool isSelected = false)
        {
            Category = category;
            DisplayName = displayName;
            ElementCount = elementCount;
            IsSelected = isSelected;
        }
    }
}
