# Feature Specification: COBie Parameter Management for Revit

**Feature Branch**: `001-cobie-params`
**Created**: 2025-02-05
**Status**: Draft
**Input**: User description: "we are going to develope a revit addin to manage COBIe parameters please take a look at the plan.md file to have good understandign"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - APS Authentication and Parameter Discovery (Priority: P1)

As a BIM manager, I want to authenticate with Autodesk Platform Services and retrieve the standard COBie parameter definitions so that I can access the approved parameter catalog without manual data entry.

**Why this priority**: This is the foundational capability - without APS authentication and parameter retrieval, no other functionality can work. This establishes the connection to the authoritative source of COBie parameters.

**Independent Test**: Can be fully tested by launching the add-in, completing authentication flow, and verifying that COBie parameters appear in the list. Delivers immediate value by showing users the available standard parameters.

**Acceptance Scenarios**:

1. **Given** the user is not authenticated, **When** the add-in is launched, **Then** the system displays a login prompt and initiates APS authentication
2. **Given** the user has valid APS credentials, **When** authentication is completed, **Then** the system securely stores the authentication token for the session
3. **Given** the user is authenticated, **When** the add-in connects to APS Parameters Service, **Then** all COBie parameters from the designated collection are retrieved and displayed
4. **Given** the authentication token has expired, **When** the user attempts to retrieve parameters, **Then** the system automatically refreshes the token without requiring user re-authentication
5. **Given** authentication fails, **When** an error occurs, **Then** the system displays a clear error message explaining the failure reason

---

### User Story 2 - Parameter Selection and Preview (Priority: P2)

As a BIM manager, I want to browse, search, filter, and preview COBie parameters so that I can select the correct parameters for my project without overwhelming complexity.

**Why this priority**: Once parameters are retrieved, users need an efficient way to find and evaluate them before adding to the project. This prevents errors from adding incorrect parameters and improves workflow efficiency.

**Independent Test**: Can be fully tested by loading parameters and using search/filter/preview functions. Delivers value by enabling informed parameter selection decisions.

**Acceptance Scenarios**:

1. **Given** COBie parameters are loaded, **When** the user types in the search box, **Then** the parameter list filters to show only matching parameters (name, description, or labels)
2. **Given** COBie parameters are loaded, **When** the user applies category filters (Architectural, Structural, MEP), **Then** only parameters relevant to the selected categories are displayed
3. **Given** COBie parameters are loaded, **When** the user clicks on a parameter, **Then** a details panel shows parameter name, description, data type, and classification (instance vs. type)
4. **Given** a filtered parameter list, **When** the user clicks "Select All", **Then** all visible parameters are marked for selection
5. **Given** some parameters are selected, **When** the user clicks "Deselect All", **Then** all selections are cleared

---

### User Story 3 - Parameter Creation and Project Binding (Priority: P3)

As a BIM manager, I want to add selected COBie parameters to my Revit project and bind them to the appropriate categories so that the parameters become available for use in the project.

**Why this priority**: This is the core value delivery - actually making the parameters available in the Revit project. It depends on the previous two stories but completes the primary user workflow.

**Independent Test**: Can be fully tested by selecting parameters and executing "Add to Project". Delivers complete value by enabling downstream COBie workflows.

**Acceptance Scenarios**:

1. **Given** parameters are selected, **When** the user clicks "Add to Project", **Then** the system validates all selected parameters before creation
2. **Given** a parameter already exists in the project (same name and type), **When** "Add to Project" is executed, **Then** the system skips creating a duplicate and reports the existing parameter
3. **Given** a valid new parameter, **When** it is created, **Then** the parameter appears in Revit's Properties palette bound to the correct categories
4. **Given** instance parameters are selected, **When** they are created, **Then** they are bound to element-level categories
5. **Given** type parameters are selected, **When** they are created, **Then** they are bound to type-level categories
6. **Given** parameter creation encounters an error for one parameter, **When** processing multiple parameters, **Then** the system continues processing remaining parameters and reports individual successes and failures
7. **Given** parameter creation completes, **When** the operation finishes, **Then** a summary shows total parameters attempted, successful, failed, and skipped

---

### Edge Cases

- **APS service unavailable**: If cached parameters exist from a previous session, display them with a warning that live data is unavailable. Allow parameter creation to proceed. If no cache exists, display error message with retry option.
- **Missing category metadata**: Prompt the user to select target categories during parameter creation. Display a category selection dialog with available Revit categories organized by discipline.
- **Name conflicts with built-in parameters**: Skip parameter creation and log a detailed warning explaining that the parameter name conflicts with a Revit built-in parameter and cannot be created as a shared parameter.
- **Unsupported data types**: Skip parameter creation with a warning indicating which data type is not supported by Revit.
- **Document closed during operation**: Detect document closure and gracefully terminate the operation, reporting progress up to the point of interruption.
- **Large parameter selections (100+)**: Process in batches with progress updates; no hard limit but performance may degrade.

### Remaining Edge Cases (Deferred to Planning)

- What happens when the user cancels authentication mid-flow?

## Requirements *(mandatory)*

### Functional Requirements

#### Authentication Requirements

- **FR-001**: System MUST authenticate users against Autodesk Platform Services using OAuth 2.0 with PKCE flow (same as ACG.APS.Core)
- **FR-002**: System MUST securely store authentication tokens using the same storage mechanism as ACG.APS.Core (file-based in %LocalAppData%\ACG_Bridge\token.json, persistent across Revit sessions)
- **FR-003**: System MUST automatically refresh expired tokens without requiring user re-authentication
- **FR-004**: System MUST display current authentication status to the user
- **FR-005**: System MUST allow users to sign out and clear stored credentials

#### Parameter Retrieval Requirements

- **FR-006**: System MUST retrieve COBie parameter definitions from the APS Parameters Service
- **FR-007**: System MUST retrieve the following attributes for each parameter: ID, name, description, data type, classification (instance/type), and grouping labels
- **FR-008**: System MUST organize parameters within a designated "COBie Collection" from APS
- **FR-009**: System MUST support fallback parameter search by name, label, and description keywords if the COBie Collection is unavailable
- **FR-010**: System MUST display a loading indicator while retrieving parameters
- **FR-011**: System MUST cache retrieved parameters for the current session to avoid repeated API calls

#### User Interface Requirements

- **FR-012**: System MUST display retrieved parameters in a searchable, filterable list
- **FR-013**: System MUST support search by parameter name, description, and labels
- **FR-014**: System MUST support filtering by discipline categories (Architectural, Structural, MEP)
- **FR-015**: System MUST display parameter details (name, description, data type, classification) when a parameter is selected
- **FR-016**: System MUST support multi-select of parameters using checkboxes
- **FR-017**: System MUST provide "Select All" and "Deselect All" actions
- **FR-018**: System MUST provide a "Refresh" action to reload parameters from APS
- **FR-019**: System MUST provide clear visual feedback for all user actions (loading states, success, error)
- **FR-020**: System MUST display progress indication during parameter creation operations

#### Parameter Creation Requirements

- **FR-021**: System MUST create selected parameters as shared parameters in the Revit document
- **FR-022**: System MUST check for existing parameters by name AND data type before creation to avoid duplicates
- **FR-023**: System MUST bind parameters to appropriate Revit categories based on the parameter definition
- **FR-024**: System MUST apply instance or type binding based on the parameter's classification
- **FR-025**: System MUST assign parameters to appropriate Properties palette groups
- **FR-026**: System MUST continue processing remaining parameters if individual parameter creation fails
- **FR-027**: System MUST provide a summary report showing successful, failed, and skipped parameters

#### Logging and Error Handling Requirements

- **FR-028**: System MUST log all operations with timestamps for troubleshooting
- **FR-029**: System MUST store logs locally per user/machine
- **FR-030**: System MUST display user-friendly error messages for all failure scenarios
- **FR-031**: System MUST provide specific error details for authentication failures
- **FR-032**: System MUST provide specific error details for parameter creation failures

### Key Entities

- **COBie Parameter Definition**: Represents a parameter definition from APS, containing: ID, name, description, data type specification, classification (instance or type), grouping/labels, and target category metadata

- **Authentication Session**: Represents the user's authenticated state with APS, containing: access token, refresh token, expiration time, and user identity information

- **Parameter Binding Result**: Represents the outcome of a parameter creation operation, containing: parameter definition, success/failure status, error message (if applicable), and skip reason (if applicable)

- **Category Set**: Represents a group of Revit categories for parameter binding, organized by discipline (Architectural, Structural, MEP)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete end-to-end authentication and retrieve COBie parameters within 30 seconds on a typical network connection

- **SC-002**: Users can find and select specific parameters using search/filter within 10 seconds

- **SC-003**: Users can add up to 50 parameters to a project within 60 seconds

- **SC-004**: 95% of parameter creation operations complete successfully without errors

- **SC-005**: All created parameters are correctly visible in Revit's Properties palette and bound to the appropriate categories

- **SC-006**: Users can successfully repeat the workflow multiple times without duplicate parameters being created (idempotent behavior)

- **SC-007**: Error messages clearly communicate the issue and resolution steps in 90% of failure scenarios

## Out of Scope

The following items are explicitly excluded from this milestone and may be addressed in future work:

- Element filtering (selecting specific Revit elements to receive parameter values)
- Writing actual parameter values to Revit elements
- COBie worksheet mapping and export functionality
- Model validation or compliance checking
- Data import from external COBie spreadsheets
- Custom parameter definition creation (all parameters come from APS)
- Parameter value population rules or logic
- Multi-document or batch project operations
- Collaboration or sharing features between users

## Clarifications

### Session 2025-02-05

- Q: How should duplicate parameter detection work? → A: By name AND data type (prevents type conflicts)
- Q: Where should authentication tokens be stored? → A: File-based in %LocalAppData%\ACG_Bridge\token.json (aligns with ACG.APS.Core)
- Q: What happens when APS service is unavailable? → A: Use cached parameters if available, allow creation, show warning
- Q: How to handle parameters missing category metadata? → A: Prompt user to select categories during creation
- Q: How to handle conflicts with built-in Revit parameters? → A: Skip creation and log detailed warning

---

## Assumptions

1. The add-in will reuse ACG.APS.Core library for authentication, using the same OAuth 2.0 PKCE flow, token storage mechanism, and scopes (`data:read data:write data:create account:read`)
2. Users have valid Autodesk Platform Services accounts with access to the Parameters Service
2. A designated COBie Collection exists in APS containing the standard parameter definitions
3. The Revit document is modifiable (user has appropriate permissions and the document is not in a read-only state)
4. Users have network connectivity to access Autodesk Platform Services during authentication and parameter retrieval
5. The APS Parameters Service API is available and responsive during add-in usage
6. Shared parameter GUIDs from APS will remain stable for the same parameter definitions
7. Category metadata for parameters is either provided by APS or can be mapped to predefined category sets
8. Users are working on Revit 2023 or later (as defined in the project technical stack)
