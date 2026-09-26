# Development Guide

## Making UI Changes

- Forms use Windows Forms Designer (`.Designer.cs` files)
- Apply theme via `ThemeHelper.ApplyTheme(this)` in form constructor
- Use `DoubleBufferedListView` for flicker-free lists

## Adding Steam API Features

1. Add interface definition in `SAM.API\Interfaces\`
2. Create wrapper in `SAM.API\Wrappers\`
3. Inherit from `NativeWrapper<TInterface>`
4. Use `Call<TDelegate>(functionIndex, args)` pattern

## Working with VDF Files

- Use `KeyValue.LoadAsBinary()` for binary VDF
- Navigate tree structure: `root.Children` contains list of KeyValue nodes
- Check `root.Valid` before accessing data
- See `docs/technical-details.md` for VDF format details

## Testing

- Tests use xUnit framework
- Mock Steam API interactions where possible
- Test projects have `InternalsVisibleTo` access for unit testing

Test commands (whole solution, single project, single test) live in `CLAUDE.md` -> Build Commands. Tests run under Microsoft.Testing.Platform, so single tests are selected with xunit.v3 filters after `--` (e.g. `-- --filter-method "*TestName*"`).

## Debugging Steam Integration

- Enable debug logging in `SAM.API\Client.cs`
- Check Steam logs: `Steam\logs\` directory
- Verify schema files exist: `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`
