# Feature Specification: Black Text in Color PNG Export

**Feature Branch**: `003-black-text-png`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Modified Text color in PNG images — Currently, two types of PNG images are exported: B/W and Color. When the color image is exported, make sure to change all the texts are in black. For this, all texts in DWG files should be detected automatically, then changed and exported as the black color."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Color PNG Export Has Black Text (Priority: P1)

A user exports a DWG file to PNG. Two variants are produced: a B/W PNG and a Color PNG. In the
Color PNG, the drawing geometry (lines, hatches, fills) retains its original colors, but all
text entities appear in black — regardless of what color they were set to in the DWG. This
ensures text is always legible against the white background of the color export.

**Why this priority**: This is the core requirement. Without it, text in the color PNG may be
yellow, red, or another light color that renders invisibly or with poor contrast on a white
background.

**Independent Test**: Run the color PNG export against a DWG file that contains text entities
colored in red, yellow, or any non-black color. Open the exported color PNG and visually confirm
that all text appears black while non-text elements retain their original colors.

**Acceptance Scenarios**:

1. **Given** a DWG file with text entities in red, **When** the color PNG is exported, **Then** all text in the PNG appears black and non-text geometry retains its red color.
2. **Given** a DWG file with text entities already in black, **When** the color PNG is exported, **Then** all text remains black and the export completes without error.
3. **Given** a DWG file with both DBText and MText entities in various colors, **When** the color PNG is exported, **Then** all text types appear black in the output PNG.

---

### User Story 2 - B/W PNG Export Is Unchanged (Priority: P2)

A user exports a DWG file to PNG. The B/W PNG export continues to work exactly as it did before
this feature — producing a black-and-white raster where all elements, including text, are
rendered in black or grayscale via the existing B/W plot style. The new text-override behavior
applies only to the color variant.

**Why this priority**: Regression prevention — the B/W export must not be affected by the
text-color override introduced for the color variant.

**Independent Test**: Run both color and B/W PNG exports against the same DWG. Confirm B/W PNG
output is identical to pre-feature behavior, while only the color PNG has changed text rendering.

**Acceptance Scenarios**:

1. **Given** any DWG file, **When** the B/W PNG is exported, **Then** the output is identical to what it was before this feature was introduced.
2. **Given** a DWG with colored text, **When** both PNG variants are exported, **Then** only the color PNG has the text-to-black override applied; the B/W PNG is unaffected.

---

### User Story 3 - Original DWG Colors Are Preserved After Export (Priority: P3)

After a PNG export completes (both variants), all text entities in the DWG retain their original
colors. The export process must not permanently modify the DWG — the text-color override is
temporary and only affects the rasterized output.

**Why this priority**: Data integrity — users must be able to continue editing the DWG after
export without discovering that all their text colors have been changed to black.

**Independent Test**: Open a DWG with red text, run the color PNG export, then inspect the text
entity color in the DWG. Confirm the color is still red (unchanged).

**Acceptance Scenarios**:

1. **Given** a DWG with text in various colors, **When** the color PNG export completes, **Then** all text entities in the DWG still have their original colors.
2. **Given** the export process is interrupted mid-run, **When** the DWG is inspected afterward, **Then** text colors have been restored (no partial override left in the file).

---

### Edge Cases

- What happens when a DWG has no text entities? (Export completes normally — no text override needed.)
- What if a text entity's color is set to "ByLayer" or "ByBlock"? The effective rendered color may not be the entity's own property — the system must override the final rendered color to black.
- What if the DWG contains dimension objects (which embed text)? Dimension annotation text (measurements, leaders) is also forced black, consistent with standalone text entities — all readable annotations must be legible in the color PNG.
- What happens if a text entity cannot be temporarily modified (e.g., locked layer, read-only xref)? The export should log a warning and continue rather than failing entirely.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST automatically detect all text-bearing entities — including standalone text (DBText and MText) and dimension objects (linear, angular, radial dimensions, leaders, and ordinate dimensions) — within the DWG during the color PNG export process.
- **FR-002**: During the color PNG export only, the system MUST temporarily override each detected text entity's color to black before rasterizing.
- **FR-003**: After the color PNG export completes, the system MUST restore each text entity's original color, regardless of whether the export succeeded or failed.
- **FR-004**: The text-color override MUST NOT be applied during the B/W PNG export.
- **FR-005**: The text-color override MUST NOT permanently modify the DWG file on disk.
- **FR-006**: If a text entity cannot be overridden (e.g., it is on a locked layer or is in a read-only xref), the system MUST log a warning identifying the entity and continue the export rather than aborting.
- **FR-007**: The color PNG export MUST produce an image where non-text geometry retains its original colors, and all eligible text entities appear in black.
- **FR-008**: The system MUST override the color of dimension objects (including their embedded measurement text) to black during color PNG export, in the same manner as standalone text entities.

### Key Entities

- **Text-Bearing Entity**: Any DWG element that displays readable text: standalone text (DBText or MText) or a dimension object (linear, angular, radial, leader, ordinate). Has an assigned color that may be explicit, ByLayer, or ByBlock. The color is temporarily overridden to black for color PNG export.
- **Color PNG**: The color variant of the rasterized export where drawing geometry retains original colors but all text appears in black.
- **B/W PNG**: The existing black-and-white rasterized export — behavior is unchanged by this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every color PNG exported from a DWG with non-black text entities contains those text entities rendered in black.
- **SC-002**: Every B/W PNG exported produces output that is identical to pre-feature behavior — no difference in rendering.
- **SC-003**: After any PNG export (color or B/W), the DWG text entity colors match their pre-export values — no permanent modification occurs.
- **SC-004**: No regressions — all existing automated tests continue to pass after this change is implemented.

## Clarifications

### Session 2026-02-27

- Q: Should dimension objects (which contain embedded measurement text) also have their text color forced black in the color PNG export, or only standalone DBText and MText entities? → A: All text-bearing entities including dimensions — DBText, MText, and all dimension types — are forced to black. All readable annotations must be legible.

## Assumptions

- Both PNG export variants (color and B/W) are implemented as part of this feature (there is currently no PNG export command; this feature creates it, modeled after the existing PDF export).
- "Black" means the standard drawing black color (AutoCAD color index 0 / ByBlock black, or equivalent — the darkest color that renders clearly on a white background in the exported PNG).
- The text-color override is applied at the entity level in memory (not by modifying the CTB/STB plot style), to allow non-text elements to retain their original colors.
- Text entities on locked layers are skipped with a warning rather than causing the export to fail.
- The color PNG export uses a "color" plot style (equivalent to `acad.ctb`) and the B/W PNG uses a monochrome plot style (equivalent to `monochrome.ctb`).
- PNG rasterization uses the same plot engine as the existing PDF export, configured with a PNG output driver.
