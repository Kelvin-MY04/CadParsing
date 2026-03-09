# Data Model: Fix Unknown Text Characters by Standardizing Font

**Feature**: `006-fix-text-font`
**Date**: 2026-02-28

---

## Modified Entity: AppConfig

**Location**: `CadParsing.Core/Configuration/AppConfig.cs`

| Property | Type | Old Default | New Default | Notes |
|----------|------|-------------|-------------|-------|
| `TextLayerSuffix` | `string` | `"TEXT"` | *(removed)* | Replaced by `TextLayerSuffixes` |
| `TextLayerSuffixes` | `string[]` | *(new)* | `["TEXT", "TEX"]` | Serialized as JSON array via DataContractJsonSerializer |

**Config file change** (`CadParsing/cadparsing.config.json`):
```json
// Before
{ "TextLayerSuffix": "TEXT" }

// After
{ "TextLayerSuffixes": ["TEXT", "TEX"] }
```

**Validation rules**:
- `TextLayerSuffixes` MUST NOT be null or empty (validated at usage site; defaults apply if missing from JSON)
- Each suffix MUST be a non-null, non-empty string

---

## New Concept: FontOverrideState

**Location**: `CadParsing/Helpers/TextFontOverride.cs` (internal dictionary, not a separate class)

Represents the saved font state before override is applied. Used to restore original state post-export.

| Key | Type | Description |
|-----|------|-------------|
| `EntityId` | `ObjectId` | Identity of the text entity (DBText or MText) |
| `OriginalTextStyleId` | `ObjectId` | The `TextStyleId` the entity had before override |

Stored as `Dictionary<ObjectId, ObjectId>` (entityId → originalStyleId) within `TextFontOverride` methods. No persistent storage — lives only within the export transaction lifetime.

---

## Modified Entity: LayerNameMatcher

**Location**: `CadParsing.Core/Helpers/LayerNameMatcher.cs`

**New method added**:

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `MatchesAnyLayerSuffix` | `string layerName, string[] suffixes` | `bool` | Returns true if `layerName` ends with any of the given suffixes (case-insensitive). Delegates to existing `MatchesLayerSuffix` per element. |

**Behaviour rules**:
- Returns `false` if `suffixes` is null or empty
- Returns `false` if `layerName` is null or empty
- Returns `true` on first matching suffix (short-circuits)

---

## New Class: TextFontOverride

**Location**: `CadParsing/Helpers/TextFontOverride.cs`
**Visibility**: `internal static`

Parallel class to `TextColorOverride`. Saves text style IDs, applies `Standard` font, and restores on demand.

### Method: FindTextEntitiesOnTargetLayers

| Parameter | Type | Description |
|-----------|------|-------------|
| `transaction` | `Transaction` | Active AutoCAD transaction |
| `database` | `Database` | Source drawing database |
| `targetSuffixes` | `string[]` | Layer suffixes to match (from `AppConfig.TextLayerSuffixes`) |

**Returns**: `IReadOnlyList<ObjectId>` — entity IDs of all `DBText` and `MText` entities on matching layers.

**Behaviour rules**:
- Iterates model-space block via `DatabaseHelper.GetModelSpaceBlock`
- Uses `LayerNameMatcher.MatchesAnyLayerSuffix` for layer filtering
- Skips entities that cannot be opened (try/catch per entity)
- Returns empty list (not null) when no matching entities found

### Method: ApplyStandardFontOverride

| Parameter | Type | Description |
|-----------|------|-------------|
| `transaction` | `Transaction` | Active AutoCAD transaction |
| `entityIds` | `IReadOnlyList<ObjectId>` | Entities to override (from `FindTextEntitiesOnTargetLayers`) |
| `database` | `Database` | Used to resolve the `Standard` text style |
| `editor` | `Editor` | For diagnostic messages |

**Returns**: `Dictionary<ObjectId, ObjectId>` — map of entityId → original TextStyleId (for restoration).

**Behaviour rules**:
- Resolves `Standard` style from `TextStyleTable`; creates it if absent
- Opens each entity for write, saves `TextStyleId`, sets it to Standard style's `ObjectId`
- Logs `[WARN]` via editor if an entity cannot be overridden; does not throw
- Returns the saved-state dictionary for later restoration

### Method: RestoreOriginalTextStyles

| Parameter | Type | Description |
|-----------|------|-------------|
| `transaction` | `Transaction` | Active AutoCAD transaction |
| `savedTextStyles` | `Dictionary<ObjectId, ObjectId>` | Map returned by `ApplyStandardFontOverride` |
| `editor` | `Editor` | For diagnostic messages |

**Returns**: `void`

**Behaviour rules**:
- Opens each entity for write, restores its `TextStyleId` from saved map
- Logs `[WARN]` via editor if an entity cannot be restored; does not throw
- Safe to call with an empty dictionary (no-op)

---

## Integration Point in ExportPdfCommand

**Location**: `CadParsing/Commands/ExportPdfCommand.cs`
**Method modified**: `ExportAllBorders`

**Change**: Before the border loop, call `TextFontOverride.FindTextEntitiesOnTargetLayers` then `ApplyStandardFontOverride`. After the border loop (in `finally`), call `RestoreOriginalTextStyles`.

```
ExportAllBorders flow (updated):
  1. Open transaction
  2. Get modelSpace, modelLayout
  3. Find target text entities + apply Standard font override   ← NEW
  4. try {
       for each border: ResolveBorderLabel + ExportBorderWithAllStyles
     }
  5. finally { RestoreOriginalTextStyles }                      ← NEW
  6. transaction.Commit()
```
