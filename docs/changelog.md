# Code Quality Changelog

## Known Issues & Backlog

### 🔴 Known Remaining Issues (Requires Fixing)

1. **God Classes** (MEDIUM PRIORITY)
   - `SAM.Game\Manager.cs` (1749 lines) - handles UI, network, Steam API, file I/O, timers
   - `SAM.Picker\GamePicker.cs` (1234 lines) - similar multiple responsibilities
   - Violates Single Responsibility Principle
   - Should be refactored into separate layers (UI, business logic, data access, network)

2. **HttpClient Instance Management** (MEDIUM PRIORITY)
   - Each form creates its own HttpClient instance
   - Should use IHttpClientFactory or singleton pattern
   - Current approach can lead to socket exhaustion

### 🟡 Recommended Next Steps

When resuming work on this codebase, prioritize in this order:

1. Consider refactoring God classes (long-term maintainability)
2. Implement IHttpClientFactory for HTTP client management

---

## 2025-12-31: Architecture Cleanup & Code Quality Improvements

### ✅ Completed Improvements

1. **x64 Architecture Enforcement**
   - Removed runtime architecture detection in `SAM.API\Steam.cs`
   - Hardcoded to only load `steamclient64.dll` (removed x86 fallback)
   - Hardcoded pointer format to X16 in `SAM.API\NativeWrapper.cs`
   - Ensures pure x64 design from API to UI layer

2. **Steamworks.NET Dependency Removal**
   - Removed cross-platform target framework from SAM.API (Windows-only now)
   - Removed Steamworks.NET package dependency
   - Eliminated all `#if WINDOWS / #else` conditional compilation
   - Deleted ~86 lines of cross-platform compatibility code
   - Reduced binary size by 314 KB (Steamworks.NET.dll)

3. **Test Project Dependencies Cleanup**
   - Removed 20+ unnecessary package references from SAM.Picker.Tests:
     - Linux runtime packages (debian, fedora, opensuse)
     - OpenSSL packages (not needed on Windows)
     - Outdated packages (System.Net.Http 4.3.4, System.Text.RegularExpressions 4.3.1)
     - Unused packages (Newtonsoft.Json, Microsoft.NETCore.Platforms)
   - Changed target framework to Windows-only (`net10.0-windows`)
   - Reduced package count from 26 to 4 essential packages only

4. **Exception Handling Improvements**
   - Added exception logging to empty catch blocks in:
     - `SAM.Picker\GamePicker.cs:682-686` (image validation failures)
     - `SAM.Game\Manager.cs:911-915` (image validation failures)
   - Now uses `DebugLogger.Log()` for proper error tracking
   - Helps debugging by recording why cache files are deleted

5. **Async/Sync Deadlock Prevention**
   - Fixed deadlock risk in `SAM.Picker\GameList.cs:42-57`
   - Fixed deadlock risk in `SAM.Picker\GamePicker.cs:726-729`
   - Wrapped async operations with `Task.Run()` to prevent thread pool starvation
   - Added `ConfigureAwait(false)` to avoid context capture
   - Eliminates UI freezing risk when downloading game lists and logos

6. **Memory Leak Prevention**
   - Added event handler cleanup in `SAM.Picker\GamePicker.Designer.cs:36-40`
   - Unsubscribes `_AppDataChangedCallback.OnRun` in Dispose() method
   - Ensures proper form garbage collection
   - Prevents memory accumulation when opening/closing multiple game pickers

7. **Magic Numbers Extraction**
   - Extracted magic numbers to named constants in 3 files:
     - `SAM.Picker\GameList.cs`: `CacheExpirationMinutes` (30), `StreamReadBufferSize` (81920)
     - `SAM.Picker\GamePicker.cs`: `HttpClientTimeoutSeconds` (30)
     - `SAM.Game\Manager.cs`: `HttpClientTimeoutSeconds` (30), `MaxTimerTextLength` (6), `MouseMoveDistance` (15), `MouseMoveDelayMs` (12)
   - Improves code readability and maintainability
   - Makes configuration values easier to find and modify

### 📊 Metrics After All Improvements

| Metric | Value |
|--------|-------|
| Code reduced | ~86 lines of conditional compilation removed |
| Binary size reduced | 314 KB (Steamworks.NET.dll removed) |
| Package dependencies reduced | 22 packages removed from test project |
| Build warnings | 0 (previously had 2 MSB3245/MSB3243 warnings) |
| Test pass rate | 100% (27/27 tests passing) |
| Critical issues fixed | 2/2 high-priority issues resolved (deadlock risk + memory leaks) |
| Exception handling | 4 empty catch blocks now properly log errors |
| Magic numbers extracted | 7 constants created across 3 files |
