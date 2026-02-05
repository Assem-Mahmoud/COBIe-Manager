# Implementation Tasks: COBie Parameter Management

**Feature**: COBie Parameter Management for Revit
**Branch**: `001-cobie-params`
**Created**: 2025-02-05
**Status**: Draft

---

## Overview

This document contains the implementation tasks for the COBie Parameter Management feature, organized by user story to enable independent implementation and testing.

---

## User Stories Summary

| Story | Priority | Description | Independent Test |
|-------|----------|-------------|-------------------|
| US1 | P1 | APS Authentication and Parameter Discovery | Launch add-in, authenticate, verify parameters appear |
| US2 | P2 | Parameter Selection and Preview | Load parameters, test search/filter/preview |
| US3 | P3 | Parameter Creation and Project Binding | Select parameters, add to project, verify in Revit |

---

## Task Format Legend

- `- [ ]` = Checkbox (incomplete)
- `T###` = Sequential task ID
- `[P]` = Parallelizable (can be done simultaneously with other [P] tasks)
- `[US#]` = User Story label (US1, US2, US3)

---

## Phase 1: Setup

**Goal**: Initialize project structure and dependencies

### 1.1 Create APS Bridge Project

- [x] T001 Create APS.Bridge project directory at solution root
- [x] T002 Add APS.Bridge/APS.Bridge.csproj with .NET 8.0 target framework
- [x] T003 Add package reference to Microsoft.AspNetCore.App in APS.Bridge.csproj
- [x] T004 Add project reference to ACG.APS.Core from ../ACG-BulkFoldersUpload/ACG.Aps.Core/ACG.Aps.Core.csproj
- [x] T005 Add Newtonsoft.Json package reference to APS.Bridge.csproj

### 1.2 Create Feature Folder Structure

- [x] T006 Create Features/CobieParameters directory structure
- [x] T007 Create Features/CobieParameters/Commands folder
- [x] T008 Create Features/CobieParameters/ViewModels folder
- [x] T009 Create Features/CobieParameters/Views folder
- [x] T010 Create Features/CobieParameters/Models folder

### 1.3 Create Shared Infrastructure

- [x] T011 Create Shared/APS directory for bridge communication
- [x] T012 Create Shared/APS/Models directory for DTOs
- [x] T013 Create Shared/Interfaces directory for service interfaces

### 1.4 Add NuGet Packages

- [x] T014 [P] Add Newtonsoft.Json package to COBIeManager.csproj
- [x] T015 [P] Add Microsoft.Extensions.Hosting.Abstractions package to COBIeManager.csproj (for process management)

---

## Phase 2: Foundational

**Goal**: Implement core services and models needed by all user stories

### 2.1 Bridge Communication Models

- [x] T016 [P] Create Shared/APS/Models/ApsParameterRequest.cs with request DTOs
- [x] T017 [P] Create Shared/APS/Models/ApsParameterResponse.cs with response DTOs
- [x] T018 [P] Create Shared/APS/Models/TokenRequestDto.cs with token data transfer object
- [x] T019 [P] Create Shared/APS/Models/AuthStatusResponse.cs with authentication status

### 2.2 Bridge Client Interface

- [x] T020 Create Shared/Interfaces/IApsBridgeClient.cs with methods for health check, auth, and parameters
- [x] T021 Create Shared/APS/ApsBridgeClient.cs with HttpClient implementation
- [x] T022 Implement health check method in ApsBridgeClient.cs
- [x] T023 Implement authentication status check in ApsBridgeClient.cs
- [x] T024 Implement login initiation in ApsBridgeClient.cs
- [x] T025 Implement parameter retrieval in ApsBridgeClient.cs

### 2.3 Core Data Models

- [x] T026 [P] Create Features/CobieParameters/Models/CobieParameterDefinition.cs with all properties
- [x] T027 [P] Create Features/CobieParameters/Models/ParameterDataType.cs enum
- [x] T028 [P] Create Features/CobieParameters/Models/ParameterType.cs enum (Instance/Type)
- [x] T029 [P] Create Features/CobieParameters/Models/AuthenticationSession.cs
- [x] T030 [P] Create Features/CobieParameters/Models/ParameterBindingResult.cs
- [x] T031 [P] Create Features/CobieParameters/Models/BindingStatus.cs enum
- [x] T032 [P] Create Features/CobieParameters/Models/CategorySet.cs
- [x] T033 [P] Create Features/CobieParameters/Models/Discipline.cs enum
- [x] T034 [P] Create Features/CobieParameters/Models/RevitCategory.cs

### 2.4 Parameter Cache

- [x] T035 Create Shared/Services/ParameterCacheService.cs with cache file operations
- [x] T036 Implement cache save to %APPDATA%\COBIeManager\Parameters\cache.json
- [x] T037 Implement cache load from file with timestamp validation
- [x] T038 Implement cache invalidation logic (24-hour expiry)

---

## Phase 3: User Story 1 - APS Authentication and Parameter Discovery

**Goal**: Enable users to authenticate with APS and retrieve COBie parameters

**Independent Test Criteria**:
1. Launch add-in and click COBie Parameters button
2. See login prompt, complete authentication in browser
3. Parameters appear in list (from APS or cache)
4. Status shows "Authenticated"

### 3.1 APS Bridge - Authentication Endpoints

- [x] T039 [US1] Create APS.Bridge/Controllers/AuthController.cs
- [x] T040 [US1] Implement POST /auth/login endpoint using ACG.APS.Core ApsAuthService
- [x] T041 [US1] Implement GET /auth/status endpoint returning AuthenticationSession
- [x] T042 [US1] Implement POST /auth/logout endpoint clearing tokens
- [x] T043 [US1] Implement POST /auth/token/refresh endpoint using ApsSessionManager
- [x] T044 [US1] Create APS.Bridge/Models/BridgeAuthStatus.cs for internal state tracking

### 3.2 APS Bridge - Parameters Endpoints

- [x] T045 [US1] Create APS.Bridge/Controllers/ParametersController.cs
- [x] T046 [US1] Implement GET /health endpoint
- [x] T047 [US1] Implement GET /parameters endpoint with account ID query parameter
- [x] T048 [US1] Add forceRefresh query parameter support to /parameters
- [x] T049 [US1] Implement GET /parameters/specs endpoint for data types
- [x] T050 [US1] Implement GET /parameters/categories endpoint for Revit categories
- [x] T051 [US1] Create APS.Bridge/Services/ApsParametersService.cs
- [x] T052 [US1] Implement APS Parameters API client calling https://developer.api.autodesk.com/parameters/v1
- [x] T053 [US1] Implement parameter collection retrieval with caching
- [x] T054 [US1] Implement offline fallback using ParameterCacheService

### 3.3 APS Bridge - Program Setup

- [x] T055 [US1] Create APS.Bridge/Program.cs with Kestrel server setup
- [x] T056 [US1] Configure HTTP server on localhost:5000
- [x] T057 [US1] Register ACG.APS.Core services in dependency injection
- [x] T058 [US1] Register AuthController and ParametersController
- [x] T059 [US1] Add CORS policy for local requests
- [x] T060 [US1] Implement graceful shutdown handling

### 3.4 Add-in Command Entry Point

- [x] T061 [US1] Create Features/CobieParameters/Commands/CobieParametersCommand.cs
- [x] T062 [US1] Implement IExternalCommand with RevitTask.Initialize() call
- [x] T063 [US1] Add Transaction(TransactionMode.Manual) attribute
- [x] T064 [US1] Add Regeneration(RegenerationOption.Manual) attribute

### 3.5 View Model - Authentication

- [x] T065 [US1] Create Features/CobieParameters/ViewModels/CobieParametersViewModel.cs
- [x] T066 [US1] Inherit from ObservableObject with CommunityToolkit.Mvvm
- [x] T067 [US1] Add [ObservableProperty] for StatusMessage (string)
- [x] T068 [US1] Add [ObservableProperty] for IsAuthenticated (bool)
- [x] T069 [US1] Add [ObservableProperty] for IsLoading (bool)
- [x] T070 [US1] Add ObservableCollections for Parameters and SelectedParameters
- [x] T071 [US1] Implement LoginCommand with [RelayCommand]
- [x] T072 [US1] Implement LoadParametersCommand with [RelayCommand]
- [x] T073 [US1] Implement CheckAuthStatusAsync method polling bridge
- [x] T074 [US1] Implement PollForAuthAsync with 2-second intervals
- [x] T075 [US1] Add error handling for bridge connection failures

### 3.6 View - Main Window

- [x] T076 [US1] Create Features/CobieParameters/Views/CobieParametersWindow.xaml
- [x] T077 [US1] Set up Material Design card as root element
- [x] T078 [US1] Create authentication status section with Login button
- [x] T079 [US1] Create parameter list placeholder (ListBox)
- [x] T080 [US1] Create footer with StatusMessage and Add to Project button
- [x] T081 [US1] Create code-behind CobieParametersWindow.xaml.cs
- [x] T082 [US1] Initialize ViewModel in constructor AFTER InitializeComponent()

### 3.7 Bridge Process Management

- [x] T083 [US1] Create Shared/Services/ApsBridgeProcessService.cs
- [x] T084 [US1] Implement StartBridgeAsync() method to launch APS.Bridge.exe
- [x] T085 [US1] Implement StopBridgeAsync() method for graceful shutdown
- [x] T086 [US1] Implement health check before bridge operations
- [x] T087 [US1] Add auto-start on first authenticated API call
- [x] T088 [US1] Add idle timeout to auto-stop bridge after 5 minutes

### 3.8 Ribbon Button Registration

- [x] T089 [US1] Update App.cs OnStartup method
- [x] T090 [US1] Register ApsBridgeProcessService in ServiceCollection
- [x] T091 [US1] Register IApsBridgeClient in ServiceCollection
- [x] T092 [US1] Register ParameterCacheService in ServiceCollection
- [x] T093 [US1] Create PushButtonData for COBie Parameters ribbon button
- [x] T094 [US1] Set button tooltip to "Manage COBie parameters from Autodesk Platform Services"

---

## Phase 4: User Story 2 - Parameter Selection and Preview

**Goal**: Enable browsing, searching, filtering, and previewing parameters

**Independent Test Criteria**:
1. Parameters are loaded (from US1)
2. Type in search box → list filters immediately
3. Select category filter → only matching parameters show
4. Click parameter → details panel shows info
5. Select All / Deselect All work correctly

### 4.1 View Model - Search and Filter

- [ ] T095 [US2] Add SearchText [ObservableProperty] to CobieParametersViewModel.cs
- [ ] T096 [US2] Add SelectedDiscipline [ObservableProperty] (All/Architectural/Structural/MEP)
- [ ] T097 [US2] Add SelectedParameter [ObservableProperty] for details preview
- [ ] T098 [US2] Implement FilteredParameters collection with filtering logic
- [ ] T099 [US2] Add SearchText_PropertyChanged handler to update FilteredParameters
- [ ] T100 [US2] Add SelectedDiscipline_PropertyChanged handler to update FilteredParameters
- [ ] T101 [US2] Implement case-insensitive search across name, description, labels
- [ ] T102 [US2] Implement category filtering by APS category bindings

### 4.2 View - Search and Filter UI

- [ ] T103 [US2] Add search TextBox to CobieParametersWindow.xaml
- [ ] T104 [US2] Add MaterialDesign HintAssist for search placeholder
- [ ] T105 [US2] Add category filter ComboBox (All/Architectural/Structural/MEP)
- [ ] T106 [US2] Bind SearchText to ViewModel with UpdateSourceTrigger=PropertyChanged
- [ ] T107 [US2] Bind SelectedDiscipline to ViewModel

### 4.3 View - Parameter List

- [ ] T108 [US2] Replace placeholder ListBox with full implementation
- [ ] T109 [US2] Set ItemsSource to FilteredParameters
- [ ] T110 [US2] Enable SelectionMode="Extended"
- [ ] T111 [US2] Add CheckBox template for IsSelected binding
- [ ] T112 [US2] Display parameter name in CheckBox content
- [ ] T113 [US2] Add data template for parameter details (data type, instance/type indicator)

### 4.4 View - Details Panel

- [ ] T114 [US2] Add details panel Grid to CobieParametersWindow.xaml
- [ ] T115 [US2] Display parameter name when SelectedParameter is set
- [ ] T116 [US2] Display description with text wrapping
- [ ] T117 [US2] Display data type with user-friendly formatting
- [ ] T118 [US2] Display classification (Instance or Type)
- [ ] T119 [US2] Display associated labels (if any)
- [ ] T120 [US2] Add empty state message when no parameter selected
- [ ] T121 [US2] Style details panel with Material Design card

### 4.5 View Model - Selection Actions

- [ ] T122 [US2] Implement SelectAllCommand [RelayCommand]
- [ ] T123 [US2] Implement DeselectAllCommand [RelayCommand]
- [ ] T124 [US2] Implement RefreshCommand [RelayCommand] to reload from APS
- [ ] T125 [US2] Add SelectAll logic checking against already-selected
- [ ] T126 [US2] Add DeselectAll logic clearing all selections
- [ ] T127 [US2] Add Refresh logic with forceRefresh=true and loading indicator

### 4.6 View - Selection UI

- [ ] T128 [US2] Add Select All and Deselect All buttons
- [ ] T129 [US2] Add Refresh button with icon
- [ ] T130 [US2] Position buttons in toolbar row
- [ ] T139 [US2] Bind all buttons to respective commands
- [ ] T140 [US2] Disable Select/Deselect when no parameters loaded

### 4.7 Loading States

- [ ] T141 [US2] Show loading overlay when IsLoading is true
- [ ] T142 [US2] Update StatusMessage during parameter loading
- [ ] T143 [US2] Handle empty parameter list with friendly message
- [ ] T144 [US2] Handle offline mode (cached parameters) with warning indicator

---

## Phase 5: User Story 3 - Parameter Creation and Project Binding

**Goal**: Add selected parameters as shared parameters to Revit project

**Independent Test Criteria**:
1. Select parameters and click Add to Project
2. Parameters appear in Revit Properties palette
3. Correct categories are bound
4. Duplicate parameters are skipped
5. Summary shows successful/failed/skipped counts

### 5.1 Parameter Service Interface

- [ ] T145 [US3] Create Shared/Interfaces/ICobieParameterService.cs
- [ ] T146 [US3] Define CreateParameters method accepting Document and parameter list
- [ ] T147 [US3] Define CheckForDuplicate method with name and type parameters
- [ ] T148 [US3] Define BindParameterToCategories method
- [ ] T149 [US3] Define GetOrCreateSharedParameter method

### 5.2 Parameter Service Implementation

- [ ] T150 [US3] Create Shared/Services/CobieParameterService.cs
- [ ] T151 [US3] Implement CreateParameters with Transaction using Revit document
- [ ] T152 [US3] Implement validation loop before parameter creation
- [ ] T153 [US3] Implement duplicate check by name AND data type
- [ ] T154 [US3] Implement skip logic for duplicates with logging
- [ ] T155 [US3] Implement shared parameter creation using ExternalDefinitionCreateOptions
- [ ] T156 [US3] Implement category binding using InstanceBinding or TypeBinding
- [ ] T157 [US3] Implement error handling continuing on individual failures
- [ ] T158 [US3] Return List<ParameterBindingResult> with status for each

### 5.3 Data Type Mapping

- [ ] T159 [US3] Create Shared/Services/ApsDataTypeMapper.cs
- [ ] T160 [US3] Implement mapping from APS DataTypeId to Revit ParameterType
- [ ] T161 [US3] Map autodesk.revit.spec:text to ForgeTypeId for Text
- [ ] T162 [US3] Map autodesk.revit.spec:length to ForgeTypeId for Length
- [ ] T163 [US3] Map autodesk.revit.spec:area to ForgeTypeId for Area
- [ ] T164 [US3] Map autodesk.revit.spec:volume to ForgeTypeId for Volume
- [ ] T165 [US3] Map autodesk.revit.spec:integer to ForgeTypeId for Integer
- [ ] T166 [US3] Map autodesk.revit.spec:familyType to ForgeTypeId for ElementId
- [ ] T167 [US3] Add fallback to Unknown for unsupported types

### 5.4 Category Mapping

- [ ] T168 [US3] Create Shared/Services/RevitCategoryMapper.cs
- [ ] T169 [US3] Define category mapping dictionary from APS IDs to BuiltInCategory
- [ ] T170 [US3] Map autodesk.revit.category.instances:walls-1.0.0 to OST_Walls
- [ ] T171 [US3] Map autodesk.revit.category.instances:doors-1.0.0 to OST_Doors
- [ ] T172 [US3] Map autodesk.revit.category.instances:windows-1.0.0 to OST_Windows
- [ ] T173 [US3] Map autodesk.revit.category.instances:columns-1.0.0 to OST_Columns
- [ ] T174 [US3] Map autodesk.revit.category.instances:structuralcolumns-1.0.0 to OST_StructuralColumns
- [ ] T175 [US3] Map autodesk.revit.category.instances:structuralframing-1.0.0 to OST_StructuralFraming
- [ ] T176 [US3] Map autodesk.revit.category.instances:floors-1.0.0 to OST_Floors
- [ ] T177 [US3] Map autodesk.revit.category.instances:ceilings-1.0.0 to OST_Ceilings
- [ ] T178 [US3] Map autodesk.revit.category.instances:roofs-1.0.0 to OST_Roofs
- [ ] T179 [US3] Map autodesk.revit.category.instances:rooms-1.0.0 to OST_Rooms

### 5.5 Category Selection Dialog

- [ ] T180 [US3] Create Features/CobieParameters/Views/CategorySelectionDialog.xaml
- [ ] T181 [US3] Create Features/CobieParameters/ViewModels/CategorySelectionViewModel.cs
- [ ] T182 [US3] Add AvailableCategories ObservableCollection
- [ ] T183 [US3] Add SelectedCategories ObservableCollection
- [ ] T184 [US3] Implement LoadCategories for discipline (Architectural/Structural/MEP)
- [ ] T185 [US3] Add OK and Cancel buttons with DialogResult
- [ ] T186 [US3] Style dialog with Material Design

### 5.6 View Model - Add to Project

- [ ] T187 [US3] Add AddToProjectCommand [RelayCommand] to CobieParametersViewModel.cs
- [ ] T188 [US3] Implement AddToProjectAsync method
- [ ] T189 [US3] Get active Document from UIDocument
- [ ] T190 [US3] Call ICobieParameterService.CreateParameters
- [ ] T191 [US3] Handle missing category metadata with CategorySelectionDialog
- [ ] T192 [US3] Update StatusMessage with progress during batch creation
- [ ] T193 [US3] Collect ParameterBindingResult list for summary

### 5.7 View - Summary Dialog

- [ ] T194 [US3] Create Features/CobieParameters/Views/ParameterSummaryDialog.xaml
- [ ] T195 [US3] Create Features/CobieParameters/ViewModels/ParameterSummaryViewModel.cs
- [ ] T196 [US3] Add Results ObservableCollection<ParameterBindingResult>
- [ ] T197 [US3] Display successful count with green styling
- [ ] T198 [US3] Display skipped count with yellow styling
- [ ] T199 [US3] Display failed count with red styling
- [ ] T200 [US3] Add DataGrid showing individual parameter results
- [ ] T201 [US3] Include error messages for failed/skipped parameters
- [ ] T202 [US3] Add Close button to dismiss dialog

### 5.8 Progress Indication

- [ ] T203 [US3] Add ProgressBar to CobieParametersWindow.xaml
- [ ] T204 [US3] Bind ProgressBar visibility to IsLoading
- [ ] T205 [US3] Add progress text showing "Processing X of Y parameters"
- [ ] T206 [US3] Update progress during batch creation in CobieParameterService

### 5.9 Error Handling

- [ ] T207 [US3] Implement built-in parameter conflict detection in CobieParameterService
- [ ] T208 [US3] Skip creation with warning for built-in conflicts
- [ ] T209 [US3] Implement unsupported data type detection
- [ ] T210 [US3] Skip creation with warning for unsupported types
- [ ] T211 [US3] Implement document closure detection
- [ ] T212 [US3] Gracefully terminate operation if document closed

---

## Phase 6: Polish & Cross-Cutting Concerns

**Goal**: Complete edge cases, improve UX, ensure quality

### 6.1 Error Messages

- [ ] T213 Create Shared/Services/ApsErrorMessageService.cs
- [ ] T214 Add user-friendly error messages for common APS failures
- [ ] T215 Add error messages for network connectivity issues
- [ ] T216 Add error messages for token expiration
- [ ] T217 Add error messages for invalid credentials

### 6.2 Logging Enhancements

- [ ] T218 Add operation logging to CobieParameterService
- [ ] T219 Log parameter creation attempts with timestamps
- [ ] T220 Log authentication events
- [ ] T221 Log cache hits/misses

### 6.3 UI Polish

- [ ] T222 Add keyboard shortcuts (Ctrl+A for Select All)
- [ ] T223 Add double-click to select/deselect individual parameters
- [ ] T224 Add sorting by name, data type, or classification
- [ ] T225 Add status icons for instance/type parameters
- [ ] T226 Add tooltip for Add to Project button showing selection count

### 6.4 Edge Cases

- [ ] T227 Implement handling for cancelled authentication (user closes browser)
- [ ] T228 Implement retry mechanism when APS service unavailable
- [ ] T229 Add batch processing for 100+ parameters (50 at a time)
- [ ] T230 Add validation for read-only documents
- [ ] T231 Add warning for family parameters without loaded families

### 6.5 Performance

- [ ] T232 Implement client-side filtering for <10 second search requirement
- [ ] T233 Add virtualization for large parameter lists
- [ ] T234 Optimize parameter creation batch processing
- [ ] T235 Add early exit for no selections

### 6.6 Documentation

- [ ] T236 Update README.md with COBie Parameters feature documentation
- [ ] T237 Add troubleshooting section for common APS issues
- [ ] T238 Document category mappings in code comments
- [ ] T239 Document offline mode behavior

---

## Dependencies

### Story Dependencies

```
US1 (P1) ──────┬────> US2 (P2) ─────> US3 (P3)
                │
                └─────────────────────────────┘
                     (US3 depends on both US1 and US2)
```

**Notes**:
- US1 can be implemented and tested independently
- US2 requires US1's parameter loading but is otherwise independent
- US3 requires both US1 (parameters) and US2 (selection mechanism)

### Critical Path

```
Setup → Foundational → US1 → US2 → US3 → Polish
```

---

## Parallel Execution Opportunities

### Phase 1 (Setup) - Parallel Tasks

```bash
# Can run in parallel:
T014 (Add Newtonsoft.Json)
T015 (Add hosting abstractions)

# After creating folders:
T016-T019 (Bridge DTOs)
T026-T034 (Core models)
```

### Phase 2 (Foundational) - Parallel Tasks

```bash
# After interfaces defined:
T021-T025 (ApsBridgeClient implementation)
T026-T034 (Core data models)
```

### Phase 3 (US1) - Parallel Tasks

```bash
# Bridge endpoints (after controller created):
T040-T044 (Auth endpoints)
T045-T054 (Parameters endpoints)
```

### Phase 4 (US2) - Parallel Tasks

```bash
# View implementation:
T095-T101 (Search/filter logic)
T103-T107 (Search UI)
T108-T113 (List UI)
```

---

## Implementation Strategy

### MVP Scope (First Deliverable)

**Minimum Viable Product**: User Story 1 only
- User can authenticate with APS
- Parameters are retrieved and displayed
- Basic list view (no search/filter yet)
- Validated by completing authentication flow and seeing parameters

**Tasks for MVP**: T001-T094 (Phases 1-3)

### Incremental Delivery

1. **Sprint 1**: Setup + US1 (Authentication & Discovery)
2. **Sprint 2**: US2 (Selection & Preview)
3. **Sprint 3**: US3 (Parameter Creation & Binding)
4. **Sprint 4**: Polish & Edge Cases

### Testing Strategy

Since TDD was not explicitly requested in the spec, unit tests will be added during implementation rather than before. Focus areas:
- Bridge API contract testing
- Parameter service validation
- Category mapping accuracy
- Cache functionality

---

## Task Summary

| Phase | Task Count | Description |
|-------|------------|-------------|
| Phase 1: Setup | 15 tasks | Project initialization, folders, packages |
| Phase 2: Foundational | 23 tasks | Core models, interfaces, bridge client |
| Phase 3: US1 | 56 tasks | Authentication, parameter retrieval, bridge |
| Phase 4: US2 | 50 tasks | Search, filter, preview, selection UI |
| Phase 5: US3 | 67 tasks | Parameter creation, category mapping, summary |
| Phase 6: Polish | 27 tasks | Error handling, logging, UX, edge cases |
| **Total** | **238 tasks** | Complete implementation |

---

## Validation Checklist

- [x] All tasks follow checkbox format: `- [ ] T###`
- [x] User Story tasks have [US#] labels
- [x] Parallel tasks marked with [P]
- [x] All tasks include file paths
- [x] Each user story has independent test criteria
- [x] Dependencies documented
- [x] Parallel opportunities identified
- [x] MVP scope defined
- [x] Incremental delivery strategy outlined

---

## Next Steps

1. Review tasks.md and confirm task breakdown
2. Begin implementation with Phase 1 (Setup)
3. Complete MVP (US1) before proceeding to US2
4. Use `/speckit.implement` to execute tasks automatically (optional)
