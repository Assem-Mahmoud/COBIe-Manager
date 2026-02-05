# COBie Revit Add-in
## Milestone 1 – Parameter Retrieval & Project Binding Plan

---

## 1. Purpose

This document defines the **technical and execution plan** for **Milestone 1** of the COBie Revit Add-in.

Milestone 1 focuses on:
- Retrieving COBie parameter definitions from Autodesk Platform Services (APS) Parameters Service
- Presenting them to the user for selection
- Adding and binding selected parameters into the active Revit project

Parameter value population, element filtering, and COBie data validation are **out of scope** for this milestone.

---

## 2. Milestone 1 Objectives

1. Authenticate users against Autodesk Platform Services (APS)
2. Retrieve COBie parameter definitions from APS Parameters Service
3. Display parameters in a structured Revit UI
4. Allow user selection of parameters
5. Create and bind parameters inside Revit projects
6. Provide execution feedback and logging

---

## 3. Scope Definition

### 3.1 In Scope
- APS authentication (3-legged OAuth)
- Parameter definition retrieval
- Parameter selection UI
- Parameter creation in Revit
- Category binding (instance / type)
- Logging and execution reporting

### 3.2 Out of Scope
- Element filtering
- Writing parameter values
- COBie worksheet mapping
- Model validation or export
- Data compliance checks

---

## 4. Technical Architecture Overview

### 4.1 Components

- **Revit Add-in**
  - UI (WPF / MVVM)
  - Revit API interaction
- **APS Integration Layer**
  - OAuth handling
  - Parameters API client
- **Parameter Management Layer**
  - Parameter catalog
  - Mapping and validation
- **Revit Binding Layer**
  - Parameter creation
  - Category binding logic
- **Logging Layer**
  - UI feedback
  - Local execution logs

---

## 5. Authentication Strategy

- Use **APS 3-legged OAuth**
- Embedded browser or system browser login
- Secure token storage (Windows Credential Manager or DPAPI)
- Automatic token refresh
- Session reuse across Revit sessions where possible

---

## 6. Parameter Source Strategy

### 6.1 Primary Source
- APS Parameters Service
- Parameters organized in a dedicated **COBie Collection**

### 6.2 Fallback Strategy
- Parameter search using:
  - Name
  - Label
  - Description keywords (e.g., "COBie")

### 6.3 Data Retrieved Per Parameter
- Parameter ID
- Name
- Description
- Data type / specification
- Instance or Type classification
- Grouping / labels (if available)

---

## 7. User Interface Plan

### 7.1 UI Features
- Login / authentication status
- Parameter list with:
  - Search
  - Filters
  - Multi-select checkboxes
- Parameter details preview
- Action buttons:
  - Refresh
  - Select / deselect
  - Add to project
- Progress indicator
- Execution summary

### 7.2 UI Principles
- MVVM pattern
- Non-blocking UI (async operations)
- Clear user feedback
- Minimal Revit UI disruption

---

## 8. Revit Parameter Creation Strategy

### 8.1 Parameter Type
- **Shared parameters** (preferred for GUID stability)
- Project parameters supported if explicitly approved

### 8.2 Parameter Existence Check
- Check by:
  - Name
  - Data type
  - GUID (if shared)

### 8.3 Binding Strategy
- Instance vs Type binding based on parameter definition
- Category binding:
  - Defaults from APS (if available)
  - Predefined category sets:
    - Architectural
    - Structural
    - MEP

### 8.4 Parameter Grouping
- Assign to appropriate Properties palette group
- Fallback to a default group if unspecified

---

## 9. Execution Workflow

1. User opens Revit Add-in
2. User authenticates with APS
3. Add-in retrieves COBie parameter definitions
4. Parameters displayed in UI
5. User selects parameters
6. User triggers "Add to Project"
7. Add-in:
   - Validates parameters
   - Checks for existing parameters
   - Creates parameters
   - Binds to categories
8. Execution summary displayed to user

---

## 10. Logging & Error Handling

### 10.1 Logging
- Operation-level logging
- Timestamped entries
- Stored locally per user/machine

### 10.2 Error Handling
- Graceful failure per parameter
- Continue execution on partial failure
- Clear user-facing error messages

---

## 11. Acceptance Criteria

Milestone 1 is considered complete when:

- APS authentication works reliably
- COBie parameters are retrieved from APS
- User can select parameters via UI
- Parameters are created and visible in Revit
- Category binding is correct
- Execution results are clearly reported

---

## 12. Risks & Mitigation

| Risk | Mitigation |
|----|----|
| Parameter spec mismatch | Validation before creation |
| Missing category metadata | Use predefined category sets |
| Duplicate parameters | Idempotent existence checks |
| Token expiration | Automatic refresh logic |

---

## 13. Milestone 1 Deliverables

- Revit Add-in binaries
- Source code
- plan.md (this document)
- Basic user guide
- Technical documentation

---

## 14. Next Milestone Preview (Milestone 2)

- Element filtering
- COBie data population
- Rule-based value assignment
- Validation and reporting

(Not included in this milestone)
