# Quickstart: Split Export Folders for Color-PDF and BW-PDF

**Feature**: 005-split-export-folders
**Date**: 2026-02-27

## Running Unit Tests

Run the full test suite (includes new `ExportPathBuilderTests`):

```bash
dotnet test CadParsing.Tests/CadParsing.Tests.csproj
```

Expected: all tests pass (22 existing + new `ExportPathBuilder` tests).

## Verifying the Feature End-to-End

1. Build the plugin:

   ```bash
   BuildAndRun.bat
   ```

2. Run the export via `accoreconsole.exe` using the existing script.

3. Inspect the output folder. Given a drawing named `Building-A.dwg` with two floor plans ("Level 1" and "Level 2"):

   **Expected structure**:
   ```
   <ExportRoot>/
   └── <relative-path>/
       └── Building-A/
           ├── Color-PDF/
           │   ├── Level 1.pdf
           │   └── Level 2.pdf
           └── BW-PDF/
               ├── Level 1.pdf
               └── Level 2.pdf
   ```

4. Verify no PDFs exist at the `Building-A/` root level (only the two subfolders).

5. Verify no `_color` or `_bw` suffixes appear in any PDF filename.

## Checking for Old-Format Files

If the drawing was previously exported with the old format, the flat files (`Level 1_color.pdf`, `Level 1_bw.pdf`) will still exist in `Building-A/` alongside the new subfolders. This is expected — they are left in place and do not affect the new export.

## Edge Case: All Borders Skipped

If no floor plan names can be resolved (e.g., wrong layer suffix in config), both subfolders are still created in the drawing output folder, but they will be empty. Check the AutoCAD console for `[ERROR] Floor plan name not found for border N` messages.
