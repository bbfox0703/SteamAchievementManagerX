# Steam Achievement Manager X (SAM.X)

A fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager), upgraded to .NET 10 with x64 architecture. SAM.X allows users to unlock/lock Steam achievements and modify game statistics through direct Steam API integration.

## Features

### Core Functionality
- **Achievement Management**: Unlock/lock Steam achievements for any owned game
- **Statistics Editor**: Modify game statistics (integers and floats)
- **Countdown Timer**: Set delayed achievement unlocking with configurable timers per achievement

### Enhanced Features
- **Multi-language Support**: View achievements in different languages with refresh capability
- **Advanced Search/Filter**: Search achievements by name or description
- **Icon & Game List Caching**: Faster loading with local cache in `appcache/` directory
- **Windows 11 Theming**: Dark/Light mode with Mica effects and rounded corners
- **Anti-Idle**: Prevents Steam from showing "Away" status during operation
- **Column Sorting**: Sort achievements by any column in the list view

## Requirements

- **Windows 10/11** (x64 only)
- **.NET 10 Runtime**
- **Steam Client** must be running and logged in

## Installation

Download the latest release from the [Releases](https://github.com/bbfox0703/SteamAchievementManagerX/releases) page, extract, and run `SAM.Picker.exe`.

## Building from Source

```bash
# Clone the repository
git clone https://github.com/bbfox0703/SteamAchievementManagerX.git
cd SteamAchievementManagerX

# Build (Debug)
dotnet build SAM.sln -c Debug -p:Platform=x64

# Build (Release)
dotnet build SAM.sln -c Release -p:Platform=x64

# Run tests
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj -p:Platform=x64
dotnet test SAM.Game.Tests/SAM.Game.Tests.csproj -p:Platform=x64
```

**Note:** This project is x64-only. Always use `-p:Platform=x64` when building.

## Usage

1. **Start Steam** and log in to your account
2. **Run `SAM.Picker.exe`** - displays your owned games
3. **Select a game** - launches the Achievement Manager
4. **Manage achievements**:
   - Check/uncheck to unlock/lock achievements
   - Use the countdown timer for delayed unlocking
   - Click "Commit" to save changes

## Architecture

```
SAM.Picker.exe ──┬──> SAM.API (Steam wrapper)
                 └──> SAM.WinForms (theming)
                      │
                      └──> launches SAM.Game.exe
                                │
                                ├──> SAM.API
                                └──> SAM.WinForms
```

| Project | Description |
|---------|-------------|
| **SAM.API** | Steam API wrapper with native interop |
| **SAM.Picker** | Game selection launcher (WinForms) |
| **SAM.Game** | Achievement manager UI (WinForms) |
| **SAM.WinForms** | Shared theming library |

## Key Modifications in This Fork

1. **x64 Architecture**: Windows 64-bit only
2. **.NET 10**: Latest runtime with performance improvements
3. **Multi-language Support**: Achievement text in multiple languages
4. **Countdown Timer**: Delayed achievement unlocking
5. **Advanced Search**: Filter achievements by name/description
6. **Icon/Game Caching**: Faster loading with local cache
7. **Windows 11 Theme**: Dark/Light mode with Mica effects
8. **Security Improvements**: DLL signature validation, path traversal prevention
9. **Code Quality**: Improved error handling and reduced vulnerabilities

---

## Disclaimer

1. **Source**
   This is a fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager). Many thanks to the original contributors for their excellent work.

2. **Relationship with the Original Project**
   This is an **independently maintained fork** and is **not officially affiliated** with the original project.
   I do not intend to submit any changes or pull requests back to the original repository.

3. **Purpose**
   The primary goal of this fork is to add features and functionalities that I personally find useful.
   Later, most code was rewritten via Claude Code CLI.

4. **License and Copyright**
   This project follows the same license as the original repository. For details, see the [LICENSE](https://github.com/bbfox0703/SteamAchievementManagerX?tab=License-1-ov-file#) file.

5. **Disclaimer**
   This software is provided "as-is" for educational and personal use only.
   It is the user's responsibility to comply with any applicable laws and terms of service.
   **Some achievements are server-authoritative and cannot be unlocked via SAM.**

## Attribution

Icons from:
* [Fugue Icons](https://p.yusukekamiyamane.com/)
* [Flaticon](https://www.flaticon.com/) (free license)
