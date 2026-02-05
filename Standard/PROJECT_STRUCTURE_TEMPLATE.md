# Revit Add-in Project Structure Template

**Use this template to quickly scaffold a new Revit add-in project**

---

## Quick Start: Create New Project

### Option 1: Manual Setup (Recommended for learning)

1. **Create Solution Structure:**
```
YourRevitPlugin/
├── YourRevitPlugin.sln
├── YourRevitPlugin.csproj
├── App.cs
├── App.xaml
├── Styles.xaml
├── build.ps1
├── setup-revit-libs.ps1
├── YourPlugin.addin
└── Templates/                   # Code templates (reuse existing)
```

2. **Create Feature Folders:**
```
Features/
└── YourFeature/
    ├── Commands/
    │   └── YourFeatureCommand.cs
    ├── Models/
    │   └── YourFeatureModel.cs
    ├── ViewModels/
    │   └── YourFeatureViewModel.cs
    └── Views/
        └── YourFeatureView.xaml
```

3. **Create Shared Infrastructure:**
```
Shared/
├── DependencyInjection/
│   ├── ServiceCollection.cs
│   ├── ServiceLocator.cs
│   └── IServiceProvider.cs
├── Interfaces/
│   └── IYourService.cs
├── Services/
│   └── YourService.cs
├── Logging/
│   ├── ILogger.cs
│   └── FileLogger.cs
├── Utils/
│   └── ImageUtils.cs
├── Exceptions/
│   └── CustomExceptions.cs
└── Resources/
    └── Icons/
```

### Option 2: Copy from Template (Fastest)

1. **Copy the entire RevitCadConverter folder:**
```powershell
Copy-Item -Path "RevitCadConverter" -Destination "YourRevitPlugin" -Recurse
cd YourRevitPlugin
```

2. **Rename files and namespaces:**
```
RevitCadConverter.csproj → YourRevitPlugin.csproj
ACGCadStruct.addin → YourPlugin.addin
App.cs (update namespace)
All feature folders (rename or delete)
```

3. **Search and replace namespaces:**
```
Find: RevitCadConverter
Replace: YourRevitPlugin

Find: ACGCadStruct
Replace: YourPluginFolder
```

4. **Update .csproj:**
```xml
<RootNamespace>YourRevitPlugin</RootNamespace>
<AssemblyName>YourRevitPlugin</AssemblyName>
```

---

## Essential Files Checklist

### 1. Project File (.csproj)

**Required Properties:**
```xml
<PropertyGroup>
  <TargetFramework>net48</TargetFramework>
  <UseWPF>true</UseWPF>
  <LangVersion>latest</LangVersion>
  <PlatformTarget>x64</PlatformTarget>
  <Configurations>Debug2023;Release2023;Debug2024;Release2024;...</Configurations>
</PropertyGroup>
```

**Required Packages:**
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="MaterialDesignThemes" Version="5.2.1" />
<PackageReference Include="MaterialDesignColors" Version="5.2.1" />
<PackageReference Include="Revit.Async" Version="2.1.1" />
```

### 2. App.cs (IExternalApplication)

**Minimum Implementation:**
```csharp
public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication app)
    {
        InitializeDependencyInjection();
        CreateRibbonUI(app);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app)
    {
        return Result.Succeeded;
    }
}
```

### 3. ServiceCollection.cs

**Copy from Templates folder** - No changes needed for basic DI.

### 4. ServiceLocator.cs

**Copy from Templates folder** - No changes needed.

### 5. FileLogger.cs

**Copy from Templates folder** - Update log directory name.

### 6. Styles.xaml

**Copy from Templates folder** - Customize colors if needed.

### 7. PowerShell Scripts

**Copy and update:**
- `build.ps1` - Update project name
- `setup-revit-libs.ps1` - Can use as-is

---

## Feature Scaffolding Checklist

For each new feature, create:

### Commands/
- [ ] `FeatureCommand.cs` (IExternalCommand entry point)

### Models/
- [ ] `FeatureModel.cs` (Domain data model)

### ViewModels/
- [ ] `FeatureMainViewModel.cs` (Main window VM)
- [ ] `FeatureTabViewModel.cs` (Tab VM if using tabs)

### Views/
- [ ] `FeatureMainView.xaml` (Main window)
- [ ] `FeatureTabView.xaml` (Tab view if using tabs)

---

## Service Registration Template

Add to `App.cs` in `InitializeDependencyInjection()`:

```csharp
private void InitializeDependencyInjection()
{
    var logger = new FileLogger();
    var services = new ServiceCollection();

    // Core services
    services.RegisterSingleton<ILogger>(logger);
    services.RegisterSingleton<IUnitConversionService>(new UnitConversionService(logger));

    // Your custom services
    services.RegisterSingleton<IYourService>(new YourService(logger));

    // Initialize ServiceLocator
    ServiceLocator.Initialize(services.BuildServiceProvider());
}
```

---

## Ribbon UI Registration Template

Add to `App.cs` in `OnStartup()`:

```csharp
private void CreateRibbonUI(UIControlledApplication app)
{
    string tabName = "Your Tab";
    try { app.CreateRibbonTab(tabName); } catch { }

    RibbonPanel panel = app.CreateRibbonPanel(tabName, "Your Panel");
    string assemblyPath = Assembly.GetExecutingAssembly().Location;

    PushButtonData buttonData = new PushButtonData(
        "YourBtnId",
        "Button\nText",
        assemblyPath,
        "YourNamespace.Features.YourFeature.Commands.YourFeatureCommand");

    PushButton button = panel.AddItem(buttonData) as PushButton;
    button.ToolTip = "Your button tooltip";

    // Add icons if available
    // button.Image = ImageUtils.LoadImage(assembly, "Icon_16.png");
    // button.LargeImage = ImageUtils.LoadImage(assembly, "Icon_32.png");
}
```

---

## .addin Manifest Template

Create `YourPlugin.addin`:

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>YourPluginName</Name>
    <Assembly>"YourPluginFolder/YourRevitPlugin.dll"</Assembly>
    <AddInId>GENERATE-NEW-GUID-HERE</AddInId>
    <FullClassName>YourNamespace.App</FullClassName>
    <VendorId>YOUR_VENDOR_ID</VendorId>
    <VendorDescription>Your plugin description</VendorDescription>
  </AddIn>
</RevitAddIns>
```

**Generate GUID:**
- C#: `Guid.NewGuid().ToString()`
- PowerShell: `[Guid]::NewGuid().ToString()`
- Online: https://www.guidgenerator.com/

---

## .gitignore Template

```
# Revit API DLLs (local copies)
lib/

# Build outputs
bin/
obj/

# Visual Studio
*.user
*.suo
*.userosscache
*.sln.docstates
.vs/

# ReSharper
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# Logs
*.log

# OS
.DS_Store
Thumbs.db
```

---

## Minimum Feature Example

### Command (YourFeatureCommand.cs)

```csharp
[Transaction(TransactionMode.Manual)]
public class YourFeatureCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        RevitTask.Initialize(commandData.Application);

        try
        {
            var window = new YourFeatureView();
            var vm = window.DataContext as YourFeatureViewModel;
            vm.Initialize(commandData.Application.ActiveUIDocument.Document);
            window.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
```

### ViewModel (YourFeatureViewModel.cs)

```csharp
public partial class YourFeatureViewModel : ObservableObject
{
    private readonly ILogger _logger;

    [ObservableProperty]
    private string _title = "Your Feature";

    public YourFeatureViewModel()
    {
        _logger = ServiceLocator.GetService<ILogger>();
    }

    public void Initialize(Document doc)
    {
        _logger.Info("Feature initialized");
    }

    [RelayCommand]
    private void DoWork()
    {
        _logger.Info("Work done");
    }
}
```

### View (YourFeatureView.xaml)

```xml
<UserControl x:Class="YourNamespace.Features.YourFeature.Views.YourFeatureView">
    <UserControl.DataContext>
        <viewModel:YourFeatureViewModel />
    </UserControl.DataContext>

    <Grid Margin="16">
        <StackPanel>
            <TextBlock Text="{Binding Title}"
                       FontSize="24"
                       FontWeight="Bold" />
            <Button Content="Do Work"
                    Command="{Binding DoWorkCommand}"
                    Margin="0,16,0,0" />
        </StackPanel>
    </Grid>
</UserControl>
```

---

## Post-Build Setup (in .csproj)

```xml
<Target Name="PostBuildCopyToAddins" AfterTargets="Build">
  <PropertyGroup>
    <RevitAddinsRootFolder>$(APPDATA)\Autodesk\Revit\Addins\$(RevitVersion)</RevitAddinsRootFolder>
    <PluginFolder>$(RevitAddinsRootFolder)\YourPluginFolder</PluginFolder>
  </PropertyGroup>

  <MakeDir Directories="$(PluginFolder)" />
  <Copy SourceFiles="$(OutDir)YourRevitPlugin.dll" DestinationFolder="$(PluginFolder)" />
  <Copy SourceFiles="$(ProjectDir)YourPlugin.addin" DestinationFolder="$(RevitAddinsRootFolder)" />
</Target>
```

---

## Validation Checklist

Before first build:

- [ ] All namespaces updated
- [ ] Project name in .csproj updated
- [ ] .addin manifest GUID generated
- [ ] Service registration in App.cs
- [ ] Ribbon UI buttons registered
- [ ] PowerShell scripts updated
- [ ] Revit API DLLs copied (run setup-revit-libs.ps1)
- [ ] .gitignore created
- [ ] Solution builds without errors
- [ ] Can load plugin in Revit

---

## Development Workflow

```powershell
# 1. Setup (once)
.\setup-revit-libs.ps1

# 2. Build
.\build.ps1

# 3. Debug
# - Open solution in VS
# - Select Debug2024
# - Press F5 (Revit launches)

# 4. Test
# - Load Revit project
# - Click your ribbon button
```

---

## Common First-Time Issues

| Issue | Solution |
|-------|----------|
| Revit API not found | Run `.\setup-revit-libs.ps1` |
| Plugin doesn't appear | Check .addin file location |
| ServiceLocator error | Check App.OnStartup() completed |
| Build fails | Check all namespaces updated |
| Wrong icons | Update icon references in App.cs |

---

**Need more help?**
- See: `REVIT_ADDIN_TEMPLATE_STANDARD.md` (detailed guide)
- See: `QUICK_REFERENCE.md` (command reference)
- See: `Templates/` folder (code templates)
