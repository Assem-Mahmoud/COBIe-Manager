# Feature Specification: Auto-Fill Revit Parameters

**Feature Branch**: `003-auto-fill-parameters`
**Created**: 2025-02-12
**Status**: Draft
**Input**: User description: "Auto-fill Revit project parameters based on level range and room ownership"

## Clarifications

### Session 2025-02-12

- Q: What should be explicitly documented as out-of-scope for Milestone 1? → A: Walls, Floors, Ceilings, and any element categories requiring solid/geometry intersection for room detection (these require SpatialElementGeometryCalculator)
- Q: What format should the detailed log export use? → A: Plain text file (.txt) saved to a user-selected location with timestamp in filename
- Q: How should the system handle phase mismatches between elements and rooms? → C: Use the document's active phase for all room lookups (element's phase ignored)

## User Scenarios & Testing *(mandatory)*

### Scope

**Milestone 1 - In Scope**:
- Element Categories: Doors, Windows, Furniture, Mechanical Equipment, Generic Models
- Level assignment using bounding box intersection band rule
- Room assignment using direct Room property, FromRoom/ToRoom for doors, and point-in-room test

**Milestone 1 - Out of Scope**:
- Walls, Floors, Ceilings (require SpatialElementGeometryCalculator for geometry-based room intersection)
- Any element categories requiring solid/geometry intersection for room detection
- Multi-level batch processing (processing all levels at once)
- Background processing with progress UI

### User Story 1 - Fill Parameters by Level Range (Priority: P1)

A Revit user needs to automatically assign level information to multiple model elements (doors, windows, furniture, equipment) based on their vertical position between two selected levels.

**Why this priority**: This is the core automation capability that eliminates manual data entry for 4D/5D workflow parameters, delivering immediate time savings.

**Independent Test**: Can be fully tested by selecting two levels in a Revit model with elements between them and verifying that the level parameter is correctly assigned to all elements within the vertical band.

**Acceptance Scenarios**:

1. **Given** a Revit document with levels defined and elements placed between them, **When** the user selects Base Level (e.g., "Level 1") and Top Level (e.g., "Level 2") and executes the fill command, **Then** all elements whose bounding box intersects the vertical band between these levels receive the level parameter value set to the Base Level name
2. **Given** an element whose bounding box crosses multiple levels, **When** using the intersection band rule, **Then** the element is assigned to the base level if any part of its bounding box intersects the band
3. **Given** an element with no bounding box, **When** processing occurs, **Then** the element is skipped and logged with a warning

---

### User Story 2 - Fill Parameters by Room Ownership (Priority: P2)

A Revit user needs to automatically assign room information (number, name) to model elements based on which room each element belongs to or is located within.

**Why this priority**: Room assignment is critical for facilities management and COBie data exchange, building on the level assignment foundation.

**Independent Test**: Can be fully tested by having elements placed in rooms and verifying that room number and name parameters are correctly assigned based on room association.

**Acceptance Scenarios**:

1. **Given** a FamilyInstance (door, window, furniture) with a Room property available, **When** the element has an associated room, **Then** the room number and name parameters are populated from that room
2. **Given** a door with FromRoom and ToRoom properties, **When** processing occurs, **Then** FromRoom is preferred, falling back to ToRoom if FromRoom is null
3. **Given** an element with a LocationPoint, **When** no direct Room property exists, **Then** the system performs a point-in-room test to find the containing room
4. **Given** an element with a LocationCurve (e.g., certain beam types), **When** no direct Room property exists, **Then** the system uses the curve midpoint for the point-in-room test
5. **Given** an element not located in any room, **When** processing occurs, **Then** the element is logged with the reason and room parameters remain unchanged (or set to N/A based on user preference)

---

### User Story 3 - Preview and Apply with Overwrite Control (Priority: P3)

A Revit user wants to see what changes will be made before committing them, and control whether existing parameter values should be preserved or overwritten.

**Why this priority**: This provides safety and user confidence, preventing accidental data loss while still allowing bulk updates when desired.

**Independent Test**: Can be fully tested by running the preview to see counts of affected elements, then applying with and without overwrite enabled to verify different behaviors.

**Acceptance Scenarios**:

1. **Given** elements with existing parameter values, **When** overwrite option is disabled, **Then** only empty (null) parameters are filled and existing values are preserved
2. **Given** elements with existing parameter values, **When** overwrite option is enabled, **Then** all matching parameters are updated regardless of current values
3. **Given** a fill operation configured, **When** the user clicks Preview, **Then** a summary is displayed showing counts of elements to be processed, elements to be skipped, and estimated changes
4. **Given** a completed fill operation, **When** processing finishes, **Then** a summary report is displayed showing total scanned, parameters filled, and elements skipped with reasons

---

### User Story 4 - Category Selection (Priority: P4)

A Revit user wants to choose which element categories to process, allowing selective parameter filling for specific element types.

**Why this priority**: Provides flexibility for users who only need to process certain element types, reducing processing time and avoiding unwanted changes.

**Independent Test**: Can be fully tested by selecting only "Doors" and "Windows" categories and verifying that only those elements are processed.

**Acceptance Scenarios**:

1. **Given** multiple element categories in the model, **When** the user selects specific categories (e.g., Doors, Windows, Furniture), **Then** only elements from the selected categories are processed
2. **Given** the category selection UI, **When** presented, **Then** the default selection includes Doors, Windows, Furniture, Mechanical Equipment, and Generic Models
3. **Given** a category with no elements in the model, **When** processing occurs, **Then** the category is handled gracefully with zero elements processed

---

### User Story 5 - Fill Parameters by Group (Priority: P2)

A Revit user needs to automatically assign group identification information to elements based on their GroupId parameter value, filling the ACG-BOXId parameter with the corresponding group name.

**Why this priority**: Group-based parameter filling is essential for tracking elements that belong to specific assembly groups (e.g., prefabricated units, equipment clusters) for facilities management and COBie data exchange.

**Independent Test**: Can be fully tested by having elements with GroupId parameter values set and verifying that the ACG-BOXId parameter is correctly filled with the group name for those elements.

**Acceptance Scenarios**:

1. **Given** a Revit document with elements that have a GroupId parameter containing a group identifier value, **When** the user selects the "Fill by Group" option and executes the command, **Then** all elements with a non-empty GroupId parameter receive the corresponding group name in their ACG-BOXId parameter
2. **Given** an element with a GroupId parameter containing value "GRP-001", **When** processing occurs, **Then** the system looks up the group name associated with "GRP-001" and assigns it to the element's ACG-BOXId parameter
3. **Given** an element with an empty or null GroupId parameter, **When** processing occurs, **Then** the element is skipped and logged with an informational message
4. **Given** an element with a GroupId value that does not match any known group, **When** processing occurs, **Then** the element is skipped and logged with a warning message
5. **Given** elements with existing ACG-BOXId parameter values, **When** overwrite option is disabled, **Then** only elements with empty ACG-BOXId parameters are updated
6. **Given** elements with existing ACG-BOXId parameter values, **When** overwrite option is enabled, **Then** all elements with valid GroupId values receive the group name regardless of existing ACG-BOXId values

---

### Edge Cases

- What happens when an element's bounding box is null or invalid? → Skip with warning log
- What happens when a room cannot be determined for an element? → Skip room assignment for that element, log the ElementId and reason
- What happens when the target parameter doesn't exist on an element? → Skip that parameter for that element, log warning
- What happens when the target parameter is read-only? → Skip with warning log, do not attempt to set
- What happens when Base Level elevation equals or exceeds Top Level elevation? → Show validation error and prevent execution
- What happens when an element belongs to a different phase than the rooms? → Use the document's active phase for all room lookups (element's phase is ignored)
- What happens when processing is interrupted (document closed, Revit shutdown)? → Transaction should roll back, no partial changes committed
- What happens when there are no rooms in the model? → Level assignment proceeds, room assignment is skipped with informational log
- What happens when an element's GroupId parameter value doesn't match any known group? → Skip that element and log warning with the GroupId value
- What happens when an element doesn't have a GroupId parameter? → Skip that element for group filling, continue processing other elements

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow user to select Base Level and Top Level from available levels in the Revit document
- **FR-002**: System MUST validate that Base Level elevation is less than Top Level elevation before processing
- **FR-003**: System MUST collect all elements from user-selected categories that are not element types
- **FR-004**: System MUST determine which elements are located within the vertical band between Base Level and Top Level using the intersection band rule (element bounding box intersects the band defined by z0 < bbox.Max.Z AND z1 > bbox.Min.Z)
- **FR-005**: System MUST assign the Base Level name to the level parameter (e.g., "ACG-4D-Level") for all elements within the vertical band
- **FR-006**: System MUST determine room ownership for elements using a tiered strategy: (1) direct Room property if available, (2) FromRoom/ToRoom for doors, (3) point-in-room test using LocationPoint or LocationCurve midpoint
- **FR-007**: System MUST assign room number, room name, and combined reference to the respective parameters for elements with an associated room
- **FR-008**: System MUST skip elements without bounding boxes and log them with warnings
- **FR-009**: System MUST skip elements without location information for room assignment and log them with warnings
- **FR-010**: System MUST only set parameters that exist and are writable on each element
- **FR-011**: System MUST provide an option to preserve or overwrite existing parameter values
- **FR-012**: System MUST display a summary report after processing showing: total elements scanned, parameters filled, elements skipped with counts by reason. System MUST also support exporting a detailed log as a plain text file (.txt) with timestamp in filename to a user-selected location.
- **FR-013**: System MUST provide a preview mode showing expected changes before applying
- **FR-014**: System MUST allow user to select which element categories to process
- **FR-015**: System MUST use a single transaction for processing to ensure atomicity or transaction group for large models with periodic commits
- **FR-016**: System MUST support the following parameter names as defaults (configurable): "ACG-4D-Level", "ACG-4D-RoomNumber", "ACG-4D-RoomName", "ACG-4D-RoomRef"
- **FR-017**: System MUST allow user to select "Fill by Group" mode to fill ACG-BOXId parameter based on element GroupId parameter values
- **FR-018**: System MUST read the GroupId parameter value from each element to determine group membership
- **FR-019**: System MUST skip elements that have an empty or null GroupId parameter value
- **FR-020**: System MUST look up the group name corresponding to each element's GroupId parameter value
- **FR-021**: System MUST assign the group name to the ACG-BOXId parameter for elements with valid GroupId values
- **FR-022**: System MUST skip elements whose GroupId value does not match any known group and log a warning
- **FR-023**: System MUST respect the overwrite option when filling ACG-BOXId parameter (preserve existing values when overwrite is disabled)

### Key Entities

- **Level Assignment Rule**: Defines how elements are matched to levels based on their vertical position (bounding box intersection with level band)
- **Room Assignment Result**: Contains the found room (if any) and the method used to determine it (direct property, point-in-room test, etc.)
- **Group Assignment Result**: Contains the group name derived from an element's GroupId parameter value
- **Processing Summary**: Aggregate counts and statistics for the operation including total processed, successful assignments, skips by reason
- **Element Category**: Revit element categories eligible for processing (Doors, Windows, Furniture, Mechanical Equipment, Generic Models)
- **Parameter Configuration**: Mapping of logical parameter names (Level, RoomNumber, RoomName, RoomRef, GroupId, BOXId) to actual Revit parameter names

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can fill level and room parameters for 1000 elements in under 30 seconds
- **SC-002**: 95% of eligible elements (FamilyInstances with valid locations) successfully receive room assignments
- **SC-003**: 100% of elements with valid bounding boxes within the level band receive correct level assignment
- **SC-004**: Zero data corruption occurs - processing can be rolled back completely if interrupted
- **SC-005**: Users can accurately predict processing outcomes through preview mode (actual results match preview within 5% variance)
- **SC-006**: All skipped elements are logged with specific reasons enabling user investigation and correction
- **SC-007**: The feature works on models with up to 10,000 processable elements without performance degradation
- **SC-008**: Users report a 90% reduction in time spent manually entering level and room parameters compared to previous workflow
- **SC-009**: 100% of elements with valid GroupId parameter values successfully receive the correct group name in their ACG-BOXId parameter
- **SC-010**: Elements without GroupId values are correctly identified and skipped with appropriate logging
