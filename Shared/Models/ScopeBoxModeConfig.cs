using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Scope Box-based parameter filling.
    /// This mode assigns the scope box name to elements
    /// that are contained within the scope box's 3D bounding box.
    /// Multiple scope boxes can be selected - elements within each scope box
    /// will be assigned that scope box's name.
    /// </summary>
    public class ScopeBoxModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// The selected scope boxes to use for filling
        /// Each scope box will assign its name to elements within its bounds
        /// </summary>
        public ObservableCollection<ElementId> SelectedScopeBoxIds { get; set; } = new ObservableCollection<ElementId>();

        /// <summary>
        /// The actual scope box elements (populated at runtime)
        /// </summary>
        public ObservableCollection<Element> SelectedScopeBoxes { get; set; } = new ObservableCollection<Element>();

        /// <summary>
        /// Tolerance to extend the scope box bounds in internal units (feet).
        /// UI input is in millimeters and converted to feet automatically.
        /// This extends the scope box's bounding box in all directions (X, Y, Z).
        /// Elements must be COMPLETELY INSIDE the extended range.
        /// Default: 0 (strict - exact scope box bounds)
        /// Example: 1.0 (stored) = ~305mm UI input extends the bounds by 1 foot in all directions
        /// </summary>
        public double Tolerance { get; set; } = 0.0;

        /// <summary>
        /// Custom scope box names mapped by scope box ElementId.
        /// Key: Scope Box ElementId, Value: Custom name (or empty to use scope box name).
        /// </summary>
        public Dictionary<ElementId, string> CustomScopeBoxNames { get; set; } = new Dictionary<ElementId, string>();

        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.ScopeBox;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Building";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill with scope box names (Building)";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "RectangleOutline";

        /// <summary>
        /// Validates the configuration for ScopeBox mode
        /// </summary>
        public override bool IsValid()
        {
            if (!IsEnabled) return true; // Not enabled means no validation needed

            return SelectedScopeBoxIds != null && SelectedScopeBoxIds.Count > 0;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            if (!IsEnabled) return null;

            if (SelectedScopeBoxIds == null || SelectedScopeBoxIds.Count == 0)
            {
                return "At least one scope box must be selected when Building mode is enabled";
            }

            return null;
        }

        /// <summary>
        /// Gets the fill value for a specific scope box (custom name or scope box name)
        /// </summary>
        public string GetFillValue(ElementId scopeBoxId)
        {
            // First check for custom name
            if (CustomScopeBoxNames != null && CustomScopeBoxNames.TryGetValue(scopeBoxId, out var customName) && !string.IsNullOrWhiteSpace(customName))
            {
                return customName;
            }

            // Fall back to scope box name
            if (SelectedScopeBoxes != null)
            {
                var scopeBox = SelectedScopeBoxes.FirstOrDefault(sb => sb.Id == scopeBoxId);
                if (scopeBox != null)
                {
                    return scopeBox.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets all selected scope box IDs
        /// </summary>
        public IList<ElementId> GetSelectedScopeBoxIds()
        {
            return SelectedScopeBoxIds ??= new ObservableCollection<ElementId>();
        }

        /// <summary>
        /// Checks if a specific scope box is selected
        /// </summary>
        public bool IsScopeBoxSelected(ElementId scopeBoxId)
        {
            return SelectedScopeBoxIds != null && SelectedScopeBoxIds.Contains(scopeBoxId);
        }

        /// <summary>
        /// Adds a scope box to the selection
        /// </summary>
        public void AddScopeBox(ElementId scopeBoxId)
        {
            if (SelectedScopeBoxIds == null)
            {
                SelectedScopeBoxIds = new ObservableCollection<ElementId>();
            }

            if (!SelectedScopeBoxIds.Contains(scopeBoxId))
            {
                SelectedScopeBoxIds.Add(scopeBoxId);
            }
        }

        /// <summary>
        /// Removes a scope box from the selection
        /// </summary>
        public void RemoveScopeBox(ElementId scopeBoxId)
        {
            SelectedScopeBoxIds?.Remove(scopeBoxId);
        }

        /// <summary>
        /// Clears all selected scope boxes
        /// </summary>
        public void ClearScopeBoxes()
        {
            SelectedScopeBoxIds?.Clear();
            SelectedScopeBoxes?.Clear();
        }

        /// <summary>
        /// Sets a custom name for a scope box
        /// </summary>
        /// <param name="scopeBoxId">The scope box ElementId to set the custom name for</param>
        /// <param name="customName">The custom name (empty or null to remove)</param>
        public void SetCustomScopeBoxName(ElementId scopeBoxId, string customName)
        {
            if (string.IsNullOrWhiteSpace(customName))
            {
                // Remove custom name if empty
                if (CustomScopeBoxNames != null && CustomScopeBoxNames.ContainsKey(scopeBoxId))
                {
                    CustomScopeBoxNames.Remove(scopeBoxId);
                }
            }
            else
            {
                // Set or update custom name
                CustomScopeBoxNames ??= new Dictionary<ElementId, string>();
                CustomScopeBoxNames[scopeBoxId] = customName.Trim();
            }
        }

        /// <summary>
        /// Gets the custom name for a scope box
        /// </summary>
        /// <param name="scopeBoxId">The scope box ElementId to get the custom name for</param>
        /// <returns>Custom name if set, otherwise null</returns>
        public string GetCustomScopeBoxName(ElementId scopeBoxId)
        {
            if (CustomScopeBoxNames != null && CustomScopeBoxNames.TryGetValue(scopeBoxId, out var customName))
            {
                return customName;
            }

            return null;
        }

        /// <summary>
        /// Checks if a custom name exists for a scope box
        /// </summary>
        /// <param name="scopeBoxId">The scope box ElementId to check</param>
        /// <returns>True if a custom name exists, false otherwise</returns>
        public bool HasCustomScopeBoxName(ElementId scopeBoxId)
        {
            if (CustomScopeBoxNames != null && CustomScopeBoxNames.TryGetValue(scopeBoxId, out var customName))
            {
                return !string.IsNullOrWhiteSpace(customName);
            }
            return false;
        }
    }
}
