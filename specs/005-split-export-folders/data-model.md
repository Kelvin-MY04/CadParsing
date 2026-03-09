# Data Model: Split Export Folders for Color-PDF and BW-PDF

**Feature**: 005-split-export-folders
**Date**: 2026-02-27

## New Entity: ExportPathBuilder

**Location**: `CadParsing.Core/Helpers/ExportPathBuilder.cs`
**Namespace**: `CadParsing.Helpers`
**Type**: `public static class`

Responsible for constructing type-segregated PDF output paths and creating the `Color-PDF`/`BW-PDF` subfolder structure. Contains no AutoCAD SDK references.

### Constants

| Name | Type | Value | Purpose |
|------|------|-------|---------|
| `ColorPdfFolderName` | `public const string` | `"Color-PDF"` | Name of the colour PDF subfolder within the drawing output folder |
| `BwPdfFolderName` | `public const string` | `"BW-PDF"` | Name of the B/W PDF subfolder within the drawing output folder |

### Methods

| Signature | Returns | Responsibility |
|-----------|---------|----------------|
| `BuildPdfPath(string drawingSubDirectory, string typeSubFolderName, string sanitizedBorderLabel)` | `string` | Returns the full file path: `<drawingSubDirectory>/<typeSubFolderName>/<sanitizedBorderLabel>.pdf` |
| `CreateTypeSubFolders(string drawingSubDirectory)` | `void` | Creates both `Color-PDF` and `BW-PDF` subfolders inside `drawingSubDirectory` (idempotent) |

### Invariants

- All parameters are non-null and non-empty (callers are responsible; `ArgumentNullException` thrown on violation).
- `drawingSubDirectory` must be an absolute path (callers responsible).
- `BuildPdfPath` does not create directories — that is `CreateTypeSubFolders`'s responsibility.
- No AutoCAD SDK types referenced anywhere in this class.

---

## Modified Entity: ExportPdfCommand

**Location**: `CadParsing/Commands/ExportPdfCommand.cs`

### Removed Members

| Member | Reason |
|--------|--------|
| `private const string ColorStyleSuffix = "_color"` | Replaced by subfolder organisation; suffix no longer appended to filenames |
| `private const string MonochromeStyleSuffix = "_bw"` | Same as above |
| `private static readonly string[] StyleSuffixes` | No longer used; removed to eliminate dead code |

### Added Members

| Member | Value | Purpose |
|--------|-------|---------|
| `private static readonly string[] StyleSubFolders` | `{ ExportPathBuilder.ColorPdfFolderName, ExportPathBuilder.BwPdfFolderName }` | Maps style sheet index to the corresponding output subfolder name |

### Modified Methods

#### `ExportPdf()` — setup change

Before:
```
Directory.CreateDirectory(outputDirectory);
// drawingSubDirectory creation deferred to inside the loop
```

After:
```
Directory.CreateDirectory(outputDirectory);
Directory.CreateDirectory(drawingSubDirectory);          ← moved here (was inside loop)
ExportPathBuilder.CreateTypeSubFolders(drawingSubDirectory);  ← new
```

Both subfolders are now created unconditionally before the border export loop begins.

#### `ExportAllBorders(...)` — cleanup

- Remove `Directory.CreateDirectory(drawingSubDirectory)` (moved to `ExportPdf()`).

#### `ExportBorderWithAllStyles(...)` — path derivation change

Before:
```
string pdfFilePath = Path.Combine(drawingSubDirectory,
    string.Format("{0}{1}.pdf", borderLabel, StyleSuffixes[styleIndex]));
```

After:
```
string pdfFilePath = ExportPathBuilder.BuildPdfPath(
    drawingSubDirectory, StyleSubFolders[styleIndex], borderLabel);
```

---

## Output Path Structure

```
<ExportRoot>/
└── <relative-path-from-DownloadRoot>/
    └── <DrawingFileName>/          ← drawingSubDirectory
        ├── Color-PDF/
        │   ├── <FloorPlanName1>.pdf
        │   └── <FloorPlanName2>.pdf
        └── BW-PDF/
            ├── <FloorPlanName1>.pdf
            └── <FloorPlanName2>.pdf
```

Where `<FloorPlanName>` is the sanitized floor plan text label with no style suffix.
