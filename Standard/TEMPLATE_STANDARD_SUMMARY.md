# Revit Add-in Development Template - Summary

**Production-ready template and standards for Revit plugin development**

---

## Overview

This template package provides a complete set of standards, templates, and reference materials for developing Revit add-ins based on the proven architecture of **RevitCadConverter** - a production Revit plugin supporting versions 2023-2026.

---

## Documents Included

### 1. REVIT_ADDIN_TEMPLATE_STANDARD.md
**Complete Standard - 80+ pages of detailed documentation**

Covers:
- Technology stack and framework choices
- Complete project structure and organization
- Architecture patterns (MVVM, DI, Service Layer)
- Build system and multi-version support
- Development workflows
- Coding standards and conventions
- UI/UX guidelines with Material Design
- Testing standards
- Deployment procedures
- Comprehensive checklists

**Use for:** Deep understanding of the architecture and as a reference during development.

### 2. QUICK_REFERENCE.md
**Fast-Path Reference - Common tasks and commands**

Covers:
- First-time setup commands
- Build commands
- Code templates (Command, ViewModel, Service, Transaction)
- Service registration
- Ribbon button registration
- Important patterns (with right/wrong examples)
- Async patterns
- Logging patterns
- Unit conversion
- Level operations
- Snackbar notifications
- Version-specific code
- Common issues and solutions

**Use for:** Daily development - quick lookup of common patterns and commands.

### 3. Templates/PROJECT_STRUCTURE_TEMPLATE.md
**Project Scaffolding Guide**

Covers:
- Quick start options (manual vs. copy-based)
- Essential files checklist
- Feature scaffolding checklist
- Service registration template
- Ribbon UI registration template
- Manifest template (.addin)
- .gitignore template
- Minimum feature example
- Validation checklist

**Use for:** Starting a new project or feature.

### 4. Templates/Code Templates
**Ready-to-use code templates**

| File | Purpose |
|------|---------|
| `CommandTemplate.cs` | IExternalCommand entry point |
| `ViewModelTemplate.cs` | MVVM ViewModel with DI support |
| `ServiceInterfaceTemplate.cs` | Service interface definition |
| `ServiceImplementationTemplate.cs` | Service implementation with logging |
| `ViewTemplate.xaml` | Material Design XAML view |

**Use for:** Starting new features - copy and customize.

---

## Template Features

### Architecture

```
MVVM + DI + Service Layer
    ↓
Feature-Driven Structure
    ↓
Multi-Version Support (2023-2026)
    ↓
Material Design UI
```

### Key Benefits

| Feature | Benefit |
|---------|---------|
| **MVVM with CommunityToolkit.Mvvm** | Clean separation, source generators |
| **Custom DI Container** | Lightweight, no external dependencies |
| **Feature-Driven** | Independent feature development |
| **Multi-Version Support** | Single codebase for all Revit versions |
| **Material Design UI** | Modern, consistent interface |
| **Comprehensive Logging** | Debug and troubleshoot easily |
| **Async Support** | Responsive UI with Revit.Async |
| **Service Layer** | Testable, maintainable code |

---

## Quick Start Guide

### For New Projects

1. **Copy RevitCadConverter folder:**
```powershell
Copy-Item -Path "RevitCadConverter" -Destination "YourRevitPlugin" -Recurse
```

2. **Update namespaces and names:**
```
RevitCadConverter → YourRevitPlugin
ACGCadStruct → YourPluginFolder
```

3. **Run setup:**
```powershell
cd YourRevitPlugin
.\setup-revit-libs.ps1
.\build.ps1
```

### For New Features

1. **Create feature folder:**
```
Features/
└── YourFeature/
    ├── Commands/
    ├── Models/
    ├── ViewModels/
    └── Views/
```

2. **Copy templates:**
```
Templates/CommandTemplate.cs → Commands/YourFeatureCommand.cs
Templates/ViewModelTemplate.cs → ViewModels/YourFeatureViewModel.cs
Templates/ViewTemplate.xaml → Views/YourFeatureView.xaml
```

3. **Update placeholders:**
```
[NAMESPACE] → YourNamespace
[FEATURENAME] → YourFeature
[COMMANDCLASS] → YourFeatureCommand
[VIEWMODELCLASS] → YourFeatureViewModel
```

4. **Register in App.cs:**
```csharp
// Register ribbon button
// Register service (if needed)
```

---

## File Reference

### Core Files (Required)

| File | Purpose | Template |
|------|---------|----------|
| `App.cs` | IExternalApplication entry point | Manual |
| `ServiceCollection.cs` | DI container | Copy from RevitCadConverter |
| `ServiceLocator.cs` | Global service access | Copy from RevitCadConverter |
| `FileLogger.cs` | Logging infrastructure | Copy from RevitCadConverter |
| `Styles.xaml` | Material Design styles | Copy and customize |
| `build.ps1` | Build automation | Copy and update project name |
| `setup-revit-libs.ps1` | Revit API setup | Copy as-is |

### Feature Files (Per Feature)

| File | Purpose | Template |
|------|---------|----------|
| `*Command.cs` | IExternalCommand entry point | CommandTemplate.cs |
| `*ViewModel.cs` | MVVM ViewModel | ViewModelTemplate.cs |
| `*View.xaml` | XAML View | ViewTemplate.xaml |
| `I*Service.cs` | Service interface | ServiceInterfaceTemplate.cs |
| `*Service.cs` | Service implementation | ServiceImplementationTemplate.cs |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Revit                                 │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IExternalApplication (App.cs)                       │   │
│  │  ├─ OnStartup()                                     │   │
│  │  │  ├─ InitializeDependencyInjection()              │   │
│  │  │  └─ CreateRibbonUI()                            │   │
│  │  └─ OnShutdown()                                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                  │
│                           ▼                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Ribbon Button                                       │   │
│  └──────────────────┬──────────────────────────────────┘   │
│                     ▼                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IExternalCommand (*Command.cs)                     │   │
│  │  └─ Execute()                                       │   │
│  │     ├─ RevitTask.Initialize()                       │   │
│  │     ├─ Create View                                  │   │
│  │     └─ Show Window                                  │   │
│  └──────────────────┬──────────────────────────────────┘   │
│                     ▼                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  View (XAML)                                         │   │
│  │  └─ DataContext → ViewModel                         │   │
│  └──────────────────┬──────────────────────────────────┘   │
│                     ▼                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ViewModel (MVVM)                                    │   │
│  │  ├─ ServiceLocator.GetService<T>()                  │   │
│  │  ├─ [ObservableProperty]                            │   │
│  │  └─ [RelayCommand]                                  │   │
│  └──────────────────┬──────────────────────────────────┘   │
│                     ▼                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Service Layer                                       │   │
│  │  ├─ IElementCreationService                         │   │
│  │  ├─ IFamilyService                                  │   │
│  │  ├─ ILevelService                                   │   │
│  │  └─ IYourService                                    │   │
│  └──────────────────┬──────────────────────────────────┘   │
│                     ▼                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Revit API Operations                                │   │
│  │  ├─ Transaction                                     │   │
│  │  ├─ Element Creation                                │   │
│  │  └─ Document Modification                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Coding Patterns

### 1. Property Initialization (CRITICAL)

**❌ WRONG:**
```csharp
[ObservableProperty]
private ChildViewModel child = new();  // Runs before ServiceLocator ready
```

**✅ CORRECT:**
```csharp
[ObservableProperty]
private ChildViewModel child;

public MainViewModel()
{
    // ServiceLocator is ready here
    child = new ChildViewModel();
}
```

### 2. Service Access

**From ViewModels:**
```csharp
var service = ServiceLocator.GetService<IYourService>();
```

**Factory Pattern:**
```csharp
var factory = ServiceLocator.GetService<Func<Document, ImportInstance, IService>>();
var service = factory(doc, importInstance);
```

### 3. Transaction Pattern

```csharp
using (var t = new Transaction(doc, "Operation"))
{
    t.Start();
    try
    {
        // Do work
        t.Commit();
    }
    catch
    {
        t.RollBack();
        throw;
    }
}
```

### 4. Async Pattern

```csharp
public async Task ProcessAsync(Document doc, Data data)
{
    // Heavy work off Revit thread
    var results = await Task.Run(() => ProcessData(data));

    // Back on Revit thread
    using (var t = new Transaction(doc, "Update"))
    {
        t.Start();
        // Create elements
        t.Commit();
    }
}
```

---

## Dependencies

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
<PackageReference Include="Moq" Version="4.20.70" />
```

### Local References

Revit API DLLs are stored in `lib/RevitXXXX/` folders (not in NuGet).

---

## Build System

### PowerShell Scripts

| Script | Purpose |
|--------|---------|
| `setup-revit-libs.ps1` | Copy Revit API DLLs (run once) |
| `build.ps1` | Build for specific version |
| `build.ps1 -All` | Build all versions |
| `build.ps1 -Clean` | Clean and build |

### Configurations

```
Debug2023, Release2023
Debug2024, Release2024
Debug2025, Release2025
Debug2026, Release2026
```

---

## Testing

### Unit Test Template

```csharp
public class ServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly YourService _service;

    public ServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _service = new YourService(_loggerMock.Object);
    }

    [Fact]
    public void DoSomething_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var input = new Input();

        // Act
        var result = _service.DoSomething(input);

        // Assert
        Assert.NotNull(result);
    }
}
```

---

## Deployment

### Automatic (Post-build)

The `.csproj` post-build target copies files to:
```
%APPDATA%\Autodesk\Revit\Addins\202X\YourPluginFolder\
```

### Manual Installation

1. Copy DLL to: `%APPDATA%\Autodesk\Revit\Addins\202X\YourPluginFolder\`
2. Copy `.addin` to: `%APPDATA%\Autodesk\Revit\Addins\202X\`
3. Restart Revit

---

## Support Resources

| Document | When to Use |
|----------|-------------|
| `REVIT_ADDIN_TEMPLATE_STANDARD.md` | Learning architecture, detailed reference |
| `QUICK_REFERENCE.md` | Daily development, common tasks |
| `Templates/PROJECT_STRUCTURE_TEMPLATE.md` | Starting new project |
| `Templates/*.cs` | Starting new feature/code |
| `CLAUDE.md` | Project-specific guidance |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial template based on RevitCadConverter |

---

## Contributing

When improving this template:

1. Update all documents consistently
2. Update code templates
3. Add examples for new patterns
4. Update this summary document

---

## License

This template is based on the internal architecture of RevitCadConverter.
Adapt and use for your Revit add-in development projects.

---

**Template Version:** 1.0
**Based on:** RevitCadConverter Production Project
**For Questions:** Contact your development team lead
