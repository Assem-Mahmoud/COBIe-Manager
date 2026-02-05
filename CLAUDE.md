# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**COBIe Manager** - A Revit plugin for COBie data management and exchange.

**Tech Stack:**
- Framework: .NET Framework 4.8
- UI: WPF with Material Design theme
- Plugin Framework: Revit API 2023/2024/2025/2026
- MVVM: CommunityToolkit.Mvvm
- Testing: xUnit + Moq (configured but not yet implemented)
- DI Container: Custom lightweight ServiceCollection + ServiceLocator

## Quick Start for Common Tasks

**⚠️ IMPORTANT: First-time setup required!**
```powershell
# Run this ONCE to copy Revit API DLLs to local lib folder
.\setup-revit-libs.ps1
```

| Task | Command |
|------|---------|
| **Setup Revit API DLLs** | `.\setup-revit-libs.ps1` (run once, or after Revit updates) |
| **Build Release (Revit 2024)** | `.\build.ps1` |
| **Build Debug (Revit 2024)** | `.\build.ps1 Debug` |
| **Build for Revit 2023** | `.\build.ps1 Release 2023` |
| **Build for Revit 2025** | `.\build.ps1 Release 2025` |
| **Build for Revit 2026** | `.\build.ps1 Release 2026` |
| **Build all versions** | `.\build.ps1 -All` |
| **Run tests** | `dotnet test COBIeManager.Tests/COBIeManager.Tests.csproj` |
| **Open in Visual Studio** | `start "COBIe Manager.sln"` |

## Project Structure

```
COBIeManager/
├── Features/
│   └── ExampleFeature/         # Template/example feature
│       ├── Commands/           # IExternalCommand entry points
│       ├── ViewModels/         # MVVM ViewModels
│       ├── Views/              # WPF Views (XAML)
│       └── Models/             # Data models
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

## Creating a New Feature

### Step 1: Create Feature Folder Structure

```
Features/
└── YourFeature/
    ├── Commands/
    │   └── YourFeatureCommand.cs
    ├── ViewModels/
    │   └── YourFeatureViewModel.cs
    ├── Views/
    │   └── YourFeatureWindow.xaml
    │   └── YourFeatureWindow.xaml.cs
    └── Models/
        └── YourModel.cs
```

### Step 2: Implement the Command

Copy `Features/ExampleFeature/Commands/ExampleFeatureCommand.cs` and modify:
- Rename class to `YourFeatureCommand`
- Update namespace
- Implement your feature logic in `Execute()`
- **CRITICAL:** Call `RevitTask.Initialize()` first for async support

### Step 3: Create ViewModel

Copy `Features/ExampleFeature/ViewModels/ExampleFeatureViewModel.cs`:
- Inherit from `ObservableObject`
- Use `[ObservableProperty]` for properties
- Use `[RelayCommand]` for commands
- Access services via `ServiceLocator.GetService<T>()`

### Step 4: Create View

Copy `Features/ExampleFeature/Views/ExampleFeatureWindow.xaml`:
- Set up bindings to ViewModel
- Use Material Design components
- **IMPORTANT:** Initialize ViewModel in constructor AFTER `InitializeComponent()`

### Step 5: Register Ribbon Button in App.cs

```csharp
// In App.OnStartup(), after creating ribbon panel:
PushButtonData buttonData = new PushButtonData(
    "YourFeatureBtn",
    "Your Feature",
    assemblyPath,
    "COBIeManager.Features.YourFeature.Commands.YourFeatureCommand");

PushButton button = panel.AddItem(buttonData) as PushButton;
button.ToolTip = "Description of your feature";
```

## MVVM Architecture

**Pattern:** MVVM with CommunityToolkit.Mvvm

**ViewModel Pattern:**
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";  // Auto-generates StatusMessage property

    [RelayCommand]
    private void DoSomething()  // Auto-generates DoSomethingCommand property
    {
        StatusMessage = "Done!";
    }
}
```

**Critical: ViewModel Initialization in Views**

**❌ WRONG - Property initializer runs too early:**
```csharp
public MyWindow()
{
    InitializeComponent();
}
public MyViewModel ViewModel { get; } = new();  // Runs BEFORE ServiceLocator ready
```

**✅ CORRECT - Initialize in constructor:**
```csharp
public MyWindow()
{
    InitializeComponent();
    ViewModel = new MyViewModel();  // Runs AFTER ServiceLocator ready
    DataContext = ViewModel;
}
public MyViewModel ViewModel { get; }
```

## Core Services (Pre-registered in DI)

All services are registered in `App.OnStartup()` and accessible via `ServiceLocator`:

| Service | Purpose |
|---------|---------|
| `ILogger` | File-based logging to `%APPDATA%\COBIeManager\Logs\` |
| `IUnitConversionService` | Unit conversions between CAD and Revit |
| `ILevelService` | Manage Revit levels/stories |
| `IFamilyService` | Manage Revit family types and symbols |
| `IWarningSuppressionService` | Suppress non-critical Revit warnings |
| `IElementCreationService` | Create Revit elements (columns, beams, walls, etc.) |
| `IRevitOperationsFacade` | High-level facade combining all services |
| `Func<Document, ImportInstance, ICadGeometryService>` | Factory for CAD geometry service |

**Accessing Services:**
```csharp
// In ViewModel constructor
var facade = ServiceLocator.GetService<IRevitOperationsFacade>();
var logger = ServiceLocator.GetService<ILogger>();

// Or use specific services
var familyService = ServiceLocator.GetService<IFamilyService>();
```

## Async in Revit

**⚠️ CRITICAL: Initialize in Command.Execute()**
```csharp
public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    RevitTask.Initialize();  // MUST be called FIRST

    var task = YourAsyncMethod(commandData.Application.ActiveUIDocument);
    task.Wait();  // Block until complete

    return Result.Succeeded;
}
```

**Async Pattern:**
```csharp
private async Task YourAsyncMethod(UIDocument uiDoc)
{
    // Heavy work off Revit thread
    await Task.Run(() =>
    {
        // Computation here
    });

    // Revit API calls on main thread
    using (var t = new Transaction(doc, "Operation"))
    {
        t.Start();
        // Modify Revit document
        t.Commit();
    }
}
```

## Transaction Pattern

All Revit document modifications require a Transaction:

```csharp
using (var t = new Transaction(doc, "Operation Name"))
{
    t.Start();
    // Your modifications here
    t.Commit();
}
```

For warning suppression:
```csharp
var warningService = ServiceLocator.GetService<IWarningSuppressionService>();

using (var t = new Transaction(doc, "Operation Name"))
{
    warningService.EnableWarningSuppressionForTransaction(t);
    t.Start();
    // Your modifications here
    t.Commit();
}
```

## Multi-Version Compilation

The project supports Revit 2023-2026. Build configurations:
- `Debug2023` / `Release2023`
- `Debug2024` / `Release2024`
- `Debug2025` / `Release2025`
- `Debug2026` / `Release2026`

**Version-specific code:**
```csharp
#if REVIT_2024
    // Revit 2024 specific code
#elif REVIT_2025
    // Revit 2025 specific code
#else
    // Revit 2023/2026 code
#endif
```

## Common Issues

**Issue: ServiceLocator not initialized**
- Ensure ViewModels are created AFTER `App.OnStartup()` completes
- Never use property initializers for ViewModels that access ServiceLocator

**Issue: Build fails with missing Revit API**
- Run `.\setup-revit-libs.ps1` to copy Revit DLLs

**Issue: Plugin doesn't appear in Revit**
- Check DLL copied to: `%APPDATA%\Autodesk\Revit\Addins\{Version}\COBIeManager\`
- Check manifest exists: `%APPDATA%\Autodesk\Revit\Addins\{Version}\COBIeManager.addin`

## Naming Conventions

- **Commands:** `[Feature]Command.cs` (e.g., `ExampleFeatureCommand.cs`)
- **ViewModels:** `[Feature]ViewModel.cs` or `[Purpose]ViewModel.cs`
- **Views:** `[Feature]Window.xaml` or `[Purpose]View.xaml`
- **Models:** `[Entity]Model.cs` (e.g., `CadColumnModel.cs`)

## Documentation Files

- **CLAUDE.md** - This file (template guide)
- **README.md** - Project overview
