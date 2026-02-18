# Feature: Auto-Fill ACG-BOX-ID from Revit Group Name

## Objective
Automatically populate the **ACG-BOX-ID** parameter for all elements that belong to a **Model Group** by using the **Group Name**.

This ensures that:
- Every element inside a group carries its group identifier
- Data is consistent across schedules, tags, and exports
- No manual assignment is required

---

## Use Case
In box-based/modular workflows, each model group represents a **box unit**.

Example:
- Group Name: `BOX-A-01`
- Members: walls, doors, equipment, generic models…

After execution:

| Element | ACG-BOX-ID |
|--------|------------|
| Wall | BOX-A-01 |
| Door | BOX-A-01 |
| Equipment | BOX-A-01 |

---

## Scope

### Target Elements
- Elements inside **Model Groups**
- Optional: Model Group element itself

### Parameter
- Name: `ACG-BOX-ID`
- Type: **Text / String**
- Binding: Project Parameter
- Categories:
  - Walls
  - Doors
  - Windows
  - Furniture
  - Generic Models
  - MEP Equipment
  - Structural Elements
  - Model Groups (optional but recommended)

---

## Workflow

### Step 1 — Collect Model Groups
Use:
- Category: `OST_IOSModelGroups`
- Filter: instance elements only

Result:
- List of all placed group instances in the model

---

### Step 2 — Read Group Name
For each group:

```
boxId = group.Name
```

Rules:
- Trim whitespace
- Skip if null or empty
- Optional normalization:
  - remove spaces
  - uppercase
  - replace illegal characters

---

### Step 3 — Get Group Members

```
memberIds = group.GetMemberIds()
```

Each ID represents an element belonging to the group instance.

---

### Step 4 — Assign Parameter to Members

For each member:

1. Lookup parameter:
```
param = element.LookupParameter("ACG-BOX-ID")
```

2. Validate:
- exists
- writable
- string storage

3. Write:
```
param.Set(group.Name)
```

---

### Step 5 — Optional: Assign to Group Element Itself
Useful for:
- scheduling groups
- filtering by box
- QA dashboards

```
group.LookupParameter("ACG-BOX-ID").Set(group.Name)
```

---

## Performance Strategy

### Recommended iteration order
**Group-first approach:**

1. Collect groups
2. For each group → update members

Benefits:
- avoids scanning entire model
- ensures consistency
- faster on large models

---

## Safety Rules

1. Do not overwrite values unless enabled
2. Skip:
   - nested groups
   - elements without parameter
   - read-only parameters
3. Log skipped elements

---

## Logging

Track:

- Groups scanned
- Groups skipped (no name)
- Members scanned
- Members updated
- Members skipped:
  - missing parameter
  - read-only
  - nested group

---

## Edge Cases

### Nested groups
- Skip in Milestone 1
- Support later if required

### Elements without category
- Skip

### Elements outside group
- Ignore (feature is group-driven)

---

## Acceptance Criteria

- Running command fills **ACG-BOX-ID** for all grouped elements
- No Revit warnings
- No group breakage
- No duplicate operations
- Execution time acceptable for large models

---

## Deliverables

### Core service
`BoxIdFiller`

Responsibilities:
- Collect groups
- Extract group name
- Iterate members
- Set parameter
- Log result

### Helper
`TrySetParameter(Element e, string paramName, string value)`

### UI command
`Fill ACG-BOX-ID from Groups`

Options:
- overwrite existing values
- include group element
- preview mode

---

## Future Enhancements

- Support nested groups
- Push BOX-ID to ungrouped elements using spatial logic
- Add QA dashboard
- Batch processing across links
- APS sync for BOX metadata

