# Research: Black Text in Color PDF Export

**Branch**: `004-black-text-pdf` | **Date**: 2026-02-27

## Decision 1 — What color value represents "black" in AutoCAD SDK?

**Decision**: `Color.FromRgb(0, 0, 0)` (explicit RGB black)

**Rationale**: ACI color 7 (the AutoCAD "foreground" color) renders as black on a white
background but as white on a dark background. For PDF export this ambiguity is harmless because
the plot background is always white, but `Color.FromRgb(0, 0, 0)` is unambiguous: it is
always true black regardless of the plot background color or CTB color table. The spec
explicitly lists "explicit RGB black" as an acceptable definition, making this the safer choice.

**Alternatives Considered**:
- `Color.FromColorIndex(ColorMethod.ByAci, 7)` — conventional AutoCAD "foreground" color;
  background-dependent; rejected for ambiguity.

---

## Decision 2 — Which entity types qualify as text-bearing?

**Decision**: `DBText`, `MText`, `Dimension` (base class), `Leader`, `MLeader`

**Rationale**: `Dimension` is the abstract base class for all dimension subtypes in the
AutoCAD .NET API (AlignedDimension, RotatedDimension, RadialDimension, RadialDimensionLarge,
DiametricDimension, Point3AngularDimension, LineAngularDimension2, OrdinateDimension). A
single `is Dimension` check therefore covers all current and future dimension variants without
enumeration. `Leader` and `MLeader` display annotation text and are included per FR-001.

**Alternatives Considered**:
- Enumerate every specific Dimension subtype — more fragile; breaks when new dimension types
  are added; rejected in favour of the base-class check.
- Restrict to DBText and MText only — does not satisfy FR-001 (dimensions excluded); rejected.

---

## Decision 3 — How to traverse text entities inside nested blocks and xrefs?

**Decision**: Open each `BlockReference`'s `BlockTableRecord` via the open transaction and
recurse, tracking visited block-definition ObjectIds in a `HashSet<ObjectId>` to prevent
infinite recursion.

**Rationale**: This traverses the actual database objects (not exploded copies), so each
entity's `ObjectId` is stable and can later be opened `ForWrite` to apply the color override.
`ExplodeHelper.ExplodeRecursive` uses `entity.Explode()` which creates in-memory copies
— those copies have no persistent ObjectId and cannot be used for the write-back override.
Block definitions in external xrefs are loaded into the host database under the same API;
traversal is identical to regular blocks.

**Alternatives Considered**:
- `entity.Explode()` via ExplodeHelper — produces in-memory copies without persistent
  ObjectIds; cannot be used to write overrides back to the database; rejected.
- AutoCAD `SelectionSet` with type filter — more complex setup; inconsistent with existing
  direct-iteration pattern; rejected.

---

## Decision 4 — Transaction scope for override and restore?

**Decision**: Apply the override and restore within the same `Transaction` already open in
`ExportAllBorders`. No sub-transactions are used.

**Rationale**: Changes made to entities within an open AutoCAD transaction are visible to
the plot engine during the same AutoCAD session before `Commit()` is called. Using the
existing transaction avoids sub-transaction overhead and means `transaction.Abort()` on any
unhandled failure automatically reverts all color changes — providing natural rollback at
no extra cost. The transaction is committed only after all borders have been exported and all
colors restored, so no modified colors ever reach the on-disk DWG.

**Alternatives Considered**:
- Nested (sub-) transactions — added complexity with no correctness or safety benefit;
  rejected.
- Close and reopen the transaction around each border — breaks the existing
  `ExportAllBorders` commit model and risks data loss if the reopen fails; rejected.

---

## Decision 5 — Per-border or global override scope?

**Decision**: Per-border — apply override immediately before each border's color PDF plot
and restore immediately after, inside a try/finally block.

**Rationale**: The spec explicitly states: "The override is applied per-border immediately
before that border's color PDF plot, and restored immediately after." This prevents any
color bleed between borders (relevant when multiple borders share a DWG and are exported
in a single command run).

**Alternatives Considered**:
- Single global override before all borders, restore after all — simpler but violates the
  spec's atomicity guarantee per border; rejected.

---

## Decision 6 — How to handle entities that cannot be opened ForWrite?

**Decision**: Wrap each `transaction.GetObject(id, OpenMode.ForWrite)` in try-catch. On
exception, log a warning using `editor.WriteMessage("[WARN] ...")` identifying the entity
ObjectId and the exception message, then continue. Do not abort the export.

**Rationale**: FR-006 requires exactly this behavior — log and continue rather than abort.
The `editor.WriteMessage` pattern is consistent with all existing warning/error logging in
the codebase (no separate Logger class exists; logging is done directly via the editor).

**Alternatives Considered**:
- Check `entity.IsReadOnly` before attempting write — might not catch all cases (e.g.,
  xref entities); rejected in favour of the universal try-catch.
- Abort export on any ForWrite failure — violates FR-006; rejected.

---

## Decision 7 — Where to detect the color variant vs. B/W variant?

**Decision**: Add a private named constant `ColorStyleSheet = "acad.ctb"` and a private
method `IsColorExport(string styleSheet)` to `ExportPdfCommand`. The existing magic strings
`"acad.ctb"`, `"monochrome.ctb"`, `"_color"`, `"_bw"` are all promoted to named constants
to satisfy the DRY and no-magic-values constitution requirements.

**Rationale**: The style loop already iterates `StyleSheets[]` by index. A named method
`IsColorExport` makes the intent explicit without depending on a fragile array index (0 =
color, 1 = B/W).

**Alternatives Considered**:
- Use `styleIndex == 0` as the color check — depends on undocumented array ordering; rejected.
- Runtime config flag — adds unnecessary configurability for a fixed architectural property;
  rejected.

---

## Decision 8 — Testing strategy given AutoCAD entity constraints?

**Decision**: No new NUnit unit tests (all new logic is AutoCAD-dependent). Integration and
E2E validation via `accoreconsole.exe` against real DWG test files. Regression covered by
the existing 22-test suite in CadParsing.Tests (all pass; must continue to pass).

**Rationale**: `DBText`, `MText`, `Dimension`, `Leader`, `MLeader`, and all related AutoCAD
types are COM-backed and can only be instantiated within a live AutoCAD process. The project
has established the pattern (from features 001 and 002) of not mocking AutoCAD objects and
testing purely AutoCAD-dependent code via real DWG files through `accoreconsole.exe`.

**Alternatives Considered**:
- Wrapper interfaces + fakes for every AutoCAD entity type — would require a parallel
  interface hierarchy across the entire plugin; complexity far exceeds benefit for a
  two-class feature; rejected.
- Roslyn-based static analysis tests — validates structure but not runtime behavior; not
  sufficient as a substitute for behavioral tests; rejected as primary strategy.
