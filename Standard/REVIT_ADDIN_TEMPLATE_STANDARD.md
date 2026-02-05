# Revit Add-in Development Template & Standard

**Version:** 1.0
**Based on:** RevitCadConverter (Production-Ready Revit Plugin)
**Last Updated:** 2024
**Supported Revit Versions:** 2023, 2024, 2025, 2026

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Architecture Patterns](#architecture-patterns)
5. [Build System](#build-system)
6. [Development Workflow](#development-workflow)
7. [Coding Standards](#coding-standards)
8. [UI/UX Guidelines](#uiux-guidelines)
9. [Testing Standards](#testing-standards)
10. [Deployment](#deployment)
11. [Checklists](#checklists)

---

## Project Overview

This template provides a production-ready foundation for Revit add-in development based on the proven architecture of RevitCadConverter. It implements modern software engineering practices including MVVM, Dependency Injection, comprehensive logging, and multi-version support.

### Key Features

- **Multi-Version Support**: Single codebase supports Revit 2023-2026
- **MVVM Architecture**: Clean separation of concerns with CommunityToolkit.Mvvm
- **Dependency Injection**: Custom lightweight DI container for testable code
- **Material Design UI**: Modern, consistent WPF interface with MaterialDesignThemes
- **Comprehensive Logging**: File-based logging with multiple severity levels
- **Async Support**: Safe async operations with Revit.Async
- **Feature-Driven Structure**: Modular organization for independent feature development

---

## Technology Stack

### Core Framework
| Component | Version | Purpose |
|-----------|---------|---------|
| .NET Framework | 4.8 | Target framework |
| C# Language | latest | Latest language features |
| Platform | x64 | Revit requires 64-bit |

### UI Framework
| Package | Version | Purpose |
|---------|---------|---------|
| MaterialDesignThemes | 5.2.1 | Material Design UI components |
| MaterialDesignColors | 5.2.1 | Material Design color system |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.135 | WPF behaviors for interactivity |

### MVVM Framework
| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.4.0 | Modern MVVM with source generators |

### Revit Integration
| Package | Version | Purpose |
|---------|---------|---------|
| Revit.Async | 2.1.1 | Safe async operations in Revit |
| Revit API | 2023-2026 | Local DLL references (lib folder) |

### Build Tools
| Tool | Purpose |
|------|---------|
| MSBuild | Build system (via Visual Studio) |
| PowerShell | Build automation scripts |

### Testing (Optional)
| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.6.2 | Unit testing framework |
| Moq | 4.20.70 | Mocking framework |

---

## Project Structure

### Folder Organization

```
ProjectName/
├── lib/                          # Revit API DLLs (gitignored)
│   ├── Revit2023/               # Version-specific DLLs
│   ├── Revit2024/
│   ├── Revit2025/
│   └── Revit2026/
│
├── Features/                     # Feature-driven modules
│   ├── FeatureName1/            # Self-contained feature
│   │   ├── Commands/            # IExternalCommand entry points
│   │   │   └── FeatureCommand.cs
│   │   ├── Models/              # Feature-specific data models
│   │   │   └── FeatureModel.cs
│   │   ├── ViewModels/          # MVVM ViewModels
│   │   │   ├── FeatureMainViewModel.cs
│   │   │   └── FeatureTabViewModel.cs
│   │   └── Views/               # XAML views
│   │       ├── FeatureMainView.xaml
│   │       └── FeatureTabView.xaml
│   │
│   └── FeatureName2/
│       └── ...
│
├── Shared/                       # Shared components
│   ├── DependencyInjection/     # DI container
│   │   ├── ServiceCollection.cs
│   │   ├── ServiceLocator.cs
│   │   └── IServiceProvider.cs
│   ├── Interfaces/              # Service interfaces
│   │   ├── IElementCreationService.cs
│   │   ├── IFamilyService.cs
│   │   ├── ILevelService.cs
│   │   └── ...
│   ├── Services/                # Service implementations
│   │   ├── ElementCreationService.cs
│   │   ├── FamilyService.cs
│   │   └── ...
│   ├── Logging/                 # Logging infrastructure
│   │   ├── ILogger.cs
│   │   └── FileLogger.cs
│   ├── Converters/              # XAML value converters
│   ├── Controls/                # Reusable WPF controls
│   ├── Utils/                   # Utility classes
│   ├── Exceptions/              # Custom exceptions
│   ├── Validators/              # Validation rules
│   └── Resources/               # Icons, images, strings
│
├── Properties/                  # Project properties
│   └── Revit.addin              # Revit manifest (copy to output)
│
├── App.cs                       # IExternalApplication implementation
├── App.xaml                     # WPF application resources
├── Styles.xaml                  # Material Design styles
├── ProjectName.csproj           # Project configuration
├── ProjectName.sln              # Solution file
├── build.ps1                    # Build script
├── setup-revit-libs.ps1         # Revit API setup
└── README.md                    # Project documentation
```

### File Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Commands | `[Feature][Purpose]Command.cs` | `CadStructureCommand.cs` |
| ViewModels (Main) | `[Feature][Purpose]ViewModel.cs` | `CadStructureMainViewModel.cs` |
| ViewModels (Tab) | `[Element]TapViewModel.cs` or `[Element]TabViewModel.cs` | `ColumnTapViewModel.cs` |
| Views | `[Purpose]View.xaml` | `MainView.xaml` |
| Models | `[Domain][Entity]Model.cs` | `CadColumnModel.cs` |
| Services Interface | `I[Purpose]Service.cs` | `IElementCreationService.cs` |
| Services Implementation | `[Purpose]Service.cs` | `ElementCreationService.cs` |

---

## Architecture Patterns

### 1. MVVM Pattern (Model-View-ViewModel)

#### ViewModel Base Class
All ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm):

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MyViewModel : ObservableObject
{
    // Observable property with auto-generated backing field
    [ObservableProperty]
    private string _title;

    // Command with auto-generated relay command
    [RelayCommand]
    private void ExecuteAction()
    {
        // Command logic
    }
}
```

#### Property Initialization Pattern

**CRITICAL:** Never use property initializers for DI-dependent ViewModels:

**❌ WRONG:**
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ChildViewModel childViewModel = new();  // TOO EARLY!
}
```

**✅ CORRECT:**
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ChildViewModel childViewModel;  // No initializer

    public MainViewModel()
    {
        // ServiceLocator is ready here
        ChildViewModel = new ChildViewModel();
    }
}
```

### 2. Dependency Injection Pattern

#### Service Registration (App.cs)

```csharp
private void InitializeDependencyInjection()
{
    var logger = new FileLogger();
    var services = new ServiceCollection();

    // Register singletons
    services.RegisterSingleton<ILogger>(logger);
    services.RegisterSingleton<IUnitConversionService>(new UnitConversionService(logger));

    // Register with dependencies
    services.RegisterSingleton<IElementCreationService>(
        new ElementCreationService(
            services.Resolve<ILevelService>(),
            services.Resolve<IUnitConversionService>(),
            logger));

    // Build and initialize ServiceLocator
    ServiceLocator.Initialize(services.BuildServiceProvider());
}
```

#### Service Access Patterns

**Pattern 1: From ViewModels (Recommended)**
```csharp
public class MyViewModel
{
    private readonly IElementCreationService _elementService;

    public MyViewModel()
    {
        var facade = ServiceLocator.GetService<IRevitOperationsFacade>();
        _elementService = facade.Elements;
    }
}
```

**Pattern 2: Factory Pattern (For Runtime Context)**
```csharp
// Register factory
services.RegisterSingleton<Func<Document, ImportInstance, ICadGeometryService>>(
    (doc, cadLink) => new CadGeometryService(doc, cadLink, logger));

// Use in code
var factory = ServiceLocator.GetService<Func<Document, ImportInstance, ICadGeometryService>>();
var service = factory(document, cadLink);
```

### 3. Service Layer Pattern

#### Service Interface Design

```csharp
public interface IElementCreationService
{
    /// <summary>
    /// Creates a structural column at the specified location.
    /// </summary>
    FamilyInstance CreateColumn(
        Document doc,
        CadColumnModel column,
        Level baseLevel,
        Level topLevel,
        FamilySymbol familySymbol,
        double baseOffset,
        double topOffset);
}
```

#### Service Implementation Pattern

```csharp
public class ElementCreationService : IElementCreationService
{
    private readonly ILogger _logger;
    private readonly ILevelService _levelService;
    private readonly IUnitConversionService _unitService;

    public ElementCreationService(
        ILevelService levelService,
        IUnitConversionService unitService,
        ILogger logger)
    {
        _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public FamilyInstance CreateColumn(...)
    {
        try
        {
            _logger.Debug($"Creating column at {column.Position}");

            // Implementation

            _logger.Info("Column created successfully");
            return column;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create column: {ex.Message}", ex);
            throw new RevitElementCreationException($"Column creation failed", ex);
        }
    }
}
```

### 4. Command Pattern

#### External Command Entry Point

```csharp
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class MyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // CRITICAL: Initialize Revit async support FIRST
        RevitTask.Initialize(commandData.Application);

        try
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Create and show main window
            var window = new MainView();
            var vm = window.DataContext as MyViewModel;
            vm.Initialize(doc, someParameter);
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
```

### 5. Feature-Driven Architecture

Each feature is self-contained with:
- **Commands**: Entry points (IExternalCommand)
- **Models**: Domain models
- **ViewModels**: MVVM presentation logic
- **Views**: XAML UI

Features can evolve independently while sharing services from the DI container.

---

## Build System

### Project Configuration (.csproj)

#### Essential Properties

```xml
<PropertyGroup>
  <TargetFramework>net48</TargetFramework>
  <UseWPF>true</UseWPF>
  <LangVersion>latest</LangVersion>
  <PlatformTarget>x64</PlatformTarget>
  <Prefer32Bit>false</Prefer32Bit>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
```

#### Multi-Version Configuration

```xml
<!-- All configurations -->
<Configurations>
  Debug2023;Release2023;
  Debug2024;Release2024;
  Debug2025;Release2025;
  Debug2026;Release2026
</Configurations>

<!-- Version mapping -->
<PropertyGroup>
  <RevitVersion Condition="'$(Configuration)' == 'Debug2024'">2024</RevitVersion>
  <RevitVersion Condition="'$(Configuration)' == 'Release2024'">2024</RevitVersion>
  <!-- ... repeat for other versions -->
</PropertyGroup>

<!-- Conditional compilation symbols -->
<PropertyGroup>
  <DefineConstants Condition="'$(Configuration)' == 'Debug2024'">DEBUG;TRACE;REVIT_2024</DefineConstants>
  <!-- ... repeat for other versions -->
</PropertyGroup>
```

#### Revit API References

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug2024' Or '$(Configuration)' == 'Release2024'">
  <Reference Include="RevitAPI">
    <HintPath>lib\Revit2024\RevitAPI.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="RevitAPIUI">
    <HintPath>lib\Revit2024\RevitAPIUI.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="UIFramework">
    <HintPath>lib\Revit2024\UIFramework.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

#### Post-Build Deployment

```xml
<Target Name="PostBuildCopyToAddins" AfterTargets="Build">
  <PropertyGroup>
    <RevitAddinsRootFolder>$(APPDATA)\Autodesk\Revit\Addins\$(RevitVersion)</RevitAddinsRootFolder>
    <PluginFolder>$(RevitAddinsRootFolder)\YourPluginFolder</PluginFolder>
  </PropertyGroup>

  <MakeDir Directories="$(PluginFolder)" />

  <!-- Copy DLLs -->
  <Copy SourceFiles="$(OutDir)YourPlugin.dll" DestinationFolder="$(PluginFolder)" />
  <ItemGroup>
    <DllFiles Include="$(OutDir)*.dll" Exclude="$(OutDir)RevitAPI.dll;$(OutDir)RevitAPIUI.dll" />
  </ItemGroup>
  <Copy SourceFiles="@(DllFiles)" DestinationFolder="$(PluginFolder)" />

  <!-- Copy manifest -->
  <Copy SourceFiles="$(ProjectDir)YourPlugin.addin" DestinationFolder="$(RevitAddinsRootFolder)" />
</Target>
```

### Build Script (build.ps1)

```powershell
param(
    [Parameter(Position = 0)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Position = 1)]
    [ValidateSet('2023', '2024', '2025', '2026')]
    [string]$RevitVersion = '2024',

    [switch]$All,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

# Get MSBuild path from Visual Studio
function Get-MsbuildPath {
    $vsPath = & "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
    $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
    return $msbuildPath
}

function Build-Configuration {
    param([string]$Config, [string]$Version)

    $fullConfig = "${Config}${Version}"
    Write-Host "Building $fullConfig..." -ForegroundColor Cyan

    $msbuild = Get-MsbuildPath
    $args = @(
        "YourProject.sln",
        "/p:Configuration=$fullConfig",
        "/p:Platform=x64",
        "/v:minimal"
    )

    if ($Clean) {
        & $msbuild $args "/t:Clean"
    }

    & $msbuild $args
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $fullConfig"
        exit 1
    }

    Write-Host "✓ Build completed: $fullConfig" -ForegroundColor Green
}

# Execute build
if ($All) {
    $versions = @('2023', '2024', '2025', '2026')
    foreach ($version in $versions) {
        Build-Configuration 'Debug' $version
        Build-Configuration 'Release' $version
    }
} else {
    Build-Configuration $Configuration $RevitVersion
}
```

### Revit API Setup Script (setup-revit-libs.ps1)

```powershell
param(
    [Parameter(Position = 0)]
    [ValidateSet('2023', '2024', '2025', '2026', 'All')]
    [string]$Version = 'All'
)

$revitVersions = @{
    '2023' = 'C:\Program Files\Autodesk\Revit 2023'
    '2024' = 'C:\Program Files\Autodesk\Revit 2024'
    '2025' = 'C:\Program Files\Autodesk\Revit 2025'
    '2026' = 'C:\Program Files\Autodesk\Revit 2026'
}

$requiredDlls = @('RevitAPI.dll', 'RevitAPIUI.dll', 'UIFramework.dll')

function Copy-RevitDlls {
    param([string]$RevitVersion, [string]$InstallPath)

    if (-not (Test-Path $InstallPath)) {
        Write-Host "  [SKIP] Revit $RevitVersion not found" -ForegroundColor Yellow
        return
    }

    $targetDir = Join-Path $scriptDir "lib\Revit$RevitVersion"
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

    foreach ($dll in $requiredDlls) {
        $sourcePath = Join-Path $InstallPath $dll
        $targetPath = Join-Path $targetDir $dll

        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $targetPath -Force
            Write-Host "  [OK] $dll" -ForegroundColor Green
        }
    }
}

# Execute
if ($Version -eq 'All') {
    foreach ($ver in $revitVersions.Keys) {
        Copy-RevitDlls -RevitVersion $ver -InstallPath $revitVersions[$ver]
    }
} else {
    Copy-RevitDlls -RevitVersion $Version -InstallPath $revitVersions[$Version]
}
```

---

## Development Workflow

### First-Time Setup

```powershell
# 1. Clone repository
git clone <repository-url>
cd ProjectName

# 2. Copy Revit API DLLs (run once)
.\setup-revit-libs.ps1

# 3. Restore NuGet packages
dotnet restore

# 4. Build project
.\build.ps1 Release 2024
```

### Daily Development

```powershell
# Build for current Revit version
.\build.ps1

# Build for different version
.\build.ps1 Release 2025

# Clean and build
.\build.ps1 -Clean

# Debug in Visual Studio
# 1. Select Debug2024 configuration
# 2. Press F5 to launch Revit
```

### Adding a New Feature

1. **Create Feature Folder Structure:**
```
Features/
└── NewFeature/
    ├── Commands/
    ├── Models/
    ├── ViewModels/
    └── Views/
```

2. **Create Command:**
```csharp
[Transaction(TransactionMode.Manual)]
public class NewFeatureCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        RevitTask.Initialize(commandData.Application);
        // Implementation
    }
}
```

3. **Register Ribbon Button (App.cs):**
```csharp
PushButtonData buttonData = new PushButtonData(
    "NewFeatureBtn",
    "New Feature",
    assemblyPath,
    "ProjectName.Features.NewFeature.Commands.NewFeatureCommand");
```

4. **Create ViewModel:**
```csharp
public partial class NewFeatureViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly IYourService _service;

    public NewFeatureViewModel()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _service = ServiceLocator.GetService<IYourService>();
    }
}
```

5. **Create View (XAML):**
```xml
<UserControl x:Class="ProjectName.Features.NewFeature.Views.NewFeatureView">
    <Grid>
        <!-- UI implementation -->
    </Grid>
</UserControl>
```

### Version-Specific Code

```csharp
#if REVIT_2024
    // Revit 2024 specific code
#elif REVIT_2025
    // Revit 2025 specific code
#else
    // Default code
#endif
```

---

## Coding Standards

### C# Coding Guidelines

#### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `ElementCreationService` |
| Interfaces | PascalCase with `I` prefix | `IElementCreationService` |
| Methods | PascalCase | `CreateColumn()` |
| Properties | PascalCase | `SelectedLevel` |
| Private fields | _camelCase | `_logger`, `_elementService` |
| Constants | PascalCase | `MaximumColumnHeight` |
| Local variables | camelCase | `columnCount` |

#### XML Documentation

All public members must have XML documentation:

```csharp
/// <summary>
/// Creates a structural column at the specified location.
/// </summary>
/// <param name="doc">The Revit document.</param>
/// <param name="column">The column model containing position and dimensions.</param>
/// <param name="baseLevel">The base level for the column.</param>
/// <param name="topLevel">The top level for the column.</param>
/// <returns>The created FamilyInstance.</returns>
/// <exception cref="ArgumentNullException">Thrown when doc or column is null.</exception>
/// <exception cref="RevitElementCreationException">Thrown when column creation fails.</exception>
public FamilyInstance CreateColumn(Document doc, CadColumnModel column, Level baseLevel, Level topLevel)
{
    // Implementation
}
```

#### Exception Handling Pattern

```csharp
public void DoSomething()
{
    try
    {
        _logger.Debug("Starting operation...");

        // Implementation

        _logger.Info("Operation completed successfully");
    }
    catch (ArgumentNullException ex)
    {
        _logger.Error($"Null argument: {ex.ParamName}", ex);
        throw; // Re-throw for caller to handle
    }
    catch (Exception ex)
    {
        _logger.Error($"Operation failed: {ex.Message}", ex);
        throw new CustomOperationException("Operation failed", ex);
    }
}
```

#### Transaction Pattern

```csharp
public void CreateElements(Document doc)
{
    // Always use transactions for Revit modifications
    using (var transaction = new Transaction(doc, "Create Elements"))
    {
        transaction.Start();

        try
        {
            // Create elements
            var element = CreateElement(doc);

            transaction.Commit();
            _logger.Info("Elements created successfully");
        }
        catch (Exception ex)
        {
            // Roll back on failure
            if (transaction.GetStatus() == TransactionStatus.Started)
            {
                transaction.RollBack();
            }
            _logger.Error($"Failed to create elements: {ex.Message}", ex);
            throw;
        }
    }
}
```

### Async/Revit Pattern

```csharp
public async Task ProcessLargeDataSetAsync(Document doc, DataSet data)
{
    // Heavy processing off Revit thread
    var results = await Task.Run(() =>
    {
        return ProcessData(data);
    });

    // Back on Revit thread for modifications
    using (var t = new Transaction(doc, "Update Model"))
    {
        t.Start();
        foreach (var result in results)
        {
            CreateElement(doc, result);
        }
        t.Commit();
    }
}
```

### Service Implementation Template

```csharp
public class MyService : IMyService
{
    private readonly ILogger _logger;
    private readonly IDependency _dependency;

    public MyService(IDependency dependency, ILogger logger)
    {
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.Info($"{nameof(MyService)} initialized");
    }

    public Result DoWork(Input input)
    {
        try
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            _logger.Debug($"Starting work with input: {input}");

            var result = _dependency.Process(input);

            _logger.Info("Work completed successfully");
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.Error($"Work failed: {ex.Message}", ex);
            throw;
        }
    }
}
```

---

## UI/UX Guidelines

### Material Design Theme

#### Color Palette (Customizable)

```xml
<!-- Styles.xaml -->
<Color x:Key="PrimaryColor">#E65100</Color>      <!-- Main brand color -->
<Color x:Key="SecondaryColor">#37474F</Color>    <!-- Secondary color -->
<Color x:Key="SuccessGreen">#4CAF50</Color>      <!-- Success states -->
<Color x:Key="WarningAmber">#FFA726</Color>      <!-- Warning states -->
<Color x:Key="ErrorRed">#E53935</Color>          <!-- Error states -->
```

#### Typography Scale

| Style | Size | Weight | Usage |
|-------|------|--------|-------|
| Heading | 24px | Bold | Page titles |
| Section Header | 16px | SemiBold | Section titles |
| Subheader | 13px | Medium | Subsection titles |
| Body | 14px | Regular | Body text |
| Caption | 12px | Regular | Helper text |

### Component Library

#### Card Styles

```xml
<Style x:Key="ModernCard" TargetType="materialdesign:Card">
    <Setter Property="Padding" Value="20"/>
    <Setter Property="Margin" Value="0,0,0,16"/>
    <Setter Property="materialdesign:ElevationAssist.Elevation" Value="Dp2"/>
</Style>
```

#### Button Styles

```xml
<!-- Primary Action Button -->
<Style x:Key="PrimaryActionButton" TargetType="Button"
       BasedOn="{StaticResource MaterialDesignRaisedButton}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
</Style>

<!-- Secondary Action Button -->
<Style x:Key="SecondaryActionButton" TargetType="Button"
       BasedOn="{StaticResource MaterialDesignOutlinedButton}">
    <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
</Style>
```

### XAML Best Practices

#### Data Binding

```xml
<!-- Observable property binding -->
<TextBox Text="{Binding Title, UpdateSourceTrigger=PropertyChanged}" />

<!-- Command binding -->
<Button Command="{Binding SaveCommand}" />

<!-- Collection binding -->
<ComboBox ItemsSource="{Binding Levels}"
          SelectedItem="{Binding SelectedLevel}"
          DisplayMemberPath="Name" />
```

#### Validation

```xml
<TextBox Style="{StaticResource ValidationStyleTxt}">
    <TextBox.Text>
        <Binding Path="Quantity" UpdateSourceTrigger="PropertyChanged">
            <Binding.ValidationRules>
                <local:PositiveNumberValidationRule />
            </Binding.ValidationRules>
        </Binding>
    </TextBox.Text>
</TextBox>
```

#### MVVM Compliance

- **No code-behind logic** (except InitializeComponent())
- **Commands** for all user actions
- **Observable properties** for all bindable data
- **Converters** for value transformations

---

## Testing Standards

### Unit Test Structure

```
ProjectName.Tests/
├── Services/
│   ├── ElementCreationServiceTests.cs
│   └── FamilyServiceTests.cs
├── ViewModels/
│   └── MainViewModelTests.cs
└── DependencyInjection/
    └── ServiceCollectionTests.cs
```

### Test Template (xUnit + Moq)

```csharp
using Xunit;
using Moq;
using ProjectName.Shared.Services;

public class ElementCreationServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILevelService> _levelServiceMock;
    private readonly ElementCreationService _service;

    public ElementCreationServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _levelServiceMock = new Mock<ILevelService>();
        _service = new ElementCreationService(
            _levelServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullLevelService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ElementCreationService(null, _loggerMock.Object));
    }

    [Fact]
    public void CreateColumn_ValidParameters_ReturnsFamilyInstance()
    {
        // Arrange
        var docMock = new Mock<Document>();
        var column = new CadColumnModel { Position = XYZ.Zero };
        var levelMock = new Mock<Level>();

        // Act
        var result = _service.CreateColumn(
            docMock.Object, column, levelMock.Object, levelMock.Object, null, 0, 0);

        // Assert
        Assert.NotNull(result);
        _loggerMock.Verify(l => l.Info("Column created successfully"), Times.Once);
    }
}
```

### Test Coverage Goals

| Component | Target Coverage |
|-----------|-----------------|
| Services | 80%+ |
| ViewModels | 70%+ |
| Commands | 60%+ |
| Utils | 90%+ |

---

## Deployment

### Revit Manifest File (.addin)

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>YourPluginName</Name>
    <Assembly>"YourPluginFolder/YourPlugin.dll"</Assembly>
    <AddInId>XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX</AddInId>
    <FullClassName>Namespace.App</FullClassName>
    <VendorId>YOUR_VENDOR_ID</VendorId>
    <VendorDescription>Your plugin description</VendorDescription>
  </AddIn>
</RevitAddIns>
```

### Generate GUID for AddInId

```csharp
// In C# or Visual Studio
Guid.NewGuid().ToString()
// Example: "8B2D905C-1234-5678-ABCD-1234567890AB"
```

### Deployment Locations

| Revit Version | Addins Folder |
|---------------|---------------|
| 2023 | `%APPDATA%\Autodesk\Revit\Addins\2023\` |
| 2024 | `%APPDATA%\Autodesk\Revit\Addins\2024\` |
| 2025 | `%APPDATA%\Autodesk\Revit\Addins\2025\` |
| 2026 | `%APPDATA%\Autodesk\Revit\Addins\2026\` |

### Manual Installation

1. Create folder: `%APPDATA%\Autodesk\Revit\Addins\202X\YourPluginFolder\`
2. Copy files:
   - `YourPlugin.dll`
   - `YourPlugin.dll.config` (if present)
   - Dependencies (MaterialDesign*.dll, etc.)
3. Copy `.addin` manifest to: `%APPDATA%\Autodesk\Revit\Addins\202X\`
4. Restart Revit

### Automatic Deployment (Post-Build)

The post-build target in `.csproj` automatically handles deployment during development.

---

## Checklists

### Feature Implementation Checklist

- [ ] Create feature folder structure (Commands/Models/ViewModels/Views)
- [ ] Implement IExternalCommand entry point
- [ ] Create domain models
- [ ] Create ViewModels with ObservableObject
- [ ] Create XAML views with Material Design
- [ ] Register services in DI container (if needed)
- [ ] Register ribbon button in App.cs
- [ ] Implement logging throughout
- [ ] Handle exceptions properly
- [ ] Add XML documentation
- [ ] Test in Revit (all target versions)
- [ ] Update README.md

### Code Review Checklist

- [ ] Follows naming conventions
- [ ] Has XML documentation
- [ ] Uses DI pattern correctly
- [ ] Handles exceptions appropriately
- [ ] Logs important operations
- [ ] Uses transactions for Revit modifications
- [ ] No hardcoded values (use constants/config)
- [ ] MVVM compliant (no code-behind logic)
- [ ] Async operations use Revit.Async
- [ ] Version-specific code properly guarded

### Release Checklist

- [ ] All tests pass
- [ ] Build succeeds for all target Revit versions
- [ ] Documentation updated (README, CLAUDE.md)
- [ ] Change log documented
- [ ] Version numbers updated
- [ ] Manifest file correct
- [ ] Tested in clean Revit installation
- [ ] Dependencies verified
- [ ] Log files reviewed for errors
- [ ] Performance acceptable

---

## Appendix

### Required NuGet Packages

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="MaterialDesignThemes" Version="5.2.1" />
<PackageReference Include="MaterialDesignColors" Version="5.2.1" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.135" />
<PackageReference Include="Revit.Async" Version="2.1.1" />
```

### Optional Testing Packages

```xml
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.70" />
```

### Common Utility Patterns

#### Unit Conversion

```csharp
var unitService = ServiceLocator.GetService<IUnitConversionService>();
double feet = unitService.ToInternalUnits(100, UnitTypeId.Millimeters);
```

#### Level Retrieval

```csharp
var levelService = ServiceLocator.GetService<ILevelService>();
Level level = levelService.GetLevelByName(doc, "Level 1");
```

#### Snackbar Notifications

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
```

---

**Document Version:** 1.0
**Template Based On:** RevitCadConverter Production Project
**For Questions or Updates:** Contact your development team lead
