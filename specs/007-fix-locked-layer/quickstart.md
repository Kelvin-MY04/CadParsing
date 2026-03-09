# Quickstart: Fix PDF Export Failure on Locked Layers

**Feature**: 007-fix-locked-layer
**Date**: 2026-02-28

---

## Prerequisites

- Visual Studio 2022 (or MSBuild 17+)
- AutoCAD 2023 installed at `C:\Program Files\Autodesk\AutoCAD 2023\`
- .NET Framework 4.8 SDK
- NUnit 3.x (installed via NuGet — restored automatically)

---

## Build

```bash
# From repo root
dotnet build CadParsing.sln
# or
msbuild CadParsing.slnx
```

---

## Run Unit Tests

```bash
dotnet test CadParsing.Tests/CadParsing.Tests.csproj
```

Expected: **48 pass, 3 skipped** (the 2 existing `TextFontOverrideTests` plus the new
`LayerLockOverrideTests` — all skipped because they require a live AutoCAD session).

---

## Manual Integration Test (for locked-layer fix)

1. Open AutoCAD 2023.
2. Load a DWG file that has text entities on locked layers. (If none available, create one:
   add a text entity to a layer, then lock the layer via Layer Manager.)
3. Type `EXPORTPDF` at the AutoCAD command prompt.

**Expected console output (success):**
```
[INFO] LayerLockOverride: Temporarily unlocking layer 'LOCKED_LAYER_NAME' for override.
[INFO] PDF exported: C:\...\BorderLabel.pdf
```

**Expected console output (failure — bug NOT fixed):**
```
[WARN] TextColorOverride: Cannot override color for entity (...): eOnLockedLayer
[ERROR] PDF export failed: eInvalidInput
```

4. After export, open Layer Manager and confirm the layer is still locked (no permanent change).
5. Repeat with a DWG that has **no** locked layers and confirm behaviour is identical to pre-fix
   (no regression).

---

## Key Files for this Feature

| File | Purpose |
|---|---|
| `CadParsing/Helpers/LayerLockOverride.cs` | New helper — unlock/restore layer lock state |
| `CadParsing/Commands/ExportPdfCommand.cs` | Modified — adds unlock/restore calls |
| `CadParsing.Tests/Unit/LayerLockOverrideTests.cs` | New test file (marked `[Ignore]`) |
| `specs/007-fix-locked-layer/spec.md` | Feature specification |
| `specs/007-fix-locked-layer/research.md` | Root cause analysis and API decisions |
