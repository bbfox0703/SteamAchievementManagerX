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

**Run all tests:**
```bash
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj -p:Platform=x64
dotnet test SAM.Game.Tests/SAM.Game.Tests.csproj -p:Platform=x64
```

**Run a single test by name:**
```bash
dotnet test SAM.Picker.Tests/SAM.Picker.Tests.csproj --filter "FullyQualifiedName~TestName" -p:Platform=x64
```

## Debugging Steam Integration

- Enable debug logging in `SAM.API\Client.cs`
- Check Steam logs: `Steam\logs\` directory
- Verify schema files exist: `Steam\appcache\stats\UserGameStatsSchema_{appId}.bin`
