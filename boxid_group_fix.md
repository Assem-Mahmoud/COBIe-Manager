# LLM Prompt — Fix Revit Group Warning & Fill ACG-BOX-ID

## Objective
Implement a Revit add-in fix that safely fills the project parameter:

ACG-BOX-ID

for all elements inside Model Groups, using:

GroupType.Name

as the value.

This must be done without triggering Revit warnings, group edit mode, or breaking group constraints.

---

## Problem

The current implementation:
- Iterates all group instances
- Writes parameter values on group members
- Causes Revit warnings:
  - editing grouped elements outside group edit mode
  - modifying multiple group instances
  - group constraint violations

We must fix this behavior.

---

## Required Solution Strategy

### 1) Process per GroupType (NOT per Group instance)

Implementation logic:

1. Collect all model group instances
FilteredElementCollector(doc).OfClass(Group)

2. Group them by:
Group.GroupType.Id

3. For each group type:
   - Select ONE representative group instance
   - Get member elements:
     group.GetMemberIds()
   - Write parameter values on members once
   - Skip all other instances of the same group type

This updates the group definition safely and avoids multi-instance editing warnings.

---

### 2) Parameter Assignment Rules

Parameter:
ACG-BOX-ID

Value:
groupInstance.GroupType.Name

Apply to:
- all member elements
- skip nested groups

Validation before write:
- parameter exists
- writable
- string storage type

---

### 3) Suppress Group Warnings via Failure API

Implement IFailuresPreprocessor

Suppress:
- BuiltInFailures.GroupFailures.AtomViolationWhenOnePlaceInstance
- BuiltInFailures.GroupFailures.AtomViolationWhenMultiPlacedInstances
- BuiltInFailures.GroupFailures.ModifyingMultiGroups

Attach suppressor to transaction:
tx.GetFailureHandlingOptions().SetFailuresPreprocessor(...)

---

### 4) Implementation Requirements

Create method:
FillBoxIdFromGroupTypes(Document doc)

Behavior:
- Collect model groups
- Group by GroupType
- For each group type:
    - get representative instance
    - read GroupType.Name
    - iterate members
    - set ACG-BOX-ID on each member
- Commit transaction

Must be idempotent.

---

### 5) Constraints

Do NOT:
- ungroup elements
- edit each group instance individually
- trigger edit group mode
- modify multiple instances separately
- write different values per instance

We are updating the GROUP DEFINITION only.

---

### 6) Expected Result

After execution:
- Every element inside model groups has ACG-BOX-ID
- No Revit warnings appear
- No edit-group prompts
- No group breakage
- Fast execution on large models

---

### 7) Required Output from LLM

Provide:
1. Production-ready C# implementation
2. Failure suppressor class
3. Helper method:
   TrySetStringParameter(Element e, string name, string value)
4. Result summary object

---

Important:
This works ONLY when ACG-BOX-ID = GroupType.Name.
