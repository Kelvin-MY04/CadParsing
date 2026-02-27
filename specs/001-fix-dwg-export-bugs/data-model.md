# Data Model: Fix DWG Export Bugs — Border Detection & Floor Plan Naming

**Branch**: `001-fix-dwg-export-bugs` | **Date**: 2026-02-26

---

## Entities

### AppConfig

Runtime configuration loaded from `cadparsing.config.json`. Replaces all hard-coded
values in `Constants.cs`. If the config file is absent or unreadable, the loader returns
an instance populated with the default values listed below.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `BorderLayerSuffix` | `string` | `"PAPER-EX"` | Case-insensitive suffix matched against entity layer names to identify border polylines |
| `TextLayerSuffix` | `string` | `"TEX"` | Case-insensitive suffix matched against entity layer names to identify floor plan name text |
| `FloorPlanTextHeight` | `double` | `400.0` | Exact text height (in drawing units) of the floor plan name label |
| `TextHeightTolerance` | `double` | `0.5` | Maximum allowed deviation from `FloorPlanTextHeight`; actual range is `[height - tol, height + tol]` |
| `AcceptClosedPolylinesOnly` | `bool` | `true` | When `true`, open polylines on the border layer are silently ignored |
| `DownloadRoot` | `string` | `C:\Users\pphyo\Downloads` | Absolute path to the root of the user's download directory; used to route output to the export directory |
| `ExportRoot` | `string` | `C:\Users\pphyo\Downloads\export\pdf` | Absolute path to the output directory for exported PDFs |

**Validation rules:**
- `BorderLayerSuffix` MUST be non-empty.
- `TextLayerSuffix` MUST be non-empty.
- `FloorPlanTextHeight` MUST be > 0.
- `TextHeightTolerance` MUST be ≥ 0.

**Lifecycle:** Loaded once at plugin startup (first command invocation). Cached in memory
for the session. Reload requires restarting AutoCAD or accoreconsole.exe.

---

### BorderCandidate

A transient in-memory entity representing a detected border polyline during one export
operation. Not persisted.

| Field | Type | Description |
|-------|------|-------------|
| `EntityId` | `ObjectId` | AutoCAD database handle for the border polyline entity |
| `BoundingBoxArea` | `double` | Area of the bounding box (width × height) in drawing units²; used to sort borders largest-first |

**Derivation:** Produced by `BorderHelper.FindBordersInModelSpace()`.
**Sort order:** Descending by `BoundingBoxArea` (largest border processed first).

---

### FloorPlanText

A transient in-memory entity representing a candidate text entity evaluated during floor
plan name extraction. Not persisted.

| Field | Type | Description |
|-------|------|-------------|
| `EntityId` | `ObjectId` | AutoCAD database handle for the TEXT or MTEXT entity |
| `InsertionPoint` | `Point3d` | The text's placement anchor (`DBText.Position` or `MText.Location`) |
| `Height` | `double` | Text height in drawing units |
| `TextValue` | `string` | Raw string content of the text entity |

**Derivation:** Produced by `TextHelper.FindFloorPlanNameInModelSpace()`.
**Selection rule:** The entity with `|height - FloorPlanTextHeight| ≤ TextHeightTolerance`
and `InsertionPoint` inside the border's bounding box.

---

## Relationships

```
AppConfig ─── loaded by ──► ConfigLoader (singleton per process)
    │
    ├── BorderLayerSuffix ──► LayerNameMatcher ──► BorderHelper (FindBordersInModelSpace)
    ├── TextLayerSuffix ────► LayerNameMatcher ──► TextHelper   (FindFloorPlanNameInModelSpace)
    ├── FloorPlanTextHeight ► TextHelper
    ├── TextHeightTolerance ► TextHelper
    └── AcceptClosedPolylinesOnly ► BorderHelper

BorderCandidate (1) ─── has ──► (1) FloorPlanText
    (one border maps to exactly one floor plan name in valid DWG files)

BorderCandidate (1) ─── produces ──► (2) PDF files
    (one _color.pdf + one _bw.pdf per border)
```

---

## State Transitions

### Config Load State

```
Unloaded ──► [DLL loads / first command call] ──► Loading
Loading ──► [file found, parsed successfully] ──► Loaded (user values)
Loading ──► [file missing or parse error] ──► Loaded (defaults) + warning logged
```

### Border Processing State (per DWG file)

```
Start ──► Iterate model space ──► Collect closed *PAPER-EX polylines
       ──► Sort by area desc ──► For each border:
           ──► Find floor plan name (insertion point in bounds)
           ──► Sanitize name
           ──► Export color PDF
           ──► Export B/W PDF
       ──► Done
```

---

## Config File Location

The config file is located by resolving the directory of the executing plugin assembly:

```
Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    + Path.DirectorySeparatorChar
    + "cadparsing.config.json"
```

This ensures the config file travels with the DLL regardless of the AutoCAD installation
path.
