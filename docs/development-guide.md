# Development Guide

## Making UI Changes

- Forms use Windows Forms Designer (`.Designer.cs` files)
- Theme a form the way the existing forms' `UpdateColors()` does: set `BackColor`/`ForeColor` for the current Windows theme, call `ThemeHelper.ApplyTheme(this, this.BackColor, this.ForeColor)`, re-run it from a `SystemEvents.UserPreferenceChanged` handler, and unsubscribe that handler in `Dispose`
- Use `DoubleBufferedListView` for flicker-free lists

## Adding Steam API Features

1. Add interface definition in `SAM.API\Interfaces\`
2. Create wrapper in `SAM.API\Wrappers\`
3. Inherit from `NativeWrapper<TInterface>`
4. Invoke native functions through the vtable struct: `this.Call<TReturn, TDelegate>(this.Functions.Name, this.ObjectAddress, ...)`, or `Call<TDelegate>(...)` for void functions

## Working with VDF Files

- Use `KeyValue.LoadAsBinary()` for binary VDF
- Navigate tree structure: `root.Children` contains list of KeyValue nodes
- Check `root.Valid` before accessing data
- See `docs/technical-details.md` for VDF format details

## Testing

- Tests use xUnit framework
- No test needs a running Steam client and there is no mocking library: HTTP paths use hand-written `HttpMessageHandler` stubs (`GameListTests`, `ImageDownloaderTests`), VDF parsing reads schemas built in memory (`KeyValueTests`), and cache tests work in a per-test temp directory
- `SAM.API` exposes its internals to `SAM.Picker.Tests` via `InternalsVisibleTo`

Test commands (whole solution, single project, single test) live in `CLAUDE.md` -> Build Commands. Tests run under Microsoft.Testing.Platform, so single tests are selected with xunit.v3 filters after `--` (e.g. `-- --filter-method "*TestName*"`).

## Debugging Steam Integration

- Logging: `SAM.API\DebugLogger.cs` writes to `logs\sam_<yyyyMMdd>.log` beside the executable (`FileLoggingEnabled`, on by default). `DebugLogger.Log(...)` calls compile only in Debug builds; `LogWarning`/`LogError`/`LogAlways` also run in Release
- Check Steam logs: `Steam\logs\` directory
- Verify schema files exist: `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`
