# Tasks: Split Export Folders for Color-PDF and BW-PDF

**Input**: Design documents from `/specs/005-split-export-folders/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**TDD Note**: Constitution Principle VI (TDD) is NON-NEGOTIABLE. Test tasks are included and MUST be written before their implementation tasks.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2)

---

## Phase 1: Setup

**Purpose**: Confirm clean baseline before introducing changes

- [ ] T001 Verify project builds cleanly on branch `005-split-export-folders` via `dotnet build CadParsing.slnx` — zero errors, zero new warnings

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Create compilable `ExportPathBuilder` stub so test files can reference the class in Phase 3 and 4

**⚠️ CRITICAL**: Both user story phases depend on this being compiled before test writing begins

- [ ] T002 Create `CadParsing.Core/Helpers/ExportPathBuilder.cs` — public static class with `public const string ColorPdfFolderName = "Color-PDF"`, `public const string BwPdfFolderName = "BW-PDF"`, stub `public static string BuildPdfPath(string drawingSubDirectory, string typeSubFolderName, string sanitizedBorderLabel)` (returns `string.Empty`), and stub `public static void CreateTypeSubFolders(string drawingSubDirectory)` (empty body)

**Checkpoint**: Foundation ready — run `dotnet build CadParsing.slnx` to confirm stubs compile

---

## Phase 3: User Story 1 — Organized PDF Output by Type (Priority: P1) 🎯 MVP

**Goal**: PDF export produces `Color-PDF/` and `BW-PDF/` subfolders inside the drawing output folder; all colour PDFs land in `Color-PDF/` and all B/W PDFs land in `BW-PDF/`

**Independent Test**: Run `EXPORTPDF` on a DWG file with at least one floor plan → drawing output folder must contain exactly `Color-PDF/` and `BW-PDF/` subfolders, each containing one PDF per floor plan, with zero PDFs at the drawing folder root

### Tests for User Story 1 (TDD — write FIRST, verify FAIL before implementing T007)

- [X] T003 [P] [US1] Write failing unit test `BuildPdfPath_GivenColorStyle_ReturnsColorSubfolderPath` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — asserts `BuildPdfPath("C:/out/Bldg", "Color-PDF", "Level 1")` returns `"C:/out/Bldg/Color-PDF/Level 1.pdf"`
- [X] T004 [P] [US1] Write failing unit test `BuildPdfPath_GivenBwStyle_ReturnsBwSubfolderPath` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — asserts `BuildPdfPath("C:/out/Bldg", "BW-PDF", "Level 1")` returns `"C:/out/Bldg/BW-PDF/Level 1.pdf"`
- [X] T005 [P] [US1] Write failing unit test `CreateTypeSubFolders_CreatesBothSubfolders` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — creates temp dir, calls `CreateTypeSubFolders`, asserts both `Color-PDF` and `BW-PDF` subdirectories exist
- [X] T006 [P] [US1] Write failing unit test `CreateTypeSubFolders_IsIdempotent_WhenFoldersAlreadyExist` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — calls `CreateTypeSubFolders` twice on same temp dir; asserts no exception thrown and both subfolders still exist

### Implementation for User Story 1

- [X] T007 [US1] Implement `ExportPathBuilder.BuildPdfPath` and `CreateTypeSubFolders` in `CadParsing.Core/Helpers/ExportPathBuilder.cs` — `BuildPdfPath` returns `Path.Combine(drawingSubDirectory, typeSubFolderName, sanitizedBorderLabel + ".pdf")`; `CreateTypeSubFolders` calls `Directory.CreateDirectory` for both `ColorPdfFolderName` and `BwPdfFolderName` subpaths — run tests, all T003–T006 must pass
- [X] T008 [US1] Add `private static readonly string[] StyleSubFolders = { ExportPathBuilder.ColorPdfFolderName, ExportPathBuilder.BwPdfFolderName }` to `ExportPdfCommand` in `CadParsing/Commands/ExportPdfCommand.cs`
- [X] T009 [US1] In `ExportPdf()` in `CadParsing/Commands/ExportPdfCommand.cs`: move `Directory.CreateDirectory(drawingSubDirectory)` to immediately after `Directory.CreateDirectory(outputDirectory)` (before `ExportAllBorders` is called) and add `ExportPathBuilder.CreateTypeSubFolders(drawingSubDirectory)` on the next line
- [X] T010 [US1] Remove `Directory.CreateDirectory(drawingSubDirectory)` from inside `ExportAllBorders` border loop body in `CadParsing/Commands/ExportPdfCommand.cs` (no longer needed — creation moved to `ExportPdf()`)
- [X] T011 [US1] In `ExportBorderWithAllStyles` in `CadParsing/Commands/ExportPdfCommand.cs`: replace `Path.Combine(drawingSubDirectory, string.Format("{0}{1}.pdf", borderLabel, StyleSuffixes[styleIndex]))` with `ExportPathBuilder.BuildPdfPath(drawingSubDirectory, StyleSubFolders[styleIndex], borderLabel)`

**Checkpoint**: Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` — all T003–T006 must pass. Run `EXPORTPDF` and verify `Color-PDF/` and `BW-PDF/` subfolders appear in the drawing output folder.

---

## Phase 4: User Story 2 — File Naming Without Style Suffix (Priority: P2)

**Goal**: Every PDF filename inside `Color-PDF/` and `BW-PDF/` equals `<FloorPlanName>.pdf` with no `_color` or `_bw` suffix

**Independent Test**: After export, list files in both subfolders and confirm zero filenames contain `_color` or `_bw`

### Tests for User Story 2 (TDD — write FIRST, verify FAIL before implementing T014)

- [X] T012 [P] [US2] Write failing unit test `BuildPdfPath_FilenameContainsNoColorSuffix` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — asserts `Path.GetFileNameWithoutExtension(BuildPdfPath("C:/out/Bldg", "Color-PDF", "Level 1"))` does not contain `"_color"`
- [X] T013 [P] [US2] Write failing unit test `BuildPdfPath_FilenameContainsNoBwSuffix` in `CadParsing.Tests/Unit/ExportPathBuilderTests.cs` — asserts `Path.GetFileNameWithoutExtension(BuildPdfPath("C:/out/Bldg", "BW-PDF", "Level 1"))` does not contain `"_bw"`

### Implementation for User Story 2

- [X] T014 [US2] Remove dead constants `ColorStyleSuffix`, `MonochromeStyleSuffix`, and `StyleSuffixes` from `CadParsing/Commands/ExportPdfCommand.cs` — run `dotnet build CadParsing.slnx` to confirm no remaining references; run `dotnet test` to confirm T012–T013 pass

**Checkpoint**: Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` — all tests pass. Run `EXPORTPDF` and verify PDF filenames contain no `_color` or `_bw` suffix.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final build validation and E2E verification

- [X] T015 [P] Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm all tests pass (22 pre-existing + 6 new `ExportPathBuilder` tests = 28 total)
- [X] T016 [P] Run `dotnet build CadParsing.slnx` in Release configuration and confirm zero errors and zero warnings
- [ ] T017 Run E2E export on a real DWG file using `BuildAndRun.bat` and verify output folder structure matches the expected layout in `specs/005-split-export-folders/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS Phases 3 and 4
- **Phase 3 (US1)**: Depends on Phase 2; test tasks T003–T006 can start immediately after T002; implementation T007–T011 depend on T003–T006 being written (and failing)
- **Phase 4 (US2)**: Depends on Phase 2; test tasks T012–T013 can start after T002; T014 depends on T007 (implementation must exist to compile tests)
- **Phase 5 (Polish)**: Depends on all prior phases complete

### User Story Dependencies

- **US1 (P1)**: Can start immediately after Phase 2 — independent of US2
- **US2 (P2)**: Can start immediately after Phase 2 — independent of US1 (shares `ExportPathBuilder`)

### Within Each User Story

- Test tasks (T003–T006, T012–T013) MUST be written and FAIL before implementation begins
- T008, T009, T010, T011 depend on T007 (requires `ExportPathBuilder` fully implemented)
- T014 depends on T007 (requires `ExportPathBuilder.BuildPdfPath` to be implemented)

### Parallel Opportunities

- T003, T004, T005, T006 — all different test methods, write in parallel
- T012, T013 — different test methods, write in parallel
- T008, T009, T010, T011 — same file (`ExportPdfCommand.cs`), write sequentially
- T015, T016 — independent verification commands, run in parallel

---

## Parallel Example: User Story 1

```bash
# Write all failing tests for US1 in parallel (all in ExportPathBuilderTests.cs):
Task T003: BuildPdfPath_GivenColorStyle_ReturnsColorSubfolderPath
Task T004: BuildPdfPath_GivenBwStyle_ReturnsBwSubfolderPath
Task T005: CreateTypeSubFolders_CreatesBothSubfolders
Task T006: CreateTypeSubFolders_IsIdempotent_WhenFoldersAlreadyExist

# After T007 implementation, T008–T011 in ExportPdfCommand.cs are sequential (same file)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002)
3. Write failing tests T003–T006
4. Implement T007–T011
5. **STOP and VALIDATE**: Run tests + `EXPORTPDF` — verify `Color-PDF/` and `BW-PDF/` subfolders created

### Incremental Delivery

1. Complete Phases 1–2 → Foundation ready
2. Complete Phase 3 (US1) → Subfolders working → MVP validated
3. Complete Phase 4 (US2) → Suffix removed → Full feature complete
4. Complete Phase 5 → Final clean build + E2E sign-off

---

## Notes

- All `[P]` test tasks write to the same file (`ExportPathBuilderTests.cs`) but to independent methods — coordinate to avoid line conflicts
- `CreateTypeSubFolders` tests use `Path.GetTempPath()` for temporary directories; clean up in `TearDown`
- Old flat-format PDFs from prior exports are intentionally left in place (per clarification Q1)
- Both `Color-PDF` and `BW-PDF` subfolders are created unconditionally even if no PDFs will be written (per clarification Q2)
