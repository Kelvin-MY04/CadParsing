# Quickstart: Black Text in Color PDF Export

**Branch**: `004-black-text-pdf` | **Date**: 2026-02-27

## What Was Built

The `EXPORTPDF` command now temporarily overrides all text-bearing entities (DBText, MText,
all dimension types, Leader, MLeader) to explicit black (RGB 0,0,0) before producing the
color PDF for each border, then restores every entity to its original color immediately after.
The B/W PDF and the DWG on disk are unaffected.

---

## Prerequisites

- AutoCAD 2023 installed at `C:\Program Files\Autodesk\AutoCAD 2023\`
- `CadParsing.dll` built and loaded as a plugin (or `accoreconsole.exe` configured)
- A test DWG containing:
  - At least one border polyline on the `PAPER-EX`-suffixed layer
  - Text entities in non-black colors (e.g., red ACI 1, yellow ACI 2)
  - Optionally: dimensions, leaders, or text inside block inserts

---

## Build

```bash
# From repo root
dotnet build CadParsing/CadParsing.csproj -c Release -f net48
```

---

## Manual Integration Test

### Scenario 1 — Color PDF has black text; non-text retains color

1. Open a DWG with red/yellow text entities in AutoCAD (or run via accoreconsole.exe).
2. Run `EXPORTPDF`.
3. Open the `*_color.pdf` output.
4. **Expected**: All text appears black. Lines, hatches, and fills retain their original colors.

### Scenario 2 — B/W PDF is unchanged

1. Use the same DWG and run.
2. Open the `*_bw.pdf` output.
3. **Expected**: Output is identical to pre-feature B/W PDF (monochrome, no visible change).

### Scenario 3 — DWG colors are preserved after export

1. Before running the export, note the text entity colors in the DWG (e.g., red = ACI 1).
2. Run `EXPORTPDF`.
3. Select a text entity in the DWG and inspect its color properties.
4. **Expected**: Color is still the original value (e.g., red ACI 1); not black.

### Scenario 4 — Text inside block inserts appears black

1. Use a DWG where colored text exists inside a block reference (not directly in model space).
2. Run `EXPORTPDF`.
3. Open the `*_color.pdf`.
4. **Expected**: Text inside block inserts also appears black.

### Scenario 5 — Entities on locked layers produce a warning, not an abort

1. Lock a layer containing text entities in AutoCAD.
2. Run `EXPORTPDF`.
3. **Expected**: The command continues; a `[WARN]` line is printed to the editor for the
   unmodifiable entity; the color PDF is produced (that entity renders in its original color);
   no crash or abort occurs.

---

## Run Existing Unit Tests

```bash
dotnet test CadParsing.Tests/CadParsing.Tests.csproj
```

**Expected**: All 22 tests pass. No regressions from this feature.

---

## accoreconsole.exe Batch Run

```bash
# From repo root — adjust DWG path as needed
"C:\Program Files\Autodesk\AutoCAD 2023\accoreconsole.exe" \
  /i "C:\Users\pphyo\Downloads\sample.dwg" \
  /s "scripts\export_pdf.scr"
```

Where `scripts\export_pdf.scr` contains:
```
EXPORTPDF
QUIT
Y
```

Check the output directory under `C:\Users\pphyo\Downloads\export\pdf\` for the produced
`*_color.pdf` and `*_bw.pdf` files.

---

## Key Files

| File | Purpose |
|------|---------|
| `CadParsing/Helpers/TextEntityFinder.cs` | Collects ObjectIds of all text-bearing entities |
| `CadParsing/Helpers/TextColorOverride.cs` | Applies black override; restores originals |
| `CadParsing/Commands/ExportPdfCommand.cs` | Orchestrates override/restore in style loop |
| `specs/004-black-text-pdf/research.md` | All design decisions with rationale |
| `specs/004-black-text-pdf/data-model.md` | Entity definitions and state transitions |
