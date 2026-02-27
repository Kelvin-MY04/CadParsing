# Feature Specification: Fix DWG Export Bugs — Border Detection & Floor Plan Naming

**Feature Branch**: `001-fix-dwg-export-bugs`
**Created**: 2026-02-26
**Status**: Draft
**Input**: User description: Fix Bugs — DWG-to-PDF CAD parsing program border detection and floor plan name export

## User Scenarios & Testing *(mandatory)*

### User Story 1 — All DWG Files Export Successfully (Priority: P1)

A user runs the export command on a folder of DWG files. Currently, some DWG files produce
no PDF output at all because their border layers are not detected. After the fix, every DWG
file that contains a layer whose name ends with `PAPER-EX` must be detected and exported as
one or more PDF files — regardless of Xref prefixes, casing, or how the layer name is composed.

**Why this priority**: No export at all is the most critical failure. Without detecting the
border, no PDF is produced and the file is silently skipped, causing data loss.

**Independent Test**: Open a DWG file known to have a `*PAPER-EX` layer that currently
produces no PDF. Run the export command. The file MUST produce at least one PDF output.

**Acceptance Scenarios**:

1. **Given** a DWG file whose border layer name ends with `PAPER-EX` (e.g., `A-PAPER-EX`,
   `XREF|FLOOR-PAPER-EX`, `paper-ex`), **When** the export command is run, **Then** the
   system MUST detect the border and export at least one PDF for that file.

2. **Given** a DWG file where the border layer name contains `PAPER-EX` in any casing (e.g.,
   `A-Paper-Ex`, `A-PAPER-ex`), **When** the export command is run, **Then** the border
   MUST be detected as valid.

3. **Given** a DWG file where the border layer comes from an external reference (Xref) and
   the layer name appears as `XrefName|LayerName-PAPER-EX`, **When** the export command is
   run, **Then** the border MUST still be detected and exported.

4. **Given** a DWG file with no layer ending in `PAPER-EX`, **When** the export command is
   run, **Then** the system MUST log a clear message that no border was found and produce no
   output for that file (graceful skip, not a crash).

---

### User Story 2 — All Exported PDFs Are Named With Floor Plan Names (Priority: P2)

A user opens the exported PDF folder. Currently, some PDFs are named with Korean floor plan
names (correct) while others are named with numbers like `1.pdf`, `2.pdf` (incorrect). After
the fix, every exported PDF must be named using the floor plan name found in the `TEX` layer
within that border's boundary. If a floor plan name cannot be found, the user must be clearly
informed — but the system must never silently fall back to a numeric name.

**Why this priority**: Incorrect file naming makes the exports unusable for project
documentation. However, the files are at least produced (unlike Bug 1 where no file is
produced at all).

**Independent Test**: Run the export on a batch of DWG files. Inspect the output folder.
Every PDF MUST have a name derived from its floor plan's text label. Zero PDFs may have a
purely numeric name.

**Acceptance Scenarios**:

1. **Given** a border on a `*PAPER-EX` layer that contains text on a `*TEX` layer within
   its boundary, **When** the export runs, **Then** the exported PDF MUST be named after
   that text content (Korean characters included), not a number.

2. **Given** a floor plan name that contains characters invalid for file names (e.g., `/`,
   `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`), **When** the export runs, **Then** the system
   MUST sanitize those characters and still use the floor plan name as the basis for the
   file name.

3. **Given** multiple text entities on `*TEX` layer within a single border, **When** the
   export runs, **Then** the system MUST select the text entity whose height is exactly 400
   as the floor plan name.

---

### Edge Cases

- Open polylines or non-polyline shapes on `*PAPER-EX` layers are not valid borders and
  are silently ignored. Only closed polylines qualify as border entities.
- What happens when the TEX-layer text encoding uses non-standard character sets beyond
  Korean (e.g., other Unicode ranges)?
- A `*TEX` layer text entity that is partially inside and partially outside the border
  boundary is included if its insertion point falls inside the border boundary, and excluded
  otherwise.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST detect border entities on all layers whose names end with
  `PAPER-EX`, regardless of prefix, Xref source, or casing differences.
- **FR-002**: The border detection MUST use a case-insensitive matching strategy so that
  `PAPER-EX`, `paper-ex`, `Paper-Ex`, and variants are all treated as valid border layers.
- **FR-003**: The border detection MUST handle Xref-prefixed layer names (e.g.,
  `XrefFileName|LayerName-PAPER-EX`) and still recognize layers ending with `PAPER-EX`.
- **FR-004**: The floor plan name extraction MUST identify text entities on `*TEX` layers
  within a border boundary whose height is exactly 400 units. Membership within the boundary
  MUST be determined by the text entity's insertion point, not its bounding box. The
  detection logic MUST reliably locate this text for every border in the DWG file.
- **FR-005**: Every exported PDF file MUST be named using the floor plan name discovered in
  the `TEX` layer of the corresponding border boundary.
- **FR-006**: The floor plan name detection MUST always succeed for every border, as every
  border in a valid DWG file is guaranteed to contain a TEX-layer label. If detection
  unexpectedly returns no result, the system MUST log an error identifying the DWG file and
  border position and MUST NOT fall back to a numeric file name.
- **FR-007**: The system MUST sanitize floor plan names to produce valid file names, replacing
  or removing characters that are illegal in file names on the target operating system.
- **FR-008**: The system MUST continue processing remaining borders and DWG files even when
  an unexpected detection failure occurs on a single border.
- **FR-009**: The system MUST log the result of border detection for each DWG file processed,
  indicating how many borders were found and which were successfully exported.

### Key Entities

- **Border**: A closed polyline entity residing on a layer whose name ends with `PAPER-EX`.
  Represents the printable boundary of a single floor plan sheet.
- **Floor Plan Name**: A text entity on a layer ending with `TEX`, spatially contained within
  a Border's boundary. Represents the human-readable identifier of the floor plan.
- **PDF Export**: The output artifact for a single Border: one color PDF and one black-and-white
  PDF. The base file name is derived from the Floor Plan Name.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of DWG files that contain at least one layer ending with `PAPER-EX` produce
  at least one PDF export. Zero DWG files are silently skipped due to undetected borders.
- **SC-002**: 100% of exported PDF files are named using a floor plan name derived from the
  `TEX` layer. Zero exported PDFs use a purely numeric file name.
- **SC-003**: After the fix, no exported PDF uses a numeric file name. Every border's floor
  plan name is successfully detected and used as the PDF file name, since floor plan names
  are guaranteed to exist in all valid DWG files.
- **SC-004**: All DWG files in a batch that previously failed to export now produce correct
  PDF output after the fix, with no regressions in files that previously exported correctly.
- **SC-005**: The export completes without crashing when encountering Xref-prefixed layers,
  non-standard text heights, or Korean/Unicode floor plan names.

## Clarifications

### Session 2026-02-26

- Q: When two borders within the same DWG file produce the same floor plan name after sanitization, how should the system handle the collision? → A: Floor plan names within a single DWG file are always unique by design. Each border has a distinct TEX-layer label. No collision handling or suffix logic is required.
- Q: When a border has no floor plan name found in the TEX layer, should the system skip the border, use a placeholder name, or stop the export? → A: This scenario does not occur in practice. Every border in every valid DWG file is guaranteed to have a floor plan name in the TEX layer. The current numeric-name failures are caused by a detection bug, not by missing data.
- Q: When multiple TEX-layer text entities exist within a border boundary, which should be selected as the floor plan name? → A: The text entity with height exactly 400 units. The height criterion must be kept; the bug lies in the spatial detection logic, not in the height filter value.
- Q: When a TEX-layer text entity is partially inside and partially outside the border boundary, should it be included? → A: Include it if its insertion point falls inside the border boundary; exclude it otherwise.
- Q: Should open polylines or non-polyline shapes on `*PAPER-EX` layers be treated as valid borders? → A: No. Only closed polylines are valid borders. Open or non-polyline shapes on `*PAPER-EX` layers are silently ignored.

## Assumptions

- DWG files processed by this system always reside in the configured download root directory.
- The `*PAPER-EX` border layers contain only closed polylines (LWPOLYLINE or POLYLINE
  entity types) representing floor plan boundaries.
- Every border in a valid DWG file is guaranteed to contain a floor plan name text entity
  on a `*TEX` layer with height exactly 400 units. Failures to find this text are always
  due to detection bugs, not missing data.
- Floor plan names within a single DWG file are always unique; no two borders share the
  same TEX-layer label, so no duplicate file name collision handling is needed.
- Korean Unicode characters in floor plan names are valid for use in PDF file names after
  sanitizing OS-illegal characters.
- The existing plot configuration (A3, scale-to-fit, color + B/W) is correct and does not
  need to change as part of this fix.
