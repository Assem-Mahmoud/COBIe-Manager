# Revit Add-in Quick Reference Guide

**Fast-path reference for common development tasks**

---

## First-Time Setup

```powershell
# Run ONCE to copy Revit API DLLs
.\setup-revit-libs.ps1

# Build project
.\build.ps1 Release 2024
```

---

## Common Commands

| Task | Command |
|------|---------|
| Build Release (Revit 2024) | `.\build.ps1` |
| Build Debug | `.\build.ps1 Debug` |
| Build for Revit 2025 | `.\build.ps1 Release 2025` |
| Build all versions | `.\build.ps1 -All` |
| Clean build | `.\build.ps1 -Clean` |
| Run tests | `dotnet test` |
| Open in VS | `start YourProject.sln` |

---

## Code Templates

### IExternalCommand Template

```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;

namespace YourProject.Features.YourFeature.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class YourFeatureCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // CRITICAL: Initialize FIRST
            RevitTask.Initialize(commandData.Application);

            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Create and show window
                var window = new YourView();
                var vm = window.DataContext as YourViewModel;
                vm.Initialize(doc);
                window.Show();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

### ViewModel Template

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevitCadConverter.Shared.DependencyInjection;
using RevitCadConverter.Shared.Logging;
using System.Windows;

namespace YourProject.Features.YourFeature.ViewModels
{
    public partial class YourViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly IYourService _service;

        [ObservableProperty]
        private string _title;

        public YourViewModel()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _service = ServiceLocator.GetService<IYourService>();
            _logger.Info($"{nameof(YourViewModel)} initialized");
        }

        [RelayCommand]
        private void ExecuteAction()
        {
            try
            {
                _logger.Info("Executing action...");
                // Your logic here
                _logger.Info("Action completed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Action failed: {ex.Message}", ex);
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Reset()
        {
            // Clear all state for CAD file change
            Title = string.Empty;
        }
    }
}
```

### Service Interface Template

```csharp
using Autodesk.Revit.DB;

namespace YourProject.Shared.Interfaces
{
    /// <summary>
    /// Service for [describe purpose].
    /// </summary>
    public interface IYourService
    {
        /// <summary>
        /// [Brief description].
        /// </summary>
        /// <param name="doc">The Revit document.</param>
        /// <returns>[Return value description].</returns>
        Result DoSomething(Document doc);
    }
}
```

### Service Implementation Template

```csharp
using YourProject.Shared.Interfaces;
using YourProject.Shared.Logging;
using Autodesk.Revit.DB;

namespace YourProject.Shared.Services
{
    public class YourService : IYourService
    {
        private readonly ILogger _logger;

        public YourService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info($"{nameof(YourService)} initialized");
        }

        public Result DoSomething(Document doc)
        {
            try
            {
                if (doc == null)
                    throw new ArgumentNullException(nameof(doc));

                _logger.Debug("Starting operation...");

                // Implementation here

                _logger.Info("Operation completed successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.Error($"Operation failed: {ex.Message}", ex);
                throw;
            }
        }
    }
}
```

### Transaction Template

```csharp
public void CreateElements(Document doc)
{
    using (var transaction = new Transaction(doc, "Create Elements"))
    {
        transaction.Start();

        try
        {
            // Create elements here
            var element = CreateElement(doc);

            transaction.Commit();
            _logger.Info("Elements created successfully");
        }
        catch (Exception ex)
        {
            if (transaction.GetStatus() == TransactionStatus.Started)
            {
                transaction.RollBack();
            }
            _logger.Error($"Failed: {ex.Message}", ex);
            throw;
        }
    }
}
```

---

## Service Registration (App.cs)

```csharp
private void InitializeDependencyInjection()
{
    var logger = new FileLogger();
    var services = new ServiceCollection();

    // Register singletons
    services.RegisterSingleton<ILogger>(logger);
    services.RegisterSingleton<IYourService>(new YourService(logger));

    // Initialize ServiceLocator
    ServiceLocator.Initialize(services.BuildServiceProvider());
}
```

---

## Ribbon Button Registration (App.cs)

```csharp
// Create ribbon tab
string tabName = "Your Tab";
try { app.CreateRibbonTab(tabName); } catch { }

RibbonPanel panel = app.CreateRibbonPanel(tabName, "Your Panel");
string assemblyPath = Assembly.GetExecutingAssembly().Location;

// Create button
PushButtonData buttonData = new PushButtonData(
    "YourBtnId",
    "Button\nText",
    assemblyPath,
    "YourProject.Features.YourFeature.Commands.YourFeatureCommand");

PushButton button = panel.AddItem(buttonData) as PushButton;
button.ToolTip = "Your button tooltip";
```

---

## Important Patterns

### ❌ WRONG: Property Initializer with DI

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ChildViewModel child = new();  // WRONG! Runs too early
}
```

### ✅ CORRECT: Initialize in Constructor

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ChildViewModel child;

    public MainViewModel()
    {
        // ServiceLocator is ready here
        Child = new ChildViewModel();
    }
}
```

---

## Async Pattern

```csharp
public async Task ProcessDataAsync(Document doc, DataSet data)
{
    // Heavy work OFF Revit thread
    var results = await Task.Run(() => HeavyProcessing(data));

    // Revit API calls ON Revit thread
    using (var t = new Transaction(doc, "Update"))
    {
        t.Start();
        foreach (var r in results)
        {
            CreateElement(doc, r);
        }
        t.Commit();
    }
}
```

---

## Logging Patterns

```csharp
// In constructor
_logger = ServiceLocator.GetService<ILogger>();

// Usage
_logger.Debug("Detailed diagnostic info");
_logger.Info("General information");
_logger.Warn("Warning message");
_logger.Error("Error occurred", exception);
_logger.Fatal("Critical failure", exception);
```

---

## Unit Conversion

```csharp
var unitService = ServiceLocator.GetService<IUnitConversionService>();

// To Revit internal units (feet)
double feet = unitService.ToInternalUnits(1000, UnitTypeId.Millimeters);

// From Revit internal units
double mm = unitService.FromInternalUnits(10, UnitTypeId.Millimeters);
```

---

## Level Operations

```csharp
var levelService = ServiceLocator.GetService<ILevelService>();

// Get all levels
var levels = levelService.GetLevels(doc);

// Get by name
Level level = levelService.GetLevelByName(doc, "Level 1");

// Get by elevation
Level level = levelService.GetLevelByElevation(doc, 10.0);
```

---

## Snackbar Notifications

```xml
<!-- In XAML -->
<materialDesign:Snackbar MessageQueue="{Binding SnackbarMessageQueue}" />
```

```csharp
// In ViewModel
public SnackbarMessageQueue SnackbarMessageQueue { get; } =
    new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

// Show message
SnackbarMessageQueue.Enqueue("Operation completed!");
SnackbarMessageQueue.Enqueue("Error occurred", "UNDO", _ => UndoAction());
```

---

## Version-Specific Code

```csharp
#if REVIT_2024
    // Revit 2024 only
#elif REVIT_2025
    // Revit 2025 only
#else
    // Default
#endif
```

---

## Adding New Revit Version (2027+)

1. Update `.csproj` Configurations: Add `Debug2027;Release2027`
2. Add RevitVersion property mapping
3. Add DefineConstants for `REVIT_2027`
4. Add `<ItemGroup>` with `lib\Revit2027\` references
5. Update `build.ps1` ValidateSet: Add `'2027'`
6. Update `setup-revit-libs.ps1`: Add Revit 2027 path
7. Run `.\setup-revit-libs.ps1`

---

## .gitignore Entries

```
lib/
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
```

---

## File Locations

| Item | Location |
|------|----------|
| Revit API DLLs | `lib\RevitXXXX\` |
| Output DLLs | `bin\[Debug|Release]XXXX\` |
| Logs | `%APPDATA%\YourProject\Logs\` |
| Revit Addins | `%APPDATA%\Autodesk\Revit\Addins\XXXX\` |

---

## Debugging Tips

1. **Set breakpoint** in your code
2. **Select Debug configuration** (e.g., Debug2024)
3. **Press F5** - Visual Studio launches Revit
4. **Load project** in Revit
5. **Run your plugin** - breakpoint will hit

**Log file location:** `%APPDATA%\YourProject\Logs\`

---

## Common Issues

| Issue | Solution |
|-------|----------|
| ServiceLocator not initialized | Ensure App.OnStartup() completed |
| Property initializer too early | Initialize in constructor |
| Elements created wrong location | Check unit conversion (inches → feet) |
| Plugin doesn't appear | Check .addin file location |
| Build fails | Run `.\setup-revit-libs.ps1` first |

---

**For detailed documentation, see: REVIT_ADDIN_TEMPLATE_STANDARD.md**
