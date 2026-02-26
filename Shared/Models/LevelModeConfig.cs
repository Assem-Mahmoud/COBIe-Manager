using Autodesk.Revit.DB;
using System.Collections.Generic;

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
        /// The lower level defining the vertical band bottom
        /// </summary>
        public Level BaseLevel { get; set; }

        /// <summary>
        /// The upper level defining the vertical band top
        /// </summary>
        public Level TopLevel { get; set; }

        /// <summary>
        /// Custom level name to use instead of the base Revit level name.
        /// When set, this value will be used to fill parameters instead of BaseLevel.Name.
        /// Leave empty to use the Revit level name.
        /// </summary>
        public string CustomLevelName { get; set; }

        /// <summary>
        /// Custom level name to use instead of the top Revit level name.
        /// When set, this value will be used to fill parameters for excluded categories assigned to TopLevel.
        /// Leave empty to use the Revit level name.
        /// </summary>
        public string CustomTopLevelName { get; set; }

      
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

            return BaseLevel != null && TopLevel != null &&
                   BaseLevel.Elevation < TopLevel.Elevation;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            if (!IsEnabled) return null;

            if (BaseLevel == null)
            {
                return "Base level must be selected when Level mode is enabled";
            }

            if (TopLevel == null)
            {
                return "Top level must be selected when Level mode is enabled";
            }

            if (BaseLevel.Elevation >= TopLevel.Elevation)
            {
                return "Top level must be above base level";
            }

            return null;
        }
    }
}
