# Research Report: COBie Parameter Management

**Feature**: 001-cobie-params
**Date**: 2025-02-05
**Phase**: Phase 0 - Research

## Summary

This document consolidates research findings for implementing COBie parameter management in the Revit add-in. Key findings include APS Parameters API endpoints, authentication requirements, .NET Framework to .NET 8 IPC options, and Windows Credential Manager implementation approaches.

---

## 1. APS Parameters API Research

### Decision: Use APS Parameters Service v1

**API Base URL**: `https://developer.api.autodesk.com/parameters/v1`

### Authentication Requirements

| Requirement | Value |
|-------------|-------|
| Authentication Type | OAuth 2.0 - 3-Legged (user context required) |
| Authorize URL | `https://developer.api.autodesk.com/authentication/v2/authorize` |
| Token URL | `https://developer.api.autodesk.com/authentication/v2/token` |
| Required Scopes | `data:read data:create data:write data:search` |

### Key Endpoints for COBie Parameters

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/accounts/:account_id/groups/:group_id/collections` | List parameter collections |
| GET | `/accounts/:account_id/groups/:group_id/collections/:collection_id/parameters` | Get all parameters in a collection |
| POST | `/accounts/:account_id/groups/:group_id/collections/:collection_id/parameters:search` | Search parameters with filters |
| GET | `/parameters/:parameter_id` | Get specific parameter definition |
| GET | `/specs` | List available data types |
| GET | `/classifications/categories` | List Revit categories |

### Parameter Definition JSON Schema

```json
{
    "id": "a25628e6737f5e49b9754b648e4467d9",
    "name": "Parameter Name",
    "description": "Parameter description",
    "dataTypeId": "autodesk.revit.spec:familyType-1.0.0",
    "readOnly": false,
    "metadata": [
        {
            "id": "instanceTypeAssociation",
            "value": "INSTANCE"
        },
        {
            "id": "categoryBindingIds",
            "value": ["autodesk.revit.category.instances:walls-1.0.0"]
        }
    ]
}
```

### Key Metadata Fields

| Metadata ID | Possible Values | Description |
|-------------|-----------------|-------------|
| `instanceTypeAssociation` | `INSTANCE`, `TYPE` | Whether parameter applies to instances or types |
| `categoryBindingIds` | Array of category IDs | Revit categories for binding |
| `labelIds` | Array of label IDs | Associated labels |
| `isHidden` | `true`, `false` | Visibility in UI |
| `isArchived` | `true`, `false` | Archived status |

### Data Type ID Format

APS parameter data types follow the pattern: `autodesk.revit.{type}:{name}-{version}`

Examples:
- `autodesk.revit.spec:text-1.0.0` - Text
- `autodesk.revit.spec:length-1.0.0` - Length
- `autodesk.revit.spec:area-1.0.0` - Area
- `autodesk.revit.spec:volume-1.0.0` - Volume
- `autodesk.revit.spec:familyType-1.0.0` - Family Type

### Category ID Format

Revit categories follow the pattern: `autodesk.revit.category.{level}:{name}-{version}`

Examples:
- `autodesk.revit.category.instances:walls-1.0.0`
- `autodesk.revit.category.instances:doors-1.0.0`
- `autodesk.revit.category.instances:columns-1.0.0`
- `autodesk.revit.category.types:walls-1.0.0`

### Rate Limits

| Endpoint Pattern | Rate Limit |
|------------------|------------|
| `/accounts/*` endpoints | 300 requests/minute |
| Reference data (specs, units, classifications) | 14,000 requests/minute |

### Important Notes

1. **Group ID = Account ID**: Currently only one group per account, `group_id` equals `account_id`
2. **Account ID Required**: Must first retrieve user's ACC hub to get account ID
3. **COBie Collection**: Assumes a designated "COBie" collection exists in the user's ACC account

---

## 2. .NET Framework to .NET 8 IPC Research

### Decision: Use HTTP (localhost) for IPC

**Rationale**:
- Simpler to implement than Named Pipes with async patterns
- Easy to debug (can use browser/tools)
- No additional NuGet dependencies
- Sufficient for local communication

**Architecture**:
```
┌─────────────────────────┐
│  Revit Add-in (net48)   │
│  - HttpClient           │
└────────────┬────────────┘
             │ HTTP (localhost:5000)
             ▼
┌─────────────────────────┐
│  APS Bridge (net8.0)    │
│  - ASP.NET Core Kestrel │
│  - ACG.APS.Core         │
└─────────────────────────┘
```

**Alternatives Considered**:

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| HTTP (localhost) | Simple, debuggable, standard | Slightly slower than Named Pipes | ✅ SELECTED |
| Named Pipes | Faster on Windows | Complex async handling, harder to debug | ❌ Rejected |
| gRPC | Type-safe, efficient | Additional dependencies, proto files | ❌ Rejected |

### Bridge API Design

**Endpoints**:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check |
| POST | `/auth/login` | Initiate OAuth login |
| POST | `/auth/callback` | OAuth callback processing |
| GET | `/auth/status` | Check authentication status |
| POST | `/auth/token/refresh` | Refresh access token |
| POST | `/auth/logout` | Clear tokens |
| GET | `/parameters` | Retrieve COBie parameters |
| GET | `/parameters/specs` | Get available data types |
| GET | `/parameters/categories` | Get Revit categories |

### Token Storage Strategy

- **Bridge**: Uses ACG.APS.Core TokenStorage (file-based in %LocalAppData%\ACG_Bridge\token.json)
- **Add-in**: Receives tokens from bridge via HTTP, stores in memory only
- **Synchronization**: Bridge manages all token operations including refresh

---

## 3. Token Storage Strategy (ACG.APS.Core Alignment)

### Decision: Use ACG.APS.Core TokenStorage (File-based)

**Rationale**:
- Reuses existing ACG.APS.Core infrastructure
- Consistent token storage across all ACG applications
- No additional dependencies
- JSON format for easy debugging

**File Location**: `%LocalAppData%\ACG_Bridge\token.json`

**Stored Data Structure**:
```json
{
  "AccessToken": "...",
  "RefreshToken": "...",
  "ExpiresAt": "2025-02-05T12:34:56Z"
}
```

**Implementation**: Use ACG.APS.Core's `TokenStorage` class and `ITokenStorage` interface directly in the bridge executable.

**Implementation Notes**:
- Store JSON string containing: `{"accessToken": "...", "refreshToken": "...", "expiresAt": "ISO8601"}`
- Credential Manager handles encryption via Windows DPAPI
- Credentials are user-specific (roaming profile supported)

### Alternatives Considered

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| P/Invoke advapi32 | No dependencies, built-in | More code to write | ✅ SELECTED |
| NuGet package `CredentialManagement` | Simple API | External dependency | ❌ Rejected |
| NuGet package `WindowsCredentialManager` | Simple API | May not support .NET Framework 4.8 | ❌ Rejected |

---

## 4. JSON Serialization Research

### Decision: Use Newtonsoft.Json (Json.NET)

**Rationale**:
- Already compatible with .NET Framework 4.8
- Mature, stable library
- More flexible than System.Text.Json for complex scenarios
- May already be available in project dependencies

**Package**: `Newtonsoft.Json` (add via NuGet)

### Alternatives Considered

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| Newtonsoft.Json | Mature, flexible, net48 support | Slightly slower than STJ | ✅ SELECTED |
| System.Text.Json | Built-in (.NET 6+) | Requires polyfill for net48 | ❌ Rejected |
| DataContractJsonSerializer | Built-in | Less flexible, verbose | ❌ Rejected |

---

## 5. Revit Shared Parameter GUID Research

### Decision: Use APS Parameter ID as GUID

**Rationale**:
- APS parameters have stable IDs (SHA256-based)
- Can convert to GUID format for Revit
- Ensures consistency across projects

**Implementation**:
- APS parameter IDs are 32-character hex strings
- Convert to GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- Store in SharedParameterElement in Revit

**Caveats**:
- Not all APS IDs are valid GUIDs (may need padding)
- Fallback: Generate new GUID if APS ID is invalid
- Document mapping for troubleshooting

---

## 6. Revit Category Mapping Research

### Decision: Pre-defined Category Mapping Table

**Mapping Strategy**:

| APS Category ID | Revit BuiltInCategory | Discipline |
|-----------------|----------------------|------------|
| `autodesk.revit.category.instances:walls-1.0.0` | `OST_Walls` | Architectural |
| `autodesk.revit.category.instances:doors-1.0.0` | `OST_Doors` | Architectural |
| `autodesk.revit.category.instances:windows-1.0.0` | `OST_Windows` | Architectural |
| `autodesk.revit.category.instances:columns-1.0.0` | `OST_Columns` | Structural/Arch |
| `autodesk.revit.category.instances:structuralcolumns-1.0.0` | `OST_StructuralColumns` | Structural |
| `autodesk.revit.category.instances:structuralframing-1.0.0` | `OST_StructuralFraming` | Structural |
| `autodesk.revit.category.instances:floors-1.0.0` | `OST_Floors` | Architectural |
| `autodesk.revit.category.instances:ceilings-1.0.0` | `OST_Ceilings` | Architectural |
| `autodesk.revit.category.instances:roofs-1.0.0` | `OST_Roofs` | Architectural |
| `autodesk.revit.category.instances:rooms-1.0.0` | `OST_Rooms` | Architectural |

**Fallback Handling**:
- If APS category not in mapping table → Prompt user to select
- Retrieve all available categories from APS `/classifications/categories` endpoint
- Cache for offline use

---

## 7. Reference Data Endpoints

### Available Reference Data

| Endpoint | Purpose | Cache Strategy |
|----------|---------|----------------|
| `/specs` | Get all data types | Cache indefinitely |
| `/units` | Get all units | Cache indefinitely |
| `/disciplines` | Get disciplines | Cache indefinitely |
| `/classifications/groups` | Get classification systems | Cache indefinitely |
| `/classifications/categories` | Get Revit categories | Cache indefinitely |

**Note**: Reference data changes infrequently and should be cached locally for offline support.

---

## 8. Documentation Links

| Resource | URL |
|----------|-----|
| Parameters API Overview | https://aps.autodesk.com/developer/overview/parameters-api |
| API Reference | https://aps.autodesk.com/en/docs/parameters/v1/reference |
| Getting Started | https://aps.autodesk.com/en/docs/parameters/v1/tutorials/getting-started/ |
| Postman Collection | https://github.com/autodesk-platform-services/aps-parameters-postman.collection |
| OAuth Scopes | https://aps.autodesk.com/en/docs/oauth/v2/developers_guide/scopes |
| Rate Limits | https://aps.autodesk.com/en/docs/parameters/v1/overview/rate-limits/ |

---

## 9. Open Questions Resolved

| Question | Answer | Impact |
|----------|--------|--------|
| APS Parameters API endpoint | `https://developer.api.autodesk.com/parameters/v1` | Bridge implementation |
| Required scopes | `data:read data:create data:write account:read` (ACG.APS.Core) | Auth configuration |
| JSON schema | Documented above | Data model design |
| IPC protocol | HTTP localhost:5000 | Bridge architecture |
| Token Storage | ACG.APS.Core file-based (%LocalAppData%\ACG_Bridge\token.json) | Reuse existing infrastructure |
| JSON serialization | Newtonsoft.Json | Add NuGet package |
| GUID handling | Convert APS ID to GUID | Parameter creation |
| Category mapping | Pre-defined table + fallback | UI implementation |

---

## 10. Technical Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| IPC Protocol | HTTP localhost | Simple, debuggable |
| JSON Library | Newtonsoft.Json | .NET Framework 4.8 compatible |
| Token Storage | ACG.APS.Core file-based | Aligns with existing ACG infrastructure |
| Parameter Cache | JSON file | Simple, human-readable |
| Bridge Lifecycle | Start on demand, timeout idle | Resource efficiency |
| GUID Strategy | APS ID as GUID | Consistency |
| Category Mapping | Pre-defined + user prompt | Flexibility |
| OAuth Flow | PKCE (ACG.APS.Core) | Secure, matches existing pattern |
