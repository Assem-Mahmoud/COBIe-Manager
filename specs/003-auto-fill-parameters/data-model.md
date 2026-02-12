# Data Model: Auto-Fill Revit Parameters

**Feature**: 003-auto-fill-parameters
**Date**: 2025-02-12
**Status**: Milestone 1

## Entity Definitions

### FillConfiguration

User configuration for the parameter fill operation.

| Property | Type | Description | Validation |
|-----------|----------|-------------|--------------|
| BaseLevel | Level | The lower level defining the vertical band bottom | Must not be null |
| TopLevel | Level | The upper level defining the vertical band top | Must not be null, Elevation > BaseLevel.Elevation |
| SelectedCategories | IList<BuiltInCategory> | Element categories to process | Must contain at least one category |
| OverwriteExisting | bool | Whether to overwrite existing parameter values | Default: false |
| ParameterMapping | ParameterMapping | Mapping of logical names to Revit parameter names | Default: ACG-4D-* names |

**Relationships**: None (root configuration object)

---

### ParameterMapping

Maps logical parameter names to actual Revit parameter names.

| Property | Type | Description | Default Value |
|-----------|----------|-------------|------------------|
| LevelParameter | string | Parameter name for level assignment | "ACG-4D-Level" |
| RoomNumberParameter | string | Parameter name for room number | "ACG-4D-RoomNumber" |
| RoomNameParameter | string | Parameter name for room name | "ACG-4D-RoomName" |
| RoomRefParameter | string | Parameter name for combined room reference | "ACG-4D-RoomRef" |

**Relationships**: Used by FillConfiguration

**Validation**: All parameter names must be non-empty strings

---

### ProcessingSummary

Aggregate statistics from a parameter fill operation.

| Property | Type | Description |
|-----------|----------|-------------|
| TotalElementsScanned | int | Total elements evaluated for processing |
| ElementsProcessed | int | Elements that had at least one parameter set |
| LevelParametersFilled | int | Count of level parameters successfully set |
| RoomParametersFilled | int | Count of room parameters successfully set |
| SkippedNoBoundingBox | int | Elements skipped due to missing bounding box |
| SkippedNoLocation | int | Elements skipped due to missing location for room detection |
| SkippedNoRoomFound | int | Elements skipped because no room was found |
| SkippedParameterMissing | int | Elements skipped because target parameter doesn't exist |
| SkippedParameterReadOnly | int | Elements skipped because parameter is read-only |
| SkippedNotInLevelBand | int | Elements outside the selected level band |
| SkippedValueExists | int | Elements skipped due to existing value (when overwrite=false) |
| ProcessingDuration | TimeSpan | Time taken for the operation |
| SkippedElementIds | Dictionary<string, List<int>> | Element IDs grouped by skip reason (for detailed log) |

**Relationships**: None (output aggregate)

**Display Format**: Summary shown in UI and exported to log file

---

### PreviewSummary

Lightweight summary for preview mode (no document modification).

| Property | Type | Description |
|-----------|----------|-------------|
| EstimatedElementsToProcess | int | Estimated elements within level band |
| EstimatedRoomAssignments | int | Estimated elements with assignable rooms |
| CategoriesWithNoElements | List<string> | Categories that have zero elements in model |
| ValidationWarnings | List<string> | Configuration issues (e.g., level elevation invalid) |

**Relationships**: None (preview-only output)

---

### ParameterAssignmentResult

Result of attempting to assign a single parameter.

| Property | Type | Description |
|-----------|----------|-------------|
| Success | bool | Whether the parameter was successfully set |
| Skipped | bool | Whether the assignment was skipped (parameter missing, read-only, or value exists) |
| SkipReason | string? | Reason for skipping (if applicable) |
| ElementId | int | The element's ID for logging |

**Relationships**: Used by ProcessingSummary

---

### RoomAssignmentResult

Result of room detection and parameter assignment for an element.

| Property | Type | Description |
|-----------|----------|-------------|
| Room | Room? | The detected room (null if not found) |
| DetectionMethod | RoomDetectionMethod | How the room was detected |
| ParametersAssigned | int | Count of parameters set (0-3) |
| ElementId | int | The element's ID for logging |

**Relationships**: Used by ProcessingSummary

---

### RoomDetectionMethod

Enum describing how a room was associated with an element.

| Value | Description |
|--------|-------------|
| DirectRoomProperty | Element.Room property was used |
| FromRoomProperty | Door.FromRoom property was used |
| ToRoomProperty | Door.ToRoom property was used (fallback) |
| PointInRoom | Room.GetRoomAtPoint() was used |
| NoLocation | Element has no LocationPoint or LocationCurve |
| NoRoomFound | Point tested but no room contains it |

---

### LevelBandPosition

Enum describing element's position relative to level band.

| Value | Description |
|--------|-------------|
| BelowBand | Element.Max.Z <= BaseLevel.Elevation |
| AboveBand | Element.Min.Z >= TopLevel.Elevation |
| InBand | Element intersects band (Min.Z < TopLevel.Elevation AND Max.Z > BaseLevel.Elevation) |
| NoBoundingBox | Element has no bounding box |

---

## Data Flow

```
FillConfiguration
       |
       v
[FilteredElementCollector by Category]
       |
       v
[Level Band Filter]
       |
       +---> InBand --> [Room Detection] --> [Parameter Write]
       |
       +---> BelowBand --> Skip (SkippedNotInLevelBand++)
       |
       +---> AboveBand --> Skip (SkippedNotInLevelBand++)
       |
       +---> NoBoundingBox --> Skip (SkippedNoBoundingBox++)
```

## State Transitions

### Element Processing State

```
[Element Collected]
       |
       v
[Has BoundingBox?] --> No --> [SkippedNoBoundingBox++]
       |
       Yes
       v
[In Level Band?] --> No --> [SkippedNotInLevelBand++]
       |
       Yes
       v
[Has Location?] --> No --> [SkippedNoLocation++]
       |
       Yes
       v
[Find Room]
       |
       v
[Room Found?] --> No --> [SkippedNoRoomFound++]
       |
       Yes
       v
[Parameters Exist?] --> No --> [SkippedParameterMissing++]
       |
       Yes
       v
[Parameters Writable?] --> No --> [SkippedParameterReadOnly++]
       |
       Yes
       v
[Overwrite OR Value Empty?] --> No --> [SkippedValueExists++]
       |
       Yes
       v
[Set Parameter] --> [Success++]
```

## Validation Rules

1. **Level Validation**: BaseLevel.Elevation < TopLevel.Elevation (strictly less than)
2. **Category Validation**: At least one category must be selected
3. **Parameter Name Validation**: All parameter names must be non-empty
4. **Element Filter**: Exclude element types (ElementType) - only process instances
5. **Phase Validation**: Use document.ActivePhase for all room operations
6. **Write Safety**: Never write to read-only parameters

## Defaults

| Setting | Default Value |
|----------|----------------|
| OverwriteExisting | false |
| SelectedCategories | [OST_Doors, OST_Windows, OST_Furniture, OST_MechanicalEquipment, OST_GenericModel] |
| LevelParameter | "ACG-4D-Level" |
| RoomNumberParameter | "ACG-4D-RoomNumber" |
| RoomNameParameter | "ACG-4D-RoomName" |
| RoomRefParameter | "ACG-4D-RoomRef" |
