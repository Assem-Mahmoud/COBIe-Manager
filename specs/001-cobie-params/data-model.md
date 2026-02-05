# Data Model: COBie Parameter Management

**Feature**: 001-cobie-params
**Date**: 2025-02-05
**Phase**: Phase 1 - Design

## Overview

This document defines the data entities and their relationships for the COBie Parameter Management feature.

---

## Core Entities

### 1. CobieParameterDefinition

Represents a COBie parameter definition retrieved from APS.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique identifier (32-char hex from APS) |
| Name | string | Display name of the parameter |
| Description | string | Human-readable description |
| DataTypeId | string | APS data type identifier (e.g., `autodesk.revit.spec:text-1.0.0`) |
| DataType | ParameterDataType | Normalized data type enum |
| InstanceTypeAssociation | ParameterType | INSTANCE or TYPE |
| CategoryBindingIds | string[] | Array of APS category identifiers |
| Labels | string[] | Associated label names |
| IsHidden | bool | Whether parameter is hidden |
| IsArchived | bool | Whether parameter is archived |
| GroupBindingId | string | Property group identifier for Revit |

**Enums**:
```csharp
public enum ParameterDataType
{
    Text,
    Integer,
    Number,
    Length,
    Area,
    Volume,
    Angle,
    FamilyType,
    YesNo,
    MultiValue,
    Unknown
}

public enum ParameterType
{
    Instance,
    Type
}
```

**Validation Rules**:
- `Id` must be non-empty and valid hex string
- `Name` must be non-empty
- `DataTypeId` must match APS pattern
- At least one `CategoryBindingIds` required (or prompt user)

---

### 2. AuthenticationSession

Represents the current APS authentication state.

| Property | Type | Description |
|----------|------|-------------|
| AccessToken | string | OAuth access token |
| RefreshToken | string | OAuth refresh token |
| ExpiresAt | DateTime | Token expiration timestamp |
| UserId | string | APS user identifier |
| AccountId | string | APS account ID (hub ID) |
| IsAuthenticated | bool | Whether session is valid |

**Validation Rules**:
- `AccessToken` required when `IsAuthenticated` is true
- `RefreshToken` required for token refresh
- `ExpiresAt` must be future-dated for valid session

---

### 3. ParameterBindingResult

Represents the outcome of a parameter creation operation.

| Property | Type | Description |
|----------|------|-------------|
| ParameterDefinition | CobieParameterDefinition | The parameter being created |
| Status | BindingStatus | Success, Skipped, or Failed |
| SkipReason | string | Reason for skipping (if applicable) |
| ErrorMessage | string | Error details (if failed) |
| CreatedParameterId | ElementId | Revit ElementId of created parameter |

**Enums**:
```csharp
public enum BindingStatus
{
    Success,
    Skipped,
    Failed
}
```

---

### 4. CategorySet

Represents a group of Revit categories for parameter binding.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique identifier |
| Name | string | Display name |
| Discipline | Discipline | Architectural, Structural, or MEP |
| Categories | RevitCategory[] | Array of Revit categories |
| IsDefault | bool | Whether this is a default set |

**Enums**:
```csharp
public enum Discipline
{
    Architectural,
    Structural,
    MEP
}
```

---

### 5. RevitCategory

Represents a single Revit category mapping.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | APS category identifier |
| BuiltInCategory | BuiltInCategory | Revit BuiltInCategory enum value |
| Name | string | Display name |

---

### 6. BridgeClientConfig

Configuration for communicating with the APS Bridge process.

| Property | Type | Description |
|----------|------|-------------|
| BaseUrl | string | HTTP base URL (default: `http://localhost:5000`) |
| Timeout | TimeSpan | Request timeout (default: 30 seconds) |
| MaxRetryAttempts | int | Maximum retry attempts (default: 3) |

---

### 7. ParameterCache

Cached parameter data for offline support.

| Property | Type | Description |
|----------|------|-------------|
| Parameters | CobieParameterDefinition[] | Cached parameter list |
| CachedAt | DateTime | Cache timestamp |
| Version | string | Cache version for invalidation |
| AccountId | string | APS account ID for cache |

**Cache Key**: `COBIeManager_ParameterCache_{accountId}`

---

### 8. WindowsCredentialData

Data structure stored in Windows Credential Manager.

| Property | Type | Description |
|----------|------|-------------|
| AccessToken | string | OAuth access token |
| RefreshToken | string | OAuth refresh token |
| ExpiresAt | string | ISO8601 formatted timestamp |
| AccountId | string | APS account ID |

**Stored As**: JSON string in Credential Manager blob.

---

## Entity Relationships

```
┌──────────────────────────┐
│  AuthenticationSession   │
├──────────────────────────┤
│  AccessToken             │───┐
│  RefreshToken            │   │
│  AccountId               │   │
└──────────────────────────┘   │
                              │
                              ▼
┌──────────────────────────────────────────────┐
│            APS Bridge (net8.0)               │
│  ┌────────────────────────────────────────┐ │
│  │  Uses ACG.APS.Core                     │ │
│  │  - TokenStorage (Windows Credential)   │ │
│  │  - ApsAuthService                      │ │
│  │  - ApsSessionManager                   │ │
│  └────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
                │ HTTP (localhost:5000)
                │
                ▼
┌──────────────────────────────────────────────┐
│         Revit Add-in (net48)                 │
│  ┌────────────────────────────────────────┐ │
│  │  ApsBridgeClient                       │ │
│  │    └── Retrieves:                     │ │
│  │        - CobieParameterDefinition[]   │ │
│  └────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────┐ │
│  │  CobieParameterService                 │ │
│  │    ├── Creates:                       │ │
│  │    │   - ParameterBindingResult       │ │
│  │    └── Uses:                         │ │
│  │        - CategorySet                  │ │
│  │        - ParameterCache               │ │
│  └────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

---

## Data Flow

### Authentication Flow

1. User clicks "Login" in add-in
2. `ApsBridgeClient` sends POST `/auth/login` to bridge
3. Bridge initiates OAuth via `ApsAuthService`
4. User completes OAuth in browser
5. Bridge receives callback, stores token in Windows Credential Manager
6. Add-in polls `/auth/status` until authenticated
7. Add-in receives `AuthenticationSession` from bridge

### Parameter Retrieval Flow

1. Add-in sends GET `/parameters` to bridge with account ID
2. Bridge uses ACG.APS.Core to call APS Parameters API
3. Bridge returns `CobieParameterDefinition[]`
4. Add-in caches parameters in `ParameterCache`
5. Add-in displays parameters in UI

### Parameter Creation Flow

1. User selects parameters in UI
2. Add-in calls `CobieParameterService.CreateParameters()`
3. For each parameter:
   - Check for duplicates (name + type)
   - Validate data type compatibility
   - Get or prompt for categories
   - Create shared parameter in Revit
   - Bind to categories
   - Return `ParameterBindingResult`
4. Display summary report

---

## State Transitions

### Authentication Session States

```
┌─────────┐    Login    ┌──────────────┐    Success    ┌──────────────┐
│ Unknown │─────────────▶│ Authenticating│──────────────▶│ Authenticated│
└─────────┘              └──────────────┘              └──────────────┘
                              │                             │
                         Failure│                      Token Refresh
                              ▼                             ▼
                        ┌──────────┐                  ┌──────────┐
                        │ Failed   │◀─────────────────│Refreshing│
                        └──────────┘  Auto-refresh    └──────────┘
```

### Parameter Creation States

```
┌──────────┐  Select  ┌───────────┐  Validate  ┌──────────┐
│ Available│─────────▶│Selected   │───────────▶│ Validated│
└──────────┘          └───────────┘            └────┬─────┘
                                                 │
                      ┌──────────────────────────┴─────────┐
                      │                                   │
                      ▼                                   ▼
               ┌──────────┐                        ┌──────────┐
               │ Created  │                        │  Skipped │
               │  (Success)│                       │(Duplicate)│
               └──────────┘                        └──────────┘
                      │
                      ▼
               ┌──────────┐
               │  Failed  │
               │  (Error) │
               └──────────┘
```

---

## Data Validation Summary

| Entity | Validation Rules |
|--------|------------------|
| CobieParameterDefinition | Non-empty ID, Name; valid DataTypeId |
| AuthenticationSession | Valid tokens, future expiration |
| ParameterBindingResult | Status required, error message if failed |
| CategorySet | At least one category |
| ParameterCache | Valid parameters, recent timestamp (within 24h) |
