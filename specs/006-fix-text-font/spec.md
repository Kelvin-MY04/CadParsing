# Feature Specification: Fix Unknown Text Characters by Standardizing Font

**Feature Branch**: `006-fix-text-font`
**Created**: 2026-02-28
**Status**: Draft
**Input**: User description: "Change all text font to `Standard`. There are some unknown text content with `????`. To solve this issue change all text font in `TEXT` and `TEX` layer to `Standard`."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export PDF Without Garbled Text (Priority: P1)

A user runs the DWG-to-PDF export on a drawing that contains text entities whose font is not available on the machine. Instead of seeing `????` (garbled placeholder characters) in the exported PDF, all text in layers matching the `TEXT` and `TEX` suffixes renders as legible characters.

**Why this priority**: This is the core problem — unreadable text in exported PDFs makes the output useless. Fixing it is the primary deliverable of this feature.

**Independent Test**: Open a DWG file known to produce `????` in exported PDFs, run the export command, and verify the resulting PDF shows readable text instead of `????`.

**Acceptance Scenarios**:

1. **Given** a DWG file with text entities in a layer ending in `TEX` using an unavailable font, **When** the export command is run, **Then** the exported PDF shows readable text characters with no `????` placeholders.
2. **Given** a DWG file with text entities in a layer ending in `TEXT` using an unavailable font, **When** the export command is run, **Then** the exported PDF shows readable text characters with no `????` placeholders.
3. **Given** a DWG file where some text already uses the `Standard` font, **When** the export command is run, **Then** those entities are unaffected and still render correctly.

---

### User Story 2 - Original DWG File Remains Unmodified (Priority: P2)

A user exports a DWG file to PDF. After the export completes, the original DWG file on disk is unchanged — the font standardization is applied only during the export session and is not persisted back to the source file.

**Why this priority**: Permanently altering source drawings would be a destructive side effect that could corrupt a user's design files.

**Independent Test**: Check the DWG file's last-modified timestamp and content before and after running the export; both must be identical.

**Acceptance Scenarios**:

1. **Given** a DWG file with text using a non-standard font, **When** the export command completes, **Then** the source DWG file is byte-for-byte identical to what it was before the export.

---

### Edge Cases

- What happens when a DWG file contains no layers ending in `TEXT` or `TEX`? The export proceeds normally; no font changes are applied.
- What happens when a layer matching `TEXT` or `TEX` contains no text entities? The export proceeds normally; no changes needed.
- How does the system handle MTEXT (multi-line text) entities in addition to single-line TEXT entities? Both types must have their font standardized.
- What happens if the `Standard` text style itself is not defined in the DWG? The system must ensure the style exists or is created before applying it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The export process MUST change the text style to `Standard` for **all** text entities (single-line and multi-line) residing in layers whose name ends with `TEX` or `TEXT` (case-insensitive), regardless of whether their current font is missing or available, before generating the PDF output.
- **FR-002**: The font standardization MUST be applied within the active drawing session only; it MUST NOT be saved back to the source DWG file on disk.
- **FR-003**: The `Standard` text style MUST be available in the drawing during export; if it does not already exist, the system MUST create it before applying it to text entities.
- **FR-004**: The export output (PDF) MUST contain no `????` placeholder characters where readable text was present in the original drawing.
- **FR-005**: Text entities in layers that do NOT match any configured font-target suffix MUST NOT have their font altered.
- **FR-006**: The list of layer suffixes eligible for font standardization MUST be configurable (e.g., defaulting to `["TEX", "TEXT"]`), allowing new suffixes to be added without code changes.

### Assumptions

- The `Standard` text style uses the default AutoCAD font (`txt.shx`) and is capable of rendering all ASCII and common characters present in the target drawings.
- The layer suffix matching rules follow the same `EndsWith` case-insensitive logic already used in the codebase, applied against a configurable list of suffixes (defaulting to `["TEX", "TEXT"]`).
- Non-standard characters (CJK, special symbols) that truly cannot be represented by `Standard` are out of scope for this feature.
- The fix applies to both single-line `TEXT`/`DTEXT` entities and multi-line `MTEXT` entities.

## Clarifications

### Session 2026-02-28

- Q: Should the font change apply to all text entities on those layers, or only to entities with a missing/unavailable font? → A: Apply to all text entities on `TEX`/`TEXT` layers, regardless of current font.
- Q: Should `TEXT` be added as a second configurable suffix (extending config) or hardcoded alongside `TEX`? → A: Extend config to support multiple layer suffixes for font standardization (e.g., `TextLayerSuffixes: ["TEX", "TEXT"]`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of text entities in layers matching any configured font-target suffix use the `Standard` font during PDF export.
- **SC-002**: The exported PDF contains zero `????` placeholder characters in positions where the source DWG had readable text content.
- **SC-003**: The source DWG file is not modified after the export — its content and last-modified timestamp are unchanged.
- **SC-004**: The export process completes without errors or warnings related to missing fonts for text entities in `TEX` and `TEXT` layers.
