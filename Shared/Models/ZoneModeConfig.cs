using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Configuration specific to Zone-based parameter filling.
    /// This mode assigns the zone name to elements
    /// that are contained within the zone's (scope box) 3D bounding box.
    /// Multiple zones can be selected - elements within each zone
    /// will be assigned that zone's name.
    /// Zones are represented by scope boxes in Revit.
    /// </summary>
    public class ZoneModeConfig : FillModeConfigBase
    {
        /// <summary>
        /// The selected scope boxes (representing zones) to use for filling
        /// Each scope box will assign its name to elements within its bounds
        /// </summary>
        public ObservableCollection<ElementId> SelectedZoneIds { get; set; } = new ObservableCollection<ElementId>();

        /// <summary>
        /// The actual zone elements (scope boxes) (populated at runtime)
        /// </summary>
        public ObservableCollection<Element> SelectedZones { get; set; } = new ObservableCollection<Element>();

        /// <summary>
        /// Custom zone names mapped by zone ElementId.
        /// Key: Zone ElementId, Value: Custom name (or empty to use scope box name).
        /// </summary>
        public Dictionary<ElementId, string> CustomZoneNames { get; set; } = new Dictionary<ElementId, string>();

        /// <summary>
        /// The fill mode this configuration applies to
        /// </summary>
        public override FillMode Mode => FillMode.Zone;

        /// <summary>
        /// Display name for this mode
        /// </summary>
        public override string DisplayName => "Zone";

        /// <summary>
        /// Description for this mode
        /// </summary>
        public override string Description => "Fill with zone names (using scope boxes)";

        /// <summary>
        /// Icon kind for Material Design
        /// </summary>
        public override string IconKind => "CheckboxBlankCircleOutline";

        /// <summary>
        /// Validates the configuration for Zone mode
        /// </summary>
        public override bool IsValid()
        {
            if (!IsEnabled) return true; // Not enabled means no validation needed

            return SelectedZoneIds != null && SelectedZoneIds.Count > 0;
        }

        /// <summary>
        /// Gets validation error message if configuration is invalid
        /// </summary>
        public override string GetValidationError()
        {
            if (!IsEnabled) return null;

            if (SelectedZoneIds == null || SelectedZoneIds.Count == 0)
            {
                return "At least one zone must be selected when Zone mode is enabled";
            }

            return null;
        }

        /// <summary>
        /// Gets the fill value for a specific zone (scope box name)
        /// </summary>
        public string GetFillValue(ElementId zoneId)
        {
            if (SelectedZones != null)
            {
                var zone = SelectedZones.FirstOrDefault(z => z.Id == zoneId);
                if (zone != null)
                {
                    return zone.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets all selected zone IDs
        /// </summary>
        public IList<ElementId> GetSelectedZoneIds()
        {
            return SelectedZoneIds ??= new ObservableCollection<ElementId>();
        }

        /// <summary>
        /// Checks if a specific zone is selected
        /// </summary>
        public bool IsZoneSelected(ElementId zoneId)
        {
            return SelectedZoneIds != null && SelectedZoneIds.Contains(zoneId);
        }

        /// <summary>
        /// Adds a zone to the selection
        /// </summary>
        public void AddZone(ElementId zoneId)
        {
            if (SelectedZoneIds == null)
            {
                SelectedZoneIds = new ObservableCollection<ElementId>();
            }

            if (!SelectedZoneIds.Contains(zoneId))
            {
                SelectedZoneIds.Add(zoneId);
            }
        }

        /// <summary>
        /// Removes a zone from the selection
        /// </summary>
        public void RemoveZone(ElementId zoneId)
        {
            SelectedZoneIds?.Remove(zoneId);
        }

        /// <summary>
        /// Clears all selected zones
        /// </summary>
        public void ClearZones()
        {
            SelectedZoneIds?.Clear();
            SelectedZones?.Clear();
        }
    }
}
