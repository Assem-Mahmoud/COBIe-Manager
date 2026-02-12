# Research: Auto-Fill Revit Parameters

**Feature**: 003-auto-fill-parameters
**Date**: 2025-02-12
**Status**: Complete

## Overview

This document captures research findings for implementing the Auto-Fill Parameters feature. All technical decisions are based on Revit API best practices and existing codebase patterns.

## Research Areas

### 1. Revit API: Room Detection Methods

**Question**: What is the most reliable and performant way to detect room ownership for elements?

**Decision**: Use a tiered strategy in priority order:
1. **FamilyInstance.Room** - Direct property available on many FamilyInstances
2. **FamilyInstance.FromRoom / ToRoom** - Door-specific properties, prefer FromRoom
3. **Room.GetRoomAtPoint(XYZ)** - Point-in-room test using element location

**Rationale**:
- Room property is fastest (O(1) lookup) and works for most placed instances
- FromRoom/ToRoom are door-specific and provide clear semantics for doors between rooms
- GetRoomAtPoint() is reliable for elements without direct room association
- SpatialElementGeometryCalculator is excluded from Milestone 1 (deferred for Walls/Floors/Ceilings)

**Alternatives Considered**:
- SpatialElementGeometryCalculator: More accurate for complex geometry but significantly slower and more complex. Deferred to Milestone 2.

### 2. Revit API: BoundingBox Access by Category

**Question**: How to reliably get bounding boxes for target element categories?

**Decision**: Use `element.get_BoundingBox(null)` for all element types

**Rationale**:
- The `null` parameter returns the bounding box in model coordinates (not view-specific)
- Works consistently across FamilyInstance (Doors, Windows, Furniture, Equipment, Generic Models)
- BoundingBox.Min.Z and BoundingBox.Max.Z provide vertical position
- Z values are in project units (feet for imperial, meters for metric)

**Alternatives Considered**:
- View-specific bounding box: Not appropriate since we need model-wide positioning
- Geometry computation: More accurate but significantly slower; not needed for simple vertical band detection

### 3. Revit API: Transaction Pattern for Bulk Updates

**Question**: What transaction pattern balances performance with rollback safety?

**Decision**: Use tiered transaction strategy:
- Models with < 5,000 elements: Single Transaction
- Models with >= 5,000 elements: TransactionGroup with sub-transactions every 1,000 elements

**Rationale**:
- Single Transaction is fastest but holds Revit lock for entire duration
- TransactionGroup with periodic commits allows UI responsiveness updates
- 1,000 element batch size balances commit overhead with progress feedback

**Alternatives Considered**:
- One transaction per element: Too slow (1000+ transactions would take minutes)
- Sub-transactions with independent commits: Unnecessary complexity for Milestone 1 scope

### 4. Revit API: Phase Handling for Room Lookup

**Question**: How to handle elements in different phases than rooms?

**Decision**: Use `document.ActivePhase` for all room queries (element phase ignored)

**Rationale**:
- Simplifies implementation and user mental model
- Users typically work in one phase at a time
- Matches clarified requirement from specification

**Alternatives Considered**:
- Match element's CreatedPhase or DemolishedPhase: More complex but potentially more accurate
- Query all phases and match: Significantly slower, unnecessary for typical workflow

### 5. Revit API: Parameter Write Safety

**Question**: How to safely write parameters without exceptions?

**Decision**: Implement defensive checks before writing:
1. Check parameter exists on element: `element.LookupParameter(parameterName) != null`
2. Check parameter is writable: `!parameter.IsReadOnly`
3. Respect overwrite flag: Check existing value before writing when overwrite is false

**Rationale**:
- Prevents exceptions that would roll back entire transaction
- Respects user's overwrite preference
- Gracefully handles missing or read-only parameters

**Alternatives Considered**:
- Try/catch per parameter: Too much overhead, harder to distinguish expected vs unexpected failures
- Assume all parameters exist: Would fail on documents without COBie parameters bound

## API References

### Key Revit API Classes

| Class | Purpose |
|-------|----------|
| `FilteredElementCollector` | Efficient element collection by category |
| `Level` | Level element with Elevation property |
| `Room` | Room element with Number, Name properties |
| `FamilyInstance` | Placed family instances (doors, windows, etc.) |
| `BoundingBoxXYZ` | Element bounding box with Min/Max XYZ coordinates |
| `Parameter` | Element parameter with Set() method and IsReadOnly property |
| `Transaction` | Atomic document modification |
| `TransactionGroup` | Group of related transactions |
| `Phase` | Document phase for phase-aware operations |

### Key Revit API Methods

| Method | Purpose |
|---------|----------|
| `element.get_BoundingBox(null)` | Get model-space bounding box |
| `Room.GetRoomAtPoint(XYZ)` | Find room at specific point |
| `element.LookupParameter(string)` | Find parameter by name |
| `parameter.Set(string)` | Set string parameter value |
| `document.ActivePhase` | Get current active phase |

## Performance Considerations

1. **FilteredElementCollector**: Use category filters to reduce iteration
2. **Level elevation comparison**: Store as double for O(1) comparison
3. **Room lookup caching**: Cache room collections by phase for repeated queries
4. **Progress reporting**: Report after every 100 elements for responsive UI

## Compatibility Notes

- Tested for Revit 2023-2026 API
- Uses conditional compilation for version-specific APIs if needed
- Follows existing multi-version build configuration (Debug2023, Release2024, etc.)
