# Implementation Plan: Fix DWG Export Bugs — Border Detection & Floor Plan Naming

**Branch**: `001-fix-dwg-export-bugs` | **Date**: 2026-02-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-fix-dwg-export-bugs/spec.md`

## Summary

Two bugs prevent reliable DWG-to-PDF export:

1. **Bug 1 — Border Detection**: `editor.SelectAll()` searches only the current active
   AutoCAD space (model or paper space, depending on the user's viewport). When run via
   `accoreconsole.exe`, or when a drawing opens in a paper-space layout, this call finds
   zero entities even though valid `*PAPER-EX` border polylines exist in model space.
   **Fix**: Replace `editor.SelectAll()` with direct model-space `BlockTableRecord`
   iteration via the database transaction API, using case-insensitive `EndsWith` for
   layer name matching.

2. **Bug 2 — Floor Plan Name Extraction**: `editor.SelectCrossingWindow()` uses a
   bounding-box crossing test, which selects text entities whose bounding box overlaps
   the search window — including text from adjacent borders. When closely-spaced borders
   are present, text from the wrong border can be selected, or the correct text may be
   missed depending on entity ordering. **Fix**: Replace `SelectCrossingWindow()` with
   direct model-space iteration, then verify the text entity's insertion point lies
   inside the border's bounding box.

3. **Config File**: All pattern constants (`PAPER-EX`, `TEX`, text height, polyline
   type rules, file paths) are moved to `cadparsing.config.json` placed beside the
   plugin DLL, with hard-coded defaults as fallback.

## Technical Context

**Language/Version**: C# (.NET Framework 4.8)
**Primary Dependencies**: AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll), NUnit 3.x (test project only)
**Storage**: JSON config file (`cadparsing.config.json`) — read-only at startup
**Testing**: NUnit 3.x in a separate `CadParsing.Tests` project targeting `net48`; E2E via `accoreconsole.exe` + bash script
**Target Platform**: Windows x64, AutoCAD 2023 / accoreconsole.exe
**Project Type**: AutoCAD plugin library (DLL) + headless batch runner (bash + AutoCAD script)
**Performance Goals**: No regression in export speed; config file loaded once at startup
**Constraints**: Must not add NuGet dependencies to the main plugin project (AutoCAD SDK version conflicts); test project may use NuGet
**Scale/Scope**: Batch DWG files in a local directory; no concurrency concerns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. Meaningful Naming | All new class/method/variable names MUST be descriptive and intention-revealing | ✅ All names in this plan follow the convention (e.g., `LoadConfigFromFile`, `FindBordersInModelSpace`, `IsInsideBounds`) |
| II. Single Responsibility | Each method ≤ 20 lines; one purpose per method and class | ✅ New methods are decomposed: config loading, layer matching, point-in-bounds, and entity traversal are separate methods |
| III. SOLID & OOP | Dependency inversion via `IConfigProvider`; open/closed via `LayerMatcher` strategy | ✅ Config is injected; `LayerMatcher` is a standalone testable class |
| IV. DRY | Layer name matching logic (`EndsWith(suffix, OrdinalIgnoreCase)`) defined once in `LayerNameMatcher`; referenced from both `BorderHelper` and `TextHelper` | ✅ |
| V. Error Handling | Every method that reads files or traverses entities MUST have explicit exception handling | ✅ `ConfigLoader` wraps file I/O in try/catch; entity traversal guards null entities |
| VI. TDD | Unit tests written before implementation for `ConfigLoader`, `LayerNameMatcher`, and `BoundsChecker` | ✅ Test-first approach mandated; E2E tests use accoreconsole.exe |

**Post-Design Re-check**: All gates pass. No violations requiring Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-fix-dwg-export-bugs/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
CadParsing/
├── Commands/
│   ├── DetectBorderCommand.cs       (unchanged)
│   ├── ExportPdfCommand.cs          (modified — no logic change, uses updated helpers)
│   └── ...
├── Configuration/                   (NEW)
│   ├── AppConfig.cs                 (NEW — data class for all config values)
│   └── ConfigLoader.cs              (NEW — reads cadparsing.config.json, returns AppConfig)
├── Helpers/
│   ├── BorderHelper.cs              (MODIFIED — database traversal replaces SelectAll)
│   ├── LayerNameMatcher.cs          (NEW — case-insensitive EndsWith layer matching)
│   ├── BoundsChecker.cs             (NEW — insertion-point-in-Extents3d test)
│   ├── TextHelper.cs                (MODIFIED — database traversal replaces SelectCrossingWindow)
│   └── ExplodeHelper.cs             (unchanged)
├── Constants.cs                     (MODIFIED — delegates to AppConfig singleton)
├── CadParsing.csproj                (unchanged — no new production NuGet packages)
└── cadparsing.config.json           (NEW — runtime config; CopyToOutputDirectory = Always)

CadParsing.Tests/
├── Unit/
│   ├── ConfigLoaderTests.cs
│   ├── LayerNameMatcherTests.cs
│   └── BoundsCheckerTests.cs
└── CadParsing.Tests.csproj          (NEW — net48, references NUnit 3.x)

scripts/
└── export-pdf.sh                    (NEW — bash wrapper for accoreconsole.exe batch export)
```

**Structure Decision**: Single-plugin layout. Test project is a sibling to the main plugin
project. No web, mobile, or multi-service concerns.

## Complexity Tracking

*No constitution violations.*

---

## Phase 0: Research Findings

See [research.md](research.md) for full details. Key decisions:

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Border detection API | Database `BlockTableRecord` iteration | `editor.SelectAll()` is space-dependent; DB iteration always hits model space |
| Text search API | Database iteration + insertion-point check | `SelectCrossingWindow` is a bounding-box crossing test, not insertion-point test |
| Config deserialization | `DataContractJsonSerializer` (built-in .NET FX) | Zero new production dependencies; avoids AutoCAD SDK Newtonsoft version conflicts |
| Layer matching | `string.EndsWith(suffix, OrdinalIgnoreCase)` | Handles Xref-prefixed names (`XREF\|LAYER-PAPER-EX`) and any casing variant |
| Test framework | NUnit 3.x in `CadParsing.Tests` | No dependency on AutoCAD SDK; tests pure logic only |
| E2E runner | `accoreconsole.exe` + bash script + AutoCAD script file | Matches user's stated run target |

---

## Phase 1: Design & Contracts

### AppConfig — Configuration Schema

The JSON config file `cadparsing.config.json` is placed next to the plugin DLL. If missing
or unreadable, `ConfigLoader` returns a default `AppConfig` instance with the same values
previously hard-coded in `Constants.cs`.

**Config schema** (see [data-model.md](data-model.md) for full field definitions):

```json
{
  "BorderLayerSuffix": "PAPER-EX",
  "TextLayerSuffix": "TEX",
  "FloorPlanTextHeight": 400.0,
  "TextHeightTolerance": 0.5,
  "AcceptClosedPolylinesOnly": true,
  "DownloadRoot": "C:\\Users\\pphyo\\Downloads",
  "ExportRoot": "C:\\Users\\pphyo\\Downloads\\export\\pdf"
}
```

### Border Detection — Algorithm Change

**Before** (`editor.SelectAll()` + `SelectionFilter`):
- Searches current active space only
- Returns `PromptStatus.Error` when no matching entities in the active space
- Silent failure when drawing is in paper-space layout

**After** (database `BlockTableRecord` iteration):

```
1. Open model-space BlockTableRecord via transaction
2. For each ObjectId in model-space:
   a. Open entity ForRead
   b. Guard: entity must be Polyline or Polyline2d
   c. Guard: entity.Layer must end with BorderLayerSuffix (OrdinalIgnoreCase)
   d. Guard: entity must be closed (Polyline.Closed or Polyline2d.Closed)
   e. Compute bounding-box area via GeometricExtents
   f. If area > 0 → add to candidates list
3. Sort candidates descending by area
4. Return candidates
```

### Floor Plan Name Extraction — Algorithm Change

**Before** (`SelectCrossingWindow()` with `Extents3d`):
- Selects entities whose bounding box crosses the window
- May select text from adjacent borders
- Space-dependent (same issue as `SelectAll`)

**After** (database iteration + insertion-point check):

```
1. Open model-space BlockTableRecord via transaction
2. For each ObjectId in model-space:
   a. Open entity ForRead
   b. Guard: entity must be DBText or MText
   c. Guard: entity.Layer must end with TextLayerSuffix (OrdinalIgnoreCase)
   d. Extract height and insertion point per entity type:
      - DBText: height = .Height, insertionPoint = .Position
      - MText:  height = .TextHeight, insertionPoint = .Location
   e. Guard: |height - FloorPlanTextHeight| <= TextHeightTolerance
   f. Guard: insertionPoint lies inside borderExtents
      (MinPoint.X ≤ x ≤ MaxPoint.X  AND  MinPoint.Y ≤ y ≤ MaxPoint.Y)
   g. Track candidate with highest height (for tiebreak)
3. Return the text value of the winning candidate (or null if none)
```

### LayerNameMatcher — Extracted Shared Logic

Both `BorderHelper` and `TextHelper` need the same layer name check. Extract to a
dedicated, statically-testable class:

```
LayerNameMatcher.MatchesLayerSuffix(string layerName, string suffix) → bool
  - Returns true iff layerName ends with suffix (OrdinalIgnoreCase)
  - Handles null/empty inputs safely (returns false)
```

### BoundsChecker — Extracted Shared Logic

```
BoundsChecker.IsInsideBounds(Point3d point, Extents3d bounds) → bool
  - Returns true iff point.X in [bounds.MinPoint.X, bounds.MaxPoint.X]
                  AND point.Y in [bounds.MinPoint.Y, bounds.MaxPoint.Y]
```

### Bash Script Contract

See [contracts/export-pdf-script.md](contracts/export-pdf-script.md) for the CLI interface
of `scripts/export-pdf.sh`.

### Agent Context Updated

After writing plan.md, the agent context update script was run:

```bash
powershell -ExecutionPolicy Bypass -File \
  .specify/scripts/powershell/update-agent-context.ps1 -AgentType claude
```

Results in `CLAUDE.md` updated with C# .NET Framework 4.8 + AutoCAD 2023 SDK technology
stack entry.
