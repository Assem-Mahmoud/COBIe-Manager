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
        /// Custom level name to use instead of the Revit level name.
        /// When set, this value will be used to fill parameters instead of BaseLevel.Name.
        /// Leave empty to use the Revit level name.
        /// </summary>
        public string CustomLevelName { get; set; }

        /// <summary>
        /// Tolerance to extend BELOW the base level in internal units (feet).
        /// UI input is in millimeters and converted to feet automatically.
        /// This extends the bottom of the level band downward.
        /// Elements must be COMPLETELY INSIDE the extended range (base - tolerance to top + topTolerance).
        /// Default: 0 (strict - original level range)
        /// Example: 0.5 (stored) = ~152mm UI input extends the base level downward by 6 inches
        /// </summary>
        public double BaseTolerance { get; set; } = 0.0;

        /// <summary>
        /// Tolerance to extend ABOVE the top level in internal units (feet).
        /// UI input is in millimeters and converted to feet automatically.
        /// This extends the top of the level band upward.
        /// Elements must be COMPLETELY INSIDE the extended range (base - baseTolerance to top + tolerance).
        /// Default: 0 (strict - original level range)
        /// Example: 1.0 (stored) = ~305mm UI input extends the top level upward by 1 foot
        /// </summary>
        public double TopTolerance { get; set; } = 0.0;

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
