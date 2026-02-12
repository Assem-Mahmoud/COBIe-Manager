# Milestone 1 — Auto-Fill Revit Project Parameters (Level + Room)

## Objective
You already created Revit **Project Parameters** from APS definitions.  
Now you will **automatically fill** selected parameters for elements based on:

1. **Level range** (e.g., elements between *Level 1* and *Level 2* → assign *Level 1*)
2. **Room ownership** (detect which Room an element belongs to → assign `RoomNumber-RoomName`)

This milestone focuses on **reliable automation** and **clear logging**.

---

## Scope (Milestone 1)
### Parameters to Fill (example)
- `ACG-4D-Level` (string) — set to base level name (e.g., `Level 1`)
- `ACG-4D-RoomNumber` (string) — set to room number (e.g., `101`)
- `ACG-4D-RoomName` (string) — set to room name (e.g., `Office`)




### Element Categories (suggested initial set)
- Doors, Windows, Furniture, Mechanical Equipment, Generic Models  
(These are easiest for Room association in Milestone 1.)

> Walls/Floors/Ceilings can be added later (Milestone 2) using heavier geometry logic.

---

## User Inputs (UI)
1. **Base Level**: `L0` (e.g., Level 1)
2. **Top Level**: `L1` (e.g., Level 2)
---

## High-Level Workflow

### Step 1 — Collect Levels
- Fetch `Level` elements and pick:
  - `BaseLevel` (L0)
  - `TopLevel` (L1)

Store elevations:
- `z0 = BaseLevel.Elevation`
- `z1 = TopLevel.Elevation`

> Revit internal length units are feet. Compare values directly (same unit system).

---

### Step 2 — Collect Target Elements
Use `FilteredElementCollector(doc)`:
- `WhereElementIsNotElementType()`
- Filter by  categories
- Skip elements:
  - without category
  - view-specific (optional filter)
  - elements that cannot accept bound parameters (rare, but keep log)

---

## Part A — Fill by Level Range

### A1) Define “between levels” rule
Choose one rule (recommended: **Intersection band**).

#### Rule A (simple, fast)
Assign BaseLevel if:
- `bbox.Min.Z >= z0 && bbox.Min.Z < z1`

#### Rule B (recommended, robust)
Assign BaseLevel if element intersects the vertical band:
- `(bbox.Min.Z < z1) && (bbox.Max.Z > z0)`

> Rule B handles tall elements that cross levels.

### A2) Procedure
For each element:
1. `bbox = element.get_BoundingBox(null)`
2. If bbox is null → skip with warning
3. Evaluate rule A or B
4. If match:
   - set `ACG-4D-Level = BaseLevel.Name`
   

### A3) Output
- Count:
  - scanned elements
  - assigned level
  - skipped (no bbox, out of range)

---

## Part B — Fill by Room Ownership

Room assignment differs by element type. Use a tiered strategy:

### B1) Strategy priority
#### Method 1 (best for many instances)
Use built-in properties on `FamilyInstance`:
- `fi.Room` (if available)
- doors: `fi.FromRoom` / `fi.ToRoom`

**Rule:**
- Prefer `Room`, else `FromRoom`, else `ToRoom`

#### Method 2 (general, works for many types)
Use point-in-room test:
1. Determine a representative point:
   - If `LocationPoint` → use point
   - If `LocationCurve` → use midpoint
2. Find the room containing the point:
   - Iterate candidate rooms and use `room.IsPointInRoom(point)` (or equivalent available in your Revit version)
3. If room found → fill parameters

> For stability, you can adjust the point Z slightly above the room level elevation.

#### Method 3 (Milestone 2+)
Solid/geometry intersection:
- `SpatialElementGeometryCalculator` to get room solid
- intersect with element solid

Not required for this milestone.

---

### B2) Room data to write
If room found:
- `ACG-4D-RoomNumber = room.Number`
- `ACG-4D-RoomName = room.Name`
- `ACG-4D-RoomRef = $"{room.Number}-{room.Name}"`

If room not found:
- leave fields empty OR set `N/A` (your choice)
- log the element id + category + reason (no location / no room match)

---

## Transactions & Performance

### Recommended transaction approach
- Use a single `Transaction` for fastest performance in mid-size models
- For huge models:
  - use `TransactionGroup`
  - commit every N elements (e.g., 500–2000)
  - keep UI responsive

### Logging (must-have)
Maintain counters + sample ids:
- Total elements scanned
- Filled level count
- Filled room count
- Skipped counts:
  - missing bbox
  - missing location
  - no room found
  - parameter missing / read-only

---

## Safety Rules
1. **Only set parameter if it exists** and is writable:
   - `param != null && !param.IsReadOnly`
2. Avoid exceptions by type-safe setting:
   - string params: `param.Set(value)`
   - integer: `param.Set(intValue)`
   - yes/no: `param.Set(0/1)` or boolean mapping
3. Don’t overwrite user data unless explicitly enabled:
   - Add option: “Overwrite existing values”

---

## Deliverables (Milestone 1)
1. `LevelAssigner` service
   - `bool IsBetweenLevels(Element e, Level baseLevel, Level topLevel)`
   - `void AssignLevelParam(Element e, string paramName, Level baseLevel)`
2. `RoomAssigner` service
   - `Room? GetRoomForElement(Element e, IList<Room> rooms, Phase phase)`
   - `void AssignRoomParams(Element e, Room room, ...)`
3. `ParameterWriter` helper (safe lookup & set)
4. UI page:
   - level selection, top selection
   - categories selection
   - preview/apply + overwrite option
5. Summary report + detailed log export

---

## Acceptance Criteria
- Run completes without exceptions on typical project model
- Elements between BaseLevel and TopLevel get correct `ACG-4D-Level`
- Most placed instances (doors/furniture/equipment/generic models) get correct room values
- Skips are logged clearly with reasons

---

## Notes / Next Milestone Ideas
- Add support for Walls/Floors/Ceilings via geometry-based room intersection
- Add phase-aware room selection
- Add multi-level assignment (Level 1, Level 2, Level 3 … automatically)
- Add background processing + progress UI

