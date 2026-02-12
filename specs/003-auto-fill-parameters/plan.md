# Implementation Plan: Auto-Fill Revit Parameters

**Branch**: `003-auto-fill-parameters` | **Date**: 2025-02-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-auto-fill-parameters/spec.md`

## Summary

This feature adds a new command to the COBIe Manager Revit plugin that automatically fills project parameters on model elements based on:
1. **Level Range**: Elements located vertically between a Base Level and Top Level receive a level parameter value
2. **Room Ownership**: Elements associated with rooms receive room number, name, and reference parameters

The implementation follows existing patterns in the codebase: MVVM with CommunityToolkit.Mvvm, Material Design WPF UI, service-based architecture with dependency injection, and Revit API best practices.

## Technical Context

**Language/Version**: C# 10, .NET Framework 4.8, Revit API 2023-2026
**Primary Dependencies**:
- Revit API (Autodesk.Revit.DB, Autodesk.Revit.UI)
- Revit.Async (for async operations in Revit context)
- CommunityToolkit.Mvvm (MVVM framework)
- MaterialDesignThemes (WPF UI components)
- System.ComponentModel.Composition (DI container)

**Storage**: File-based logging to %APPDATA%\COBIeManager\Logs\
**Target Platform**: Windows 10+, Revit 2023/2024/2025/2026
**Project Type**: Single (Revit plugin with shared services)
**Performance Goals**:
- Fill parameters for 1000 elements in under 30 seconds
- Support models with up to 10,000 processable elements
- Preview mode should return counts in under 5 seconds

**Constraints**:
- All Revit API calls must occur within a Transaction
- Cannot use Revit API on background threads
- Must support multi-version Revit (2023-2026)
- Must handle documents with no rooms gracefully

**Scale/Scope**:
- 5 element categories (Doors, Windows, Furniture, Mechanical Equipment, Generic Models)
- 4 target parameters (Level, RoomNumber, RoomName, RoomRef)
- Default parameter names: "ACG-4D-Level", "ACG-4D-RoomNumber", "ACG-4D-RoomName", "ACG-4D-RoomRef"

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Note**: No project constitution exists yet (.specify/memory/constitution.md is a template). The following gates are based on established project patterns documented in CLAUDE.md.

| Gate | Status | Notes |
|-------|--------|-------|
| Follows established feature structure | PASS | Will use Features/ParameterFiller/ folder pattern |
| Uses MVVM with CommunityToolkit.Mvvm | PASS | ViewModel will inherit from ObservableObject |
| Uses Material Design WPF | PASS | XAML views will use MaterialDesign components |
| Services registered in DI | PASS | Will register services in App.cs InitializeDependencyInjection() |
| Command uses RevitTask.Initialize() | PASS | ExternalCommand will follow async pattern |
| Transaction handling | PASS | Single transaction with rollback support |
| Uses ServiceLocator for service access | PASS | ViewModels will access services via ServiceLocator |

## Project Structure

### Documentation (this feature)

```text
specs/003-auto-fill-parameters/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── contracts/           # Phase 1 output (internal service contracts)
    └── parameter-fill-services.yaml
```

### Source Code (repository root)

```text
Features/ParameterFiller/
├── Commands/
│   └── ParameterFillCommand.cs          # IExternalCommand entry point
├── ViewModels/
│   └── ParameterFillViewModel.cs       # MVVM ViewModel with observable properties
├── Views/
│   ├── ParameterFillWindow.xaml          # Main WPF window
│   └── ParameterFillWindow.xaml.cs     # Code-behind
└── Models/
    ├── LevelSelection.cs                  # Level selection UI model
    ├── CategorySelection.cs               # Category selection UI model
    ├── ProcessingSummary.cs               # Summary statistics
    └── ParameterMapping.cs               # Parameter name configuration

Shared/
├── Interfaces/
│   ├── ILevelAssignmentService.cs        # Level band detection
│   ├── IRoomAssignmentService.cs         # Room detection
│   ├── IParameterFillService.cs          # Orchestration service
│   └── IProcessingLogger.cs            # Log export service
├── Services/
│   ├── LevelAssignmentService.cs          # Implements level band logic
│   ├── RoomAssignmentService.cs          # Implements room detection logic
│   ├── ParameterFillService.cs           # Orchestrates fill operation
│   └── ProcessingLogger.cs              # Handles log file export
```

**Structure Decision**: Single project structure following established Features/ pattern. All new services go into Shared/ following the existing service architecture.

## Phase 0: Research

### Research Tasks

| Task | Status | Decision | Rationale |
|-------|----------|------------|------------|
| Revit API: Room detection methods | COMPLETE | Use tiered approach: Room property, FromRoom/ToRoom, then Room.GetRoomAtPoint() | Room property is fastest; point-in-room is reliable fallback |
| Revit API: BoundingBox access for categories | COMPLETE | Use element.get_BoundingBox(null) for all element types | Most FamilyInstances have valid bounding boxes |
| Revit API: Transaction pattern for bulk updates | COMPLETE | Single Transaction with periodic commits for large models | Balances performance with rollback safety |
| Revit API: Phase handling for room lookup | COMPLETE | Use document.ActivePhase for all room queries | Clarified requirement - use active phase |
| Revit API: Parameter write safety | COMPLETE | Check Parameter.IsReadOnly and existence before Set() | Prevents exceptions on locked/missing params |

### Research Findings

**Revit API Room Detection Strategy**:
- FamilyInstance.Room property - fastest, directly available for many instances
- FamilyInstance.FromRoom/ToRoom - door-specific, prefer FromRoom
- Room.GetRoomAtPoint(new XYZ(x, y, z)) - reliable point-in-room test
- SpatialElementGeometryCalculator - NOT used in Milestone 1 (deferred for Walls/Floors/Ceilings)

**Bounding Box Considerations**:
- get_BoundingBox(null) returns bounding box in model coordinates
- Z values are in project units (feet for imperial, meters for metric)
- Intersection band rule: (bbox.Min.Z < topLevel.Elevation) AND (bbox.Max.Z > baseLevel.Elevation)

**Transaction Pattern**:
- For models < 5000 elements: Single Transaction
- For models >= 5000 elements: TransactionGroup with sub-transactions every 1000 elements
- All operations within a try-catch with Rollback on exception

## Phase 1: Design & Contracts

### Data Model

See [data-model.md](./data-model.md) for complete entity definitions.

### Service Contracts

#### ILevelAssignmentService

```yaml
interface: ILevelAssignmentService
description: Determines if elements are within a level band and assigns level parameter
methods:
  - name: IsElementInLevelBand
    input:
      element: Element
      baseLevel: Level
      topLevel: Level
    output: boolean
    description: Returns true if element's bounding box intersects the level band
  - name: AssignLevelParameter
    input:
      element: Element
      parameterName: string
      levelName: string
      overwrite: boolean
    output: ParameterAssignmentResult
    description: Safely assigns level parameter value to element
```

#### IRoomAssignmentService

```yaml
interface: IRoomAssignmentService
description: Determines room ownership for elements and assigns room parameters
methods:
  - name: GetRoomForElement
    input:
      element: Element
      document: Document
      phase: Phase
    output: Room?
    description: Returns associated room using tiered detection strategy
  - name: AssignRoomParameters
    input:
      element: Element
      room: Room
      parameterNames: RoomParameterMapping
      overwrite: boolean
    output: RoomAssignmentResult
    description: Assigns room number, name, and reference parameters
```

#### IParameterFillService

```yaml
interface: IParameterFillService
description: Orchestrates the parameter fill operation
methods:
  - name: PreviewFill
    input:
      document: Document
      config: FillConfiguration
    output: PreviewSummary
    description: Analyzes elements and returns preview counts without modifying document
  - name: ExecuteFill
    input:
      document: Document
      config: FillConfiguration
      progressAction: Action<int, string>
    output: ProcessingSummary
    description: Executes the fill operation with progress reporting
```

#### IProcessingLogger

```yaml
interface: IProcessingLogger
description: Handles processing log export
methods:
  - name: ExportLog
    input:
      summary: ProcessingSummary
      filePath: string
    output: void
    description: Exports detailed processing log to text file
```

### UI Contracts

#### FillConfiguration

```yaml
name: FillConfiguration
description: User configuration for parameter fill operation
properties:
  - name: BaseLevel
    type: Level
    description: The lower level of the vertical band
  - name: TopLevel
    type: Level
    description: The upper level of the vertical band
  - name: SelectedCategories
    type: IList<BuiltInCategory>
    description: Element categories to process
  - name: OverwriteExisting
    type: boolean
    description: Whether to overwrite existing parameter values
  - name: ParameterMapping
    type: ParameterMapping
    description: Mapping of logical parameter names to actual Revit parameter names
```

### Quickstart

See [quickstart.md](./quickstart.md) for developer onboarding guide.

## Complexity Tracking

> No constitution violations - this table is empty

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| | | |
