# Implementation Plan: Fix Floor Plan Name Search

**Branch**: `002-fix-floor-plan-name` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-fix-floor-plan-name/spec.md`

---

## Summary

Remove the fixed-height filter (`FloorPlanTextHeight` / `TextHeightTolerance`) from the floor
plan name search in `TextHelper`. Replace it with a largest-height-wins strategy that scans all
TEX-layer text entities inside a border's bounding extents, strips AutoCAD MText format codes
before evaluation, emits a warning when nothing is found, and returns the plain-text value of the
tallest valid candidate.

---

## Technical Context

**Language/Version**: C# / .NET Framework 4.8
**Primary Dependencies**: AutoCAD 2023 SDK (plugin project only), NUnit 3.x (test project only)
**Storage**: N/A (DWG files read; PDF files written via AutoCAD plot engine)
**Testing**: NUnit 3.x (`dotnet test CadParsing.Tests/CadParsing.Tests.csproj`)
**Target Platform**: Windows, AutoCAD 2023 / accoreconsole.exe
**Project Type**: AutoCAD plugin (batch CLI runner via accoreconsole)
**Performance Goals**: No throughput targets — single-user, sequential DWG processing
**Constraints**: .NET Framework 4.8 only; AutoCAD SDK references confined to `CadParsing` project
**Scale/Scope**: Single-user batch tool; 1–N DWG files per run

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked post-design.*

| Principle | Status | Notes |
|---|---|---|
| I. Meaningful Naming | PASS | `MTextFormatStripper`, `Strip`, `StripSemicolonCodes`, `StripSingleCharCodes` all intention-revealing |
| II. Single Responsibility | PASS | `MTextFormatStripper` does one thing; `TextHelper` methods each have one responsibility |
| III. SOLID | PASS | New class is a stateless utility; no hierarchy changes; dependencies injected via `ConfigLoader.Instance` (existing pattern) |
| IV. DRY | PASS | Format stripping logic centralized in one class; no duplication |
| V. Defensive Error Handling | PASS | Existing try/catch preserved in `FindFloorPlanNameInModelSpace`; null guard added to `MTextFormatStripper.Strip` |
| VI. TDD (NON-NEGOTIABLE) | PASS | `MTextFormatStripperTests.cs` written before `MTextFormatStripper.cs`; test list specified in task definitions |
| No Dead Code | PASS | `MatchesTargetHeight` removed (no remaining callers after fix) |
| No Magic Values | PASS | Regex patterns extracted to `private const string` fields |
| Method Length ≤ 20 lines | PASS | All new methods are ≤ 15 lines of logic |

**Complexity violations**: None. No justification table required.

---

## Project Structure

### Documentation (this feature)

```text
specs/002-fix-floor-plan-name/
├── plan.md              ← this file
├── spec.md
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── checklists/
│   └── requirements.md
└── tasks.md             ← Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (affected files only)

```text
CadParsing.Core/
└── Helpers/
    ├── BoundsChecker.cs          (unchanged)
    ├── LayerNameMatcher.cs       (unchanged)
    └── MTextFormatStripper.cs    ← NEW

CadParsing/
└── Helpers/
    └── TextHelper.cs             ← MODIFIED

CadParsing.Tests/
└── Unit/
    ├── BoundsCheckerTests.cs     (unchanged)
    ├── ConfigLoaderTests.cs      (unchanged)
    ├── LayerNameMatcherTests.cs  (unchanged)
    └── MTextFormatStripperTests.cs  ← NEW
```

**Structure Decision**: Single-project layout with `CadParsing.Core` as the AutoCAD-free pure
logic library. The new `MTextFormatStripper` follows the same pattern as `BoundsChecker` and
`LayerNameMatcher` — a public static class in `CadParsing.Core/Helpers/` with no AutoCAD
dependency, making it directly unit-testable.

---

## Phase 0: Research Findings

Full details in [research.md](research.md). Summary:

| Decision | Resolution |
|---|---|
| MText format stripping approach | Regex three-pass: semicolon-terminated codes, single-char codes, bracket removal |
| Where to put the stripper | `CadParsing.Core/Helpers/MTextFormatStripper.cs` (no AutoCAD dep, testable) |
| `MatchesTargetHeight` fate | Removed — no remaining callers, violates "No Dead Code" gate |
| Warning log format | `Console.WriteLine("[WARN] TextHelper: ...")` matching existing error convention |
| `AppConfig` field retention | `FloorPlanTextHeight` + `TextHeightTolerance` kept, not read by text search |

---

## Phase 1: Design & Contracts

### New Class: `MTextFormatStripper`

**Location**: `CadParsing.Core/Helpers/MTextFormatStripper.cs`
**Namespace**: `CadParsing.Helpers`

```text
Public API:
  + Strip(rawText: string) : string
      → null/empty input: returns ""
      → otherwise: three-pass strip, then whitespace collapse + Trim()

Private helpers (each ≤ 10 lines):
  - StripSemicolonTerminatedCodes(text: string) : string
  - StripSingleCharCodes(text: string) : string
  - RemoveGroupBrackets(text: string) : string
  - CollapseWhitespace(text: string) : string

Private constants:
  - SemicolonCodePattern  = @"\\[A-Za-z*'][^;]*;"
  - SingleCharCodePattern = @"\\[A-Za-z~]"
```

---

### Modified Class: `TextHelper`

**Location**: `CadParsing/Helpers/TextHelper.cs`

**Changes**:

1. **Remove** `MatchesTargetHeight(double height, AppConfig config)` method entirely (dead code).

2. **Modify** `ExtractTextInfo` — for `MText`, replace:
   ```
   textValue = multiLineText.Contents;
   ```
   with:
   ```
   textValue = MTextFormatStripper.Strip(multiLineText.Contents);
   ```

3. **Modify** `FindFloorPlanNameInModelSpace` — remove the height-filter guard:
   ```
   // REMOVE THIS BLOCK:
   if (!MatchesTargetHeight(height, config)) continue;
   ```
   And add a height > 0 guard:
   ```
   // ADD: skip degenerate zero/negative heights
   if (height <= 0) continue;
   ```

4. **Add** warning log after the search loop when `bestText` is null:
   ```
   Console.WriteLine(
       "[WARN] TextHelper: No eligible TEX-layer text found inside border at ("
       + borderExtents.MinPoint.X + ", " + borderExtents.MinPoint.Y + ")-("
       + borderExtents.MaxPoint.X + ", " + borderExtents.MaxPoint.Y + ")");
   ```

5. **Remove** the `AppConfig config = ConfigLoader.Instance;` assignment if `config` is no longer
   used (depends on whether any other line still reads from config — verify during implementation).

---

### Contracts

No external contracts file required. `FindFloorPlanNameInModelSpace` signature is unchanged:

```
TextHelper.FindFloorPlanNameInModelSpace(
    Transaction transaction,
    Database database,
    Extents3d borderExtents) : string
```

Callers (e.g., `ExportPdfCommand`) receive the same return type (`string` or `null`). No
downstream interface changes.

---

### Test Plan (`MTextFormatStripperTests.cs`)

Minimum 10 test cases covering the full contract:

| # | Input | Expected Output |
|---|---|---|
| 1 | `null` | `""` |
| 2 | `""` | `""` |
| 3 | `"LEVEL 1"` | `"LEVEL 1"` (plain text, no codes) |
| 4 | `"\\PLEVEL 1"` | `"LEVEL 1"` (paragraph break stripped) |
| 5 | `"\\H300;LEVEL 1"` | `"LEVEL 1"` (height code stripped) |
| 6 | `"\\fArial\\|b0\\|i0;LEVEL 1"` | `"LEVEL 1"` (font code stripped) |
| 7 | `"{\\fArial;FLOOR PLAN}"` | `"FLOOR PLAN"` (brackets and font code stripped) |
| 8 | `"\\H400;\\CFLOOR\\PPLAN"` | `"FLOOR PLAN"` (multiple codes stripped) |
| 9 | `"\\H300;"` | `""` (only format codes — empty after stripping) |
| 10 | `"   \\P  "` | `""` (only whitespace after stripping) |

---

## Agent Context Update

Run after Phase 1 design is complete:

```bash
powershell -ExecutionPolicy Bypass -File \
  ".specify/scripts/powershell/update-agent-context.ps1" -AgentType claude
```
