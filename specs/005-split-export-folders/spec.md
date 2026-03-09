# Feature Specification: Split Export Folders for Color-PDF and BW-PDF

**Feature Branch**: `005-split-export-folders`
**Created**: 2026-02-27
**Status**: Draft
**Input**: User description: "Split separate folders for exporting — Split the separate folders in the final directory level for B/W-PDF and Color-PDF for each floor plan when PDF files are exported."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organized PDF Output by Type (Priority: P1)

A user runs the PDF export command on a DWG file that contains two floor plans. Instead of finding all four PDFs (two colour, two B/W) mixed together in a single folder, the user now finds two clearly named subfolders inside the drawing's output folder: one containing all colour PDFs and one containing all B/W PDFs.

**Why this priority**: This is the entire scope of the feature. Separating output by type makes it immediately clear which files are which, eliminates the need for `_color`/`_bw` filename suffixes, and is a prerequisite for all other behaviour in this feature.

**Independent Test**: Can be tested end-to-end by running the export command on a single DWG file with one or more floor plans and verifying the output folder structure without any other changes.

**Acceptance Scenarios**:

1. **Given** a DWG file with one floor plan, **When** the PDF export command is run, **Then** the drawing's output folder contains exactly two subfolders (`Color-PDF` and `BW-PDF`), each containing one PDF file named after the floor plan.
2. **Given** a DWG file with multiple floor plans, **When** the PDF export command is run, **Then** each subfolder (`Color-PDF` and `BW-PDF`) contains one PDF per floor plan, with no PDFs placed directly in the drawing's output folder.
3. **Given** the export command is run twice on the same DWG file, **When** the second run completes, **Then** existing PDFs in both subfolders are overwritten without error.

---

### User Story 2 - File Naming Without Style Suffix (Priority: P2)

Because the PDF type is now conveyed by the containing subfolder name, the exported files carry only the floor plan name — with no `_color` or `_bw` suffix appended.

**Why this priority**: Removing the suffix is a natural consequence of the folder split and avoids redundant information in filenames. It is a dependent improvement that completes the organizational change, but the folder split itself is already valuable without it.

**Independent Test**: After a successful export, verify that each PDF filename inside `Color-PDF/` and `BW-PDF/` equals `<FloorPlanName>.pdf` with no suffix.

**Acceptance Scenarios**:

1. **Given** a floor plan named "Level 1", **When** exported, **Then** the colour output is at `Color-PDF/Level 1.pdf` and the B/W output is at `BW-PDF/Level 1.pdf`.
2. **Given** a floor plan name that previously produced `FloorPlan_A_color.pdf`, **When** exported with this feature active, **Then** the file appears as `Color-PDF/FloorPlan_A.pdf`.

---

### Edge Cases

- What happens when a floor plan name resolves to the same sanitized string for two different borders? Both PDFs land in the same subfolder with the same name — the second overwrites the first (consistent with current overwrite behaviour).
- What happens when the `Color-PDF` or `BW-PDF` subfolder already exists from a previous export? The folder is reused and files inside it are overwritten, not duplicated.
- What happens when a border's floor plan name cannot be resolved? That border is skipped (existing skip behaviour is unchanged), and neither subfolder entry is created for it.
- What happens when the drawing has no valid borders? Export aborts with an error before any folders are created (existing behaviour unchanged).
- What happens to old flat-format PDFs (`FloorPlanName_color.pdf`, `FloorPlanName_bw.pdf`) left in the drawing output folder from prior exports? They are left in place — the export command performs no cleanup of pre-existing files outside the two type subfolders.
- What happens when all borders are skipped (no floor plan names resolved)? The `Color-PDF` and `BW-PDF` subfolders are still created inside the drawing output folder; they simply contain no PDF files.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST create a `Color-PDF` subfolder and a `BW-PDF` subfolder inside the drawing's output folder whenever the drawing output folder itself is created — regardless of whether any PDFs are ultimately written into them.
- **FR-002**: The system MUST write each colour PDF into the `Color-PDF` subfolder.
- **FR-003**: The system MUST write each B/W PDF into the `BW-PDF` subfolder.
- **FR-004**: Each exported PDF filename MUST consist solely of the sanitized floor plan name followed by `.pdf`, with no style-type suffix (e.g., `FloorPlanName.pdf`, not `FloorPlanName_color.pdf`).
- **FR-005**: The system MUST create any missing intermediate directories (including `Color-PDF` and `BW-PDF`) before attempting to write a PDF file.
- **FR-006**: The overall output directory structure above the drawing-name level (i.e., `ExportRoot`, relative sub-paths) MUST remain unchanged.
- **FR-007**: Existing PDFs in `Color-PDF` or `BW-PDF` from a prior export run MUST be silently overwritten.

### Key Entities

- **Drawing Output Folder**: The folder named after the DWG file (without extension) inside the resolved export root. Contains the two type subfolders.
- **Color-PDF Subfolder**: A fixed-name child folder (`Color-PDF`) of the drawing output folder; holds all colour PDF exports for that drawing.
- **BW-PDF Subfolder**: A fixed-name child folder (`BW-PDF`) of the drawing output folder; holds all B/W PDF exports for that drawing.
- **Exported PDF File**: A single-page PDF named `<FloorPlanName>.pdf`, placed in the appropriate type subfolder.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful export of a DWG file with N floor plans, the drawing output folder contains exactly two subfolders (`Color-PDF` and `BW-PDF`), each containing exactly N PDF files — and zero PDF files at the drawing output folder level itself.
- **SC-002**: Each PDF filename inside either subfolder matches the floor plan name exactly (after sanitization) with no appended suffix, verifiable by listing the folder contents.
- **SC-003**: All PDF files that were previously produced by the export command continue to be produced correctly after this change — no floor plan is dropped, no file is corrupted.
- **SC-004**: Running the export command twice on the same drawing produces identical folder structure and file count on both runs, with no duplicate or orphaned files.

## Clarifications

### Session 2026-02-27

- Q: Should the export command clean up old flat-format PDFs (`*_color.pdf`, `*_bw.pdf`) that exist in the drawing output folder from prior export runs? → A: Leave old files alone — no cleanup action taken.
- Q: If all borders are skipped (no floor plan names resolved), should `Color-PDF` and `BW-PDF` subfolders still be created? → A: Always create both subfolders, even if no PDFs are written into them.

## Assumptions

- Subfolder names are fixed as `Color-PDF` and `BW-PDF` (matching the terminology used in the feature description). These names are not user-configurable.
- The `_color` and `_bw` filename suffixes are removed entirely; they are no longer needed once folder separation is in place.
- No changes are required to the DWG-file-to-output-root mapping logic (`DownloadRoot` → `ExportRoot` path rebase); only the final level of folder organisation changes.
- The feature applies to all exports produced by the EXPORTPDF command; there is no toggle or mode switch.
