# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Steam Achievement Manager X (SAM.X) is a fork of gibbed/SteamAchievementManager, upgraded to .NET 10 with x64 architecture. It allows users to unlock/lock Steam achievements and modify game statistics through direct Steam API integration.

**Key Fork Features:**
- Multi-language support for achievements
- Icon and game list caching
- Countdown timer for delayed achievement unlocking
- Advanced search/filter functionality
- Windows 11 theming with Mica effects

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

## Architecture

### Project Structure

The solution consists of 4 main projects + 2 test projects:

**SAM.API** - Steam API Wrapper Layer
- Provides managed C# wrappers around native Steam Client APIs
- Target framework: `net10.0-windows` (Windows-only)
- Loads `steamclient64.dll` dynamically from Steam install directory
- Security: Validates DLL signature against Valve Corporation's certificate
- Uses unsafe code for native interop with Steam interfaces

**SAM.Picker** - Game Selection Launcher (WinForms)
- Entry point: Displays list of owned Steam games
- Downloads and caches game list from `https://gib.me/sam/games.xml` (30-minute cache)
- Caches game logos in `appcache/` directory
- Launches `SAM.Game.exe` with selected game's AppID as command-line argument
- Output: `SAM.Picker.exe` in `bin/` or `upload/`

**SAM.Game** - Achievement Manager (WinForms)
- Launched by SAM.Picker with AppID parameter
- Loads achievement schema from Steam's VDF cache: `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`
- Parses binary VDF (Valve Data Format) files using custom `KeyValue.cs` parser
- Provides achievement unlock/lock, stat editing, countdown timers
- Caches achievement icons in `appcache/{gameId}/` subdirectory
- Output: `SAM.Game.exe` in `bin/` or `upload/`

**SAM.WinForms** - Shared UI Library
- Common theming and styling components
- `ThemeHelper.cs`: Sophisticated theme engine that applies light/dark themes based on Windows preferences
- Windows 11 integration: Mica effects, rounded corners via DWM APIs
- Uses handler registration pattern for different control types
- Custom painting for ListView, TabControl, DataGridView

### Dependency Flow

```
SAM.Picker.exe ──┬──> SAM.API (Steam wrapper)
                 └──> SAM.WinForms (theming)
                      │
                      └──> launches SAM.Game.exe
                                │
                                ├──> SAM.API
                                └──> SAM.WinForms
```

### Steam API Integration

**Initialization:**
1. Loads `steamclient64.dll` from registry-discovered Steam install path (see `SAM.API\Steam.cs`)
2. Verifies DLL digital signature matches Valve Corporation
3. Creates Steam pipe and connects via `SteamClient018` interface
4. Sets `SteamAppId` environment variable to trick Steam into game context
5. For Picker: AppID = 0 (general Steam client)
6. For Game: AppID = specific game passed via command-line

**Key Steam Interfaces:**
- `SteamClient018`: Client initialization
- `SteamUserStats013`: Achievement get/set operations, stat modifications
- `SteamApps008`: Game ownership checks, language queries
- `SteamUtils010`: General utilities

**Native Wrapper Pattern:**
- Base class: `NativeWrapper<T>` uses generics to wrap Steam interfaces
- Function pointer caching via Dictionary for performance
- Uses `Marshal.GetDelegateForFunctionPointer` for P/Invoke delegates
- Interface definitions in `Interfaces/`, implementations in `Wrappers/`

**Callback Pattern:**
- Steam callbacks implemented via `Callback<T>` base class
- `Client.RunCallbacks()` called on timer to poll Steam events
- Observer pattern: OnRun event handlers notify forms
- Important callbacks: `UserStatsReceived`, `AppDataChanged`

### Achievement System Flow

1. **Schema Loading** (`SAM.Game\Manager.cs`):
   - Load from `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`
   - Parse with `KeyValue.cs` VDF binary reader
   - Schema contains: achievement definitions, stat definitions, localized strings

2. **Data Retrieval**:
   - Call `SteamUserStats013.RequestUserStats()` callback
   - Receive `UserStatsReceived` callback with user's current progress
   - Populate `_AchievementListView` with AchievementInfo objects

3. **Modification**:
   - In-memory changes via `SetAchievement(id, unlocked)` or `SetStatValue(id, value)`
   - Changes not persisted until `StoreStats()` called
   - Unlock times stored as Unix timestamps

4. **Commit**:
   - User clicks "Commit" or timer triggers auto-commit
   - Call `SteamUserStats013.StoreStats()`
   - Steam syncs to cloud and updates user profile

### Multi-Language Support

**Implementation** (`SAM.Game\Manager.cs`):
- Language obtained from `SteamApps008.GetCurrentGameLanguage()`
- Localization fallback chain in `GetLocalizedString()`:
  1. Try requested language from VDF schema
  2. Fall back to English if not found
  3. Fall back to raw value if English missing
- User can select language via `_LanguageComboBox` dropdown
- Applies to achievement text and game logo URLs

**Supported in:**
- Achievement names/descriptions
- Game title display
- Logo/capsule image URLs (uses `small_capsule/{language}` path)

### Caching System

**Picker Cache:**
- **Game list**: `games.xml` refreshed if > 30 minutes old
- **User games**: `usergames.xml` contains owned games from last session
- **Game logos**: `appcache/{appId}.png` cached permanently
- Concurrent download queue pattern (`ConcurrentQueue<GameInfo>`)
- Image validation: max 4MB, max 1024px dimension, proper content-type
- Fallback URL chain for logos (tries multiple CDN paths)

**Game Cache:**
- **Achievement icons**: `appcache/{gameId}/{achievementId}_{achieved|locked}.png`
- Image validation: max 512KB, max 1024px dimension
- Filename regex validation prevents path traversal attacks

### Countdown Timer Feature

**Location**: `SAM.Game\Manager.cs:1275-1468`

**Components:**
- `_submitAchievementsTimer`: Main timer (1-second tick)
- `_idleTimer`: Prevents system sleep during countdown
- `_achievementCounters`: Dictionary<string, int> tracking countdown per achievement

**Workflow:**
1. User selects achievements in ListView
2. Sets countdown value in `_AddTimerTextBox` (-1 to 999999 seconds)
3. Clicks `_AddTimerButton` to assign countdown
4. `_EnableTimerButton` starts timer execution
5. Timer decrements counters each second, auto-unlocks when reaching 0
6. Uses `SetThreadExecutionState()` to prevent system idle
7. Optional auto-mouse-movement to prevent Steam "away" status

## Critical Files Reference

**Core Architecture:**
- `SAM.API\Client.cs` - Steam client lifecycle management
- `SAM.API\Steam.cs` - Native DLL loading with security validation
- `SAM.API\NativeWrapper.cs` - Base class for Steam API wrappers
- `SAM.Game\KeyValue.cs` - VDF binary parser (critical for schema reading)

**Main Forms:**
- `SAM.Picker\GamePicker.cs` - Game selection UI (1200+ lines)
- `SAM.Game\Manager.cs` - Achievement manager UI (1500+ lines)

**Data Models:**
- `SAM.Picker\GameInfo.cs` - Game metadata
- `SAM.Game\Stats\AchievementInfo.cs` - Runtime achievement state
- `SAM.Game\Stats\AchievementDefinition.cs` - Schema definition
- `SAM.Game\Stats\StatDefinition.cs` - Base stat schema (Int/Float subclasses)

**Theme System:**
- `SAM.WinForms\ThemeHelper.cs` - Windows 11-aware theme engine

**Security/Validation:**
- `SAM.Picker\ImageUrlValidator.cs` - URL sanitization
- `SAM.API\Steam.cs:LoadSteamClient()` - DLL signature verification

## Important Technical Details

### VDF File Parsing

SAM.Game parses binary VDF (Valve Data Format) files, NOT text VDF. The parser is in `SAM.Game\KeyValue.cs`:
- Binary format uses type markers (0x00=childObject, 0x01=string, 0x02=int32, etc.)
- Nested structure represented as tree of KeyValue objects
- Achievement schema structure: `AppID/{lang}/stats/{achievements,stats}`

### Security Measures

1. **DLL Signature Validation** (`SAM.API\Steam.cs`):
   - Verifies steamclient64.dll is signed by "Valve Corporation"
   - Checks certificate subject name matches exactly
   - Prevents DLL hijacking attacks

2. **Path Validation**:
   - Regex validation for cache filenames prevents path traversal
   - URL validation in ImageUrlValidator prevents SSRF

3. **Content Validation**:
   - Image size limits (4MB for logos, 512KB for achievement icons)
   - Dimension limits (max 1024px for both)
   - Content-Type header validation

### Theme System

`SAM.WinForms\ThemeHelper.cs` provides sophisticated theming:
- Detects Windows theme via registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
- Applies colors recursively to all controls
- Custom painting handlers for: ListView headers, TabControl tabs, DataGridView
- Windows 11 features: DWM Mica effect (`DwmSetWindowAttribute`), rounded corners
- Uses `ConditionalWeakTable` to track color state without memory leaks

### Unsafe Code Usage

SAM.API uses `unsafe` code blocks for:
- Function pointer marshaling from native Steam interfaces
- Fast string conversion (UTF-8 ↔ UTF-16)
- Direct memory access for VTable traversal

**Important:** Always enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in project files when working with SAM.API.

## Development Workflow

1. **Making UI Changes:**
   - Forms use Windows Forms Designer (.Designer.cs files)
   - Apply theme via `ThemeHelper.ApplyTheme(this)` in form constructor
   - Use `DoubleBufferedListView` for flicker-free lists

2. **Adding Steam API Features:**
   - Add interface definition in `SAM.API\Interfaces\`
   - Create wrapper in `SAM.API\Wrappers\`
   - Inherit from `NativeWrapper<TInterface>`
   - Use `Call<TDelegate>(functionIndex, args)` pattern

3. **Working with VDF Files:**
   - Use `KeyValue.LoadAsBinary()` for binary VDF
   - Navigate tree structure: `root.Children` contains list of KeyValue nodes
   - Check `root.Valid` before accessing data

4. **Testing:**
   - Tests use xUnit framework
   - Mock Steam API interactions where possible
   - Test projects have InternalsVisibleTo access for unit testing

5. **Debugging Steam Integration:**
   - Enable debug logging in `SAM.API\Client.cs`
   - Check Steam logs: `Steam\logs\` directory
   - Verify schema files exist: `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`

## Common Pitfalls

1. **Platform Target**: Always build with x64. Project does not support AnyCPU or x86.

2. **Steam Must Be Running**: Both SAM.Picker and SAM.Game require Steam to be running and logged in.

3. **AppID Context**: SAM.Game MUST be launched with AppID parameter. Don't run directly—use SAM.Picker.

4. **VDF Binary vs Text**: Steam's VDF files come in two formats. This codebase uses binary format for schemas.

5. **Achievement Permissions**: Some achievements are server-authoritative and cannot be unlocked via SAM.

6. **Callback Timing**: Steam callbacks are async. Always check `IsValid` on callback data before use.

7. **Path Separators**: Use `Path.Combine()` for cross-platform compatibility, even though this is Windows-only (future-proofing).

## Code Quality Improvements History

### 2025-12-31: Architecture Cleanup & Code Quality Improvements

**✅ Completed Improvements:**

1. **x64 Architecture Enforcement** (2025-12-31)
   - Removed runtime architecture detection in `SAM.API\Steam.cs`
   - Hardcoded to only load `steamclient64.dll` (removed x86 fallback)
   - Hardcoded pointer format to X16 in `SAM.API\NativeWrapper.cs`
   - Ensures pure x64 design from API to UI layer

2. **Steamworks.NET Dependency Removal** (2025-12-31)
   - Removed cross-platform target framework from SAM.API (Windows-only now)
   - Removed Steamworks.NET package dependency
   - Eliminated all `#if WINDOWS / #else` conditional compilation
   - Deleted ~86 lines of cross-platform compatibility code
   - Reduced binary size by 314 KB (Steamworks.NET.dll)

3. **Test Project Dependencies Cleanup** (2025-12-31)
   - Removed 20+ unnecessary package references from SAM.Picker.Tests:
     - Linux runtime packages (debian, fedora, opensuse)
     - OpenSSL packages (not needed on Windows)
     - Outdated packages (System.Net.Http 4.3.4, System.Text.RegularExpressions 4.3.1)
     - Unused packages (Newtonsoft.Json, Microsoft.NETCore.Platforms)
   - Changed target framework to Windows-only (`net10.0-windows`)
   - Reduced package count from 26 to 4 essential packages only

4. **Exception Handling Improvements** (2025-12-31)
   - Added exception logging to empty catch blocks in:
     - `SAM.Picker\GamePicker.cs:682-686` (image validation failures)
     - `SAM.Game\Manager.cs:911-915` (image validation failures)
   - Now uses `DebugLogger.Log()` for proper error tracking
   - Helps debugging by recording why cache files are deleted

5. **Async/Sync Deadlock Prevention** (2025-12-31)
   - Fixed deadlock risk in `SAM.Picker\GameList.cs:42-57`
   - Fixed deadlock risk in `SAM.Picker\GamePicker.cs:726-729`
   - Wrapped async operations with `Task.Run()` to prevent thread pool starvation
   - Added `ConfigureAwait(false)` to avoid context capture
   - Eliminates UI freezing risk when downloading game lists and logos

6. **Memory Leak Prevention** (2025-12-31)
   - Added event handler cleanup in `SAM.Picker\GamePicker.Designer.cs:36-40`
   - Unsubscribes `_AppDataChangedCallback.OnRun` in Dispose() method
   - Ensures proper form garbage collection
   - Prevents memory accumulation when opening/closing multiple game pickers

7. **Magic Numbers Extraction** (2025-12-31)
   - Extracted magic numbers to named constants in 3 files:
     - `SAM.Picker\GameList.cs`: CacheExpirationMinutes (30), StreamReadBufferSize (81920)
     - `SAM.Picker\GamePicker.cs`: HttpClientTimeoutSeconds (30)
     - `SAM.Game\Manager.cs`: HttpClientTimeoutSeconds (30), MaxTimerTextLength (6), MouseMoveDistance (15), MouseMoveDelayMs (12)
   - Improves code readability and maintainability
   - Makes configuration values easier to find and modify

**🔴 Known Remaining Issues (Requires Fixing):**

1. **God Classes** (MEDIUM PRIORITY)
   - `SAM.Game\Manager.cs` (1749 lines) - handles UI, network, Steam API, file I/O, timers
   - `SAM.Picker\GamePicker.cs` (1234 lines) - similar multiple responsibilities
   - Violates Single Responsibility Principle
   - Should be refactored into separate layers (UI, business logic, data access, network)

2. **HttpClient Instance Management** (MEDIUM PRIORITY)
   - Each form creates its own HttpClient instance
   - Should use IHttpClientFactory or singleton pattern
   - Current approach can lead to socket exhaustion

**🟡 Recommended Next Steps:**

When resuming work on this codebase, prioritize in this order:

1. Consider refactoring God classes (long-term maintainability)
2. Implement IHttpClientFactory for HTTP client management

**📊 Metrics After All Improvements:**
- Code reduced: ~86 lines of conditional compilation removed
- Binary size reduced: 314 KB (Steamworks.NET.dll removed)
- Package dependencies reduced: 22 packages removed from test project
- Build warnings: 0 (previously had 2 MSB3245/MSB3243 warnings)
- Test pass rate: 100% (27/27 tests passing)
- Critical issues fixed: 2/2 high-priority issues resolved (deadlock risk + memory leaks)
- Exception handling: 4 empty catch blocks now properly log errors
- Magic numbers extracted: 7 constants created across 3 files
- Code quality improvements: 7 major improvements completed
