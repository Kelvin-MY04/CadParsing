# Feature Specification: Fix Floor Plan Name Search

**Feature Branch**: `002-fix-floor-plan-name`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Fix getting floor plan name — currently the floor plan name can't be searched because of the restrict rules like `FontHeight = 400`. Therefore, even the borders are detected in DWG files, the PDF files are not going to export. Fix that search and get the floor plan name by the largest font height which text is in `TEX` layer of the parent detect border layer."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - PDF Export Succeeds When Text Height Varies (Priority: P1)

A user processes a DWG file where the floor plan name text exists on the TEX layer inside a detected border, but the text height is not exactly 400 (e.g., it is 300, 500, or 250). Currently the system fails to find the floor plan name and silently skips exporting the PDF. After this fix, the system finds the text with the largest font height on the TEX layer inside the border bounds and uses it as the floor plan name, allowing the PDF to export successfully.

**Why this priority**: This is the core blocking bug — PDFs silently fail to export even when borders are successfully detected. This is the highest-value outcome for users running batch DWG-to-PDF exports.

**Independent Test**: Run the tool against a DWG file containing a border with a TEX-layer text whose height is not 400. Confirm a PDF is produced and named after the detected text.

**Acceptance Scenarios**:

1. **Given** a DWG file with a detected border and a TEX-layer text inside it at height 300, **When** the export command runs, **Then** the floor plan name is the TEX-layer text with height 300 and a PDF is exported with that name.
2. **Given** a DWG file with multiple TEX-layer texts inside a border at heights 200, 350, and 500, **When** the export command runs, **Then** the floor plan name is the text with height 500 (the largest), and a PDF is exported.
3. **Given** a DWG file where the TEX-layer text has height exactly 400, **When** the export command runs, **Then** behavior is unchanged — the text is still found and the PDF is exported.

---

### User Story 2 - Largest-Height Text Selected as Floor Plan Name (Priority: P2)

When multiple TEX-layer text entities are found inside a border, the system selects the one with the largest font height as the floor plan name. This reflects the convention that the most prominent (tallest) text inside a border is the drawing title.

**Why this priority**: Ensures deterministic, correct selection when multiple candidates exist, preventing wrong-label exports.

**Independent Test**: Provide a DWG with two TEX-layer texts inside one border at different heights. Confirm the exported PDF file is named after the taller text.

**Acceptance Scenarios**:

1. **Given** a border containing TEX-layer texts "A101" at height 300 and "FLOOR PLAN" at height 500, **When** export runs, **Then** the PDF is named using "FLOOR PLAN".
2. **Given** a border containing exactly one TEX-layer text, **When** export runs, **Then** that text is used as the floor plan name regardless of its height.

---

### User Story 3 - Graceful Handling When No TEX Text Is Found (Priority: P3)

If no TEX-layer text is present inside a detected border, the system falls back gracefully (uses a default label or skips the border) rather than crashing or producing a malformed PDF name.

**Why this priority**: Defensive behavior — prevents regressions for DWG files that legitimately have no TEX text inside a border.

**Independent Test**: Process a DWG file with a detected border but no TEX-layer text inside it. Confirm the system does not crash and either skips that border or uses an existing fallback label.

**Acceptance Scenarios**:

1. **Given** a border with no TEX-layer text inside its bounds, **When** export runs, **Then** the system does not crash and the border is either skipped or exported with the existing fallback label logic.

---

### Edge Cases

- What happens when a TEX-layer text's insertion point lies exactly on the border boundary?
- How does the system handle TEX-layer texts with a height of zero or negative value?
- What if two TEX-layer texts inside a border share the same maximum height? (Either is acceptable; selection must be deterministic.)
- What if the largest-height text contains only whitespace after trimming?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST remove the fixed-height filter (`FloorPlanTextHeight` / `TextHeightTolerance`) from the floor plan name search logic.
- **FR-002**: The system MUST select the TEX-layer text entity with the largest font height whose insertion point falls within the detected border's bounding extents as the floor plan name.
- **FR-003**: The system MUST ignore TEX-layer text entities whose insertion point lies outside the parent border's bounding extents.
- **FR-004**: The system MUST strip AutoCAD MText formatting codes (e.g., `\P`, `\f{...}`, `\H...;`) from `MText` entity contents before evaluating or using the text as a floor plan name. After stripping, the system MUST ignore any entity whose text is empty or whitespace-only.
- **FR-005**: The system MUST ignore TEX-layer text entities with a font height of zero or less.
- **FR-006**: When no eligible TEX-layer text is found inside a border, the system MUST emit a warning log entry identifying the border (by its extents or layer name) and MUST return null (preserving existing fallback behavior in the export command).
- **FR-007**: The `FloorPlanTextHeight` and `TextHeightTolerance` configuration fields MUST be retained in `AppConfig` so that existing `cadparsing.config.json` files continue to parse cleanly. These fields MUST NOT affect the floor plan name search.

### Key Entities

- **Border**: A closed polyline on a layer whose name ends with the `BorderLayerSuffix`. It defines a bounding extents (MinPoint, MaxPoint) used to scope the text search.
- **Floor Plan Name Text**: A text entity on a layer whose name ends with the `TextLayerSuffix` (`TEX`), with its insertion point inside the parent border's bounding extents and a positive font height. The candidate with the largest height is selected.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every DWG file that has at least one border with at least one valid TEX-layer text inside it produces an exported PDF — regardless of the text's font height.
- **SC-002**: When multiple TEX-layer texts exist inside a single border, the exported PDF is always named after the text with the largest font height.
- **SC-003**: No regressions — all existing unit tests continue to pass after the change.
- **SC-004**: The floor plan name search produces the same result for a given DWG file on every run (deterministic output).

## Clarifications

### Session 2026-02-27

- Q: Should `FloorPlanTextHeight` and `TextHeightTolerance` be kept in `AppConfig` or removed entirely? → A: Keep both fields in `AppConfig` so existing config files continue to parse cleanly; they are no longer read by the text search.
- Q: Should `MText.Contents` be used as-is or should AutoCAD format codes be stripped before use? → A: Strip AutoCAD MText format codes before using the text as a floor plan name to prevent garbled PDF filenames.
- Q: Should the system log a diagnostic when no TEX-layer text is found inside a border? → A: Yes — emit a warning log entry identifying the border so users can diagnose missing PDFs during batch runs.

## Assumptions

- The `TextLayerSuffix` matching rule (layer name ends with `TEX`, case-insensitive) remains unchanged.
- The `BoundsChecker.IsInsideBounds` utility correctly identifies whether an insertion point is within border extents and does not need modification.
- The `FloorPlanTextHeight` and `TextHeightTolerance` config fields are kept in `AppConfig` to avoid breaking the config schema, but are no longer read by the floor plan name search.
- Tie-breaking when two texts share the maximum height is implementation-defined (first encountered in model-space iteration order).
- Texts with height zero or less are degenerate and must be excluded to avoid selecting invisible or invalid text.
