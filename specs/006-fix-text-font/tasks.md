# Tasks: Fix Unknown Text Characters by Standardizing Font

**Input**: Design documents from `specs/006-fix-text-font/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Update the runtime configuration file before any code changes.

- [X] T001 Update `CadParsing/cadparsing.config.json` to replace the `TextLayerSuffix` string key with a `TextLayerSuffixes` array: `["TEXT", "TEX"]`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core model and helper changes that all user story work depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Replace `AppConfig.TextLayerSuffix` (string) with `AppConfig.TextLayerSuffixes` (string[], default `new[] { "TEXT", "TEX" }`) in `CadParsing.Core/Configuration/AppConfig.cs`
- [X] T003 [P] Add `MatchesAnyLayerSuffix(string layerName, string[] suffixes)` method to `CadParsing.Core/Helpers/LayerNameMatcher.cs`
- [X] T004 [P] Add NUnit test cases for `MatchesAnyLayerSuffix` (match first, match second, no match, null layer, empty array) in `CadParsing.Tests/Unit/LayerNameMatcherTests.cs`
- [X] T005 [P] Create `CadParsing.Tests/Unit/AppConfigTextLayerSuffixesTests.cs` with serialization tests for the new `TextLayerSuffixes` property (default value, custom array, missing key falls back to default)
- [X] T006 Update `TextHelper.FindFloorPlanNameInModelSpace` to call `LayerNameMatcher.MatchesAnyLayerSuffix(entity.Layer, config.TextLayerSuffixes)` instead of the old single-suffix call in `CadParsing/Helpers/TextHelper.cs`

**Checkpoint**: Config updated, multi-suffix matching in place, TextHelper updated — user story implementation can begin.

---

## Phase 3: User Story 1 — Export PDF Without Garbled Text (Priority: P1) 🎯 MVP

**Goal**: All text in layers matching any configured suffix (default: `TEXT`, `TEX`) is forced to the `Standard` AutoCAD text style before PDF generation, eliminating `????` placeholders.

**Independent Test**: Run `EXPORTPDF` on a DWG file known to produce `????` in exported PDFs and verify the resulting PDF shows readable text with no `????` characters.

### Implementation for User Story 1

- [X] T007 [US1] Create `CadParsing/Helpers/TextFontOverride.cs` as an `internal static` class with `FindTextEntitiesOnTargetLayers(Transaction, Database, string[])` that iterates model-space and returns `IReadOnlyList<ObjectId>` of all `DBText` and `MText` entities whose layer ends with any configured suffix
- [X] T008 [US1] Add `ApplyStandardFontOverride(Transaction, IReadOnlyList<ObjectId>, Database, Editor)` to `CadParsing/Helpers/TextFontOverride.cs` — resolves (or creates) the `Standard` `TextStyleTableRecord`, saves each entity's `TextStyleId`, applies Standard style, logs `[WARN]` on per-entity failures, returns `Dictionary<ObjectId, ObjectId>` (entityId → original styleId)
- [X] T009 [US1] Add `RestoreOriginalTextStyles(Transaction, Dictionary<ObjectId, ObjectId>, Editor)` to `CadParsing/Helpers/TextFontOverride.cs` — reassigns each entity's `TextStyleId` from the saved dictionary, logs `[WARN]` on failures, is a no-op on empty dictionary
- [X] T010 [US1] Integrate `TextFontOverride` into `ExportPdfCommand.ExportAllBorders` in `CadParsing/Commands/ExportPdfCommand.cs`: call `FindTextEntitiesOnTargetLayers` and `ApplyStandardFontOverride` before the border loop; wrap the border loop in `try { } finally { RestoreOriginalTextStyles(...) }`

**Checkpoint**: `EXPORTPDF` on a drawing with missing fonts produces readable text in the exported PDF.

---

## Phase 4: User Story 2 — Original DWG File Remains Unmodified (Priority: P2)

**Goal**: The save-override-restore pattern guarantees the source DWG file is byte-for-byte identical before and after export. The `finally` block ensures restoration even when exceptions occur during plotting.

**Independent Test**: Compare the DWG file's last-modified timestamp and byte content before and after running `EXPORTPDF`; both must be unchanged.

### Implementation for User Story 2

- [X] T011 [US2] Create `CadParsing.Tests/Unit/TextFontOverrideTests.cs` with unit tests verifying `RestoreOriginalTextStyles` behaviour: empty dictionary is a no-op (no throws); document any test limitations due to AutoCAD SDK unavailability in the test project
- [X] T012 [US2] Review `ExportPdfCommand.ExportAllBorders` in `CadParsing/Commands/ExportPdfCommand.cs` to confirm `RestoreOriginalTextStyles` executes in `finally` unconditionally, even when a border export throws; adjust control flow if the inner `try/catch` could swallow exceptions before the outer `finally` runs

**Checkpoint**: Source DWG file is unchanged after export; restoration is exception-safe.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Clean up legacy references and validate the full build.

- [X] T013 [P] Search the codebase for any remaining uses of `config.TextLayerSuffix` (singular) and the old JSON key `TextLayerSuffix`; update all found references to `config.TextLayerSuffixes` with `MatchesAnyLayerSuffix`
- [X] T014 Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm all tests (existing 22+ plus new tests) pass; fix any failures before merging

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 (T007–T010 must exist before T011–T012)
- **Polish (Phase 5)**: Depends on all story phases complete

### Within-Phase Task Order

- T002 must complete before T006 (TextHelper uses AppConfig)
- T003 must complete before T004 (tests verify the new method)
- T007 → T008 → T009 → T010 (sequential: each method builds on previous)
- T007–T010 must complete before T011–T012

### Parallel Opportunities

- T003, T004, T005 can be worked in parallel (different files)
- T013 can run in parallel with T014

---

## Parallel Example: Foundational Phase

```
Parallel group (different files, no cross-dependency):
  T003 — LayerNameMatcher.cs (add method)
  T004 — LayerNameMatcherTests.cs (add test cases)
  T005 — AppConfigTextLayerSuffixesTests.cs (new test file)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T006) — **critical blocker**
3. Complete Phase 3: User Story 1 (T007–T010)
4. **STOP and VALIDATE**: Run `EXPORTPDF` — verify no `????` in output PDF
5. Demo/review if ready

### Incremental Delivery

1. Setup + Foundational → multi-suffix config ready
2. User Story 1 → PDF exports readable text → **MVP delivered**
3. User Story 2 → DWG safety verified → restoration exception-safe
4. Polish → clean codebase, all tests green

---

## Notes

- [P] tasks = different files, no cross-dependencies
- [Story] label maps each task to its user story for traceability
- `TextFontOverride` pattern mirrors `TextColorOverride` — follow the same error-handling and logging conventions
- The `Standard` text style creation fallback (FR-003) must be implemented in T008
- No `database.SaveAs()` is called anywhere in the export flow — this is the primary mechanism ensuring DWG immutability (US2)
