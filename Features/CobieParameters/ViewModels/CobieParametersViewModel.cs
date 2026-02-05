using System;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COBIeManager.Features.CobieParameters.Models;
using COBIeManager.Shared.APS;
using COBIeManager.Shared.APS.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.DependencyInjection;
using COBIeManager.Shared.Services;
using System.Collections.ObjectModel;

namespace COBIeManager.Features.CobieParameters.ViewModels;

/// <summary>
/// ViewModel for COBie Parameters management window
/// </summary>
public partial class CobieParametersViewModel : ObservableObject
{
    private readonly IApsBridgeClient _bridgeClient;
    private readonly ApsBridgeProcessService? _bridgeProcessService;
    private UIDocument _uiDoc;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CobieParameterDefinition> Parameters { get; }
    public ObservableCollection<CobieParameterDefinition> SelectedParameters { get; }

    public CobieParametersViewModel(UIDocument? uiDoc)
    {
        _uiDoc = uiDoc!;

        // Get services from ServiceLocator
        _bridgeClient = ServiceLocator.GetService<IApsBridgeClient>();
        _bridgeProcessService = ServiceLocator.TryGetService<ApsBridgeProcessService>();

        Parameters = new ObservableCollection<CobieParameterDefinition>();
        SelectedParameters = new ObservableCollection<CobieParameterDefinition>();

        // Only check auth status if we have a valid UIDocument
        if (uiDoc != null)
        {
            // Check authentication status on load (bridge will auto-start if needed)
            _ = CheckAuthStatusAsync();
        }
    }

    /// <summary>
    /// Set or update the UI document after construction
    /// </summary>
    public void SetUiDocument(UIDocument uiDoc)
    {
        _uiDoc = uiDoc;
        // Check authentication status now that we have a valid document
        _ = CheckAuthStatusAsync();
    }

    /// <summary>
    /// Ensure the bridge is running before making API calls
    /// </summary>
    private async Task<bool> EnsureBridgeRunningAsync()
    {
        if (_bridgeProcessService != null)
        {
            return await _bridgeProcessService.EnsureBridgeRunningAsync();
        }
        return true;
    }

    /// <summary>
    /// Update activity timestamp to prevent bridge from shutting down
    /// </summary>
    private void UpdateActivity()
    {
        _bridgeProcessService?.UpdateActivity();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            if (!await EnsureBridgeRunningAsync())
            {
                StatusMessage = "Failed to start APS Bridge. Please check if it's installed.";
                return;
            }

            StatusMessage = "Opening browser for authentication...";
            UpdateActivity();
            await _bridgeClient.LoginAsync();
            StatusMessage = "Please complete authentication in browser";
            await PollForAuthAsync();
        }
        catch (ApsBridgeClientException ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login error: {ex.Message}";
        }
    }

    private async Task CheckAuthStatusAsync()
    {
        try
        {
            await EnsureBridgeRunningAsync();
            UpdateActivity();
            var status = await _bridgeClient.GetAuthStatusAsync();
            IsAuthenticated = status.IsAuthenticated;
            if (IsAuthenticated && !string.IsNullOrEmpty(status.AccountId))
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
            try
            {
                UpdateActivity();
                var status = await _bridgeClient.GetAuthStatusAsync();
                if (status.IsAuthenticated)
                {
                    IsAuthenticated = true;
                    if (!string.IsNullOrEmpty(status.AccountId))
                    {
                        await LoadParametersAsync(status.AccountId);
                    }
                    return;
                }
            }
            catch
            {
                // Continue polling
            }
        }
        StatusMessage = "Authentication timed out. Please try again.";
    }

    [RelayCommand]
    private async Task LoadParameters()
    {
        await LoadParametersAsync(null);
    }

    private async Task LoadParametersAsync(string? accountId)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            try
            {
                await EnsureBridgeRunningAsync();
                UpdateActivity();
                var status = await _bridgeClient.GetAuthStatusAsync();
                accountId = status.AccountId;
            }
            catch
            {
                StatusMessage = "Cannot load parameters: not authenticated";
                return;
            }
        }

        if (string.IsNullOrEmpty(accountId))
        {
            StatusMessage = "Cannot load parameters: account ID not found";
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading COBie parameters...";

        try
        {
            await EnsureBridgeRunningAsync();
            UpdateActivity();
            var response = await _bridgeClient.GetParametersAsync(accountId);
            Parameters.Clear();
            foreach (var param in response.Parameters)
            {
                // The response already returns CobieParameterDefinition objects
                // Just add them directly to the collection
                Parameters.Add(param);
            }

            var cacheStatus = response.Cached ? " (cached)" : "";
            StatusMessage = $"Loaded {Parameters.Count} COBie parameters{cacheStatus}";
        }
        catch (ApsBridgeClientException ex)
        {
            StatusMessage = $"Failed to load parameters: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading parameters: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddToProject()
    {
        if (SelectedParameters.Count == 0)
        {
            StatusMessage = "No parameters selected";
            return;
        }

        StatusMessage = "Adding parameters to project...";

        // TODO: Implement parameter creation via CobieParameterService
        await Task.Delay(100); // Placeholder
        StatusMessage = "Parameter creation not yet implemented";
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
