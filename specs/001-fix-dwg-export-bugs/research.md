# Research: Fix DWG Export Bugs — Border Detection & Floor Plan Naming

**Branch**: `001-fix-dwg-export-bugs` | **Date**: 2026-02-26

---

## Bug 1 Root Cause — Border Detection via `editor.SelectAll()`

### Finding

`Editor.SelectAll(SelectionFilter)` in the AutoCAD .NET API searches only the **currently
active space** — whichever space (model space or a paper-space layout) the user's viewport
is currently set to. This is documented AutoCAD behavior.

When `accoreconsole.exe` opens a DWG file via a script, the active space at the point the
`EXPORTPDF` command fires depends on the DWG file's saved state. DWG files that were last
saved while a paper-space layout was active will open in that paper-space layout. In that
state, `editor.SelectAll()` searches the paper-space layout block, not model space. Border
polylines on `*PAPER-EX` layers in model space are therefore invisible to the query.

Additionally, when entities exist inside Xref block references (not directly in model
space), `SelectAll` cannot reach them either.

### Decision

**Replace `editor.SelectAll()` with direct model-space `BlockTableRecord` iteration.**

```
db.BlockTableId → BlockTable → BlockTableRecord.ModelSpace → foreach ObjectId
```

This approach:
- Always iterates model-space entities regardless of current viewport or active space
- Does not require `Editor` to be in any particular state
- Works identically in the AutoCAD GUI and in `accoreconsole.exe` headless mode
- Allows precise, code-controlled layer name matching (case-insensitive `EndsWith`)

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| `editor.SelectAll()` with a corrected SelectionFilter | Still space-dependent; does not solve the headless accoreconsole issue |
| Switch to model space before querying (`db.TileMode = true`) | Modifies drawing state during an export command; risky side-effect |
| Use `editor.Command("MODEL")` to force model space | Intrusive; changes user's viewport state; not safe in headless mode |

---

## Bug 2 Root Cause — Floor Plan Name via `editor.SelectCrossingWindow()`

### Finding

`Editor.SelectCrossingWindow(Point3d min, Point3d max, SelectionFilter)` selects all
entities whose **bounding box** crosses or is contained within the specified window — not
entities whose insertion point is within the window. This creates two failure modes:

1. **Over-selection**: Text from an adjacent border whose bounding box extends into the
   current border's bounding box region is selected. The height filter (`400 ± 0.5`) then
   picks whichever matching text appears first, which may be the wrong floor plan name.

2. **Space dependency**: Identical space-dependency issue as `SelectAll` — the call is
   context-sensitive to the active viewport.

The `SelectCrossingWindow` method is appropriate for interactive selection but is
unreliable for programmatic entity lookup where an exact containment rule (insertion point
inside polygon) is required.

### Decision

**Replace `editor.SelectCrossingWindow()` with direct model-space database iteration,
followed by an insertion-point containment check.**

- Extract insertion point: `DBText.Position` or `MText.Location`
- Containment check: `point.X ∈ [extents.MinPoint.X, extents.MaxPoint.X]` and
  `point.Y ∈ [extents.MinPoint.Y, extents.MaxPoint.Y]`
- Since floor-plan borders are rectangular axis-aligned polylines, the bounding-box
  containment test is geometrically equivalent to a polygon containment test.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Keep `SelectCrossingWindow`, post-filter by insertion point | Still space-dependent; two-pass approach adds complexity for no gain |
| Full polygon point-in-polygon (ray casting) | Over-engineered for rectangular borders; can be added later if non-rectangular borders appear |
| `editor.SelectFence()` or `editor.SelectWindow()` | `SelectWindow` (inside only) is closer to desired behavior, but still space-dependent |

---

## Config File — Deserialization Approach

### Finding

The plugin targets .NET Framework 4.8. Available deserialization options without adding
NuGet packages to the production project:

| Option | Pros | Cons |
|--------|------|------|
| `DataContractJsonSerializer` (built-in) | Zero deps; part of .NET FX 4.8 | Verbose; requires `[DataContract]`/`[DataMember]` attributes |
| `System.Configuration.ConfigurationManager` (XML `.config`) | Standard; intellisense support | Reads from AutoCAD.exe.config, not plugin DLL's config; path resolution is complex |
| `Newtonsoft.Json` via NuGet | Simplest API | AutoCAD 2023 ships its own Newtonsoft version; version conflict risk in the plugin host |
| Manual string parsing | Zero deps | Brittle; no schema validation |

### Decision

**Use `DataContractJsonSerializer`** (built-in `System.Runtime.Serialization.Json`).

- Config class annotated with `[DataContract]` / `[DataMember]`
- `ConfigLoader` reads `cadparsing.config.json` from the same directory as the plugin DLL
- On any IO or deserialization failure, `ConfigLoader` logs a warning and returns a
  default `AppConfig` instance (identical to the current hard-coded `Constants.cs` values)
- Config is loaded once per session and cached as a singleton

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| XML app.config | Reads from AutoCAD.exe.config, not plugin directory; path resolution fragile |
| Newtonsoft.Json NuGet | Dependency conflict risk with AutoCAD host's Newtonsoft version |

---

## Test Strategy

### Finding

AutoCAD SDK types (`Database`, `Editor`, `Transaction`, `DBText`, etc.) require an
active AutoCAD runtime to instantiate. They cannot be unit-tested without the full
AutoCAD process running. However, the new helper classes (`LayerNameMatcher`,
`BoundsChecker`, `ConfigLoader`) contain pure logic with no AutoCAD SDK dependency.

### Decision

**Two-tier testing strategy:**

1. **Unit tests** (no AutoCAD dependency): `CadParsing.Tests` project targets `net48`
   and references only NUnit 3.x. Tests cover:
   - `LayerNameMatcher.MatchesLayerSuffix()` — all casing, Xref-prefix, empty-input cases
   - `BoundsChecker.IsInsideBounds()` — boundary values, outside points, MX/MY corners
   - `ConfigLoader.LoadFromFile()` — valid JSON, missing file, malformed JSON, defaults

2. **End-to-end tests** (AutoCAD required): Bash script invokes `accoreconsole.exe` with
   a known test DWG file and verifies PDF output exists with the expected floor plan name.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Mock AutoCAD SDK interfaces | SDK classes are sealed/COM-backed; mocking is impractical |
| Integration tests in a separate AutoCAD LISP | Different language; out of scope |

---

## Layer Name Matching Strategy

### Finding

AutoCAD layer names for Xref-attached layers follow the pattern `XrefName|LayerName`.
After Xref binding, the separator changes to `$0$`. Both variants must be handled by the
layer suffix check.

- `A-PAPER-EX` → `EndsWith("PAPER-EX", OrdinalIgnoreCase)` = `true` ✅
- `XREF|FLOOR-PAPER-EX` → `EndsWith("PAPER-EX", OrdinalIgnoreCase)` = `true` ✅
- `BOUND$0$FLOOR-PAPER-EX` → `EndsWith("PAPER-EX", OrdinalIgnoreCase)` = `true` ✅
- `A-PAPER_EX` → `EndsWith("PAPER-EX", OrdinalIgnoreCase)` = `false` ✅ (correctly rejected)

### Decision

**Use `string.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)` in `LayerNameMatcher`.**
This is the canonical approach: readable, DRY, and handles all naming variants.
