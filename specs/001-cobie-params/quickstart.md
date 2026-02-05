# Quickstart Guide: COBie Parameter Management

**Feature**: 001-cobie-params
**Target Audience**: Developers implementing this feature

---

## Overview

This guide provides step-by-step instructions for implementing the COBie Parameter Management feature in the COBIe Manager Revit add-in.

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Workflow                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Launch COBIe Manager add-in in Revit                         │
│  2. Click "COBie Parameters" button                              │
│  3. Authenticate with APS (via browser)                          │
│  4. Browse and search COBie parameters                           │
│  5. Select parameters to add                                     │
│  6. Click "Add to Project"                                       │
│  7. View summary of created parameters                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       Technical Architecture                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐         HTTP          ┌─────────────────┐ │
│  │ Revit Add-in     │ ─────────────────────▶│  APS Bridge     │ │
│  │  (net48)         │   localhost:5000       │   (net8.0)      │ │
│  │                  │                        │                 │ │
│  │  - WPF UI        │                        │  - ACG.APS.Core│ │
│  │  - Revit API     │                        │  - OAuth       │ │
│  │  - Bridge Client │                        │  - APS API     │ │
│  └──────────────────┘                        └────────┬────────┘ │
│                                                        │         │
│                                                        ▼         │
│                                              ┌─────────────────┐ │
│                                              │  APS Cloud      │ │
│                                              │  Parameters API │ │
│                                              └─────────────────┘ │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Create APS Bridge Project

**Location**: Create new project at solution root

**Note**: This bridge will reuse ACG.APS.Core for authentication, using the same OAuth 2.0 PKCE flow and file-based token storage.

```bash
# Create new .NET 8.0 console app
dotnet new console -n APS.Bridge

# Add required packages
dotnet add APS.Bridge package Microsoft.AspNetCore.App
dotnet add APS.Bridge reference ../ACG-BulkFoldersUpload/ACG.Aps.Core/ACG.Aps.Core.csproj
```

**File: APS.Bridge/Program.cs**
```csharp
using ACG.Aps.Core.Services;
using ACG.Aps.Core.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Services
var tokenStorage = new TokenStorage();
var authService = new ApsAuthService();
var sessionManager = new ApsSessionManager(authService, tokenStorage);

// TODO: Add controllers for auth, parameters endpoints

app.MapGet("/health", () => new { status = "healthy", version = "1.0.0" });

app.Run();
```

---

### Step 2: Implement Bridge Controllers

**File: APS.Bridge/Controllers/AuthController.cs**
```csharp
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ApsSessionManager _sessionManager;
    private readonly ApsAuthService _authService;

    [HttpPost("login")]
    public IActionResult Login()
    {
        // Open browser for OAuth
        _authService.OpenLoginInBrowser();
        return Ok(new { loginUrl = _authService.BuildLoginUrl() });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isAuthenticated = !string.IsNullOrEmpty(_sessionManager.AccessToken),
            accountId = _sessionManager.AccountId,
            expiresAt = _sessionManager.TokenExpiresAt
        });
    }

    // TODO: Implement callback, refresh, logout
}
```

**File: APS.Bridge/Controllers/ParametersController.cs**
```csharp
[ApiController]
[Route("parameters")]
public class ParametersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetParameters(
        string accountId,
        string? collectionId = null,
        bool forceRefresh = false)
    {
        // TODO: Call APS Parameters API
        // Return CobieParameterDefinition[]
        await Task.CompletedTask;
        return Ok(new { parameters = Array.Empty<object>() });
    }

    // TODO: Implement specs, categories, search
}
```

---

### Step 3: Implement Bridge Client in Add-in

**File: Shared/APS/ApsBridgeClient.cs**
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json; // Use Newtonsoft for .NET Framework 4.8

namespace COBIeManager.Shared.APS;

public class ApsBridgeClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000";

    public ApsBridgeClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthStatus> GetAuthStatusAsync()
    {
        var response = await _httpClient.GetAsync("/auth/status");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthStatus>(json);
    }

    public async Task LoginAsync()
    {
        var response = await _httpClient.PostAsync("/auth/login", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<CobieParameterDefinition>> GetParametersAsync(
        string accountId,
        string? collectionId = null,
        bool forceRefresh = false)
    {
        var query = new Dictionary<string, string?>
        {
            ["accountId"] = accountId,
            ["collectionId"] = collectionId,
            ["forceRefresh"] = forceRefresh.ToString()
        };

        var url = QueryHelpers.AddQueryString("/parameters", query);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<ApsParametersResponse>(json);
        return data.Parameters;
    }

    public void Dispose() => _httpClient.Dispose();
}
```

---

### Step 4: Create Feature Command

**File: Features/CobieParameters/Commands/CobieParametersCommand.cs**
```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using COBIeManager.Shared.DependencyInjection;
using RevitAsync;

namespace COBIeManager.Features.CobieParameters.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CobieParametersCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        // MUST be called first for async support
        RevitTask.Initialize();

        var uiDoc = commandData.Application.ActiveUIDocument;
        var task = ShowCobieParametersWindowAsync(uiDoc);
        task.Wait();

        return Result.Succeeded;
    }

    private async Task ShowCobieParametersWindowAsync(UIDocument uiDoc)
    {
        var viewModel = new CobieParametersViewModel(uiDoc);
        var window = new CobieParametersWindow { ViewModel = viewModel };
        window.ShowDialog();
    }
}
```

---

### Step 5: Create ViewModels

**File: Features/CobieParameters/ViewModels/CobieParametersViewModel.cs**
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using COBIeManager.Shared.APS;

namespace COBIeManager.Features.CobieParameters.ViewModels;

public partial class CobieParametersViewModel : ObservableObject
{
    private readonly ApsBridgeClient _bridgeClient;
    private readonly UIDocument _uiDoc;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CobieParameterDefinition> Parameters { get; }
    public ObservableCollection<CobieParameterDefinition> SelectedParameters { get; }

    public CobieParametersViewModel(UIDocument uiDoc)
    {
        _uiDoc = uiDoc;
        _bridgeClient = new ApsBridgeClient();
        Parameters = new ObservableCollection<CobieParameterDefinition>();
        SelectedParameters = new ObservableCollection<CobieParameterDefinition>();

        // Check authentication status on load
        _ = CheckAuthStatusAsync();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            StatusMessage = "Opening browser for authentication...";
            await _bridgeClient.LoginAsync();
            StatusMessage = "Please complete authentication in browser";
            await PollForAuthAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
        }
    }

    private async Task CheckAuthStatusAsync()
    {
        try
        {
            var status = await _bridgeClient.GetAuthStatusAsync();
            IsAuthenticated = status.IsAuthenticated;
            if (IsAuthenticated)
            {
                await LoadParametersAsync(status.AccountId);
            }
        }
        catch
        {
            IsAuthenticated = false;
        }
    }

    private async Task PollForAuthAsync()
    {
        // Poll every 2 seconds for up to 2 minutes
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(2000);
            var status = await _bridgeClient.GetAuthStatusAsync();
            if (status.IsAuthenticated)
            {
                IsAuthenticated = true;
                await LoadParametersAsync(status.AccountId);
                return;
            }
        }
        StatusMessage = "Authentication timed out";
    }

    [RelayCommand]
    private async Task LoadParametersAsync(string accountId)
    {
        IsLoading = true;
        StatusMessage = "Loading COBie parameters...";

        try
        {
            var parameters = await _bridgeClient.GetParametersAsync(accountId);
            Parameters.Clear();
            foreach (var param in parameters)
            {
                Parameters.Add(param);
            }
            StatusMessage = $"Loaded {parameters.Count} parameters";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load parameters: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddToProjectAsync()
    {
        if (SelectedParameters.Count == 0)
        {
            StatusMessage = "No parameters selected";
            return;
        }

        StatusMessage = "Adding parameters to project...";

        // TODO: Implement parameter creation via CobieParameterService
        await Task.CompletedTask;
        StatusMessage = "Parameters added successfully";
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var param in Parameters)
        {
            if (!SelectedParameters.Contains(param))
            {
                SelectedParameters.Add(param);
            }
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        SelectedParameters.Clear();
    }
}
```

---

### Step 6: Create View

**File: Features/CobieParameters/Views/CobieParametersWindow.xaml**
```xml
<materialDesign:Card
    x:Class="COBIeManager.Features.CobieParameters.Views.CobieParametersWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
    Title="COBie Parameters" Height="600" Width="800"
    WindowStartupLocation="CenterOwner">

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Auth Section -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
            <TextBlock Text="Authentication Status:" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding IsAuthenticated, Converter={StaticResource BoolToAuthStatusConverter}}"
                       Margin="8,0,16,0" VerticalAlignment="Center"/>
            <Button Content="Login" Command="{Binding LoginCommand}"
                    Visibility="{Binding IsAuthenticated, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
        </StackPanel>

        <!-- Search/Filter -->
        <Grid Grid.Row="1" Margin="0,0,0,16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBox Grid.Column="0" materialDesign:HintAssist.Hint="Search parameters..."
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>

            <StackPanel Grid.Column="1" Orientation="Horizontal">
                <Button Content="Select All" Command="{Binding SelectAllCommand}" Margin="4,0"/>
                <Button Content="Deselect All" Command="{Binding DeselectAllCommand}" Margin="4,0"/>
            </StackPanel>
        </Grid>

        <!-- Parameters List -->
        <ListBox Grid.Row="2" ItemsSource="{Binding Parameters}"
                 SelectionMode="Extended"
                 materialDesign:ListBoxAssist.CheckBoxMode="None">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <CheckBox IsChecked="{Binding IsSelected}"
                              Content="{Binding Name}"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Footer -->
        <Grid Grid.Row="3" Margin="0,16,0,0">
            <TextBlock Text="{Binding StatusMessage}" VerticalAlignment="Center"/>
            <Button Content="Add to Project" Command="{Binding AddToProjectCommand}"
                    HorizontalAlignment="Right" Style="{StaticResource MaterialDesignRaisedButton}"/>
        </Grid>
    </Grid>
</materialDesign:Card>
```

---

### Step 7: Register Services

**File: App.cs - Update OnStartup method**
```csharp
protected override Result OnStartup(UIControlledApplication application)
{
    // ... existing code ...

    // Register new services
    services.RegisterSingleton<ICobieParameterService, CobieParameterService>();
    services.RegisterSingleton<IWindowsCredentialTokenStorage, WindowsCredentialTokenStorage>();

    // Create ribbon button for COBie Parameters
    var panel = application.CreateRibbonPanel("COBIe Manager");

    var buttonData = new PushButtonData(
        "CobieParametersBtn",
        "COBie Parameters",
        assemblyPath,
        "COBIeManager.Features.CobieParameters.Commands.CobieParametersCommand");

    var button = panel.AddItem(buttonData) as PushButton;
    button.ToolTip = "Manage COBie parameters from Autodesk Platform Services";

    return Result.Succeeded;
}
```

---

## Development Workflow

### Running the Application

1. **Start the APS Bridge** (in a separate terminal):
   ```bash
   cd APS.Bridge
   dotnet run
   ```

2. **Build and Debug the Revit Add-in**:
   - Open `COBIeManager.csproj` in Visual Studio
   - Set configuration to `Debug2024` (or your Revit version)
   - Press F5 to start debugging

3. **Test in Revit**:
   - Open a Revit project
   - Click the "COBie Parameters" button in the COBIe Manager ribbon
   - Authenticate with APS
   - Browse and select parameters
   - Add to project

---

## Testing Checklist

- [ ] Bridge starts successfully on localhost:5000
- [ ] Health check endpoint returns 200
- [ ] OAuth login opens browser
- [ ] Authentication status updates after login
- [ ] Parameters load from APS
- [ ] Search/filter works
- [ ] Multi-select works
- [ ] Parameters are created in Revit
- [ ] Duplicate detection works
- [ ] Category binding is correct
- [ ] Offline mode works (cached parameters)

---

## Troubleshooting

### Bridge won't start
- Check if port 5000 is already in use
- Verify .NET 8.0 SDK is installed

### Authentication fails
- Verify APS credentials
- Check redirect URI matches
- Review browser console for OAuth errors

### Parameters won't load
- Verify account ID is correct
- Check bridge logs for APS API errors
- Ensure COBie collection exists in APS

### Revit parameter creation fails
- Check Revit journal for errors
- Verify shared parameter file path
- Ensure document is modifiable (not read-only)

---

## Next Steps

After completing the quickstart:

1. Review `data-model.md` for complete entity definitions
2. Review `contracts/aps-bridge-api.yaml` for full API specification
3. Run `/speckit.tasks` to generate implementation tasks
4. Begin implementation following the task order
