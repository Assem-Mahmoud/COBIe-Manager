# Tasks: Auto-Fill Revit Parameters

**Input**: Design documents from `/specs/003-auto-fill-parameters/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

## Path Conventions

- **Single project**: `Features/`, `Shared/`
- Services: `Shared/Services/`
- Interfaces: `Shared/Interfaces/`
- Commands: `Features/[Feature]/Commands/`
- ViewModels: `Features/[Feature]/ViewModels/`
- Views: `Features/[Feature]/Views/`
- Models: `Features/[Feature]/Models/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and feature folder structure

- [X] T001 Create feature folder structure Features/ParameterFiller/ with subfolders Commands/, ViewModels/, Views/, Models/
- [X] T002 Create service interfaces in Shared/Interfaces/: ILevelAssignmentService.cs, IRoomAssignmentService.cs, IParameterFillService.cs, IProcessingLogger.cs
- [X] T003 [P] Create LevelBandPosition enum in Shared/Models/LevelBandPosition.cs
- [X] T004 [P] Create RoomDetectionMethod enum in Shared/Models/RoomDetectionMethod.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core services that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Implement LevelAssignmentService in Shared/Services/LevelAssignmentService.cs with IsElementInLevelBand() and AssignLevelParameter() methods
- [X] T006 [P] Implement RoomAssignmentService in Shared/Services/RoomAssignmentService.cs with GetRoomForElement() and AssignRoomParameters() methods
- [X] T007 [P] Implement ProcessingLogger in Shared/Services/ProcessingLogger.cs with ExportLog() and GenerateLogContent() methods
- [X] T008 Create FillConfiguration model in Features/ParameterFiller/Models/FillConfiguration.cs with BaseLevel, TopLevel, SelectedCategories, OverwriteExisting, ParameterMapping properties
- [X] T009 Create ParameterMapping model in Features/ParameterFiller/Models/ParameterMapping.cs with LevelParameter, RoomNumberParameter, RoomNameParameter, RoomRefParameter properties
- [X] T010 Create ProcessingSummary model in Features/ParameterFiller/Models/ProcessingSummary.cs with all count and skip tracking properties
- [X] T011 Create ParameterAssignmentResult model in Features/ParameterFiller/Models/ParameterAssignmentResult.cs with Success, Skipped, SkipReason, ElementId properties
- [X] T012 Create RoomAssignmentResult model in Features/ParameterFiller/Models/RoomAssignmentResult.cs with Room, DetectionMethod, ParametersAssigned, ElementId properties
- [X] T013 Create PreviewSummary model in Features/ParameterFiller/Models/PreviewSummary.cs with EstimatedElementsToProcess, EstimatedRoomAssignments, CategoriesWithNoElements, ValidationWarnings properties

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Fill Parameters by Level Range (Priority: P1) 🎯 MVP

**Goal**: Automatically assign level information to elements based on their vertical position between two selected levels

**Independent Test**: Select two levels in a Revit model with elements between them and verify that level parameter is correctly assigned to all elements within the vertical band

### Implementation for User Story 1

- [X] T014 [US1] Implement ParameterFillService in Shared/Services/ParameterFillService.cs with PreviewFill() method (depends on T005, T008, T010)
- [X] T015 [US1] Implement ParameterFillService.ExecuteFill() method with transaction handling and progress callback (depends on T005, T006, T008, T010, T014)
- [X] T016 [US1] Create ParameterFillViewModel in Features/ParameterFiller/ViewModels/ParameterFillViewModel.cs with ObservableProperties for config and preview summary
- [X] T017 [US1] Add PreviewCommand and ExecuteFillCommand RelayCommand properties to ParameterFillViewModel
- [X] T018 [US1] Create ParameterFillWindow.xaml in Features/ParameterFiller/Views/ with level selection ComboBoxes, category selection CheckBoxes, overwrite CheckBox, Preview/Apply buttons
- [X] T019 [US1] Create ParameterFillWindow.xaml.cs code-behind with ViewModel initialization in constructor AFTER InitializeComponent()
- [X] T020 [US1] Wire up Preview button to show preview summary in MessageBox
- [X] T021 [US1] Wire up Apply button to execute ParameterFillService.ExecuteFill() and display results
- [X] T022 [US1] Add validation for BaseLevel < TopLevel elevation requirement with error display
- [X] T023 [US1] Create ParameterFillCommand in Features/ParameterFiller/Commands/ParameterFillCommand.cs with RevitTask.Initialize() and async window launch
- [X] T024 [US1] Register ParameterFillCommand ribbon button in App.cs OnStartup() with tooltip "Auto-fill level and room parameters"
- [X] T025 [US1] Register services in App.cs: ILevelAssignmentService, IRoomAssignmentService, IParameterFillService, IProcessingLogger
- [X] T026 [US1] Add assembly resolver setup to ParameterFillCommand (copy from CobieParametersCommand pattern)

**Checkpoint**: User Story 1 (Level Range Fill) should be fully functional and independently testable

---

## Phase 4: User Story 2 - Fill Parameters by Room Ownership (Priority: P2)

**Goal**: Automatically assign room information (number, name) to elements based on room association

**Independent Test**: Have elements placed in rooms and verify that room number and name parameters are correctly assigned based on room association

### Implementation for User Story 2

- [ ] T027 [US2] Implement RoomAssignmentService.GetRoomForElement() with tiered strategy: Room property, FromRoom/ToRoom for doors, point-in-room fallback (depends on T006)
- [ ] T028 [P] Create helper method GetElementPoint() in RoomAssignmentService to extract LocationPoint or LocationCurve midpoint
- [ ] T029 [US2] Integrate room assignment into ParameterFillService.ExecuteFill() - call GetRoomForElement() and AssignRoomParameters() for each element (depends on T015, T027)
- [ ] T030 [US2] Update ParameterFillViewModel to include room parameter configuration UI (checkboxes to enable/disable room parameter fills)
- [ ] T031 [US2] Update ParameterFillWindow.xaml to include room parameter configuration controls
- [ ] T032 [US2] Add room assignment statistics to ProcessingSummary display

**Checkpoint**: User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Preview and Apply with Overwrite Control (Priority: P3)

**Goal**: Provide preview mode and overwrite option to prevent accidental data loss

**Independent Test**: Run preview to see counts, then apply with and without overwrite enabled to verify different behaviors

### Implementation for User Story 3

- [ ] T033 [US3] Implement ParameterFillService.PreviewFill() to return PreviewSummary without document modifications (depends on T014)
- [ ] T034 [US3] Add OverwriteExisting boolean property to FillConfiguration model
- [ ] T035 [US3] Add overwrite CheckBox to ParameterFillWindow.xaml with IsChecked binding
- [ ] T036 [US3] Update ParameterFillService.ExecuteFill() to respect OverwriteExisting flag - skip existing values when false (depends on T015, T034)
- [ ] T037 [US3] Update ProcessingLogger to include overwrite statistics in log output
- [ ] T038 [US3] Add overwrite count to ProcessingSummary model

**Checkpoint**: User Stories 1, 2, AND 3 should all work independently

---

## Phase 6: User Story 4 - Category Selection (Priority: P4)

**Goal**: Allow users to choose which element categories to process

**Independent Test**: Select only specific categories and verify that only those elements are processed

### Implementation for User Story 4

- [ ] T039 [US4] Add SelectedCategories IList<BuiltInCategory> property to FillConfiguration model
- [ ] T040 [P] Create RevitCategoryHelper in Shared/Utils/RevitCategoryHelper.cs with GetBuiltInCategories() method for target categories
- [ ] T041 [US4] Add category selection UI to ParameterFillWindow.xaml with CheckBoxes for Doors, Windows, Furniture, MechanicalEquipment, GenericModels (default checked)
- [ ] T042 [US4] Initialize default selected categories in ParameterFillViewModel constructor
- [ ] T043 [US4] Update ParameterFillService.PreviewFill() and ExecuteFill() to filter by SelectedCategories
- [ ] T044 [US4] Add CategoriesWithNoElements list to PreviewSummary for categories with zero elements in model

**Checkpoint**: All user stories should now be independently functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T045 [P] Add SaveFileDialog for log file export when user clicks "Export Log" button
- [ ] T046 [P] Implement TransactionGroup pattern for large models (>= 5000 elements) with periodic commits every 1000 elements
- [ ] T047 [P] Add progress bar UI updates during ExecuteFill() via progress callback
- [ ] T048 [P] Handle documents with no rooms gracefully - show informational message, skip room assignment
- [ ] T049 Add error handling for null bounding boxes in level assignment
- [ ] T050 Add detailed log content formatting with sections: Summary, Skipped Elements, Processing Details
- [ ] T051 [P] Add phase handling - use document.ActivePhase for all Room.GetRoomAtPoint() calls
- [ ] T052 Register new services in App.cs InitializeDependencyInjection() with singleton pattern
- [ ] T053 Update CLAUDE.md with ParameterFill feature documentation if needed
- [ ] T054 Run build.ps1 to verify compilation for all Revit versions
- [ ] T055 Test in Revit 2024 with sample model containing doors, windows, rooms

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Integrates with US1/US2 but should be independently testable
- **User Story 4 (P4)**: Can start after Foundational (Phase 2) - Integrates with US1/US2/US3 but should be independently testable

### Within Each User Story

- Models before services (except foundational models created in Phase 2)
- Services before integration
- Core implementation before UI tasks
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, User Story 1, 2, 3, and 4 can start in parallel (if team capacity allows)
- All UI tasks marked [P] can run in parallel (different XAML files)
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all parallelizable tasks for User Story 1 together:
Task: "T016 [US1] Create ParameterFillViewModel"
Task: "T018 [US1] Create ParameterFillWindow.xaml"
Task: "T019 [US1] Create ParameterFillWindow.xaml.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Build and verify functionality
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3 + Story 4
3. Stories complete and integrate independently

---

## Summary

**Total Task Count**: 55 tasks
**Tasks per User Story**:
- Setup: 4 tasks
- Foundational: 9 tasks
- User Story 1 (P1): 13 tasks
- User Story 2 (P2): 6 tasks
- User Story 3 (P3): 6 tasks
- User Story 4 (P4): 6 tasks
- Polish: 11 tasks

**Parallel Opportunities Identified**: 35+ tasks marked [P] for parallel execution

**MVP Scope**: Phase 1 + Phase 2 + Phase 3 = 26 tasks for core level fill functionality

**Independent Test Criteria**: Each user story phase includes models, services, UI, and integration to be independently testable
