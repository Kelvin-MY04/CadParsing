---
description: "Task list for feature 002-fix-floor-plan-name"
---

# Tasks: Fix Floor Plan Name Search

**Input**: Design documents from `/specs/002-fix-floor-plan-name/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: TDD is NON-NEGOTIABLE per the project constitution (Principle VI).
Unit tests for `MTextFormatStripper` are written first (RED) before implementation.
TextHelper changes are validated via the full unit test suite (regression) and
manual quickstart steps (integration/E2E — AutoCAD dependency prevents automated testing).

**Organization**: Tasks are grouped by user story to enable independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup (Baseline Verification)

**Purpose**: Confirm the starting state before any code changes.

- [x] T001 Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm exactly 22 tests pass with zero failures

---

## Phase 2: Foundational — `MTextFormatStripper` Utility

**Purpose**: Create the pure-logic text-cleaning utility that `TextHelper` will depend on.
This MUST be complete before any `TextHelper` changes (US1–US3) can be implemented.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

> **TDD Red-Green**: Write T002 first and verify all 10 tests FAIL before starting T003.

- [x] T002 Create `CadParsing.Tests/Unit/MTextFormatStripperTests.cs` with all 10 unit tests from the test plan in `plan.md` — all must FAIL (RED phase)
- [x] T003 Create `CadParsing.Core/Helpers/MTextFormatStripper.cs` with public `Strip(string rawText)` method and private helpers `StripSemicolonTerminatedCodes`, `StripSingleCharCodes`, `RemoveGroupBrackets`, `CollapseWhitespace` — implement until all 10 tests pass (GREEN phase)
- [x] T004 Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm all 32 tests pass (22 original + 10 new)

**Checkpoint**: `MTextFormatStripper` is complete and verified. User-story implementation may now begin.

---

## Phase 3: User Story 1 — PDF Export Succeeds When Text Height Varies (Priority: P1) 🎯 MVP

**Goal**: Removing the `FloorPlanTextHeight` fixed-height filter means DWG files with
TEX-layer text at any height now produce a PDF export.

**Independent Test**: Run the export command against a DWG with a TEX-layer text at height ≠ 400
inside a detected border. Confirm a PDF is produced and named after that text. See
`quickstart.md` for the full manual verification steps.

### Implementation for User Story 1

- [x] T005 [US1] Remove the `if (!MatchesTargetHeight(height, config)) continue;` call from `FindFloorPlanNameInModelSpace` in `CadParsing/Helpers/TextHelper.cs`
- [x] T006 [US1] Add `if (height <= 0) continue;` guard immediately after `ExtractTextInfo` is called in `FindFloorPlanNameInModelSpace` in `CadParsing/Helpers/TextHelper.cs`
- [x] T007 [US1] In `ExtractTextInfo` in `CadParsing/Helpers/TextHelper.cs`, replace `textValue = multiLineText.Contents;` with `textValue = MTextFormatStripper.Strip(multiLineText.Contents);` and add the required `using CadParsing.Helpers;` reference if not already present
- [x] T008 [US1] Remove the entire `MatchesTargetHeight(double height, AppConfig config)` method from `CadParsing/Helpers/TextHelper.cs` (dead code — no remaining callers after T005)
- [x] T009 [US1] Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm all 32 tests still pass (zero regressions from TextHelper changes)

**Checkpoint**: US1 complete. PDFs now export for any TEX-layer text height. Verify using `quickstart.md` manual steps before continuing.

---

## Phase 4: User Story 2 — Largest-Height Text Selected as Floor Plan Name (Priority: P2)

**Goal**: When multiple TEX-layer text entities exist inside a border, the one with the
largest font height is selected. This was already structurally correct in the loop
(`if (height > bestHeight)`) — it only failed because the height filter rejected all candidates.
Removing the filter in Phase 3 is sufficient to enable US2.

**Independent Test**: Run export against a DWG with two TEX-layer texts inside one border at
different heights. Confirm the PDF is named after the taller text.

### Implementation for User Story 2

- [x] T010 [US2] Review `FindFloorPlanNameInModelSpace` in `CadParsing/Helpers/TextHelper.cs` and confirm `if (height > bestHeight)` largest-height-wins loop is correct with no further code changes required — document the finding in a code comment if helpful

**Checkpoint**: US2 validated. Largest-height selection is confirmed. No code changes needed beyond Phase 3.

---

## Phase 5: User Story 3 — Graceful Handling When No TEX Text Is Found (Priority: P3)

**Goal**: When no eligible TEX-layer text is found inside a border, the system emits a
diagnostic warning log entry identifying the border and returns null (unchanged fallback
behavior in the export command).

**Independent Test**: Run export against a DWG with a detected border but no TEX-layer text
inside it. Confirm no crash occurs and the `[WARN]` log entry appears identifying the border
coordinates. See `quickstart.md` for the manual validation procedure.

### Implementation for User Story 3

- [x] T011 [US3] In `FindFloorPlanNameInModelSpace` in `CadParsing/Helpers/TextHelper.cs`, add `Console.WriteLine("[WARN] TextHelper: No eligible TEX-layer text found inside border at (" + borderExtents.MinPoint.X + ", " + borderExtents.MinPoint.Y + ")-(" + borderExtents.MaxPoint.X + ", " + borderExtents.MaxPoint.Y + ")");` immediately before the `return string.IsNullOrEmpty(bestText) ? null : bestText.Trim();` return statement, conditioned on `bestText == null`

**Checkpoint**: US3 complete. The system now logs a warning instead of failing silently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all three user stories.

- [x] T012 [P] Run `dotnet test CadParsing.Tests/CadParsing.Tests.csproj` and confirm all 32 tests pass (final regression gate)
- [ ] T013 [P] Follow all manual validation steps in `specs/002-fix-floor-plan-name/quickstart.md` to confirm PDF export, largest-height selection, and warning log behavior against real DWG files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 baseline confirmation — **BLOCKS all user stories**
- **Phase 3 (US1)**: Depends on Phase 2 completion
- **Phase 4 (US2)**: Depends on Phase 3 completion (US2 is enabled by US1 changes)
- **Phase 5 (US3)**: Depends on Phase 3 completion; independent of Phase 4
- **Phase 6 (Polish)**: Depends on Phases 3, 4, and 5 all complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependencies on other stories
- **US2 (P2)**: Depends on US1 completion (US1 changes enable US2 behavior)
- **US3 (P3)**: Can start after Phase 2 — independent of US1/US2 at implementation level (T011 edits the same file as T005–T008 but a different section)

### Within Each Phase

```
T002 (write failing tests) → T003 (implement to pass) → T004 (verify all pass)
T005 → T006 → T007 → T008 (all edit TextHelper.cs sequentially) → T009 (verify)
T010 (review only — no code change)
T011 (single-line addition to TextHelper.cs)
T012 ∥ T013 (parallel final checks)
```

### Parallel Opportunities

- T012 and T013 (Phase 6) can run simultaneously — different scopes (automated vs. manual)
- Phase 5 (T011) can begin immediately after Phase 2 ends, in parallel with Phase 3/4,
  since it edits a different section of `TextHelper.cs` — however, coordinating edits to the
  same file in parallel requires merging; sequential is safer for a single developer

---

## Parallel Example: Phase 2 Foundational

```
# Write failing tests (RED) first:
Task T002: Create MTextFormatStripperTests.cs with all 10 tests

# Then implement (GREEN) — only after T002 is written and verified to FAIL:
Task T003: Create MTextFormatStripper.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Baseline verification
2. Complete Phase 2: MTextFormatStripper (CRITICAL — blocks all stories)
3. Complete Phase 3: US1 (remove height filter, add height>0 guard, strip MText codes)
4. **STOP and VALIDATE**: Test US1 manually per quickstart.md
5. Proceed to US2 and US3 only if US1 is confirmed working

### Incremental Delivery

1. Phase 1 → Phase 2 → Foundation ready (MTextFormatStripper verified)
2. Phase 3 (US1) → PDF exports work for any text height → **MVP delivered**
3. Phase 4 (US2) → Largest-height wins confirmed → No code change, just validation
4. Phase 5 (US3) → Warning log added → Operational observability improved
5. Phase 6 → Final gate passed

---

## Notes

- **TDD is non-negotiable**: T002 must be committed with all 10 tests FAILING before T003 begins
- `TextHelper.cs` edits (T005–T008, T011) touch the same file — apply sequentially in a single edit session to avoid merge conflicts
- `AppConfig.cs` is **not modified** — `FloorPlanTextHeight` and `TextHeightTolerance` fields remain in place per FR-007
- `config` variable in `FindFloorPlanNameInModelSpace` is still needed for `config.TextLayerSuffix` — do NOT remove it
- Commit after each completed phase to create clean rollback points
