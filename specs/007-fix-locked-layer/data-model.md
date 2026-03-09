# Data Model: Fix PDF Export Failure on Locked Layers

**Feature**: 007-fix-locked-layer
**Date**: 2026-02-28

---

## Overview

This feature introduces no new persistent data. All state is transient, existing only within the
lifetime of a single AutoCAD transaction. The design adds one new in-memory record type used
during export.

---

## New Transient Record: LayerLockState

**Purpose**: Captures the original lock state of a layer before it is temporarily unlocked for
a text override operation, so it can be restored precisely.

| Field | Type | Description |
|---|---|---|
| `LayerId` | `ObjectId` (key) | AutoCAD ObjectId of the `LayerTableRecord` |
| `WasLocked` | `bool` (value) | The `IsLocked` value read before unlocking |

**Representation**: `Dictionary<ObjectId, bool>` — standard .NET dictionary using `ObjectId` as key.

**Lifecycle**:
1. Created by `LayerLockOverride.UnlockLayers(...)` — populated immediately before each unlock.
2. Consumed by `LayerLockOverride.RestoreLayerLocks(...)` — used to reset `IsLocked` on each layer.
3. Discarded after the `finally` block that calls `RestoreLayerLocks`.

**Invariant**: After `RestoreLayerLocks` completes, every layer in the dictionary MUST have its
`IsLocked` value equal to `WasLocked`. The DWG's lock state is identical before and after export.

---

## Existing Transient Records (unchanged)

| Record | Type | Owner | Description |
|---|---|---|---|
| Saved text colors | `Dictionary<ObjectId, Color>` | `TextColorOverride` | Original entity colors for restore |
| Saved text styles | `Dictionary<ObjectId, ObjectId>` | `TextFontOverride` | Original TextStyleId per entity for restore |

---

## Class Interactions (runtime)

```
ExportPdfCommand.ExportAllBorders
  │
  ├─► TextFontOverride.FindTextEntitiesOnTargetLayers
  │     └─ returns: IReadOnlyList<ObjectId>  (font target entity IDs)
  │
  ├─► LayerLockOverride.CollectLockedLayerIds          [NEW]
  │     └─ returns: ISet<ObjectId>  (locked layer IDs for font entities)
  │
  ├─► LayerLockOverride.UnlockLayers                   [NEW]
  │     └─ returns: Dictionary<ObjectId, bool>  (savedFontLayerLocks)
  │
  ├─► TextFontOverride.ApplyStandardFontOverride
  │     └─ returns: Dictionary<ObjectId, ObjectId>  (savedTextStyles)
  │
  ├─► [border loop → ExportBorderWithAllStyles]
  │     │
  │     ├─► TextEntityFinder.FindAllTextEntities
  │     │     └─ returns: IReadOnlyList<ObjectId>  (color target entity IDs)
  │     │
  │     ├─► LayerLockOverride.CollectLockedLayerIds    [NEW]
  │     │     └─ returns: ISet<ObjectId>  (locked layer IDs for color entities)
  │     │
  │     ├─► LayerLockOverride.UnlockLayers             [NEW]
  │     │     └─ returns: Dictionary<ObjectId, bool>  (savedColorLayerLocks)
  │     │
  │     ├─► TextColorOverride.ApplyBlackOverride
  │     │     └─ returns: Dictionary<ObjectId, Color>  (savedColors)
  │     │
  │     ├─► ExportSinglePdf
  │     │
  │     └─► [finally]
  │           ├─► TextColorOverride.RestoreOriginalColors
  │           └─► LayerLockOverride.RestoreLayerLocks  [NEW]
  │
  └─► [finally]
        ├─► TextFontOverride.RestoreOriginalTextStyles
        └─► LayerLockOverride.RestoreLayerLocks        [NEW]
```

---

## Files Changed

| File | Change |
|---|---|
| `CadParsing/Helpers/LayerLockOverride.cs` | **New** — implements the three-method unlock/restore API |
| `CadParsing/Commands/ExportPdfCommand.cs` | **Modified** — adds unlock/restore calls in `ExportAllBorders` and `ExportBorderWithAllStyles` |
| `CadParsing.Tests/Unit/LayerLockOverrideTests.cs` | **New** — `[Ignore]` test fixture with manual validation guidance |
