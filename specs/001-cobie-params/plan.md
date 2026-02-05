# Implementation Plan: COBie Parameter Management for Revit

**Branch**: `001-cobie-params` | **Date**: 2025-02-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-cobie-params/spec.md`

## Summary

This feature enables BIM managers to retrieve COBie parameter definitions from Autodesk Platform Services (APS) Parameters Service and add them as shared parameters to Revit projects. The add-in provides a WPF-based UI for parameter browsing, selection, and project binding with proper category mapping.

**Technical Approach**:
- Reuse existing ACG.APS.Core library for APS OAuth authentication with PKCE flow
- Implement APS Parameters API client for retrieving COBie parameter definitions
- Create WPF MVVM UI using existing Material Design setup
- Use Revit API for shared parameter creation and category binding

## Technical Context

**Language/Version**: C# / .NET Framework 4.8 (required for Revit 2023-2026 compatibility)

**Primary Dependencies**:
- Revit API 2023/2024/2025/2026 (multi-version support)
- CommunityToolkit.Mvvm 8.4.0 (MVVM framework)
- MaterialDesignThemes 5.2.1 (UI components)
- ACG.APS.Core (net8.0) - **COMPATIBILITY BRIDGE** via separate executable
- Newtonsoft.Json (JSON serialization for .NET Framework 4.8)

**Storage**:
- File-based token storage (ACG.APS.Core pattern): %LocalAppData%\ACG_Bridge\token.json
- Local file cache for retrieved parameters (JSON format, per user session)
- Local file logs to %APPDATA%\COBIeManager\Logs\

**Testing**: xUnit + Moq (already configured in project, not yet implemented)

**Target Platform**: Windows Desktop (Revit 2023-2026 add-in)

**Project Type**: Desktop plugin (single WPF add-in with APS bridge executable)

**Performance Goals**:
- Authentication + parameter retrieval: < 30 seconds (SC-001)
- Parameter search/filter: < 10 seconds (SC-002)
- Add 50 parameters: < 60 seconds (SC-003)

**Constraints**:
- .NET Framework 4.8 required (Revit API constraint)
- Must work offline with cached parameters
- Transaction-based Revit document modifications
- Non-blocking UI (async operations with RevitAsync)

**Scale/Scope**:
- ~100-200 COBie parameters expected
- Support 3 discipline categories (Architectural, Structural, MEP)
- Single-user desktop scenario (no multi-user collaboration)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ⚠️ Constitution not yet customized (template only)

The following principles are applied based on common software development practices:

| Principle | Status | Notes |
|-----------|--------|-------|
| Separation of Concerns | ✅ Pass | Feature-based folder structure with MVVM pattern |
| Dependency Injection | ✅ Pass | Uses existing ServiceCollection/ServiceLocator |
| Single Responsibility | ✅ Pass | Each service has one clear purpose |
| Testability | ⚠️ Pending | Unit tests to be added during implementation |
| Observability | ✅ Pass | FileLogger + operation summaries |

## Project Structure

### Documentation (this feature)

```text
specs/001-cobie-params/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (APS API contracts)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
# COBIeManager Main Project (net48)
COBIeManager/
├── Features/
│   └── CobieParameters/              # NEW FEATURE
│       ├── Commands/
│       │   └── CobieParametersCommand.cs
│       ├── ViewModels/
│       │   ├── CobieParametersViewModel.cs
│       │   └── ParameterSelectionViewModel.cs
│       ├── Views/
│       │   ├── CobieParametersWindow.xaml
│       │   └── CategorySelectionDialog.xaml
│       └── Models/
│           ├── CobieParameterDefinition.cs
│           ├── ParameterBindingResult.cs
│           └── CategorySet.cs
├── Shared/
│   ├── Services/
│   │   ├── CobieParameterService.cs          # NEW - Revit parameter operations
│   │   └── ApsTokenBridgeService.cs           # NEW - manages bridge process
│   ├── Interfaces/
│   │   ├── ICobieParameterService.cs          # NEW
│   │   └── IApsBridgeClient.cs                # NEW
│   └── APS/
│       ├── ApsBridgeClient.cs                 # NEW - communicates with bridge
│       └── Models/
│           ├── ApsParameterRequest.cs
│           ├── ApsParameterResponse.cs
│           └── TokenRequestDto.cs
└── App.cs                                      # UPDATE - register new services

# APS Bridge Executable (net8.0) - NEW PROJECT
APS.Bridge/
├── Program.cs                                 # Entry point
├── ApsBridgeService.cs                        # HTTP server (Kestrel)
├── Controllers/
│   ├── AuthController.cs                      # OAuth endpoints
│   └── ParametersController.cs                # Parameters endpoints
├── Services/
│   └── ApsParametersService.cs                 # APS Parameters API client
└── References ACG.APS.Core (net8.0)           # Uses existing TokenStorage

# Existing structure remains
Shared/
├── DependencyInjection/
├── Logging/
└── ...
```

**Structure Decision**: Single add-in project with external APS bridge executable to resolve .NET version incompatibility. The bridge runs as a local HTTP server that the .NET Framework add-in communicates with for APS operations. Authentication uses ACG.APS.Core's OAuth 2.0 PKCE flow and file-based token storage.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| External APS Bridge (net8.0) | ACG.APS.Core targets net8.0, incompatible with net48 Revit add-in | Direct reference would require rewriting ACG.APS.Core for net48, duplicating code |
| gRPC/HTTP local server | IPC mechanism between net48 add-in and net8.0 bridge | Named pipes have complex async handling in Revit context |

## Phase 0: Research (COMPLETE)

See [research.md](./research.md) for complete findings.

### Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| IPC Protocol | HTTP localhost:5000 | Simple, debuggable |
| JSON Library | Newtonsoft.Json | .NET Framework 4.8 compatible |
| Token Storage | ACG.APS.Core file-based | Aligns with existing ACG infrastructure |
| OAuth Flow | PKCE (ACG.APS.Core) | Secure, matches existing pattern |

## Phase 1: Design Artifacts (COMPLETE)

### Data Model

See [data-model.md](./data-model.md) for complete entity definitions.

### API Contracts

See [contracts/aps-bridge-api.yaml](./contracts/aps-bridge-api.yaml) for OpenAPI specification.

### Quickstart Guide

See [quickstart.md](./quickstart.md) for implementation steps.

## Dependencies on Other Features

None - this is a standalone feature.

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| APS Parameters API changes | High | Version-specific API clients, fallback to search |
| .NET version bridge failure | High | Thorough IPC testing, fallback to direct implementation |
| Revit category mapping errors | Medium | Validation before binding, user prompt for missing metadata |
| Token refresh failure | Medium | Graceful re-auth prompt, clear error messages |

## Success Criteria Mapping

| Spec Criteria | Implementation Approach |
|---------------|------------------------|
| SC-001: Auth + retrieve < 30s | Async operations, progress indicators |
| SC-002: Search/filter < 10s | Client-side filtering on cached parameters |
| SC-003: Add 50 params < 60s | Batch transaction, parallel where safe |
| SC-004: 95% success rate | Pre-validation, detailed error reporting |
| SC-005: Parameters visible in UI | Category binding verification |
| SC-006: Idempotent behavior | Name+type duplicate check (FR-022) |
| SC-007: Clear error messages | Specific error codes with user-friendly text |
