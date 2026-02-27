# Quickstart: Fix DWG Export Bugs

**Branch**: `001-fix-dwg-export-bugs` | **Date**: 2026-02-26

---

## Prerequisites

- AutoCAD 2023 installed at `C:\Program Files\Autodesk\AutoCAD 2023\`
- .NET Framework 4.8 SDK
- .NET CLI (`dotnet`) or Visual Studio 2022+
- Git Bash or WSL for running `scripts/export-pdf.sh`

---

## Build

```bash
# From repo root
dotnet build CadParsing/CadParsing.csproj -c Release
```

The plugin DLL is output to:
```
CadParsing/bin/Release/net48/CadParsing.dll
```

`cadparsing.config.json` is automatically copied to the same directory.

---

## Configure

Edit `CadParsing/cadparsing.config.json` before building to override defaults:

```json
{
  "BorderLayerSuffix": "PAPER-EX",
  "TextLayerSuffix": "TEX",
  "FloorPlanTextHeight": 400.0,
  "TextHeightTolerance": 0.5,
  "AcceptClosedPolylinesOnly": true,
  "DownloadRoot": "C:\\Users\\pphyo\\Downloads",
  "ExportRoot": "C:\\Users\\pphyo\\Downloads\\export\\pdf"
}
```

If the file is missing or unreadable, the plugin uses the above defaults automatically.

---

## Run Unit Tests

```bash
dotnet test CadParsing.Tests/CadParsing.Tests.csproj -v normal
```

All tests in `Unit/` run without AutoCAD. Expected output: all tests pass.

---

## Load Plugin in AutoCAD (interactive)

1. Open AutoCAD 2023.
2. At the command line: `NETLOAD`
3. Browse to `CadParsing/bin/Release/net48/CadParsing.dll`.
4. Open a DWG file with `*PAPER-EX` border layers.
5. Run `DETECTBORDER` to verify border detection.
6. Run `EXPORTPDF` to export PDFs.

---

## Batch Export via accoreconsole.exe (headless)

```bash
# Make the script executable
chmod +x scripts/export-pdf.sh

# Export a single DWG
./scripts/export-pdf.sh "/mnt/c/Users/pphyo/Downloads/FloorPlan_A.dwg"

# Export all DWGs in a directory
./scripts/export-pdf.sh /mnt/c/Users/pphyo/Downloads/*.dwg
```

Override the AutoCAD installation path if needed:

```bash
AUTOCAD_DIR="/mnt/c/Program Files/Autodesk/AutoCAD 2023" \
  ./scripts/export-pdf.sh "/mnt/c/Users/pphyo/Downloads/FloorPlan_A.dwg"
```

---

## Verify Export Output

After running, confirm:

1. PDF files exist in the `ExportRoot` directory (default:
   `C:\Users\pphyo\Downloads\export\pdf\`).
2. Each PDF is named after the floor plan name from the `TEX` layer (Korean text
   expected), **not** a number like `1_color.pdf`.
3. Both `_color.pdf` and `_bw.pdf` variants exist for each border.
4. The AutoCAD command line / accoreconsole output shows `[INFO] PDF exported:` for
   each border, with no `[WARN]` or `[ERROR]` messages about missing floor plan names.

---

## Troubleshooting

| Symptom | Likely Cause | Action |
|---------|--------------|--------|
| `[WARN] No borders found` | Plugin DLL not rebuilt after config change | Rebuild the project |
| PDF named `1_color.pdf` instead of Korean name | Old DLL still loaded | Restart AutoCAD or accoreconsole; rebuild |
| `Config file not found` in log | `cadparsing.config.json` not copied to output dir | Check `.csproj` for `CopyToOutputDirectory = Always` |
| `accoreconsole.exe: not found` | Wrong `AUTOCAD_DIR` in script | Set `AUTOCAD_DIR` environment variable |
