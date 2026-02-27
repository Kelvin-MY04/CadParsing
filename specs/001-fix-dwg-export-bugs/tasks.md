---

description: "Task list for Fix DWG Export Bugs — Border Detection & Floor Plan Naming"
---

# Tasks: Fix DWG Export Bugs — Border Detection & Floor Plan Naming

**Input**: Design documents from `/specs/001-fix-dwg-export-bugs/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**TDD Mandate**: Per the CadParsing Constitution (Principle VI), tests MUST be written
before implementation. Tasks in each story phase follow Red → Green order.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1 = P1, US2 = P2)
- Include exact file paths in all descriptions

## Path Conventions

```text
CadParsing/                          # Main plugin project
├── Commands/
├── Configuration/                   # NEW — config loading
├── Helpers/                         # Modified helpers + new shared helpers
├── cadparsing.config.json           # NEW — runtime config
└── CadParsing.csproj

CadParsing.Tests/                    # NEW — unit test project
└── Unit/

scripts/                             # NEW — bash + AutoCAD scripts
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create test project scaffolding and the config file before any implementation.

- [X] T001 Create CadParsing.Tests/CadParsing.Tests.csproj targeting net48 with NUnit 3.x and NUnit3TestAdapter NuGet packages; add project reference to CadParsing/CadParsing.csproj
- [X] T002 [P] Add CadParsing/Configuration/ folder to the CadParsing project (no source files yet; just the directory)
- [X] T003 [P] Create CadParsing/cadparsing.config.json with all configurable defaults: BorderLayerSuffix, TextLayerSuffix, FloorPlanTextHeight, TextHeightTolerance, AcceptClosedPolylinesOnly, DownloadRoot, ExportRoot; set CopyToOutputDirectory = Always in CadParsing.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Config system and shared layer-matching helper must be complete before any
user story implementation can begin. Tests are written first (Red phase).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Implement AppConfig data class with DataContract/DataMember attributes for all seven fields in CadParsing/Configuration/AppConfig.cs; include default-value constants for fallback
- [X] T005 [P] Write failing unit tests (Red) for ConfigLoader covering: valid JSON loads all fields, missing file returns defaults, malformed JSON returns defaults with warning, empty suffix fields are rejected — in CadParsing.Tests/Unit/ConfigLoaderTests.cs
- [X] T006 [P] Write failing unit tests (Red) for LayerNameMatcher covering: suffix match is case-insensitive, Xref-prefixed layer names (pipe separator), bound-Xref names (dollar separator), null/empty input returns false, non-matching suffix returns false — in CadParsing.Tests/Unit/LayerNameMatcherTests.cs
- [X] T007 Implement ConfigLoader in CadParsing/Configuration/ConfigLoader.cs: read cadparsing.config.json from the plugin DLL directory using DataContractJsonSerializer, validate fields, return AppConfig with defaults on any failure, log warnings via Editor.WriteMessage; make T005 tests pass (Green)
- [X] T008 [P] Implement LayerNameMatcher in CadParsing/Helpers/LayerNameMatcher.cs: single public static method MatchesLayerSuffix(string layerName, string suffix) using EndsWith with OrdinalIgnoreCase; make T006 tests pass (Green)
- [X] T009 Update CadParsing/Constants.cs to remove all hard-coded values and delegate to ConfigLoader.Instance (singleton AppConfig); preserve existing public const names as forwarding properties so callers are unchanged

**Checkpoint**: Run `dotnet test CadParsing.Tests` — all ConfigLoader and LayerNameMatcher
unit tests MUST pass before proceeding to user story phases.

---

## Phase 3: User Story 1 — All DWG Files Export Successfully (Priority: P1) 🎯 MVP

**Goal**: Replace the space-dependent `editor.SelectAll()` border search with direct
model-space `BlockTableRecord` iteration so every DWG file with a `*PAPER-EX` border layer
produces at least one PDF regardless of which space is active when the command runs.

**Independent Test**: Open a DWG file that previously produced no PDF output. Run the
`EXPORTPDF` command. At least one PDF file MUST appear in the output directory.

### Tests for User Story 1 ⚠️

> **NOTE: These tests verify observable outputs. Run DETECTBORDER/EXPORTPDF via
> accoreconsole.exe against a DWG known to previously fail (zero borders detected).**

- [X] T010 [US1] Write an E2E test script scripts/test-us1-border-detection.sh that opens a previously-failing DWG via accoreconsole.exe, runs DETECTBORDER, and asserts the output contains "[INFO]" and a border count ≥ 1

### Implementation for User Story 1

- [X] T011 [US1] Rewrite BorderHelper.FindBordersInModelSpace in CadParsing/Helpers/BorderHelper.cs: open model-space BlockTableRecord via database transaction, iterate all ObjectIds, filter for Polyline/Polyline2d entities using LayerNameMatcher.MatchesLayerSuffix with AppConfig.BorderLayerSuffix, check IsClosedPolyline, compute GeometricExtents area, collect and sort candidates descending by area; remove SelectAll and CreateBorderFilter entirely
- [X] T012 [US1] Update all callers of BorderHelper.FindBorders (DetectBorderCommand.cs, ExportPdfCommand.cs) to use the renamed FindBordersInModelSpace method signature in CadParsing/Commands/DetectBorderCommand.cs and CadParsing/Commands/ExportPdfCommand.cs
- [ ] T013 [US1] Rebuild the plugin DLL and run scripts/test-us1-border-detection.sh against a previously-failing DWG; confirm border count ≥ 1 and no "[ERROR] no border detected" message appears

**Checkpoint**: At this point, User Story 1 is independently verifiable — every tested
DWG file with a `*PAPER-EX` layer now produces PDF output.

---

## Phase 4: User Story 2 — All PDFs Named With Floor Plan Names (Priority: P2)

**Goal**: Replace `editor.SelectCrossingWindow()` in floor plan name extraction with
direct model-space iteration and an insertion-point containment check, so every exported
PDF is named using the TEX-layer Korean text rather than a fallback number.

**Independent Test**: Run `EXPORTPDF` on a batch of DWG files. Inspect the output folder.
Zero PDFs may be named with a number (e.g., `1_color.pdf`). All must carry a Korean name.

### Tests for User Story 2 ⚠️

> **NOTE: Write BoundsChecker unit tests first (Red), then implement (Green).**

- [X] T014 [P] [US2] Write failing unit tests (Red) for BoundsChecker.IsInsideBounds covering: point at exact center, point at each corner, point on each edge, point just outside each edge, all-zero extents — in CadParsing.Tests/Unit/BoundsCheckerTests.cs
- [X] T015 [P] [US2] Write an E2E test script scripts/test-us2-floor-plan-naming.sh that exports a batch of DWG files via accoreconsole.exe and asserts zero output PDF files match the pattern [0-9]+_color.pdf or [0-9]+_bw.pdf

### Implementation for User Story 2

- [X] T016 [US2] Implement BoundsChecker in CadParsing/Helpers/BoundsChecker.cs: public static IsInsideBounds(Point3d point, Extents3d bounds) returning true iff point.X ∈ [MinPoint.X, MaxPoint.X] and point.Y ∈ [MinPoint.Y, MaxPoint.Y]; make T014 tests pass (Green)
- [X] T017 [US2] Rewrite TextHelper.FindFloorPlanNameInModelSpace in CadParsing/Helpers/TextHelper.cs: open model-space BlockTableRecord via database transaction, iterate all ObjectIds, filter for DBText/MText entities using LayerNameMatcher with AppConfig.TextLayerSuffix, extract insertion point (DBText.Position or MText.Location), check Math.Abs(height - AppConfig.FloorPlanTextHeight) <= AppConfig.TextHeightTolerance, check BoundsChecker.IsInsideBounds, return the matching text value; remove SelectCrossingWindow and SelectTextsInRegion; retain FindLargestMatchingText for height-tiebreak
- [X] T018 [US2] Update ExportPdfCommand.ResolveBorderLabel in CadParsing/Commands/ExportPdfCommand.cs to call TextHelper.FindFloorPlanNameInModelSpace (renamed method); remove the fallback `(borderIndex + 1).ToString()` numeric label; log an error instead if name is unexpectedly null
- [ ] T019 [US2] Run `dotnet test CadParsing.Tests` to confirm all unit tests pass; rebuild plugin and run scripts/test-us2-floor-plan-naming.sh to confirm zero numeric PDF names in the output folder

**Checkpoint**: All user stories independently functional. US1 and US2 both verified.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Headless batch runner, final validation, and documentation checks.

- [X] T020 [P] Create scripts/export.scr AutoCAD script file containing the EXPORTPDF command sequence for use with accoreconsole.exe /s flag
- [X] T021 [P] Create scripts/export-pdf.sh bash wrapper: accept one or more DWG file paths as arguments, generate a temp .scr file per DWG, invoke accoreconsole.exe /i <dwg> /s <scr>, log stdout/stderr, clean up temp files; exit 1 if accoreconsole.exe not found; support AUTOCAD_DIR env-var override
- [X] T022 Run the complete unit test suite `dotnet test CadParsing.Tests -v normal` and confirm 100% pass rate across ConfigLoader, LayerNameMatcher, and BoundsChecker tests
- [ ] T023 Run quickstart.md validation: follow every step in specs/001-fix-dwg-export-bugs/quickstart.md end-to-end on a clean machine checkout and confirm all steps succeed without manual intervention

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on T001 (test project) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 2 completion; can start in parallel with US1 from T014 onward
- **Polish (Phase 5)**: Depends on both US1 (Phase 3) and US2 (Phase 4) passing checkpoints

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 checkpoint — no dependency on US2
- **US2 (P2)**: Can start writing tests (T014, T015) after Phase 2 checkpoint, in parallel
  with US1 implementation; T016–T019 can start once T014 is complete

### Within Each Phase

- Tests MUST be written and confirmed FAILING before the corresponding implementation task
- Models/helpers before callers
- Implementation complete before E2E verification tasks

### Parallel Opportunities

| Tasks | Parallel Because |
|-------|-----------------|
| T002, T003 | Different files, no dependency |
| T004, T005, T006 | Different files, no shared dependency |
| T007, T008 | Different files; T007 needs T004+T005, T008 needs T006 |
| T010, T014, T015 | T010 is US1 E2E script; T014/T015 are US2 tests — all different files |
| T020, T021 | Different script files |

---

## Parallel Example: Phase 2 Foundational

```bash
# These three tasks can launch simultaneously:
Task: "Implement AppConfig in CadParsing/Configuration/AppConfig.cs"           # T004
Task: "Write ConfigLoader unit tests in CadParsing.Tests/Unit/ConfigLoaderTests.cs"  # T005
Task: "Write LayerNameMatcher unit tests in CadParsing.Tests/Unit/LayerNameMatcherTests.cs" # T006

# Once T005 unblocks T007 and T006 unblocks T008 — these two can run in parallel:
Task: "Implement ConfigLoader in CadParsing/Configuration/ConfigLoader.cs"     # T007
Task: "Implement LayerNameMatcher in CadParsing/Helpers/LayerNameMatcher.cs"   # T008
```

## Parallel Example: User Story 2

```bash
# Once Phase 2 is done, these can launch together:
Task: "Write BoundsChecker unit tests in CadParsing.Tests/Unit/BoundsCheckerTests.cs"  # T014
Task: "Write US2 E2E test script in scripts/test-us2-floor-plan-naming.sh"            # T015
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (border detection fixed)
4. **STOP and VALIDATE**: Run DETECTBORDER on previously-failing DWG files
5. Deploy/demo the plugin — all DWG files now export at least one PDF

### Incremental Delivery

1. Setup + Foundational → Config system + LayerNameMatcher ready
2. User Story 1 → All borders detected → Test independently → Demo
3. User Story 2 → All PDFs correctly named → Test independently → Demo
4. Polish → Batch runner script ready for production use

### Parallel Team Strategy

With two developers after Phase 2 completes:

- **Developer A**: User Story 1 (T010–T013, border detection)
- **Developer B**: User Story 2 T014–T015 (write tests), then T016–T019 (implementation)

---

## Notes

- `[P]` tasks touch different files and have no incomplete upstream dependencies
- `[US1]` / `[US2]` labels map tasks to spec.md user stories for traceability
- TDD order is enforced: every `Write failing tests` task MUST precede its `Implement` task
- Commit after each checkpoint (Phase 2 completion, US1 checkpoint, US2 checkpoint)
- Stop at each checkpoint to validate the story independently before moving on
- Avoid modifying AutoCAD command signatures — callers in Commands/ should require only
  method rename updates, not logic changes
