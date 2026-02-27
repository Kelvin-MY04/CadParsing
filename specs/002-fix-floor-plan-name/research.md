# Research: Fix Floor Plan Name Search

**Branch**: `002-fix-floor-plan-name` | **Phase**: 0 | **Date**: 2026-02-27

---

## Topic 1: AutoCAD MText Formatting Codes — Stripping Strategy

### Problem

`MText.Contents` in the AutoCAD .NET API returns the raw internal string, which embeds
formatting instructions using a modified RTF-like syntax. Examples:

```
\fArial|b0|i0|c0|p34;LEVEL 1 PLAN
\H300;\CFLOOR PLAN 101
{\fArial|b1;Building A\P}Unit 2
```

If this raw string is used verbatim as a PDF filename, the output is unreadable.

### AutoCAD MText Format Code Reference

| Code Pattern | Meaning |
|---|---|
| `\P` | Paragraph break |
| `\~` | Non-breaking space |
| `\f<font>\|<flags>;` | Font change |
| `\H<value>;` or `\H<value>x;` | Text height override |
| `\W<value>;` | Width factor |
| `\A<value>;` | Alignment (0/1/2) |
| `\C<value>;` | AutoCAD color number |
| `\c<value>;` | True color (RGB) |
| `\T<value>;` | Tracking |
| `\Q<value>;` | Oblique angle |
| `\S<text>^<text>;` | Stacked text |
| `\o`, `\O`, `\l`, `\L`, `\k`, `\K` | Overline / underline / strikethrough on/off |
| `\p<tabstop>;` | Tab stop |
| `{`, `}` | Formatting group delimiters |

### Decision: Regex-Based Stripping Utility (No AutoCAD API)

**Chosen approach**: A dedicated `MTextFormatStripper` static class in `CadParsing.Core/Helpers/`
using two regular expressions and bracket removal.

**Algorithm** (three-pass):

1. Strip semicolon-terminated codes: `\\[A-Za-z*'][^;]*;` → remove
2. Strip single-character codes: `\\[A-Za-z~]` → replace with space (preserves word boundaries around `\P`)
3. Remove group brackets: `{` and `}` → remove
4. Collapse consecutive whitespace → single space, then Trim()

**Rationale**:

- No AutoCAD object required — fully unit-testable without an AutoCAD license.
- Placing the class in `CadParsing.Core` keeps it available to both the plugin and the test project (same pattern as `BoundsChecker` and `LayerNameMatcher`).
- The three-pass approach handles the full MText code surface area without a recursive parser.
- Regex compilation is not needed (patterns applied once per entity, not in a tight loop).

**Alternatives considered**:

| Alternative | Rejected because |
|---|---|
| `MText.ConvertFieldsToText()` | Resolves AutoCAD field expressions (date, filename) but does NOT strip format codes |
| Third-party RTF parser | Overkill — MText format codes are a subset of RTF; full RTF parser brings unnecessary dependency |
| Single omnibus regex | Hard to maintain; multi-pass is clearer and easier to extend |

---

## Topic 2: `MatchesTargetHeight` Disposal

### Decision: Remove the method entirely

**Rationale**: After removing the height-filter call from `FindFloorPlanNameInModelSpace`, the
`MatchesTargetHeight` method has no callers anywhere in the codebase (confirmed: the three test
classes — `ConfigLoaderTests`, `LayerNameMatcherTests`, `BoundsCheckerTests` — do not reference
`TextHelper`). Retaining it violates the constitution's "No Dead Code" gate (Code Quality
Standards). The method is removed.

**Risk**: None — no tests or production paths call it after the fix.

---

## Topic 3: Warning Log Format

### Decision: Use existing `Console.WriteLine` pattern with `[WARN]` prefix

**Rationale**: `TextHelper` currently logs errors with `Console.WriteLine("[ERROR] ...")`.
The warning log for the no-match case follows the same convention:
`Console.WriteLine("[WARN] TextHelper: No eligible TEX-layer text found inside border ...")`.
No new logging infrastructure is introduced; this matches the project's existing observability
approach.

---

## Summary: All Unknowns Resolved

| Unknown | Decision |
|---|---|
| MText format stripping strategy | Regex-based `MTextFormatStripper` in `CadParsing.Core/Helpers/` |
| `MatchesTargetHeight` fate | Removed (dead code after fix) |
| Warning log format | `Console.WriteLine("[WARN] ...")` matching existing convention |
| `AppConfig` field retention | `FloorPlanTextHeight` and `TextHeightTolerance` kept, not read |
