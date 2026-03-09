# Implementation Plan: Fix Unknown Text Characters by Standardizing Font

**Branch**: `006-fix-text-font` | **Date**: 2026-02-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/006-fix-text-font/spec.md`

## Summary

Text entities in `TEX` and `TEXT` annotation layers display as `????` in exported PDFs when their assigned font is unavailable on the machine. The fix standardizes all text entities on those layers to the `Standard` AutoCAD text style before each export, using a transient save-override-restore pattern that does not persist changes to the source DWG file.

The implementation extends `AppConfig` to support multiple configurable layer suffixes, adds a `MatchesAnyLayerSuffix` multi-suffix helper to `LayerNameMatcher`, introduces a new `TextFontOverride` class (mirroring `TextColorOverride`), and integrates the override into `ExportPdfCommand.ExportAllBorders`.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8
**Primary Dependencies**: AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only; `CadParsing.Core` and `CadParsing.Tests` have no AutoCAD SDK dependency
**Storage**: `System.Runtime.Serialization.DataContractJsonSerializer` (JSON config), `System.IO` (file writes)
**Testing**: NUnit 3.x — `CadParsing.Tests` project
**Target Platform**: Windows desktop (AutoCAD 2023 plugin via `accoreconsole.exe`)
**Project Type**: AutoCAD plugin + pure-logic core library
**Performance Goals**: No new performance requirements; font override adds O(n) entity traversal (same as existing color override)
**Constraints**: AutoCAD SDK not available in `CadParsing.Core` or `CadParsing.Tests`; changes must not write to the source DWG file on disk
**Scale/Scope**: Single drawing at a time; typical drawings have tens to hundreds of text entities on annotation layers

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Meaningful Naming | ✅ Pass | `TextFontOverride`, `FindTextEntitiesOnTargetLayers`, `ApplyStandardFontOverride`, `RestoreOriginalTextStyles`, `MatchesAnyLayerSuffix` — all descriptive, verb-noun |
| II. Single Responsibility | ✅ Pass | Each new method performs exactly one action; `TextFontOverride` owns only font override logic |
| III. SOLID | ✅ Pass | `TextFontOverride` is a new isolated class; `ExportPdfCommand` extended via new method calls, not modification of existing logic blocks |
| IV. DRY | ✅ Pass | `MatchesAnyLayerSuffix` delegates to existing `MatchesLayerSuffix`; no duplication; config centralized |
| V. Error Handling | ✅ Pass | Per-entity try/catch + warn pattern (mirrors `TextColorOverride`); `finally` guarantees restore |
| VI. TDD | ✅ Pass | Unit tests written first for `MatchesAnyLayerSuffix`, `AppConfig` serialization, and `TextFontOverride` logic |

**Constitution Check: PASS** — no violations, no Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/006-fix-text-font/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
CadParsing.Core/
├── Configuration/
│   └── AppConfig.cs                    MODIFY — TextLayerSuffix → TextLayerSuffixes (string[])
└── Helpers/
    └── LayerNameMatcher.cs             MODIFY — add MatchesAnyLayerSuffix method

CadParsing/
├── Commands/
│   └── ExportPdfCommand.cs             MODIFY — integrate TextFontOverride in ExportAllBorders
├── Helpers/
│   └── TextFontOverride.cs             NEW — save/apply/restore font override
└── cadparsing.config.json              MODIFY — TextLayerSuffix → TextLayerSuffixes array

CadParsing.Tests/
└── Unit/
    ├── AppConfigTextLayerSuffixesTests.cs    NEW — serialization tests for new property
    └── LayerNameMatcherTests.cs             MODIFY — add MatchesAnyLayerSuffix test cases
```

**Structure Decision**: Single-project option (existing three-project layout). All changes confined to existing project boundaries with no new projects introduced.

## Phase 0: Research

**Status: Complete** — see [research.md](research.md)

### Resolved Unknowns

| Unknown | Resolution |
|---------|-----------|
| How to change text style on DBText/MText | Assign `TextStyleId` property to the `ObjectId` of the `Standard` `TextStyleTableRecord` |
| Does `Standard` style always exist | Yes in virtually all drawings; add creation fallback for edge cases |
| DataContract serialization of `string[]` | Supported natively; property rename provides backward compat |
| Timing of font override application | Once per `ExportAllBorders` call, in `try/finally` around the border loop |
| Which entity types to target | `DBText` and `MText` only, filtered by layer suffix |
| Where to add multi-suffix matching | New method on `LayerNameMatcher` in `CadParsing.Core` |

## Phase 1: Design & Contracts

**Status: Complete** — see [data-model.md](data-model.md)

### Key Design Decisions

#### 1. AppConfig — Replace Single Suffix with Array

`AppConfig.TextLayerSuffix` (string, default `"TEXT"`) is replaced by `AppConfig.TextLayerSuffixes` (string[], default `["TEXT", "TEX"]`). The property name change provides automatic backward compatibility: old JSON files without `TextLayerSuffixes` fall back to the default array value.

The `cadparsing.config.json` runtime config file is updated to use the new array format.

All existing callers of `config.TextLayerSuffix` are updated to `config.TextLayerSuffixes` and call `LayerNameMatcher.MatchesAnyLayerSuffix`.

#### 2. LayerNameMatcher — Add MatchesAnyLayerSuffix

```
MatchesAnyLayerSuffix(string layerName, string[] suffixes) → bool
```

Iterates `suffixes`, calls `MatchesLayerSuffix` per entry, returns `true` on first match. Returns `false` for null/empty inputs. Fully unit-testable in `CadParsing.Core`.

#### 3. TextFontOverride — New Internal Static Class

Parallel structure to `TextColorOverride`. Three methods:

```
FindTextEntitiesOnTargetLayers(Transaction, Database, string[]) → IReadOnlyList<ObjectId>
ApplyStandardFontOverride(Transaction, IReadOnlyList<ObjectId>, Database, Editor) → Dictionary<ObjectId, ObjectId>
RestoreOriginalTextStyles(Transaction, Dictionary<ObjectId, ObjectId>, Editor) → void
```

`ApplyStandardFontOverride` resolves (or creates) the `Standard` `TextStyleTableRecord` from the drawing's `TextStyleTable`, then sets `TextStyleId` on each target entity. It returns a save-state dictionary. `RestoreOriginalTextStyles` reverses the change using that dictionary.

Both override and restore methods log `[WARN]` via `editor.WriteMessage` for per-entity failures without aborting the overall operation.

#### 4. ExportPdfCommand — Integration

`ExportAllBorders` gains a font override block wrapping the border loop:

```
// Before border loop
config = ConfigLoader.Instance
targetEntityIds = TextFontOverride.FindTextEntitiesOnTargetLayers(txn, db, config.TextLayerSuffixes)
savedStyles = TextFontOverride.ApplyStandardFontOverride(txn, targetEntityIds, db, editor)

try { /* border loop — unchanged */ }
finally { TextFontOverride.RestoreOriginalTextStyles(txn, savedStyles, editor) }

transaction.Commit()
```

`ExportBorderWithAllStyles` is not modified. Font override applies uniformly to all styles (color and B/W) since missing-font rendering affects both.

### Contracts

This feature exposes no public API, CLI interface, or external service contract. All changes are internal to the AutoCAD plugin. No contracts directory is required.

### Post-Design Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Meaningful Naming | ✅ Pass | All method and variable names are descriptive |
| II. Single Responsibility | ✅ Pass | `TextFontOverride` owns only font override; `LayerNameMatcher` owns only layer matching; `AppConfig` owns only config data |
| III. SOLID | ✅ Pass | No existing classes modified in a way that changes their responsibility; extension by addition |
| IV. DRY | ✅ Pass | Multi-suffix matching is in one place; save/apply/restore pattern mirrors `TextColorOverride` without copying its code |
| V. Error Handling | ✅ Pass | All AutoCAD entity operations wrapped in try/catch; finally block guarantees restoration |
| VI. TDD | ✅ Pass | Test-first for all new public logic in `CadParsing.Core`; integration verified via manual DWG test |

**Post-Design Constitution Check: PASS**
