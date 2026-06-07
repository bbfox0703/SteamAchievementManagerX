# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> For detailed specifications, refer to files in the `docs/` directory.

## Project Overview

Steam Achievement Manager X (SAM.X) is a fork of gibbed/SteamAchievementManager, upgraded to .NET 10 with x64 architecture. It allows users to unlock/lock Steam achievements and modify game statistics through direct Steam API integration.

The solution consists of 4 main projects + 2 test projects: **SAM.API** (Steam wrapper), **SAM.Picker** (game selector), **SAM.Game** (achievement manager), **SAM.WinForms** (shared UI theming).

## Build Commands

**Build entire solution:**
```bash
dotnet build SAM.sln -c Debug -p:Platform=x64
dotnet build SAM.sln -c Release -p:Platform=x64
```

**Build specific project:**
```bash
dotnet build SAM.Picker/SAM.Picker.csproj -c Debug -p:Platform=x64
dotnet build SAM.Game/SAM.Game.csproj -c Debug -p:Platform=x64
```

**Run tests:**
```bash
# Whole solution (recommended)
dotnet test --solution SAM.sln -c Release -p:Platform=x64

# Single project
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj -p:Platform=x64
dotnet test SAM.Game.Tests/SAM.Game.Tests.csproj -p:Platform=x64
```

**Run single test:**
```bash
# Tests run under Microsoft.Testing.Platform (MTP), not VSTest. The legacy
# `--filter "FullyQualifiedName~Name"` syntax does NOT work. Pass xunit.v3's
# native filter args after `--`:
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj -p:Platform=x64 -- --filter-method "*TestName*"
# Other selectors: --filter-class "*ClassName*", --filter-namespace "*Ns*"
```

**Note on test tooling:** `dotnet test` runs in MTP mode, enabled by the repo-root
`global.json` (`test.runner = Microsoft.Testing.Platform`). On the .NET 10 SDK the
legacy VSTest path is gone. Do NOT add `--nologo` (a VSTest-only flag that MTP
forwards to the test app and breaks the run). The test projects rely on `xunit.v3`
to bring `Microsoft.Testing.Platform` transitively (1.9.1) — do not re-add explicit
`Microsoft.Testing.Platform*` or `Microsoft.NET.Test.Sdk` references, which pin an
incompatible MTP v2 and cause `MissingMethodException` on `IOutputDevice.DisplayAsync`.

**Output locations:**
- Debug builds: `bin/` directory
- Release builds: `upload/` directory

**Note:** Solution is x64-only. Always use `-p:Platform=x64` when building.

## Critical Files Reference

| File | Purpose |
|------|---------|
| `SAM.API\Client.cs` | Steam client lifecycle management |
| `SAM.API\Steam.cs` | Native DLL loading (absolute path from HKLM `Software\Valve\Steam`, scoped via `AddDllDirectory` + `LoadLibrarySearchUserDirs`; no Authenticode/signature check) |
| `SAM.API\NativeWrapper.cs` | Base class for Steam API wrappers |
| `SAM.Game\KeyValue.cs` | VDF binary parser (critical for schema reading) |
| `SAM.Picker\GamePicker.cs` | Game selection UI (1200+ lines) |
| `SAM.Game\Manager.cs` | Achievement manager UI (1500+ lines) |
| `SAM.Game\Stats\AchievementInfo.cs` | Runtime achievement state |
| `SAM.Game\Stats\AchievementDefinition.cs` | Schema definition |
| `SAM.WinForms\ThemeHelper.cs` | Windows 11-aware theme engine |
| `SAM.Picker\ImageUrlValidator.cs` | URL sanitization |

## Common Pitfalls

1. **Platform Target**: Always build with x64. Project does not support AnyCPU or x86.
2. **Steam Must Be Running**: Both SAM.Picker and SAM.Game require Steam to be running and logged in.
3. **AppID Context**: SAM.Game MUST be launched with AppID parameter. Don't run directly—use SAM.Picker.
4. **VDF Binary vs Text**: Steam's VDF files come in two formats. This codebase uses binary format for schemas.
5. **Achievement Permissions**: Some achievements are server-authoritative and cannot be unlocked via SAM.
6. **Callback Timing**: Steam callbacks are async. Always check `IsValid` on callback data before use.
7. **Path Separators**: Use `Path.Combine()` for cross-platform compatibility, even though this is Windows-only (future-proofing).
8. **Unsafe Code**: Always enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` when working with SAM.API.

## docs/ Directory

| File | Contents |
|------|---------|
| `docs/architecture.md` | Project structure, Steam API integration, achievement flow, caching, countdown timer |
| `docs/technical-details.md` | VDF parsing, security measures, theme system, unsafe code |
| `docs/development-guide.md` | UI changes, adding Steam API features, testing, debugging |
| `docs/changelog.md` | Code quality history, known issues, recommended next steps |
| `docs/project-dependency-flow.svg` | Visual dependency diagram |
