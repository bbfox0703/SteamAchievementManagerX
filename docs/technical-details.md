# Technical Details

## VDF File Parsing

SAM.Game parses binary VDF (Valve Data Format) files, NOT text VDF. The parser is in `SAM.Game\KeyValue.cs`:
- Binary format uses type markers (0x00=childObject, 0x01=string, 0x02=int32, etc.)
- Nested structure represented as tree of KeyValue objects
- Achievement schema structure: `AppID/{lang}/stats/{achievements,stats}`

**Usage:**
```csharp
// Load binary VDF
var root = KeyValue.LoadAsBinary(filePath);
if (!root.Valid) return;

// Navigate tree structure
foreach (var child in root.Children) { ... }
```

## Security Measures

1. **DLL Signature Validation** (`SAM.API\Steam.cs`):
   - Verifies `steamclient64.dll` is signed by "Valve Corporation"
   - Checks certificate subject name matches exactly
   - Prevents DLL hijacking attacks

2. **Path Validation**:
   - Regex validation for cache filenames prevents path traversal
   - URL validation in `ImageUrlValidator` prevents SSRF

3. **Content Validation**:
   - Image size limits: 4MB for game logos, 512KB for achievement icons
   - Dimension limits: max 1024px for both
   - Content-Type header validation on all downloaded images

## Theme System

`SAM.WinForms\ThemeHelper.cs` provides Windows 11-aware theming:
- Detects Windows theme via registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
- Applies colors recursively to all controls
- Custom painting handlers for: ListView headers, TabControl tabs, DataGridView
- Windows 11 features: DWM Mica effect (`DwmSetWindowAttribute`), rounded corners
- Uses `ConditionalWeakTable` to track color state without memory leaks

**Applying theme to a new form:**
```csharp
// In form constructor
ThemeHelper.ApplyTheme(this);
```

## Unsafe Code Usage

SAM.API uses `unsafe` code blocks for:
- Function pointer marshaling from native Steam interfaces
- Fast string conversion (UTF-8 ↔ UTF-16)
- Direct memory access for VTable traversal

**Important:** Always enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in the `.csproj` when working with SAM.API.
