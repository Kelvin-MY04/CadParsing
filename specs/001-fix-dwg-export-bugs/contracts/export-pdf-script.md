# Contract: export-pdf.sh — Bash Wrapper for accoreconsole.exe

**File**: `scripts/export-pdf.sh`
**Purpose**: Headless batch export of DWG files to PDF via AutoCAD Core Console

---

## Interface

### Usage

```bash
./scripts/export-pdf.sh <dwg-file> [<dwg-file> ...]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<dwg-file>` | Yes (at least one) | Absolute or relative path to a `.dwg` file to export |

### Exit Codes

| Code | Meaning |
|------|---------|
| `0` | All specified DWG files processed (individual export failures are logged, not fatal) |
| `1` | Script setup error (accoreconsole.exe not found, no arguments supplied) |

### Environment Variables (optional overrides)

| Variable | Default | Description |
|----------|---------|-------------|
| `AUTOCAD_DIR` | `/mnt/c/Program Files/Autodesk/AutoCAD 2023` | Installation directory of AutoCAD 2023 |
| `ACCORECONSOLE` | `$AUTOCAD_DIR/accoreconsole.exe` | Full path to the accoreconsole.exe executable |

### Behaviour

1. Validates that `accoreconsole.exe` exists at the resolved path; exits with code 1 if not.
2. For each `<dwg-file>` argument:
   a. Generates a temporary AutoCAD script (`.scr`) that issues the `EXPORTPDF` command.
   b. Invokes `accoreconsole.exe /i "<dwg-file>" /s "<temp-script>"`.
   c. Logs stdout/stderr from accoreconsole to the terminal.
   d. Removes the temporary script file.
3. Does not modify the source DWG files.

### Example

```bash
# Export a single file
./scripts/export-pdf.sh /mnt/c/Users/pphyo/Downloads/FloorPlan_A.dwg

# Export all DWG files in a directory
./scripts/export-pdf.sh /mnt/c/Users/pphyo/Downloads/*.dwg
```

### Output

PDF files are written to the path resolved by `ExportRoot` in `cadparsing.config.json`.
Relative to the DWG source directory when the file is outside the configured `DownloadRoot`.
See `data-model.md` for path routing logic.
