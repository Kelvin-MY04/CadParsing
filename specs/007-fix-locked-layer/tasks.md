# Tasks: Fix PDF Export Failure on Locked Layers

**Input**: Design documents from `specs/007-fix-locked-layer/`
**Branch**: `007-fix-locked-layer`
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)
**User Story**: US1 — Export PDF from DWG with Locked Layers (P1, only story)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[US1]**: Belongs to User Story 1 (the only story in this feature)

---

## Phase 1: Foundational — `LayerLockOverride` Helper

**Purpose**: Create the new helper class that all bug-fix changes depend on. Must complete before any modifications to `ExportPdfCommand.cs`.

**⚠️ CRITICAL**: Phases 2 and 3 cannot begin until this phase is complete.

- [x] T001 Create `CadParsing/Helpers/LayerLockOverride.cs` with class scaffold and implement `CollectLockedLayerIds(Transaction, IReadOnlyList<ObjectId>)` — iterates entity IDs, opens each entity and its LayerTableRecord ForRead, collects unique locked layer ObjectIds into a `HashSet<ObjectId>`; all per-entity exceptions caught silently
- [x] T002 Add `UnlockLayers(Transaction, ISet<ObjectId>, Editor)` method to `CadParsing/Helpers/LayerLockOverride.cs` — for each layer ID: open ForRead, save `IsLocked` to `Dictionary<ObjectId, bool>`, call `UpgradeOpen()`, set `IsLocked = false`, log one `[INFO] LayerLockOverride: Temporarily unlocking layer '{name}'.`; per-layer exceptions caught with `[WARN]`
- [x] T003 Add `RestoreLayerLocks(Transaction, Dictionary<ObjectId, bool>, Editor)` method to `CadParsing/Helpers/LayerLockOverride.cs` — guard-return on null/empty input; for each saved entry open layer ForWrite and restore `IsLocked` to saved value; per-layer exceptions caught with `[WARN]`

**Checkpoint**: `LayerLockOverride.cs` compiles with all three methods. Ready to integrate into `ExportPdfCommand.cs`.

---

## Phase 2: User Story 1 — Integrate into ExportPdfCommand (Priority: P1) 🎯 MVP

**Goal**: Wire the new `LayerLockOverride` helper into both override sites in `ExportPdfCommand` so that locked-layer entities are unlocked before write attempts and re-locked after export.

**Independent Test**: Run `EXPORTPDF` on a DWG with text entities on locked layers. Command must complete without `[ERROR] PDF export failed`, produce PDF files on disk, and show `[INFO] LayerLockOverride: Temporarily unlocking layer '...'` once per locked layer.

- [x] T004 [US1] Modify `ExportAllBorders` in `CadParsing/Commands/ExportPdfCommand.cs` — after `TextFontOverride.FindTextEntitiesOnTargetLayers`, call `LayerLockOverride.CollectLockedLayerIds` then `LayerLockOverride.UnlockLayers`; in the `finally` block after `TextFontOverride.RestoreOriginalTextStyles`, call `LayerLockOverride.RestoreLayerLocks`
- [x] T005 [US1] Modify `ExportBorderWithAllStyles` in `CadParsing/Commands/ExportPdfCommand.cs` — inside the colour-export branch, after `TextEntityFinder.FindAllTextEntities`, call `LayerLockOverride.CollectLockedLayerIds` then `LayerLockOverride.UnlockLayers`; in the `finally` block after `TextColorOverride.RestoreOriginalColors`, call `LayerLockOverride.RestoreLayerLocks`
- [x] T006 [P] [US1] Create `CadParsing.Tests/Unit/LayerLockOverrideTests.cs` — `[TestFixture]` + `[Ignore("LayerLockOverride requires the AutoCAD SDK; validate manually via EXPORTPDF command.")` fixture following the `TextFontOverrideTests` pattern; include test stubs: `CollectLockedLayerIds_EmptyEntityList_ReturnsEmptySet`, `UnlockLayers_EmptyLayerSet_ReturnsEmptyDictionary`, `RestoreLayerLocks_NullDictionary_DoesNotThrow`, `RestoreLayerLocks_EmptyDictionary_DoesNotThrow`, `UnlockThenRestore_LockedLayer_LayerRemainsLockedAfterRestore`; add manual validation instructions matching quickstart.md

**Checkpoint**: US1 fully functional. All three acceptance scenarios from spec.md pass manually.

---

## Phase 3: Polish & Validation

**Purpose**: Confirm build succeeds and existing tests are not regressed.

- [x] T007 [US1] Build solution (`dotnet build`) and run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` — confirm 48 tests pass and exactly 3 tests are skipped (TextFontOverrideTests ×2 + new LayerLockOverrideTests ×1); zero failures

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — can start immediately
- **Phase 2 (US1)**: Requires Phase 1 complete (`LayerLockOverride.cs` must compile)
  - T004 and T005 are sequential (same file, different methods)
  - T006 is independent (new file) — can run in parallel with T004/T005
- **Phase 3 (Validation)**: Requires all of Phase 2 complete

### Task Dependencies

```
T001 → T002 → T003 → T004 → T005 → T007
                    → T006 ──────────┘
```

### Parallel Opportunities

- T006 can run alongside T004 and T005 (different file: `LayerLockOverrideTests.cs`)
- T002 and T003 are sequential within the same file but have no dependency on each other's logic

---

## Implementation Strategy

### MVP (Single User Story)

1. Complete Phase 1: Create `LayerLockOverride.cs` (T001 → T002 → T003)
2. Complete Phase 2: Wire into `ExportPdfCommand` (T004, T005) + test file (T006)
3. Complete Phase 3: Build and verify (T007)
4. **VALIDATE**: Run `EXPORTPDF` on a DWG with locked layers — confirm PDF is produced, no fatal error, layers still locked after export

---

## Notes

- [P] tasks operate on different files and have no blocking dependencies
- [US1] maps all tasks to User Story 1 (spec.md §User Story 1)
- The `[Ignore]` attribute on `LayerLockOverrideTests` is intentional and documented — AutoCAD SDK is unavailable in the test project (same constraint as `TextFontOverrideTests`, feature 006)
- Commit after each phase checkpoint to enable easy rollback
