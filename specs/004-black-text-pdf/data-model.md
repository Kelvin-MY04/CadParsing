# Data Model: Black Text in Color PDF Export

**Branch**: `004-black-text-pdf` | **Date**: 2026-02-27

---

## In-Memory Entities (per color PDF export pass)

### TextEntitySnapshot

Represents the saved state of one text-bearing entity before the black override is applied.
This is the value type stored in the `savedColors` dictionary returned by
`TextColorOverride.ApplyBlackOverride`.

| Field | Type | Description |
|-------|------|-------------|
| EntityId | `ObjectId` | Stable AutoCAD database handle for the entity |
| OriginalColor | `Color` | The entity's `Color` property value before override (may encapsulate ByLayer, ByBlock, or an explicit ACI/RGB color) |

**Stored as**: `Dictionary<ObjectId, Color>` where key = `EntityId`, value = `OriginalColor`.

**Lifecycle**:
1. Created by `TextColorOverride.ApplyBlackOverride` immediately before the color PDF plot.
2. Consumed and discarded by `TextColorOverride.RestoreOriginalColors` immediately after.
3. Never persisted to disk or passed outside the `ExportBorderWithAllStyles` stack frame.

---

## Domain Entities (AutoCAD database objects)

These entities already exist in the DWG database. This feature reads and temporarily modifies
their `Color` property; it does not create or delete any database objects.

### Text-Bearing Entity

Any AutoCAD database entity that displays readable text and is within the override scope.

| AutoCAD Type | Base Class | Scope |
|--------------|-----------|-------|
| `DBText` | `Entity` | Direct model space + nested block definitions |
| `MText` | `Entity` | Direct model space + nested block definitions |
| `Dimension` (all subtypes) | `Entity` | Direct model space + nested block definitions |
| `Leader` | `Entity` | Direct model space + nested block definitions |
| `MLeader` | `Entity` | Direct model space + nested block definitions |

**Color states before override**:
- Explicit color (ACI index or RGB) — stored and restored directly.
- ByLayer (`ColorIndex == 256`) — stored as-is via `entity.Color`; restored identically.
- ByBlock (`ColorIndex == 0`) — stored as-is via `entity.Color`; restored identically.

**Post-export invariant**: Every entity's `Color` property MUST equal its pre-export value
after `RestoreOriginalColors` completes. No permanent modification reaches the on-disk DWG.

---

## Block Traversal Scope

### BlockDefinitionVisit

Tracks which `BlockTableRecord` definitions have already been traversed to prevent infinite
recursion when block definitions reference each other (rare but possible in complex DWGs).

| Field | Type | Description |
|-------|------|-------------|
| VisitedDefinitionIds | `HashSet<ObjectId>` | ObjectIds of `BlockTableRecord` definitions already traversed |

**Lifecycle**: Created at the start of `FindAllTextEntities`, passed through every recursive
call to `CollectTextEntitiesFromBlock`, and discarded when `FindAllTextEntities` returns.

---

## State Transitions

```
Entity.Color (original)
        │
        │  ApplyBlackOverride
        ▼
Entity.Color = RGB(0,0,0)   ──▶  Color PDF plotted
        │
        │  RestoreOriginalColors
        ▼
Entity.Color (original)     ──▶  B/W PDF plotted (entity color irrelevant — monochrome CTB)
        │
        │  transaction.Commit()
        ▼
DWG on disk: Entity.Color (original)   [no permanent change]
```

---

## Relationships

```
ExportBorderWithAllStyles
        │
        │ (styleIndex == 0, color variant only)
        │
        ├─ TextEntityFinder.FindAllTextEntities
        │       └─ scans ModelSpace BlockTableRecord
        │               └─ recurses into BlockReference.BlockTableRecord (all depths)
        │                       └─ yields ObjectId for each text-bearing entity
        │
        ├─ TextColorOverride.ApplyBlackOverride
        │       └─ for each ObjectId → opens ForWrite → saves Color → sets BlackColor
        │               └─ produces Dictionary<ObjectId, Color>  (TextEntitySnapshot set)
        │
        ├─ ExportSinglePdf("acad.ctb")
        │       └─ AutoCAD plot engine renders entities at current DB state (all text = black)
        │
        └─ TextColorOverride.RestoreOriginalColors
                └─ for each entry → opens ForWrite → restores saved Color
```
