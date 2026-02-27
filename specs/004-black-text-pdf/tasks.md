# Tasks: Black Text in Color PDF Export

**Input**: Design documents from `/specs/004-black-text-pdf/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ quickstart.md ✅

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1/US2/US3 maps to spec.md user stories
- File paths are absolute from repository root

---

## Phase 1: Setup (Baseline Verification)

**Purpose**: Confirm build and existing tests pass before any changes.

- [x] T001 Verify existing 22-test suite passes — baseline for zero-regression guarantee: `dotnet test CadParsing.Tests/CadParsing.Tests.csproj`
- [x] T002 Review ExportPdfCommand.cs to confirm integration points match plan (ExportAllBorders, ExportBorderWithAllStyles, style loop at lines 144–170)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Two new helper classes and the named-constant refactor must exist before the
`ExportPdfCommand` style-loop integration (Phase 3) can proceed.

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete.

- [x] T003 [P] Create `TextEntityFinder` static class in `CadParsing/Helpers/TextEntityFinder.cs` — implement `FindAllTextEntities(Transaction, Database)`, `CollectTextEntitiesFromBlock(Transaction, BlockTableRecord, List<ObjectId>, HashSet<ObjectId>)`, and `IsTextBearingEntity(Entity)` with DBText/MText/Dimension/Leader/MLeader type checks and visited-set recursion guard per data-model.md
- [x] T004 [P] Create `TextColorOverride` static class in `CadParsing/Helpers/TextColorOverride.cs` — implement `private static readonly Color BlackColor = Color.FromRgb(0, 0, 0)`, `ApplyBlackOverride(Transaction, IReadOnlyList<ObjectId>, Editor)` returning `Dictionary<ObjectId, Color>`, and `RestoreOriginalColors(Transaction, Dictionary<ObjectId, Color>, Editor)`; each entity open wrapped in try-catch with `[WARN]` logging per research.md Decision 6
- [x] T005 Promote magic string literals in `CadParsing/Commands/ExportPdfCommand.cs` to named constants (`ColorStyleSheet = "acad.ctb"`, `MonochromeStyleSheet = "monochrome.ctb"`, `ColorStyleSuffix = "_color"`, `MonochromeStyleSuffix = "_bw"`); update `StyleSheets` and `StyleSuffixes` arrays to reference constants; add `IsColorExport(string styleSheet)` private method

**Checkpoint**: TextEntityFinder, TextColorOverride, and named constants are ready — user story integration can begin.

---

## Phase 3: User Story 1 — Color PDF Export Has Black Text (Priority: P1) 🎯 MVP

**Goal**: All text-bearing entities (DBText, MText, Dimension, Leader, MLeader) — directly in
model space and nested inside block definitions — appear black in the color PDF output.
Non-text geometry retains its original colors.

**Independent Test**: Run `EXPORTPDF` against a DWG with red/yellow text entities. Open
`*_color.pdf` and confirm all text is black while lines, hatches, and fills retain original colors.
DWG entity colors must remain unchanged after the export completes.

### Implementation for User Story 1

- [x] T006 [US1] Add `Transaction transaction` and `Database database` parameters to `ExportBorderWithAllStyles()` signature in `CadParsing/Commands/ExportPdfCommand.cs` (matches plan.md §Phase 1 design)
- [x] T007 [US1] Update the single call site in `ExportAllBorders()` in `CadParsing/Commands/ExportPdfCommand.cs` to pass `transaction` and `database` to `ExportBorderWithAllStyles()`
- [x] T008 [US1] Add color override/restore logic into the per-style loop body in `ExportBorderWithAllStyles()` in `CadParsing/Commands/ExportPdfCommand.cs`: when `IsColorExport(StyleSheets[styleIndex])`, call `TextEntityFinder.FindAllTextEntities` then `TextColorOverride.ApplyBlackOverride` before `ExportSinglePdf`; wrap `ExportSinglePdf` in try/finally; call `TextColorOverride.RestoreOriginalColors` in the finally block per data-model.md state-transition diagram
- [ ] T009 [P] [US1] Manual integration test (Scenario 1 from quickstart.md): run `EXPORTPDF` on a DWG with non-black text; verify `*_color.pdf` has black text and non-text geometry retains original colors; document result

**Checkpoint**: US1 is fully functional — color PDF consistently exports with black text.

---

## Phase 4: User Story 2 — B/W PDF Export Is Unchanged (Priority: P2)

**Goal**: The `*_bw.pdf` output is identical to pre-feature behavior — the text-color override
is never applied during the monochrome plot.

**Independent Test**: Export both PDF variants. Confirm `*_bw.pdf` is visually identical to
what the command produced before this feature. Confirm no `[WARN]` override messages appear
for the B/W pass.

### Implementation for User Story 2

- [ ] T010 [P] [US2] Manual integration test (Scenario 2 from quickstart.md): verify `*_bw.pdf` output is unchanged by the feature; confirm `IsColorExport` guard correctly prevents override for the monochrome style sheet; document result

**Checkpoint**: US2 confirmed — B/W export is unaffected.

---

## Phase 5: User Story 3 — Original DWG Colors Are Preserved After Export (Priority: P3)

**Goal**: After `EXPORTPDF` completes (both color and B/W), every text-bearing entity in the
DWG has its original pre-export color — no permanent modification to the drawing on disk.

**Independent Test**: Note text entity colors before export. Run `EXPORTPDF`. Inspect entity
colors in the DWG afterward — must match pre-export values exactly.

### Implementation for User Story 3

- [ ] T011 [P] [US3] Manual integration test (Scenario 3 from quickstart.md): after `EXPORTPDF` completes, inspect text entity `Color` properties in the DWG; verify all match pre-export values; also test Scenario 5 (locked-layer entity produces `[WARN]`, export continues, no crash); document results

**Checkpoint**: US3 confirmed — DWG data integrity maintained.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and constitution compliance check across all stories.

- [ ] T012 Run all 5 quickstart.md manual scenarios against a real DWG and record pass/fail in `specs/004-black-text-pdf/quickstart.md`
- [x] T013 Run existing 22-test suite to confirm zero regressions: `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` — all 22 must pass
- [x] T014 [P] Constitution compliance self-review: verify all new methods ≤ 20 lines, no magic values remain, no empty catch blocks, naming follows Principle I, no dead code

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS Phases 3, 4, 5
  - T003 and T004 can run in parallel (different files)
  - T005 can run in parallel with T003/T004 (different method, same file — coordinate)
- **Phase 3 (US1)**: Depends on Phase 2 — T006 → T007 → T008 (sequential); T009 after T008
- **Phase 4 (US2)**: Depends on Phase 3 complete
- **Phase 5 (US3)**: Depends on Phase 3 complete (can run in parallel with Phase 4)
- **Phase 6 (Polish)**: Depends on Phases 3, 4, 5 complete

### User Story Dependencies

- **US1 (P1)**: After Foundational — no dependencies on other stories
- **US2 (P2)**: After US1 complete (shares the same code path; US1 must be wired first)
- **US3 (P3)**: After US1 complete — can run in parallel with US2

### Within User Story 1

- T006 → T007 → T008 (sequential — each builds on the previous signature change)
- T009 after T008 (needs the implementation to exist)

---

## Parallel Opportunities

```
Phase 2 parallel:
  T003 (TextEntityFinder.cs)   — parallel with —   T004 (TextColorOverride.cs)

Phase 3/4/5 after Phase 2:
  US1 (T006→T007→T008→T009)
  US2 (T010) — after US1
  US3 (T011) — after US1, parallel with US2

Phase 6:
  T013 (test suite) — parallel with — T014 (code review)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Baseline check
2. Phase 2: Create TextEntityFinder, TextColorOverride, add named constants
3. Phase 3: Wire override/restore into ExportPdfCommand
4. **STOP and VALIDATE**: `*_color.pdf` has black text — MVP delivered

### Incremental Delivery

1. Phase 1 + 2 → helpers and named constants ready
2. Phase 3 → color PDF has black text (MVP)
3. Phase 4 → confirm B/W unchanged
4. Phase 5 → confirm DWG not mutated
5. Phase 6 → polish and close

---

## Notes

- [P] tasks = different files or verifiably non-conflicting changes — safe to parallelize
- Each US phase is independently testable via accoreconsole.exe against real DWG files
- No NUnit tests added (AutoCAD entities cannot be instantiated outside AutoCAD process — justified in plan.md Complexity Tracking)
- All 22 existing CadParsing.Tests tests must remain green throughout
- `transaction.Commit()` in ExportAllBorders runs only after all restores complete — guarantees no permanent color change on disk
