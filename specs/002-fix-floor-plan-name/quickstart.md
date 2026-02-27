# Quickstart: Fix Floor Plan Name Search

**Branch**: `002-fix-floor-plan-name` | **Date**: 2026-02-27

---

## Prerequisites

- .NET Framework 4.8 SDK (`dotnet --version` should show 4.8 or the net48 TFM)
- NUnit 3.x (restored automatically via `dotnet restore`)
- No AutoCAD installation required for unit tests

---

## Running Unit Tests

From the repository root:

```bash
dotnet test CadParsing.Tests/CadParsing.Tests.csproj
```

Expected result after this feature is implemented:

```
Test Run Successful.
Total tests: 32   (was 22 before; +10 MTextFormatStripper tests)
Passed:      32
Failed:      0
```

---

## Validating the Fix (Manual / Integration)

Use a DWG file that has:
- A border polyline on a layer ending with `$0$PAPER-EX`
- A text entity on a layer ending with `TEX`, inside the border, **with a height that is NOT 400**
  (e.g., height 250 or 600)

Run the batch export via `accoreconsole.exe`:

```bash
"C:\Program Files\Autodesk\AutoCAD 2023\accoreconsole.exe" \
  /i "path\to\drawing.dwg" \
  /s "path\to\export-script.scr"
```

**Expected**: A PDF is produced in the export directory, named after the TEX-layer text.

**Before fix**: No PDF produced (floor plan name returns null due to height filter mismatch).

**After fix**: PDF produced with the TEX-layer text value as the filename.

---

## Validating the Warning Log

Process a DWG with a border but **no TEX-layer text** inside it.

**Expected log output** (stdout from accoreconsole):

```
[WARN] TextHelper: No eligible TEX-layer text found inside border at (MinX, MinY)-(MaxX, MaxY)
```

No crash, no unhandled exception. Export continues to next border.

---

## Test Coverage Targets

| Module | Target |
|---|---|
| `CadParsing.Core` (incl. `MTextFormatStripper`) | ≥ 80% |
| `CadParsing.Tests` overall | ≥ 80% |

Coverage is verified manually via `dotnet test --collect:"Code Coverage"` or a compatible
coverage tool. No CI pipeline is currently configured.

---

## Key Files Changed

| File | Change |
|---|---|
| `CadParsing.Core/Helpers/MTextFormatStripper.cs` | NEW — regex-based format code stripper |
| `CadParsing/Helpers/TextHelper.cs` | MODIFIED — remove height filter, add stripper call, add warning log |
| `CadParsing.Tests/Unit/MTextFormatStripperTests.cs` | NEW — unit tests (written before implementation) |
