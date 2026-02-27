# Data Model: Fix Floor Plan Name Search

**Branch**: `002-fix-floor-plan-name` | **Phase**: 1 | **Date**: 2026-02-27

---

## Entities

### Border *(unchanged)*

Represents a closed polyline in model space that marks a title-block boundary.

| Attribute | Type | Notes |
|---|---|---|
| `Layer` | string | Must end with `BorderLayerSuffix` (e.g., `$0$PAPER-EX`) |
| `Extents.MinPoint` | Point (X, Y) | Bottom-left corner of the bounding box |
| `Extents.MaxPoint` | Point (X, Y) | Top-right corner of the bounding box |

**Invariants**:
- Polyline must be closed (`AcceptClosedPolylinesOnly = true`)
- Extents are derived from the polyline geometry, not stored separately

---

### Floor Plan Name Text *(refined)*

A text entity (single-line `DBText` or multi-line `MText`) that represents the drawing title
inside a border. After this feature, the selection rule changes from "must match fixed height"
to "largest positive height inside border bounds".

| Attribute | Type | Notes |
|---|---|---|
| `Layer` | string | Must end with `TextLayerSuffix` (e.g., `TEX`) |
| `InsertionPoint` | Point (X, Y) | Must lie inside parent border's bounding extents (inclusive) |
| `Height` | double | Must be > 0 |
| `RawText` | string | `DBText.TextString` or `MText.Contents` (before stripping) |
| `CleanText` | string | After trimming (`DBText`) or after format-code stripping + trimming (`MText`) |

**Selection rule**: Among all valid candidates within a border, the entity with the
**largest `Height`** is selected. Tie-breaking is by model-space iteration order (first encountered).

**Validation rules** (all must pass for a candidate to qualify):

1. Layer name ends with `TextLayerSuffix` (case-insensitive)
2. `InsertionPoint` is inside border bounding extents (inclusive of boundary)
3. `Height > 0`
4. `CleanText` is non-empty after trimming / stripping

---

### MTextFormatStripper *(new utility)*

A pure, stateless utility for transforming `MText.Contents` raw strings into plain text.

| Input | Output | Rules |
|---|---|---|
| `null` | `""` (empty string) | Guard clause |
| `""` | `""` | Pass-through |
| Raw MText with format codes | Plain text | Three-pass regex strip, then whitespace collapse and trim |

**Three-pass strip algorithm**:

1. Remove semicolon-terminated codes: pattern `\\[A-Za-z*'][^;]*;`
2. Replace single-character escape codes with a space: pattern `\\[A-Za-z~]`
3. Remove group delimiters: `{` and `}`
4. Collapse consecutive whitespace to a single space, then `Trim()`

---

## AppConfig Fields *(unchanged schema)*

The following two fields are retained in `AppConfig` for backward compatibility with existing
`cadparsing.config.json` files. They are **not read** by the floor plan name search after this fix.

| Field | Type | Default | Status after fix |
|---|---|---|---|
| `FloorPlanTextHeight` | double | 400.0 | Retained, not read by text search |
| `TextHeightTolerance` | double | 0.5 | Retained, not read by text search |

---

## State Transitions

No state machines involved. The search is a stateless, single-pass scan of model-space entities
within a transaction.

---

## Source-to-Entity Mapping

| AutoCAD Type | Entity | Key Property |
|---|---|---|
| `DBText` | Floor Plan Name Text | `TextString`, `Height`, `Position` |
| `MText` | Floor Plan Name Text | `Contents` (stripped), `TextHeight`, `Location` |
| `Polyline` | Border | `GeometricExtents` |
