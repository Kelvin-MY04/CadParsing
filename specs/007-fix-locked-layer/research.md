# Research: Fix PDF Export Failure on Locked Layers

**Feature**: 007-fix-locked-layer
**Date**: 2026-02-28
**Status**: Complete — no NEEDS CLARIFICATION items remain

---

## Finding 1: Root Cause of `eInvalidInput`

**Decision**: The crash is NOT caused by catching `eOnLockedLayer` — it is caused by **attempting** `GetObject(id, OpenMode.ForWrite)` on an entity whose layer is locked. AutoCAD's transaction manager records the failed write attempt, which leaves the transaction in an inconsistent state. `transaction.Commit()` then throws `eInvalidInput` because the transaction contains a partial or invalid object edit.

**Rationale**: Both `TextColorOverride.ApplyBlackOverride` and `TextFontOverride.ApplyStandardFontOverride` catch `eOnLockedLayer` and warn-and-continue. However, by the time the exception is raised, AutoCAD has already partially recorded the write request in the transaction journal. Committing a transaction with a partially-recorded locked-layer write results in `eInvalidInput`.

**Alternatives considered**:
- Catching and suppressing the exception (current behaviour): Does not prevent transaction corruption — confirmed by observed error.
- Re-creating the transaction after each failure: Prohibitively complex and breaks the single-transaction-per-export design.
- **Chosen fix**: Pre-check layer lock state before attempting `ForWrite`; never call `GetObject(id, OpenMode.ForWrite)` on an entity whose layer is locked.

---

## Finding 2: AutoCAD API for Layer Lock State

**Decision**: Use `LayerTableRecord.IsLocked` (read/write `bool`) via the standard AutoCAD Managed API.

**Pattern**:
```csharp
// Read lock state
LayerTableRecord layerRecord =
    (LayerTableRecord)transaction.GetObject(entity.LayerId, OpenMode.ForRead);
bool wasLocked = layerRecord.IsLocked;

// Unlock (upgrade to ForWrite)
layerRecord.UpgradeOpen();
layerRecord.IsLocked = false;

// Later restore
LayerTableRecord layerRecord =
    (LayerTableRecord)transaction.GetObject(layerId, OpenMode.ForWrite);
layerRecord.IsLocked = wasLocked;
```

**Rationale**: `UpgradeOpen()` is the standard pattern for promoting a ForRead object to ForWrite in the same transaction without re-opening it. This is safe and idiomatic in AutoCAD Managed API.

**Alternatives considered**:
- `GetObject(layerId, OpenMode.ForWrite)` directly: Also works but less efficient if the object was already opened ForRead.
- Unlocking at the `Document.LockDocument()` API level: Affects the entire drawing session, not just the transaction — not appropriate for a transient override.

---

## Finding 3: Correct Scope for Layer Unlock

**Decision**: Collect the **unique set of locked layer ObjectIds** referenced by the target entity list, unlock those layers once, then restore all at the end — not per-entity.

**Rationale**: Multiple text entities can share the same layer. Unlocking per-entity would repeat the lock-state save and unlock operation unnecessarily and risks overwriting the saved lock state. Collecting unique layer IDs first and operating once per layer is correct, efficient, and satisfies FR-003 (log once per layer).

**Data structure**: `Dictionary<ObjectId, bool>` mapping `layerId → wasLocked` (saved before unlock, used during restore).

---

## Finding 4: Interaction Between Font Override and Color Override Layer Unlocks

**Decision**: The font override (once per export) and color override (per-border, per-style, colour exports only) use separate lock-state dictionaries and are restored independently. No special coordination is needed.

**Rationale**: `ExportAllBorders` unlocks font-override layers first. By the time `ExportBorderWithAllStyles` runs the color-override unlock, those layers are already unlocked — `IsLocked` is `false` — so they do not appear in the color-override's locked-layer collection. The restoration sequence (color first, then font) correctly returns all layers to their original locked state via their respective saved dictionaries.

---

## Finding 5: Test Strategy for AutoCAD SDK–Dependent Code

**Decision**: Create `LayerLockOverrideTests.cs` marked `[Ignore]` with documented manual validation steps — identical to the `TextFontOverrideTests` precedent.

**Rationale**: `CadParsing.Tests` references only `CadParsing.Core` (no AutoCAD SDK). `LayerLockOverride` must live in `CadParsing` (it uses `LayerTableRecord`, `Transaction`, `ObjectId`) and therefore cannot be compiled or executed in the test project. This is a known architectural constraint documented since feature 006.

**Manual validation steps** (for `LayerLockOverrideTests`):
1. Open AutoCAD 2023 with a DWG that has text entities on locked layers.
2. Run `EXPORTPDF`. Confirm: no `[ERROR] PDF export failed` in console, PDF files created, console shows one `[INFO] LayerLockOverride: Temporarily unlocking layer '...'` per affected layer.
3. Open the DWG in AutoCAD and confirm all previously-locked layers are still locked (no permanent modification).
4. Run `EXPORTPDF` on a DWG with no locked layers. Confirm behaviour is identical to pre-fix.
