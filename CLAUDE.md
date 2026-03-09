# CadParsing Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-26

## Active Technologies
- C# / .NET Framework 4.8 + AutoCAD 2023 SDK (plugin project only), NUnit 3.x (test project only) (002-fix-floor-plan-name)
- N/A (DWG files read; PDF files written via AutoCAD plot engine) (002-fix-floor-plan-name)
- C# / .NET Framework 4.8 + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll), (004-black-text-pdf)
- N/A — DWG is read via AutoCAD SDK; PDF is written via AutoCAD plot engine; no (004-black-text-pdf)
- C# / .NET Framework 4.8 + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only; `CadParsing.Core` and `CadParsing.Tests` have no AutoCAD SDK dependency (005-split-export-folders)
- File system — PDF files written via AutoCAD plot engine; directory creation via `System.IO` (005-split-export-folders)
- `System.Runtime.Serialization.DataContractJsonSerializer` (JSON config), `System.IO` (file writes) (006-fix-text-font)
- N/A — all state is transient within a single AutoCAD transaction (007-fix-locked-layer)

- C# (.NET Framework 4.8) + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll), NUnit 3.x (test project only) (001-fix-dwg-export-bugs)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# (.NET Framework 4.8)

## Code Style

C# (.NET Framework 4.8): Follow standard conventions

## Recent Changes
- 007-fix-locked-layer: Added C# / .NET Framework 4.8 + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only
- 006-fix-text-font: Added C# / .NET Framework 4.8 + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only; `CadParsing.Core` and `CadParsing.Tests` have no AutoCAD SDK dependency
- 005-split-export-folders: Added C# / .NET Framework 4.8 + AutoCAD 2023 SDK (accoremgd.dll, acdbmgd.dll, acmgd.dll) — `CadParsing` project only; `CadParsing.Core` and `CadParsing.Tests` have no AutoCAD SDK dependency


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
