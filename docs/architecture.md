# Architecture

## Project Structure

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

## Dependency Flow

See `docs/project-dependency-flow.svg` for a visual diagram.

```
SAM.Picker.exe ──┬──> SAM.API (Steam wrapper)
                 └──> SAM.WinForms (theming)
                      │
                      └──> launches SAM.Game.exe
                                │
                                ├──> SAM.API
                                └──> SAM.WinForms
```

## Steam API Integration

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

## Achievement System Flow

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

## Multi-Language Support

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

## Caching System

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

## Countdown Timer Feature

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
