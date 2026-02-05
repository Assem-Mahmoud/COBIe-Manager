# COBIe Manager

A Revit plugin for COBie (Construction Operations Building Information Exchange) data management and exchange.

## Supported Revit Versions

- Revit 2023
- Revit 2024
- Revit 2025
- Revit 2026

## Features

- **Multi-version support** - Single codebase for Revit 2023-2026
- **MVVM Architecture** - Built with CommunityToolkit.Mvvm
- **Material Design UI** - Beautiful WPF interface out of the box
- **Dependency Injection** - Custom lightweight DI container
- **Logging** - File-based logging with automatic rotation
- **Pre-built Services** - Common Revit operations ready to use

## Quick Start

### 1. First-Time Setup

```powershell
# Copy Revit API DLLs from installations to local lib folder
.\setup-revit-libs.ps1
```

### 2. Build the Plugin

```powershell
# Build for Revit 2024 (default)
.\build.ps1

# Build for specific version
.\build.ps1 -RevitVersion 2025

# Build all versions
.\build.ps1 -All
```

### 3. Create Your First Feature

1. Copy the `Features/ExampleFeature` folder
2. Rename it to your feature name
3. Update namespaces and class names
4. Add a ribbon button in `App.cs`

See [CLAUDE.md](CLAUDE.md) for detailed instructions.

## Project Structure

```
COBIeManager/
├── Features/
│   └── ExampleFeature/         # Example feature template
├── Shared/                     # Shared infrastructure
│   ├── Services/               # Business logic services
│   ├── Interfaces/             # Service interfaces
│   ├── DependencyInjection/    # DI container
│   ├── Logging/                # FileLogger
│   └── Utils/                  # Utilities
├── App.cs                      # IExternalApplication entry point
├── setup-revit-libs.ps1        # Setup Revit API DLLs
└── build.ps1                   # Build script
```

## Development

### Requirements

- .NET Framework 4.8
- Visual Studio 2022
- One or more Revit installations (2023-2026)

### Build Configurations

- `Debug2023` / `Release2023`
- `Debug2024` / `Release2024`
- `Debug2025` / `Release2025`
- `Debug2026` / `Release2026`

### Debugging

1. Select the desired Debug configuration (e.g., `Debug2024`)
2. Set breakpoints
3. Press F5 - Visual Studio will launch the corresponding Revit version

## Installation

After building, the DLL is automatically copied to:
```
%APPDATA%\Autodesk\Revit\Addins\{Version}\COBIeManager\
```

Restart Revit to load the plugin.

## Documentation

- [CLAUDE.md](CLAUDE.md) - Comprehensive development guide for AI assistants
- See inline comments in `Features/ExampleFeature/` for code patterns

## Architecture

### MVVM Pattern

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";  // Auto-generates property

    [RelayCommand]
    private void DoSomething()  // Auto-generates command
    {
        StatusMessage = "Done!";
    }
}
```

### Service Access

```csharp
// Access any registered service via ServiceLocator
var logger = ServiceLocator.GetService<ILogger>();
var familyService = ServiceLocator.GetService<IFamilyService>();
```

### Async Support

```csharp
public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    RevitTask.Initialize();  // CRITICAL: Call first

    var task = YourAsyncMethod(uiDoc);
    task.Wait();

    return Result.Succeeded;
}
```

## Troubleshooting

### Build fails with "RevitAPI.dll not found"
Run `.\setup-revit-libs.ps1` to copy Revit DLLs.

### Plugin doesn't appear in Revit
1. Check DLL exists in: `%APPDATA%\Autodesk\Revit\Addins\{Version}\COBIeManager\`
2. Check manifest exists: `%APPDATA%\Autodesk\Revit\Addins\{Version}\COBIeManager.addin`
3. Check logs at: `%APPDATA%\COBIeManager\Logs\`

## License

Proprietary - Template for internal use.
