# Quickstart: Auto-Fill Revit Parameters

**Feature**: 003-auto-fill-parameters
**Target Audience**: Developers implementing this feature
**Last Updated**: 2025-02-12

## Overview

This quickstart guide helps developers implement the Auto-Fill Parameters feature for the COBIe Manager Revit plugin. The feature automatically fills project parameters on model elements based on level range and room ownership.

## Prerequisites

1. **Revit API Knowledge**: Understanding of Revit API elements, parameters, transactions
2. **WPF/XAML**: Basic familiarity with WPF and XAML data binding
3. **MVVM Pattern**: Understanding of CommunityToolkit.Mvvm patterns
4. **Project Setup**: Run `.\setup-revit-libs.ps1` to copy Revit API DLLs

## Development Workflow

### Step 1: Create Feature Folder Structure

```powershell
# Create the feature folder
mkdir Features\ParameterFiller
mkdir Features\ParameterFiller\Commands
mkdir Features\ParameterFiller\ViewModels
mkdir Features\ParameterFiller\Views
mkdir Features\ParameterFiller\Models
```

### Step 2: Implement Core Services

Create services in `Shared/Services/`:

1. **LevelAssignmentService.cs** - Implements `ILevelAssignmentService`
   - `IsElementInLevelBand()` - Check bounding box intersection
   - `AssignLevelParameter()` - Safe parameter write with skip handling

2. **RoomAssignmentService.cs** - Implements `IRoomAssignmentService`
   - `GetRoomForElement()` - Tiered room detection strategy
   - `AssignRoomParameters()` - Safe room parameter writes

3. **ParameterFillService.cs** - Implements `IParameterFillService`
   - `PreviewFill()` - Read-only analysis
   - `ExecuteFill()` - Transaction-wrapped fill with progress

4. **ProcessingLogger.cs** - Implements `IProcessingLogger`
   - `ExportLog()` - Write timestamped text file
   - `GenerateLogContent()` - Format log content

### Step 3: Create UI Models

Create models in `Features/ParameterFiller/Models/`:

1. **FillConfiguration.cs** - User settings for the operation
2. **ProcessingSummary.cs** - Aggregate statistics
3. **ParameterMapping.cs** - Parameter name mappings

### Step 4: Create ViewModel

Create `ParameterFillViewModel.cs` in `Features/ParameterFiller/ViewModels/`:

```csharp
public partial class ParameterFillViewModel : ObservableObject
{
    [ObservableProperty]
    private FillConfiguration _config;

    [ObservableProperty]
    private ProcessingSummary _previewSummary;

    [RelayCommand]
    private void ExecutePreview() { }

    [RelayCommand]
    private void ExecuteFill() { }
}
```

### Step 5: Create View

Create `ParameterFillWindow.xaml` in `Features/ParameterFiller/Views/`:

- Use MaterialDesign `Card` for sections
- Use `ComboBox` for level selection
- Use `CheckBox` for category selection
- Use `CheckBox` for overwrite option
- Use `Button` with Command binding for Preview/Apply

### Step 6: Create Command

Create `ParameterFillCommand.cs` in `Features/ParameterFiller/Commands/`:

```csharp
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ParameterFillCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        RevitTask.Initialize(commandData.Application);
        var task = ShowParameterFillWindowAsync(commandData.Application.ActiveUIDocument);
        task.Wait();
        return Result.Succeeded;
    }
}
```

### Step 7: Register Ribbon Button

Add to `App.cs` in `OnStartup()`:

```csharp
PushButtonData fillParamsButtonData = new PushButtonData(
    "ParameterFillBtn",
    "Fill Parameters",
    assemblyPath,
    "COBIeManager.Features.ParameterFiller.Commands.ParameterFillCommand");

PushButton fillParamsButton = panel.AddItem(fillParamsButtonData) as PushButton;
fillParamsButton.ToolTip = "Auto-fill level and room parameters";
```

### Step 8: Register Services

Add to `App.cs` in `InitializeDependencyInjection()`:

```csharp
services.RegisterSingleton<ILevelAssignmentService>(new LevelAssignmentService(logger));
services.RegisterSingleton<IRoomAssignmentService>(new RoomAssignmentService(logger));
services.RegisterSingleton<IParameterFillService>(new ParameterFillService(logger));
services.RegisterSingleton<IProcessingLogger>(new ProcessingLogger(logger));
```

## Key Implementation Points

### Level Band Intersection Algorithm

```csharp
public LevelBandPosition IsElementInLevelBand(Element element, Level baseLevel, Level topLevel)
{
    var bbox = element.get_BoundingBox(null);
    if (bbox == null) return LevelBandPosition.NoBoundingBox;

    double z0 = baseLevel.Elevation;
    double z1 = topLevel.Elevation;

    // Intersection band rule: element overlaps the vertical band
    bool inBand = (bbox.Min.Z < z1) && (bbox.Max.Z > z0);

    if (!inBand)
    {
        if (bbox.Max.Z <= z0) return LevelBandPosition.BelowBand;
        return LevelBandPosition.AboveBand;
    }

    return LevelBandPosition.InBand;
}
```

### Room Detection Strategy

```csharp
public Room? GetRoomForElement(Element element, Document document, Phase phase)
{
    // Method 1: Direct Room property
    if (element is FamilyInstance familyInstance)
    {
        var room = familyInstance.Room;
        if (room != null) return room;
    }

    // Method 2: Door-specific FromRoom/ToRoom
    if (element is FamilyInstance fi && fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
    {
        var room = fi.FromRoom ?? fi.ToRoom;
        if (room != null) return room;
    }

    // Method 3: Point-in-room test
    var point = GetElementPoint(element);
    if (point != null)
    {
        return new RoomCalculator(document).GetRoomAtPoint(point, phase);
    }

    return null;
}
```

### Safe Parameter Write

```csharp
public ParameterAssignmentResult AssignParameter(Element element, string paramName, string value, bool overwrite)
{
    var param = element.LookupParameter(paramName);
    if (param == null)
        return new ParameterAssignmentResult { Skipped = true, SkipReason = "Parameter not found" };

    if (param.IsReadOnly)
        return new ParameterAssignmentResult { Skipped = true, SkipReason = "Parameter is read-only" };

    if (!overwrite && param.HasValue)
        return new ParameterAssignmentResult { Skipped = true, SkipReason = "Value exists" };

    param.Set(value);
    return new ParameterAssignmentResult { Success = true };
}
```

## Testing Checklist

- [ ] Level band detection works for elements at band boundaries
- [ ] Room detection finds rooms for FamilyInstances
- [ ] Room detection handles doors with FromRoom/ToRoom
- [ ] Room detection falls back to point-in-room test
- [ ] Parameter write skips missing parameters gracefully
- [ ] Parameter write skips read-only parameters gracefully
- [ ] Overwrite flag is respected
- [ ] Preview mode doesn't modify document
- [ ] Execute mode uses transaction correctly
- [ ] Progress callback updates UI during processing
- [ ] Summary report is accurate
- [ ] Log file exports correctly
- [ ] Large models (5000+ elements) use TransactionGroup
- [ ] Documents with no rooms are handled gracefully

## Debugging Tips

1. **Enable Logging**: Check `%APPDATA%\COBIeManager\Logs\` for detailed logs
2. **Revit Lookahead**: Use RevitLookAhead to see element properties in debugger
3. **Transaction Names**: Use descriptive transaction names for easier debugging
4. **Element Ids**: Log ElementIds for skipped elements to investigate in Revit UI

## Next Steps

After implementation:
1. Run `.\build.ps1` to compile
2. Copy add-in manifest to Revit add-ins folder
3. Test in Revit with sample model
4. Run through testing checklist above
