using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Aps.Core.Interfaces;
using Aps.Core.Logging;
using Aps.Core.Models;
using Aps.Core.Services;
using COBIeManager.Features.CobieParameters.Models;
using COBIeManager.Shared.DependencyInjection;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace COBIeManager.Features.CobieParameters.ViewModels;

/// <summary>
/// ViewModel for COBie Parameters management window
/// </summary>
public partial class CobieParametersViewModel : ObservableObject
{
    // Hardcoded APS Hub and Group IDs (from production environment)
    private const string FixedHubId = "b.1dadd7e2-fbdc-433d-a286-421687997ce7";
    private const string FixedGroupId = "1dadd7e2-fbdc-433d-a286-421687997ce7";

    private readonly ApsAuthService _authService;
    private readonly ApsSessionManager _sessionManager;
    private readonly ApsParametersService _parametersService;
    private readonly ITokenStorage _tokenStorage;
    private readonly IApsLogger? _logger;
    private UIDocument _uiDoc;

    [ObservableProperty]
    private ApsCollection? _selectedCollection;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Computed properties for UI binding
    public int SelectedCount => SelectedParameters.Count;
    public int TotalCount => Parameters.Count;
    public int FilteredCount => FilteredParameters.Count;
    public bool HasNoParameters => Parameters.Count == 0;
    public bool HasNoParametersAndNotLoading => Parameters.Count == 0 && !IsLoading;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public bool CanAddToProject => IsAuthenticated && SelectedCount > 0;
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);
    public bool HasNoSearchResults => IsSearching && FilteredCount == 0 && !IsLoading;

    public ObservableCollection<SelectableParameter> Parameters { get; }
    public ObservableCollection<SelectableParameter> SelectedParameters { get; }
    public ObservableCollection<SelectableParameter> FilteredParameters { get; }
    public ObservableCollection<ApsCollection> Collections { get; }

    public CobieParametersViewModel(UIDocument? uiDoc)
    {
        _uiDoc = uiDoc!;

        // Get services from ServiceLocator
        _authService = ServiceLocator.GetService<ApsAuthService>();
        _sessionManager = ServiceLocator.GetService<ApsSessionManager>();
        _parametersService = ServiceLocator.GetService<ApsParametersService>();
        _tokenStorage = ServiceLocator.GetService<ITokenStorage>();

        // Try to get the APS logger (optional)
        try
        {
            _logger = ServiceLocator.GetService<IApsLogger>();
        }
        catch
        {
            _logger = null;
        }

        Parameters = new ObservableCollection<SelectableParameter>();
        SelectedParameters = new ObservableCollection<SelectableParameter>();
        FilteredParameters = new ObservableCollection<SelectableParameter>();
        Collections = new ObservableCollection<ApsCollection>();

        // Subscribe to collection changes to track selection changes
        Parameters.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (SelectableParameter item in e.NewItems)
                {
                    item.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(SelectableParameter.IsSelected))
                        {
                            UpdateSelectedParameters(item);
                        }
                    };
                }
            }

            // Notify computed properties that depend on Parameters collection
            OnPropertyChanged(nameof(HasNoParameters));
            OnPropertyChanged(nameof(HasNoParametersAndNotLoading));
            OnPropertyChanged(nameof(TotalCount));
        };

        // Check authentication status on load
        if (uiDoc != null)
        {
            _ = CheckAuthStatusAsync();
        }
    }

    /// <summary>
    /// Called when IsLoading property changes
    /// </summary>
    partial void OnIsLoadingChanged(bool value)
    {
        // Notify computed properties that depend on IsLoading
        OnPropertyChanged(nameof(HasNoParametersAndNotLoading));
    }

    /// <summary>
    /// Called when SearchText property changes - updates the filtered collection
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplySearchFilter();
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(IsSearching));
        OnPropertyChanged(nameof(HasNoSearchResults));
    }

    /// <summary>
    /// Applies the current search filter to update the filtered parameters collection
    /// </summary>
    private void ApplySearchFilter()
    {
        FilteredParameters.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // No search - show all parameters
            foreach (var param in Parameters)
            {
                FilteredParameters.Add(param);
            }
        }
        else
        {
            var searchText = SearchText.ToLowerInvariant();
            foreach (var param in Parameters)
            {
                var nameMatch = param.Parameter.Name.ToLowerInvariant().Contains(searchText);
                var descMatch = !string.IsNullOrEmpty(param.Parameter.Description) &&
                                param.Parameter.Description.ToLowerInvariant().Contains(searchText);

                if (nameMatch || descMatch)
                {
                    FilteredParameters.Add(param);
                }
            }
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

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            _logger?.Info("[ViewModel] LoginAsync started");
            StatusMessage = "Opening browser for authentication...";

            _authService.OpenLoginInBrowser();
            StatusMessage = "Waiting for authentication...";

            // Wait for the authorization code
            _logger?.Info("[ViewModel] Waiting for authorization code...");
            string code = await _authService.WaitForAuthorizationCodeAsync();
            _logger?.Info($"[ViewModel] Authorization code received: {code.Substring(0, Math.Min(10, code.Length))}...");

            StatusMessage = "Exchanging authorization code for token...";
            var tokenResponse = await _authService.ExchangeCodeForTokenAsync(code);

            // Store tokens in session manager
            _sessionManager.SetTokens(
                tokenResponse.access_token ?? throw new Exception("No access token received"),
                tokenResponse.refresh_token ?? throw new Exception("No refresh token received"),
                tokenResponse.expires_in);

            _logger?.Info("[ViewModel] Tokens stored in session manager");

            // Persist tokens
            _tokenStorage.SaveToken(
                _sessionManager.AccessToken,
                _sessionManager.RefreshToken,
                _sessionManager.TokenExpiresAt);

            _logger?.Info("[ViewModel] Tokens persisted to storage");

            IsAuthenticated = true;
            StatusMessage = "Authentication successful! Loading collections...";
            _logger?.Info("[ViewModel] Authentication completed successfully");

            // Load collections directly using fixed hub/group
            await LoadCollectionsAsync();
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Login failed", ex);
            StatusMessage = $"Login failed: {ex.Message}";
            IsAuthenticated = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _logger?.Info("[ViewModel] Logout requested");
        _sessionManager.ClearSession();
        SelectedCollection = null;
        IsAuthenticated = false;
        Collections.Clear();
        Parameters.Clear();
        SelectedParameters.Clear();
        StatusMessage = "Logged out successfully";
    }

    private async Task CheckAuthStatusAsync()
    {
        try
        {
            _logger?.Info("[ViewModel] Checking stored authentication status...");

            // Try to load stored token
            var storedToken = _tokenStorage.GetToken();
            if (storedToken != null)
            {
                _logger?.Info("[ViewModel] Stored token found, loading into session...");

                // Load tokens into session manager (even if expired, we need the refresh token)
                _sessionManager.SetTokens(
                    storedToken.Value.accessToken,
                    storedToken.Value.refreshToken,
                    storedToken.Value.expiresAt);

                // Check if access token is still valid
                bool isAccessTokenValid = _sessionManager.IsTokenValid();
                _logger?.Info($"[ViewModel] Access token valid: {isAccessTokenValid}");

                // Always attempt refresh if we have a refresh token
                // This handles both: expiring soon OR already expired
                try
                {
                    _logger?.Info("[ViewModel] Attempting to ensure token is valid via refresh...");
                    if (await _sessionManager.EnsureTokenValidAsync())
                    {
                        _logger?.Info("[ViewModel] Token is valid (refreshed or already good), loading collections...");
                        IsAuthenticated = true;
                        StatusMessage = "Authentication restored. Loading collections...";
                        await LoadCollectionsAsync();
                        return;
                    }
                    else
                    {
                        // Refresh failed and token was expired
                        _logger?.Warn("[ViewModel] Token validation failed and token is expired, user needs to login");
                        StatusMessage = "Could not refresh your session. Please login again.";
                    }
                }
                catch (RefreshTokenExpiredException)
                {
                    // Refresh token itself has expired (~15 days)
                    _logger?.Info("[ViewModel] Refresh token has expired, user must login again");
                    StatusMessage = "Your session has expired (refresh token expired). Please login again.";
                }
                catch (TokenRefreshException ex)
                {
                    // Token refresh failed for other reasons
                    _logger?.Error($"[ViewModel] Token refresh failed: {ex.Message}");
                    StatusMessage = $"Failed to refresh session: {ex.Message}. Please login again.";
                }
            }
            else
            {
                _logger?.Info("[ViewModel] No stored token found");
                StatusMessage = "Please login to access COBie parameters.";
            }

            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Error checking auth status", ex);
            IsAuthenticated = false;
            StatusMessage = $"Authentication error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadParameters()
    {
        await LoadParametersAsync();
    }

    /// <summary>
    /// Load collections using the fixed hub and group IDs.
    /// </summary>
    private async Task LoadCollectionsAsync()
    {
        _logger?.Info($"[ViewModel] Loading collections for hub: {FixedHubId}, group: {FixedGroupId}");

        StatusMessage = "Loading collections...";
        IsLoading = true;

        // Minimum display time for loading state (500ms)
        var minDisplayTime = TimeSpan.FromMilliseconds(500);
        var startTime = DateTime.UtcNow;

        // Force UI update to show the spinner
        await Task.Delay(50);

        try
        {
            await _sessionManager.EnsureTokenValidAsync();
            StatusMessage = "Fetching available collections...";

            // Run the network call on a background thread to keep UI responsive
            var collections = await Task.Run(async () => await _parametersService.GetCollectionsAsync(FixedHubId, FixedGroupId));

            Collections.Clear();
            foreach (var collection in collections)
            {
                Collections.Add(collection);
            }

            StatusMessage = $"Loaded {Collections.Count} collection(s). Please select a collection to continue.";
            _logger?.Info($"[ViewModel] Loaded {Collections.Count} collections");

            // Auto-select COBie collection if exists, otherwise first collection
            var defaultCollection = Collections.FirstOrDefault(c => c.IsDefaultCobieCollection) ?? Collections.FirstOrDefault();
            if (defaultCollection != null)
            {
                SelectedCollection = defaultCollection;
            }
        }
        catch (RefreshTokenExpiredException)
        {
            _logger?.Warn("[ViewModel] Refresh token expired while loading collections");
            IsAuthenticated = false;
            StatusMessage = "Your session has expired. Please login again.";
        }
        catch (TokenRefreshException ex)
        {
            _logger?.Error("[ViewModel] Token refresh failed while loading collections", ex);
            StatusMessage = $"Failed to refresh token: {ex.Message}. Please try logging in again.";
            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Failed to load collections", ex);
            StatusMessage = $"Failed to load collections: {ex.Message}";
        }
        finally
        {
            // Ensure minimum display time for smooth UI
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed < minDisplayTime)
            {
                await Task.Delay((minDisplayTime - elapsed).Milliseconds);
            }
            IsLoading = false;
        }
    }

    partial void OnSelectedCollectionChanged(ApsCollection? value)
    {
        // Auto-load parameters when collection is selected
        Parameters.Clear();
        SelectedParameters.Clear();
        FilteredParameters.Clear();
        SearchText = string.Empty; // Clear search when collection changes

        if (value != null)
        {
            StatusMessage = $"Collection '{value.Name}' selected. Loading parameters...";
            _ = LoadParametersAsync();
        }
    }

    private async Task LoadParametersAsync()
    {
        if (SelectedCollection == null)
        {
            StatusMessage = "Please select a collection first";
            return;
        }

        _logger?.Info($"[ViewModel] Loading parameters for collection: {SelectedCollection.Id}");

        StatusMessage = "Loading COBie parameters...";
        IsLoading = true;

        // Minimum display time for loading state (500ms)
        var minDisplayTime = TimeSpan.FromMilliseconds(500);
        var startTime = DateTime.UtcNow;

        // Force UI update to show the spinner
        await Task.Delay(50);

        try
        {
            await _sessionManager.EnsureTokenValidAsync();
            StatusMessage = "Fetching parameters from APS...";

            // Run the network call on a background thread to keep UI responsive
            var response = await Task.Run(async () => await _parametersService.GetParametersAsync(
                FixedHubId,
                SelectedCollection.Id));

            Parameters.Clear();

            foreach (var param in response.Parameters)
            {
                var selectableParam = new SelectableParameter { Parameter = param };
                Parameters.Add(selectableParam);
                FilteredParameters.Add(selectableParam);
            }

            var cacheStatus = response.Cached ? " (cached)" : "";
            StatusMessage = $"Loaded {Parameters.Count} COBie parameters{cacheStatus}";
            _logger?.Info($"[ViewModel] Loaded {Parameters.Count} parameters");
        }
        catch (RefreshTokenExpiredException)
        {
            _logger?.Warn("[ViewModel] Refresh token expired while loading parameters");
            IsAuthenticated = false;
            StatusMessage = "Your session has expired. Please login again.";
        }
        catch (TokenRefreshException ex)
        {
            _logger?.Error("[ViewModel] Token refresh failed while loading parameters", ex);
            StatusMessage = $"Failed to refresh token: {ex.Message}. Please try logging in again.";
            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Failed to load parameters", ex);
            StatusMessage = $"Failed to load parameters: {ex.Message}";
        }
        finally
        {
            // Ensure minimum display time for smooth UI
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed < minDisplayTime)
            {
                await Task.Delay((minDisplayTime - elapsed).Milliseconds);
            }
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

        try
        {
            // Get the selected parameter definitions
            var definitions = SelectedParameters.Select(p => p.Parameter).ToList();

            // Get services
            var creationService = ServiceLocator.GetService<IParameterCreationService>();
            var bindingService = ServiceLocator.GetService<IParameterBindingService>();
            var conflictService = ServiceLocator.GetService<IParameterConflictService>();

            // Get the Revit document
            var document = _uiDoc.Document;

            // Show loading overlay and let it render before starting work
            LoadingOverlayService.Show("Adding parameters to project...", $"Processing {definitions.Count} parameters");

            // Give UI time to render the overlay
            await Task.Delay(100);

            // Detect conflicts (must run on main thread - Revit API requirement)
            LoadingOverlayService.UpdateMessage("Detecting conflicts...", "Checking for existing parameters");
            await Task.Delay(30);
            var conflicts = conflictService.DetectConflicts(document, definitions);

            // Build a conflict lookup for efficient access
            var conflictMap = conflicts.ToDictionary(c => c.Definition.Id, c => c);

            // Determine which parameters to create and which to skip
            var definitionsToCreate = new List<CobieParameterDefinition>();
            var skippedInfos = new List<Shared.Interfaces.SkippedParameterInfo>();

            // First, filter out parameters without categories - they cannot be bound
            foreach (var definition in definitions)
            {
                if (definition.CategoryBindingIds == null || definition.CategoryBindingIds.Length == 0)
                {
                    skippedInfos.Add(new Shared.Interfaces.SkippedParameterInfo
                    {
                        Name = definition.Name,
                        Reason = "Parameter has no categories defined in APS - cannot bind to Revit categories"
                    });
                    _logger?.Warn($"[ViewModel] Skipping parameter '{definition.Name}' - no categories defined");
                    continue;
                }

                if (conflictMap.TryGetValue(definition.Id, out var conflict))
                {
                    // Skip if there's an actual conflict (type mismatch, etc.)
                    if (conflict.ConflictType != ParameterConflictType.None)
                    {
                        skippedInfos.Add(new Shared.Interfaces.SkippedParameterInfo
                        {
                            Name = definition.Name,
                            Reason = conflict.Description
                        });
                        continue;
                    }

                    // Skip if parameter already exists (exact match - ExistingParameter is not null)
                    if (conflict.ExistingParameter != null)
                    {
                        skippedInfos.Add(new Shared.Interfaces.SkippedParameterInfo
                        {
                            Name = definition.Name,
                            Reason = "Parameter already exists in document"
                        });
                        continue;
                    }

                    // No conflict and doesn't exist - create it
                    definitionsToCreate.Add(definition);
                }
                else
                {
                    // No conflict info - treat as new parameter
                    definitionsToCreate.Add(definition);
                }
            }

            // Update selected parameters to only those we'll actually try to create
            if (definitionsToCreate.Count == 0)
            {
                LoadingOverlayService.Hide();
                if (skippedInfos.Count > 0)
                {
                    StatusMessage = $"All {skippedInfos.Count} selected parameters were skipped. Check results for details.";
                }
                else
                {
                    StatusMessage = "All selected parameters already exist in the document.";
                }
                return;
            }

            // Update loading message for creation phase
            LoadingOverlayService.UpdateMessage($"Creating {definitionsToCreate.Count} COBie parameters...", "This may take a moment...");
            await Task.Delay(30);

            // Start a transaction for parameter creation (must run on main thread - Revit API requirement)
            using var transaction = new Transaction(document, "Create COBie Parameters");
            transaction.Start();

            Shared.Interfaces.ParameterCreationResult creationResult;
            Shared.Interfaces.ParameterBindingResult bindingResult;

            try
            {
                // Create parameters
                creationResult = creationService.CreateParameters(document, definitionsToCreate);

                // Bind parameters to categories
                LoadingOverlayService.UpdateMessage($"Binding {definitionsToCreate.Count} parameters to categories...", "This may take a moment...");
                await Task.Delay(30);
                bindingResult = bindingService.BindParameters(document, definitionsToCreate);

                transaction.Commit();
            }
            catch
            {
                transaction.RollBack();
                throw;
            }

            // Hide loading overlay before showing results
            LoadingOverlayService.Hide();

            // Show results - combine creation result with our manually tracked skipped parameters
            var allSkipped = new List<Shared.Interfaces.SkippedParameterInfo>(skippedInfos);
            allSkipped.AddRange(creationResult.SkippedParameters);

            // Collect errors from creation and binding
            var allErrors = new List<string>(creationResult.Errors);
            allErrors.AddRange(bindingResult.Errors);

            ShowResults(creationResult, bindingResult, allSkipped, allErrors);

            StatusMessage = $"Created {creationResult.CreatedCount} parameters. " +
                           $"Bound {bindingResult.BoundCount} parameters. " +
                           $"Skipped {allSkipped.Count} parameters. " +
                           $"Check the results window for details on which categories the parameters are bound to.";
        }
        catch (Exception ex)
        {
            LoadingOverlayService.Hide();
            _logger?.Error("[ViewModel] Failed to add parameters to project", ex);
            StatusMessage = $"Failed to add parameters: {ex.Message}";
        }
    }

    /// <summary>
    /// Shows the parameter creation results dialog.
    /// </summary>
    private void ShowResults(
        Shared.Interfaces.ParameterCreationResult creationResult,
        Shared.Interfaces.ParameterBindingResult bindingResult,
        System.Collections.Generic.IEnumerable<Shared.Interfaces.SkippedParameterInfo>? additionalSkipped = null,
        System.Collections.Generic.IEnumerable<string>? additionalErrors = null)
    {
        var allSkipped = new List<Shared.Interfaces.SkippedParameterInfo>();
        if (additionalSkipped != null)
        {
            allSkipped.AddRange(additionalSkipped);
        }
        allSkipped.AddRange(creationResult.SkippedParameters);

        var allErrors = new List<string>();
        if (additionalErrors != null)
        {
            allErrors.AddRange(additionalErrors);
        }

        // Enhance created parameters with binding information
        var enhancedCreatedParams = new List<Shared.Interfaces.CreatedParameterInfo>();
        foreach (var createdParam in creationResult.CreatedParameters)
        {
            var boundParam = bindingResult.BoundParameters.FirstOrDefault(bp => bp.Name == createdParam.Name);
            if (boundParam != null)
            {
                // Update with binding information
                createdParam.ParameterElementId = boundParam.ParameterElementId;
            }
            enhancedCreatedParams.Add(createdParam);
        }

        var resultWindow = new Views.ParameterCreationResultWindow();
        resultWindow.ViewModel.SetResults(
            creationResult.CreatedCount,
            allSkipped.Count,
            allErrors.Count,
            enhancedCreatedParams,
            allSkipped,
            allErrors,
            bindingResult.BoundParameters);

        resultWindow.Owner = Application.Current?.MainWindow;
        resultWindow.ShowDialog();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var param in Parameters)
        {
            param.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var param in Parameters)
        {
            param.IsSelected = false;
        }
        SelectedParameters.Clear();
    }

    /// <summary>
    /// Updates the SelectedParameters collection when a parameter's IsSelected property changes.
    /// </summary>
    private void UpdateSelectedParameters(SelectableParameter parameter)
    {
        if (parameter.IsSelected)
        {
            if (!SelectedParameters.Contains(parameter))
            {
                SelectedParameters.Add(parameter);
            }
        }
        else
        {
            SelectedParameters.Remove(parameter);
        }

        // Notify that computed properties have changed
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanAddToProject));
    }

    partial void OnIsAuthenticatedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddToProject));
    }
}
