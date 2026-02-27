# Feature Specification: Black Text in Color PDF Export

**Feature Branch**: `004-black-text-pdf`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Modified Text color in exported color-PDF — Currently, two types of PDF files are exported: B/W and Color. When the color-PDF is exported, make sure to change all the texts are in black. For this, all texts in DWG files should be detected automatically, then changed and exported as the black color."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Color PDF Export Has Black Text (Priority: P1)

A user runs the existing PDF export command on a DWG file. Two PDF files are produced per
detected border: a B/W PDF and a Color PDF. In the Color PDF, the drawing geometry (lines,
hatches, fills) retains its original colors, but all text entities appear in black — regardless
of what color they were assigned in the DWG. This ensures text is always legible against the
white background of the color PDF.

**Why this priority**: This is the core requirement. Without it, text in the color PDF may be
yellow, red, or another light color that renders with poor contrast on a white background,
making the drawing difficult to read or print.

**Independent Test**: Run the PDF export against a DWG file containing text entities colored
in red, yellow, or any non-black color. Open the exported color PDF and confirm all text appears
black while non-text elements (lines, hatches, fills) retain their original colors.

**Acceptance Scenarios**:

1. **Given** a DWG file with text entities in red, **When** the PDF export runs, **Then** the color PDF shows all text in black and non-text geometry retains its original red color.
2. **Given** a DWG file with text entities already in black, **When** the PDF export runs, **Then** all text remains black and both PDFs export without error.
3. **Given** a DWG with both standalone text and dimension annotation text in various colors, **When** the color PDF is exported, **Then** all text types (standalone and dimension) appear black in the color PDF.

---

### User Story 2 - B/W PDF Export Is Unchanged (Priority: P2)

A user runs the PDF export. The B/W PDF continues to render all elements — including text —
in black or grayscale via the existing monochrome plot style, exactly as before. The new
text-override behavior applies only to the color PDF variant.

**Why this priority**: Regression prevention — the B/W export must not be affected by
the text-color override introduced for the color variant. Both export variants are always
produced together in a single command run.

**Independent Test**: Export both PDF variants from the same DWG. Confirm the B/W PDF is
visually identical to what it produced before this feature, while only the color PDF has
changed text rendering.

**Acceptance Scenarios**:

1. **Given** any DWG file, **When** the PDF export runs, **Then** the B/W PDF output is identical to pre-feature behavior.
2. **Given** a DWG with colored text, **When** both PDF variants are exported, **Then** only the color PDF applies the text-to-black override; the B/W PDF is unaffected.

---

### User Story 3 - Original DWG Colors Are Preserved After Export (Priority: P3)

After the PDF export completes (both color and B/W variants), all text entities in the DWG
retain their original colors. The export must not permanently alter the DWG — the text-color
override is temporary and affects only the rendered PDF output.

**Why this priority**: Data integrity — users must be able to continue editing the DWG after
export without finding that all text colors have been permanently changed to black.

**Independent Test**: Open a DWG with red text, run the PDF export, then inspect the text
entity colors in the DWG. Confirm all text is still red (original color preserved).

**Acceptance Scenarios**:

1. **Given** a DWG with text in various colors, **When** the color PDF export completes, **Then** all text entities in the DWG still have their original pre-export colors.
2. **Given** the export process encounters an error mid-run, **When** the DWG is inspected afterward, **Then** all text colors have been restored — no partial override persists in the drawing.

---

### Edge Cases

- What happens when a DWG has no text entities? The export completes normally — no text override is needed or applied.
- What if a text entity's color is set to "ByLayer" or "ByBlock"? The effective rendered color depends on the layer or block definition. The system must override the entity's color to explicit black before plotting and restore the original color setting (ByLayer, ByBlock, or explicit index) afterward.
- What if a text entity is on a locked layer or inside a read-only external reference (xref)? The entity cannot be temporarily modified. The system must log a warning identifying the entity and continue the export rather than aborting; that entity will render in its original color in the output.
- What about text inside block inserts (regular blocks and xrefs)? Text-bearing entities nested inside any block definition — whether a locally defined block or an externally referenced (xref) block — are included in the detection and override scope. Entities that cannot be modified (e.g., those in a read-only xref) are skipped with a warning, as with any other unmodifiable entity.
- What happens when all detected borders are exported in a single transaction? Text-color overrides are applied before each border's color PDF plot and restored immediately after — ensuring no border bleeds its override into another border's export.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST automatically detect all text-bearing entities within the DWG — including standalone text (DBText and MText) and dimension objects (linear, angular, radial dimensions, leaders, and ordinate dimensions) in model space, as well as text-bearing entities nested inside block inserts (regular blocks and external reference blocks/xrefs) — before producing the color PDF.
- **FR-002**: During the color PDF export only, the system MUST temporarily override each detected text-bearing entity's color to black before the plot operation begins.
- **FR-003**: After the color PDF plot completes for each border, the system MUST restore every overridden text-bearing entity to its original color setting, regardless of whether the plot succeeded or failed.
- **FR-004**: The text-color override MUST NOT be applied during the B/W PDF export.
- **FR-005**: The text-color override MUST NOT cause any permanent modification to the DWG file on disk.
- **FR-006**: If a text-bearing entity cannot be temporarily overridden (e.g., locked layer, read-only xref), the system MUST log a warning identifying the entity and continue the export rather than aborting.
- **FR-007**: The color PDF MUST render non-text geometry in its original colors, and all eligible text-bearing entities in black.
- **FR-008**: The B/W PDF MUST be produced without any change to the existing plot workflow for that variant.

### Key Entities

- **Text-Bearing Entity**: Any DWG element that displays readable text — standalone text (DBText or MText) or a dimension object (linear, angular, radial, leader, ordinate) — whether located directly in model space or nested inside a block insert (regular block or external reference/xref). Has an assigned color that may be explicit, ByLayer, or ByBlock. The color is temporarily overridden to black for the color PDF export only.
- **Color PDF**: The color variant of the exported PDF (produced with the full-color plot style). After this feature, all text in this output appears black; non-text geometry retains original colors.
- **B/W PDF**: The existing monochrome variant — behavior is completely unchanged by this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every color PDF exported from a DWG with non-black text entities renders those text entities in black.
- **SC-002**: Every B/W PDF exported produces output identical to pre-feature behavior — no rendering change in the B/W variant.
- **SC-003**: After any PDF export run, the DWG text entity color settings match their pre-export values — no permanent modification occurs.
- **SC-004**: No regressions — all existing automated tests continue to pass after this change is implemented.

## Clarifications

### Session 2026-02-27

- Q: Should text inside block inserts (regular blocks and xref blocks) also be forced black in the color PDF, or only text entities appearing directly in model space? → A: Include text inside both regular block inserts and external xref blocks (full recursive detection across all block definitions).

## Assumptions

- The existing `EXPORTPDF` command is modified in place — no new command is added. Both color and B/W PDFs continue to be produced in a single command run.
- "Black" means the standard drawing black color (the same color that renders clearly as black on a white-background PDF, equivalent to AutoCAD color index 7 as seen in a white-background context, or explicit RGB black).
- Dimension objects are included in the text-override scope, consistent with the design decision made for the related PNG export feature (feature branch `003-black-text-png`).
- Text entities on locked layers or in read-only xrefs are skipped with a warning log entry; they appear in their original color in the exported PDF.
- Text-bearing entities nested inside block inserts (both regular and xref blocks) are included in the detection and override scope.
- The text-color override and restoration are performed within the same transaction scope (or equivalent in-memory state) to guarantee atomicity — if restoration fails, the transaction is not committed to disk.
- The override is applied per-border immediately before that border's color PDF plot, and restored immediately after, so that multiple borders in a single DWG are handled correctly and independently.
