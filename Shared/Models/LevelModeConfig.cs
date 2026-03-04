using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Level-based parameter filling.
    /// </summary>
    public class LevelModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// Categories excluded from level band assignment.
        /// These categories will use nearest-level logic instead.
        /// </summary>
        public IList<BuiltInCategory> ExcludedCategories { get; set; }

        /// <summary>
        /// The IDs of selected levels for multi-level filling.
        /// Levels will be automatically sorted by elevation during processing.
        /// </summary>
        public ObservableCollection<ElementId> SelectedLevelIds { get; set; } = new ObservableCollection<ElementId>();

        /// <summary>
        /// The actual Level elements corresponding to SelectedLevelIds.
        /// Populated during ViewModel initialization and restored from saved state.
        /// </summary>
        public ObservableCollection<Level> SelectedLevels { get; set; } = new ObservableCollection<Level>();

        /// <summary>
        /// Custom level names mapped by level ElementId.
        /// Key: Level ElementId, Value: Custom name (or empty to use Revit name).
        /// Prepared for future custom naming UI.
        /// </summary>
        public Dictionary<ElementId, string> CustomLevelNames { get; set; } = new Dictionary<ElementId, string>();

      
        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.Level;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Level";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill level-based parameters";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "Layers";

        /// <summary>
        /// Validates the configuration for Level mode
        /// </summary>
        public override bool IsValid()
        {
            if (!IsEnabled) return true; // Not enabled means no validation needed

            return SelectedLevels != null && SelectedLevels.Count >= 2;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            if (!IsEnabled) return null;

            if (SelectedLevels == null || SelectedLevels.Count == 0)
            {
                return "At least 2 levels must be selected when Level mode is enabled";
            }

            if (SelectedLevels.Count == 1)
            {
                return "At least 2 levels must be selected for level range processing";
            }

            return null;
        }
    }
}
