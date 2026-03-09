# Research: Fix Unknown Text Characters by Standardizing Font

**Feature**: `006-fix-text-font`
**Date**: 2026-02-28
**Status**: Complete — all unknowns resolved

---

## Decision 1: How to Change Text Style on DBText / MText via AutoCAD SDK

**Decision**: Assign `TextStyleId` (an `ObjectId`) on the entity to the `ObjectId` of the `"Standard"` `TextStyleTableRecord`.

**Rationale**:
- `DBText.TextStyleId` and `MText.TextStyleId` are both writable `ObjectId` properties in the AutoCAD .NET API.
- Changing them to the `ObjectId` of the `Standard` style record immediately takes effect for the next plot/render within the same transaction.
- This is an in-memory, transactional change. If the transaction is not saved to disk (no `database.SaveAs()`), the source DWG file on disk is unaffected.
- Same mechanism used by `TextColorOverride` for color overrides — proven pattern in this codebase.

**Alternatives considered**:
- Changing the font via `TextStyleTableRecord.FileName` — rejected: that would alter the style definition globally, not just the entity assignment.
- Temporarily creating a new style — rejected: unnecessary complexity; Standard always exists.

---

## Decision 2: Guaranteeing the "Standard" Style Exists

**Decision**: Retrieve the `Standard` entry from the `TextStyleTable`. If absent (rare edge case), create it with `txt.shx`.

**Rationale**:
- The `Standard` text style is created by AutoCAD when a new drawing is created and is present in virtually all DWG files.
- Using `textStyleTable["Standard"]` with a try/catch or an explicit existence check covers the edge case without risking a crash.
- Creating it requires opening the `TextStyleTable` for write, adding a new `TextStyleTableRecord` with `FileName = "txt.shx"` and `Name = "Standard"`.

**Alternatives considered**:
- Assuming Standard always exists and skipping the null check — rejected: would crash on malformed DWG files.
- Using a different always-available font — rejected: user explicitly specified `Standard`.

---

## Decision 3: Extending AppConfig from Single String to String Array

**Decision**: Replace `TextLayerSuffix` (single `string`) with `TextLayerSuffixes` (`string[]`) in `AppConfig`, defaulting to `new[] { "TEXT", "TEX" }`. Update `cadparsing.config.json` accordingly.

**Rationale**:
- `DataContractJsonSerializer` supports `string[]` natively — serialized as a JSON array.
- The property rename (`TextLayerSuffix` → `TextLayerSuffixes`) means old config JSON files without the new key will silently fall back to the default array, providing automatic backward compatibility.
- A single `TextLayerSuffix` string can no longer express the multi-suffix requirement without duplication.

**Alternatives considered**:
- Comma-separated string (e.g., `"TEXT,TEX"`) — rejected: fragile parsing, non-obvious format.
- Two separate properties (`TextLayerSuffix`, `TextLayerSuffix2`) — rejected: does not scale; violates DRY.

---

## Decision 4: Where to Apply the Font Override (Timing)

**Decision**: Apply the font override **once** at the start of `ExportAllBorders`, before any per-border export loop begins. Restore in a `finally` block, before `transaction.Commit()`.

**Rationale**:
- The font problem affects all borders uniformly — applying once is correct and efficient.
- Mirrors the structural pattern of `TextColorOverride` but differs in scope: color override is per-export (because color mode differs per style sheet), while font override is uniform across all exports.
- Placing it in a `try/finally` guarantees restoration even if a border export throws an exception.

**Alternatives considered**:
- Apply per-border per-style (like color override) — rejected: unnecessary repeated work with no benefit since font is style-sheet-agnostic.
- Apply in `ExportPdf` before `ExportAllBorders` — rejected: outside the transaction scope, which requires the transaction to be active for entity writes.

---

## Decision 5: Which Entity Types to Target

**Decision**: Target `DBText` and `MText` entities only, filtered by layer suffix match.

**Rationale**:
- `DBText` (single-line text) and `MText` (multi-line text) are the entity types that carry a `TextStyleId` property and are the ones that display `????` for missing fonts.
- `Dimension`, `Leader`, and `MLeader` entities (handled by `TextEntityFinder` for color override) do not display `????` in the same way and are not on `TEX`/`TEXT` layers in this drawing schema.
- Layer-suffix filtering ensures only annotation text layers are affected (FR-005 compliance).

**Alternatives considered**:
- Including Dimension/Leader/MLeader — rejected: these entities don't exhibit the `????` problem and are on different layers.

---

## Decision 6: Adding `MatchesAnyLayerSuffix` to `LayerNameMatcher` (CadParsing.Core)

**Decision**: Add a new static method `MatchesAnyLayerSuffix(string layerName, string[] suffixes)` to the existing `LayerNameMatcher` class in `CadParsing.Core`. This delegates to the existing `MatchesLayerSuffix` per suffix.

**Rationale**:
- DRY: the single-suffix matching logic is already proven and tested. The multi-suffix method is a thin loop over it.
- `CadParsing.Core` has no AutoCAD SDK dependency, making the logic unit-testable without AutoCAD.
- Placing it in `LayerNameMatcher` (the canonical location for layer suffix logic) keeps the design coherent.

**Alternatives considered**:
- Inline loop in `TextFontOverride` — rejected: duplicates suffix-matching knowledge outside `LayerNameMatcher`.
- New class `TextLayerSuffixMatcher` — rejected: over-engineering a two-line method.
