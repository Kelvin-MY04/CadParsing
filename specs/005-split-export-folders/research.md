# Research: Split Export Folders for Color-PDF and BW-PDF

**Feature**: 005-split-export-folders
**Date**: 2026-02-27

## Resolved Decisions

### Decision 1: Placement of Path-Building Logic

**Decision**: New `ExportPathBuilder` class placed in `CadParsing.Core/Helpers/`.

**Rationale**: `CadParsing.Tests` references only `CadParsing.Core` — it has no access to AutoCAD SDK types. To satisfy Principle VI (TDD), any unit-testable logic must reside in `CadParsing.Core`. Path construction is pure `System.IO` with no AutoCAD types, making it safe and appropriate for Core.

**Alternatives Considered**:
- Keep path logic inline in `ExportPdfCommand`: rejected — `ExportPdfCommand` is in the `CadParsing` project (AutoCAD SDK), which is unreachable by `CadParsing.Tests`. Violates Principle VI.
- Add to `CadParsing/Constants.cs`: rejected — wrong project; also `Constants.cs` is internal and mixes path derivation with config access.

---

### Decision 2: Subfolder Name Constants

**Decision**: Constants `ColorPdfFolderName = "Color-PDF"` and `BwPdfFolderName = "BW-PDF"` declared as `public const string` on `ExportPathBuilder`.

**Rationale**: Principle IV (DRY) and Code Quality Standards (no magic values) require named constants. Declaring them on the owning class keeps them co-located with the logic that uses them. `ExportPdfCommand` can reference them directly via `ExportPathBuilder.ColorPdfFolderName`.

**Alternatives Considered**:
- Separate `CadParsing.Core/Constants.cs` file: acceptable but adds a file for two constants; co-location is simpler for a focused helper.
- Make configurable via `AppConfig`: rejected — spec and clarifications explicitly state names are fixed and non-configurable.

---

### Decision 3: Folder Creation Strategy

**Decision**: `ExportPathBuilder.CreateTypeSubFolders(drawingSubDirectory)` called unconditionally in `ExportPdf()` immediately after `Directory.CreateDirectory(drawingSubDirectory)`, before the border export loop begins.

**Rationale**: `Directory.CreateDirectory` is idempotent — it succeeds silently when the directory already exists. Creating both subfolders up front is simpler, aligns with FR-001, and matches the clarification answer (always create, even if no PDFs will be written). Removes the need for `Directory.CreateDirectory(drawingSubDirectory)` inside the border loop.

**Alternatives Considered**:
- Create each subfolder immediately before writing its first PDF: adds per-PDF branching; spec requires unconditional creation; rejected.
- Keep folder creation inside `ExportAllBorders` loop: subfolder creation belongs logically to setup, not per-border iteration; rejected.

---

### Decision 4: Removal of `_color`/`_bw` Suffix Constants

**Decision**: `ColorStyleSuffix`, `MonochromeStyleSuffix`, and `StyleSuffixes` array removed from `ExportPdfCommand`. PDF filename becomes `borderLabel + ".pdf"` with no suffix.

**Rationale**: With type encoded in the subfolder name, suffixes are redundant (FR-004). Retaining them as empty strings leaves dead infrastructure. Removing them satisfies Principle IV (DRY) and the Code Quality Standard prohibiting dead code.

**Alternatives Considered**:
- Replace suffix constants with empty strings: technically produces correct filenames but leaves a `StyleSuffixes` array serving no purpose; rejected.

---

### Decision 5: `StyleSubFolders` Array to Map Style Index to Subfolder Name

**Decision**: Add `StyleSubFolders` array to `ExportPdfCommand`:
```csharp
private static readonly string[] StyleSubFolders =
{
    ExportPathBuilder.ColorPdfFolderName,
    ExportPathBuilder.BwPdfFolderName
};
```

**Rationale**: Preserves the existing `for (int styleIndex = 0; styleIndex < StyleSheets.Length; styleIndex++)` loop without restructuring. Only the path-derivation expression changes: the subfolder replaces the suffix in the `Path.Combine` call.

**Alternatives Considered**:
- Replace loop with two explicit export calls (color then B/W): removes parallelism; more duplication; violates Principle IV; rejected.

---

### Decision 6: `ExportPathBuilder.BuildPdfPath` Signature

**Decision**: Single method `BuildPdfPath(string drawingSubDirectory, string typeSubFolderName, string sanitizedBorderLabel) → string`.

**Rationale**: One general method avoids nearly-identical `BuildColorPdfPath` / `BuildBwPdfPath` duplicates (Principle IV). `ExportBorderWithAllStyles` passes `StyleSubFolders[styleIndex]` as the `typeSubFolderName` argument, keeping the loop structure intact. Unit tests cover both color and B/W paths by passing the respective constant.

**Alternatives Considered**:
- Two separate named methods (`BuildColorPdfPath`, `BuildBwPdfPath`): more readable at call site but duplicates implementation; rejected for a single-line method body.
