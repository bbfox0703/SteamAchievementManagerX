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
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj -p:Platform=x64
dotnet test SAM.Game.Tests/SAM.Game.Tests.csproj -p:Platform=x64
```

**Run single test:**
```bash
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj --filter "FullyQualifiedName~TestName" -p:Platform=x64
```

**Output locations:**
- Debug builds: `bin/` directory
- Release builds: `upload/` directory

**Note:** Solution is x64-only. Always use `-p:Platform=x64` when building.

## Critical Files Reference

| File | Purpose |
|------|---------|
| `SAM.API\Client.cs` | Steam client lifecycle management |
| `SAM.API\Steam.cs` | Native DLL loading with security validation |
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
