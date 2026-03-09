# Implementation Plan: Split Export Folders for Color-PDF and BW-PDF

**Branch**: `005-split-export-folders` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-split-export-folders/spec.md`

## Summary

Export PDF files into two type-segregated subfolders (`Color-PDF` and `BW-PDF`) inside the per-drawing output folder, and remove the `_color`/`_bw` filename suffixes. Path-construction logic is extracted to a new `ExportPathBuilder` class in `CadParsing.Core` to satisfy TDD requirements (tests can only reference `CadParsing.Core`). `ExportPdfCommand` delegates folder path resolution to `ExportPathBuilder` and creates both subfolders unconditionally before starting the border export loop.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8
**Primary Dependencies**: AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only; `CadParsing.Core` and `CadParsing.Tests` have no AutoCAD SDK dependency
**Storage**: File system — PDF files written via AutoCAD plot engine; directory creation via `System.IO`
**Testing**: NUnit 3.x (`CadParsing.Tests/Unit/` — references `CadParsing.Core` only; AutoCAD SDK types are untestable in isolation)
**Target Platform**: Windows — AutoCAD 2023 plugin loaded by `accoreconsole.exe`
**Project Type**: AutoCAD desktop plugin (DWG-to-PDF batch exporter)
**Performance Goals**: No new requirements — matches existing export throughput
**Constraints**: All new testable logic must reside in `CadParsing.Core`; subfolder names are fixed, non-configurable strings
**Scale/Scope**: Single-user, single-drawing session per `EXPORTPDF` invocation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Meaningful Naming | PASS | `ExportPathBuilder`, `ColorPdfFolderName`, `BwPdfFolderName`, `BuildPdfPath`, `CreateTypeSubFolders` — all intention-revealing; no abbreviations |
| II. Single Responsibility | PASS | `ExportPathBuilder` owns path construction exclusively; `ExportPdfCommand` retains orchestration only |
| III. SOLID | PASS | `ExportPdfCommand` extended via helper delegation; `ExportPathBuilder` is a cohesive static utility with one concern |
| IV. DRY | PASS | Subfolder name strings declared as named constants on `ExportPathBuilder`; `StyleSubFolders` array eliminates duplicated path expressions |
| V. Error Handling | PASS | `Directory.CreateDirectory` is idempotent; propagates `IOException` to the existing `catch` in `ExportPdf()`; no silent failure |
| VI. TDD | PASS | All path-building logic placed in `CadParsing.Core` (`ExportPathBuilder`), reachable by `CadParsing.Tests`; AutoCAD orchestration validated manually via E2E |

No Complexity Tracking violations.

## Project Structure

### Documentation (this feature)

```text
specs/005-split-export-folders/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — not created by /speckit.plan)
```

### Source Code (repository root)

```text
CadParsing.Core/
└── Helpers/
    ├── ExportPathBuilder.cs       ← NEW: subfolder name constants + path construction + folder creation
    ├── LayerNameMatcher.cs         (unchanged)
    ├── BoundsChecker.cs            (unchanged)
    └── MTextFormatStripper.cs      (unchanged)

CadParsing/
└── Commands/
    └── ExportPdfCommand.cs         ← MODIFIED: delegate to ExportPathBuilder; remove suffix constants;
                                       create type subfolders before border loop

CadParsing.Tests/
└── Unit/
    ├── ExportPathBuilderTests.cs   ← NEW: unit tests for path building and subfolder creation logic
    ├── ConfigLoaderTests.cs         (unchanged)
    ├── LayerNameMatcherTests.cs     (unchanged)
    ├── BoundsCheckerTests.cs        (unchanged)
    └── MTextFormatStripperTests.cs  (unchanged)
```

**Structure Decision**: Single-project plugin layout is unchanged. One new source file (`ExportPathBuilder.cs`) added to `CadParsing.Core/Helpers/`; one new test file added to `CadParsing.Tests/Unit/`; one existing file (`ExportPdfCommand.cs`) modified in-place. No new projects, packages, or configuration files required.
