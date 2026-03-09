# Feature Specification: Fix PDF Export Failure on Locked Layers

**Feature Branch**: `007-fix-locked-layer`
**Created**: 2026-02-28
**Status**: Draft
**Input**: User description: PDF export fails with eOnLockedLayer / eInvalidInput when the DWG contains text entities on locked layers.

## Clarifications

### Session 2026-02-28

- Q: Should text entities on locked layers be skipped (appear as-is in PDF) or should the layer be temporarily unlocked to apply the override? → A: Temporarily unlock the layer, apply the override, then re-lock — all text entities must receive the override regardless of layer lock status.
- Q: Should the diagnostic log message for unlocking be emitted once per entity or once per layer? → A: Once per layer unlocked, regardless of how many entities reside on it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export PDF from DWG with Locked Layers (Priority: P1)

A user runs the EXPORTPDF command on a DWG file that contains text entities on locked layers.
Currently the export fails entirely — no PDF is produced.
After this fix, the export should complete successfully, producing valid PDF files for all
detected borders, with ALL text entities receiving the standard colour and font override
regardless of whether their layer is locked.

**Why this priority**: This is a blocking bug. The primary function of the tool is broken
whenever a DWG file includes locked layers, which is a normal and common authoring pattern
in AutoCAD drawings.

**Independent Test**: Run EXPORTPDF on a DWG file that has at least one text entity on a
locked layer. The command must complete without an `[ERROR] PDF export failed` message,
must produce the expected PDF files on disk, and the locked-layer text entities must appear
with the standard overridden style (black, standard font) in the output.

**Acceptance Scenarios**:

1. **Given** a DWG file where some text entities reside on locked layers, **When** the user runs EXPORTPDF, **Then** the export completes without a fatal error, all expected PDF files are created on disk, and ALL text entities (including those on locked layers) appear with the overridden style in the output.
2. **Given** a DWG file where ALL text entities reside on locked layers, **When** the user runs EXPORTPDF, **Then** the export completes, all expected PDF files are created on disk, and all text entities appear with the overridden style — layer lock state is restored to its original value after export.
3. **Given** a DWG file with no locked layers, **When** the user runs EXPORTPDF, **Then** the existing behaviour is completely unchanged — overrides are applied and PDFs are exported exactly as before.

---

### Edge Cases

- What happens when every text entity is on a locked layer? All layers are temporarily unlocked for the override; export proceeds and all text is overridden; all layers are re-locked after export.
- What happens when a mix of locked and unlocked text entities exist? All entities receive the override; locked layers are temporarily unlocked and then re-locked.
- What happens during the restore phase? Both the text override (colour/font) AND the layer lock state must be restored before the transaction commits.
- What happens if unlocking a layer fails? The entity is skipped with a warning and the export continues — the override is best-effort for layers that cannot be unlocked.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST apply the color-to-black override to ALL text entities, including those on locked layers, by temporarily unlocking the layer without permanently altering the drawing's layer lock state.
- **FR-002**: The system MUST apply the font override to ALL text entities, including those on locked layers, by temporarily unlocking the layer without permanently altering the drawing's layer lock state.
- **FR-003**: The system MUST log exactly one diagnostic message per layer that is temporarily unlocked (not per entity), naming the layer, to preserve visibility of non-standard conditions without flooding the output.
- **FR-004**: After this fix, the PDF export MUST complete successfully and produce output files for any DWG that contains text entities on locked layers.
- **FR-005**: Text entities on unlocked layers MUST continue to receive color and font overrides exactly as before.
- **FR-006**: The system MUST restore each layer's locked state to its original value after the override is applied and the export is complete.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: PDF export produces output files for 100% of tested DWG files that contain locked-layer text entities (previously 0% succeeded).
- **SC-002**: No `[ERROR] PDF export failed` message appears in the output console when locked layers are present in the drawing.
- **SC-003**: For DWG files without locked layers, export behaviour is identical to before this fix — no regression in colour or font override.
- **SC-004**: In the exported PDF, text entities that reside on locked layers appear with the standard overridden style (black colour, standard font) — identical to text on unlocked layers.
- **SC-005**: The DWG's layer lock state is identical before and after running EXPORTPDF — no permanent modification to locked layers.

## Assumptions

- Locked layers are a valid and common AutoCAD authoring pattern; drawings containing them must be fully supported.
- Temporarily unlocking a layer during the export transaction is acceptable provided the layer's locked state is fully restored before the transaction is committed.
- The existing warn-and-continue logging strategy is sufficient for the rare case where a layer cannot be unlocked; no user-facing dialog or hard error is needed for such entities.
