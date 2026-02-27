# Implementation Plan: Black Text in Color PDF Export

**Branch**: `004-black-text-pdf` | **Date**: 2026-02-27 | **Spec**: `specs/004-black-text-pdf/spec.md`
**Input**: Feature specification from `/specs/004-black-text-pdf/spec.md`

## Summary

For each border's color PDF export, temporarily override all text-bearing entities (DBText,
MText, Dimension, Leader, MLeader — in model space and recursively in nested block definitions)
to explicit black (RGB 0, 0, 0), plot the color PDF, then restore every entity to its original
color. The B/W PDF export workflow and the DWG on disk are completely unaffected.

Two new helpers implement the override lifecycle. `ExportPdfCommand` is modified to thread the
open `Transaction` and `Database` into the per-border style loop so the override can be applied
and restored inline, with no committed changes to the database reaching disk.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8
**Primary Dependencies**: AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll),
NUnit 3.x (test project only)
**Storage**: N/A — DWG is read via AutoCAD SDK; PDF is written via AutoCAD plot engine; no
file, database, or cache storage is introduced by this feature
**Testing**: NUnit 3.x for any pure logic extractable to CadParsing.Core; manual
accoreconsole.exe run against real DWG files for integration and E2E validation
**Target Platform**: Windows — AutoCAD 2023 plugin (headless via accoreconsole.exe for batch
export)
**Project Type**: AutoCAD plugin command extension — modifies the existing `EXPORTPDF` command
in place; no new command is added
**Performance Goals**: No measurable increase in export time; text-entity traversal over all
block definitions is O(n) — single pass with visited-set deduplication
**Constraints**: AutoCAD entity types (DBText, MText, Dimension, etc.) are COM-backed objects
that require a live AutoCAD process — limits unit-testable surface to pure logic in
CadParsing.Core; entities on locked layers or in read-only xrefs cannot be opened ForWrite
**Scale/Scope**: Single-DWG context per export run; processes all entities in all block
definitions; no batch or multi-document scope

## Constitution Check

*Gate: Must pass before Phase 0 research. Re-evaluated after Phase 1 design.*

### Pre-Phase 0 Gate

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Meaningful Naming | PASS | New helpers: `TextEntityFinder`, `TextColorOverride` (noun phrases); methods: `FindAllTextEntities`, `ApplyBlackOverride`, `RestoreOriginalColors`, `IsTextBearingEntity`, `CollectTextEntitiesFromBlock`, `IsColorExport` — all verb+noun; no single-letter names; `BlackColor` constant is self-describing |
| II. Single Responsibility | PASS | `TextEntityFinder` only detects; `TextColorOverride` only overrides and restores; no method will exceed 20 lines; `ExportPdfCommand` change is a thin call-site integration |
| III. SOLID | PASS | New classes are static helpers consistent with existing codebase pattern; dependencies (`Transaction`, `Database`, `Editor`) injected as parameters; no hidden construction |
| IV. DRY | PASS | `BlackColor` constant declared once; `IsTextBearingEntity` is the single authority for type classification; reuses `DatabaseHelper.GetModelSpaceBlock` |
| V. Error Handling | PASS | ForWrite failures (locked layer, read-only xref) caught per entity: log warning via `editor.WriteMessage`, continue — satisfies FR-006 |
| VI. TDD | PARTIAL (justified — see Complexity Tracking) | AutoCAD entity types require live AutoCAD process; integration and E2E coverage via accoreconsole.exe against real DWG files |

### Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| TDD unit-test gap for AutoCAD-dependent code | `DBText`, `MText`, `Dimension`, and all related AutoCAD entity types are COM-backed and require a live AutoCAD session to instantiate; cannot be created in a standard NUnit test | Adding wrapper interfaces for every AutoCAD entity type used would require a parallel type hierarchy across the entire plugin — complexity far exceeding the benefit for a 2-class feature; the existing project has established the pattern of not mocking AutoCAD objects |

## Project Structure

### Documentation (this feature)

```text
specs/004-black-text-pdf/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
CadParsing/
├── Commands/
│   └── ExportPdfCommand.cs         (modified — add named constants, thread Transaction/Database
│                                    into ExportBorderWithAllStyles, add IsColorExport check,
│                                    apply/restore color override in style loop)
└── Helpers/
    ├── TextEntityFinder.cs         (NEW — recursively collects ObjectIds of all text-bearing
    │                                entities from model space and nested block definitions)
    ├── TextColorOverride.cs        (NEW — applies black override to collected entities,
    │                                restores originals; skips unwritable entities with warning)
    ├── TextHelper.cs               (unchanged)
    ├── BorderHelper.cs             (unchanged)
    ├── DatabaseHelper.cs           (unchanged)
    └── ExplodeHelper.cs            (unchanged)

CadParsing.Core/
└── (no changes — cannot hold AutoCAD-SDK-dependent logic)

CadParsing.Tests/
└── Unit/
    └── (no new unit tests — all new logic depends on live AutoCAD session)
```

**Structure Decision**: Single-project modification. No new projects. Two new helper classes
follow the established `CadParsing/Helpers/` static-class pattern. CadParsing.Core is
unchanged because all new logic requires AutoCAD SDK types.

## Phase 0: Research

See `specs/004-black-text-pdf/research.md` for all resolved decisions.

## Phase 1: Design

### Class: TextEntityFinder

**File**: `CadParsing/Helpers/TextEntityFinder.cs`
**Responsibility**: Collect ObjectIds of all text-bearing entities from model space and
all reachable nested block definitions.

```
FindAllTextEntities(Transaction, Database) → IReadOnlyList<ObjectId>
  └─ CollectTextEntitiesFromBlock(Transaction, BlockTableRecord, List<ObjectId>, HashSet<ObjectId>)
       ├─ IsTextBearingEntity(Entity) → bool  [DBText | MText | Dimension | Leader | MLeader]
       └─ [BlockReference] → recurse into its BlockTableRecord (visited-set guard)
```

**Error handling**: Each entity open is wrapped in try-catch; on failure the entity is skipped
(not added to the result list); a warning is written to the editor.

### Class: TextColorOverride

**File**: `CadParsing/Helpers/TextColorOverride.cs`
**Responsibility**: Apply explicit black (RGB 0,0,0) to each entity in the supplied list and
restore the saved original Color on demand.

```
ApplyBlackOverride(Transaction, IReadOnlyList<ObjectId>, Editor)
    → Dictionary<ObjectId, Color>     ← keyed by ObjectId; value = original Color
RestoreOriginalColors(Transaction, Dictionary<ObjectId, Color>, Editor)
    → void
```

**Named constant**: `private static readonly Color BlackColor = Color.FromRgb(0, 0, 0);`

**Error handling per entity**: Open ForWrite in try-catch; on exception log warning via
`editor.WriteMessage("[WARN] ...")` and skip (ApplyBlackOverride skips adding to savedColors;
RestoreOriginalColors skips restoring that entry).

### Modified: ExportPdfCommand

**Changes**:

1. Extract magic string literals to named constants:
   ```csharp
   private const string ColorStyleSheet = "acad.ctb";
   private const string MonochromeStyleSheet = "monochrome.ctb";
   private const string ColorStyleSuffix = "_color";
   private const string MonochromeStyleSuffix = "_bw";
   ```
   Update `StyleSheets` and `StyleSuffixes` arrays to reference these constants.

2. Add private method:
   ```csharp
   private static bool IsColorExport(string styleSheet)
   // returns true when styleSheet == ColorStyleSheet (OrdinalIgnoreCase)
   ```

3. Thread `Transaction` and `Database` into `ExportBorderWithAllStyles` (add parameters).
   Update the single call site in `ExportAllBorders`.

4. Inside the per-style loop in `ExportBorderWithAllStyles`:
   ```
   if IsColorExport(StyleSheets[styleIndex]):
       textEntityIds ← TextEntityFinder.FindAllTextEntities(transaction, database)
       savedColors   ← TextColorOverride.ApplyBlackOverride(transaction, textEntityIds, editor)
   try:
       ExportSinglePdf(...)
   finally:
       if IsColorExport and savedColors != null:
           TextColorOverride.RestoreOriginalColors(transaction, savedColors, editor)
   ```

### Data flow diagram

```
ExportAllBorders(transaction)
    └─ for each border:
         ExportBorderWithAllStyles(... transaction, database)
              ├─ styleIndex=0 [color]:
              │    TextEntityFinder.FindAllTextEntities      → textEntityIds
              │    TextColorOverride.ApplyBlackOverride      → savedColors
              │    ExportSinglePdf("acad.ctb")               [black text in color PDF]
              │    TextColorOverride.RestoreOriginalColors   ← savedColors
              └─ styleIndex=1 [B/W]:
                   ExportSinglePdf("monochrome.ctb")         [unchanged]
transaction.Commit()   ← all entity colors restored to original at commit time
```

### Quickstart

See `specs/004-black-text-pdf/quickstart.md` for manual testing procedure.

### Data model

See `specs/004-black-text-pdf/data-model.md` for entity definitions and relationships.

## Post-Phase 1 Constitution Check

All six principles remain PASS after design. No new violations introduced. The
`ExportBorderWithAllStyles` signature change (adding two parameters) does not violate any
principle — it eliminates implicit global state access and makes dependencies explicit
(satisfies SOLID-D). Named constants introduced for all magic string literals (satisfies IV).
