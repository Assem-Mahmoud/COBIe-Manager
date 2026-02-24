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

    // Track selected parameters across collections (preserves selections when switching collections)
    private readonly HashSet<string> _selectedParameterIds = new();

    // Cache all parameters by collection ID for local filtering
    private readonly Dictionary<string, List<SelectableParameter>> _allParametersByCollection = new();

    // Special "All" collection that combines all parameters
    private ApsCollection? _allCollection;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _labelSearchText = string.Empty;

    /// <summary>
    /// When false, parameter values are aligned across group instances (default - recommended for COBie).
    /// When true, parameter values can vary per group instance.
    /// This controls the "values are aligned per group" option in Revit.
    /// </summary>
    [ObservableProperty]
    private bool _variesAcrossGroups = false;

    // Computed properties for UI binding
    public int SelectedCount => _selectedParameterIds.Count;
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
    public ObservableCollection<ApsLabel> Labels { get; }
    public ObservableCollection<ApsLabel> FilteredLabels { get; }

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
        Labels = new ObservableCollection<ApsLabel>();
        FilteredLabels = new ObservableCollection<ApsLabel>();

        // Helper method to subscribe to PropertyChanged for a parameter
        void SubscribeToParameter(SelectableParameter item)
        {
            item.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SelectableParameter.IsSelected))
                {
                    UpdateSelectedParameters(item);
                }
            };
        }

        // Subscribe to Parameters collection changes
        Parameters.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (SelectableParameter item in e.NewItems)
                {
                    SubscribeToParameter(item);
                }
            }

            // Notify computed properties that depend on Parameters collection
            OnPropertyChanged(nameof(HasNoParameters));
            OnPropertyChanged(nameof(HasNoParametersAndNotLoading));
            OnPropertyChanged(nameof(TotalCount));
        };

        // Subscribe to FilteredParameters collection changes (for DataGrid binding)
        FilteredParameters.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (SelectableParameter item in e.NewItems)
                {
                    // Only subscribe if not already subscribed (avoid duplicate subscriptions)
                    if (!Parameters.Contains(item))
                    {
                        SubscribeToParameter(item);
                    }
                }
            }
        };

        // Check authentication status on load
        if (uiDoc != null)
        {
            _ = CheckAuthStatusAsync();
        }
    }

    /// <summary>
    /// Called when a label's IsSelected property changes - updates the filter
    /// </summary>
    public void OnLabelSelectionChanged()
    {
        ApplySearchFilter();
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
    }

    [RelayCommand]
    private void ToggleLabel(ApsLabel label)
    {
        if (label == null) return;
        label.IsSelected = !label.IsSelected;
        // The PropertyChanged event from the label will trigger OnLabelSelectionChanged
    }

    [RelayCommand]
    private void ClearLabelFilters()
    {
        LabelSearchText = string.Empty;
        foreach (var label in Labels)
        {
            label.IsSelected = false;
        }
        // Filter will be updated automatically via PropertyChanged events
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
    /// Called when LabelSearchText property changes - filters the labels list
    /// </summary>
    partial void OnLabelSearchTextChanged(string value)
    {
        FilterLabels();
    }

    /// <summary>
    /// Filters the labels collection based on LabelSearchText and updates FilteredLabels
    /// </summary>
    private void FilterLabels()
    {
        FilteredLabels.Clear();

        if (string.IsNullOrWhiteSpace(LabelSearchText))
        {
            // No search - show all labels
            foreach (var label in Labels)
            {
                FilteredLabels.Add(label);
            }
        }
        else
        {
            var searchText = LabelSearchText.ToLowerInvariant();
            foreach (var label in Labels)
            {
                if (label.Name.ToLowerInvariant().Contains(searchText))
                {
                    FilteredLabels.Add(label);
                }
            }
        }
    }

    /// <summary>
    /// Applies the current search filter and label filter to update the filtered parameters collection
    /// </summary>
    private void ApplySearchFilter()
    {
        FilteredParameters.Clear();

        // Get selected label IDs for filtering
        var selectedLabelIds = Labels.Where(l => l.IsSelected).Select(l => l.Id).ToHashSet();
        var hasLabelFilter = selectedLabelIds.Count > 0;

        foreach (var param in Parameters)
        {
            // Apply label filter
            if (hasLabelFilter)
            {
                // Check if parameter has ANY of the selected labels
                var paramLabels = param.Parameter.Labels ?? Array.Empty<string>();
                var hasMatchingLabel = paramLabels.Any(l => selectedLabelIds.Contains(l));
                if (!hasMatchingLabel)
                {
                    continue; // Skip this parameter
                }
            }

            // Apply text search filter
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No text search - show all parameters that passed label filter
                FilteredParameters.Add(param);
            }
            else
            {
                var searchText = SearchText.ToLowerInvariant();
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
        _selectedParameterIds.Clear();
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
        if (SelectedCollection == null)
        {
            StatusMessage = "Please select a collection first";
            return;
        }

        _logger?.Info("[ViewModel] Refreshing parameters");

        LoadingOverlayService.Show("Refreshing Parameters", "Fetching latest parameters from APS...");

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            // Clear existing cache
            _allParametersByCollection.Clear();
            Parameters.Clear();
            FilteredParameters.Clear();

            // Fetch parameters for all collections in parallel
            var fetchTasks = Collections.Select(async collection =>
            {
                try
                {
                    var response = await _parametersService.GetParametersAsync(FixedHubId, collection.Id);
                    return (Collection: collection, Response: response);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[ViewModel] Failed to load parameters for collection '{collection.Name}'", ex);
                    return (Collection: collection, Response: null);
                }
            }).ToList();

            var results = await Task.WhenAll(fetchTasks);

            // Process results and cache by collection ID
            foreach (var result in results)
            {
                if (result.Response != null)
                {
                    var parameters = result.Response.Parameters.Select(p =>
                    {
                        var selectableParam = new SelectableParameter { Parameter = p, CollectionName = result.Collection.Name };
                        // Restore selection state if previously selected
                        if (_selectedParameterIds.Contains(selectableParam.Id))
                        {
                            selectableParam.IsSelected = true;
                        }
                        return selectableParam;
                    }).ToList();

                    _allParametersByCollection[result.Collection.Id] = parameters;
                    _logger?.Info($"[ViewModel] Cached {parameters.Count} parameters from collection '{result.Collection.Name}'");
                }
                else
                {
                    // Store empty list for failed collections
                    _allParametersByCollection[result.Collection.Id] = new List<SelectableParameter>();
                }
            }

            var totalParams = _allParametersByCollection.Values.Sum(list => list.Count);
            StatusMessage = $"Refreshed {totalParams} parameters from {Collections.Count} collections.";
            _logger?.Info($"[ViewModel] Refreshed total of {totalParams} parameters from all collections");

            // Re-display parameters for the currently selected collection
            DisplayParametersForCollection();
        }
        catch (RefreshTokenExpiredException)
        {
            _logger?.Warn("[ViewModel] Refresh token expired while refreshing parameters");
            IsAuthenticated = false;
            StatusMessage = "Your session has expired. Please login again.";
        }
        catch (TokenRefreshException ex)
        {
            _logger?.Error("[ViewModel] Token refresh failed while refreshing parameters", ex);
            StatusMessage = $"Failed to refresh token: {ex.Message}. Please try logging in again.";
            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Failed to refresh parameters", ex);
            StatusMessage = $"Failed to refresh parameters: {ex.Message}";
        }
        finally
        {
            LoadingOverlayService.Hide();
        }
    }

    /// <summary>
    /// Load collections using the fixed hub and group IDs.
    /// Also creates the special "All" collection and loads all parameters.
    /// </summary>
    private async Task LoadCollectionsAsync()
    {
        _logger?.Info($"[ViewModel] Loading collections for hub: {FixedHubId}, group: {FixedGroupId}");

        StatusMessage = "Loading collections...";
        LoadingOverlayService.Show("Loading COBie Parameters", "Fetching collections and labels...");

        try
        {
            await _sessionManager.EnsureTokenValidAsync();
            StatusMessage = "Fetching available collections...";

            // Run the network call on a background thread to keep UI responsive
            var collections = await Task.Run(async () => await _parametersService.GetCollectionsAsync(FixedHubId, FixedGroupId));

            Collections.Clear();

            // Create the special "All" collection
            _allCollection = new ApsCollection
            {
                Id = "all-collections-special-id",
                Name = "All",
                Description = "All parameters from all collections",
                IsDefaultCobieCollection = false
            };
            Collections.Add(_allCollection);

            // Add the actual collections
            foreach (var collection in collections)
            {
                Collections.Add(collection);
            }

            _logger?.Info($"[ViewModel] Loaded {Collections.Count - 1} collections (plus 'All')");

            // Load labels from APS
            await LoadLabelsAsync();

            // Load all parameters from all collections
            await LoadAllParametersAsync();

            // Select "All" as the default collection
            if (_allCollection != null)
            {
                SelectedCollection = _allCollection;
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
            LoadingOverlayService.Hide();
        }
    }

    partial void OnSelectedCollectionChanged(ApsCollection? value)
    {
        // Clear the displayed parameters when collection changes
        // DO NOT clear SelectedParameters or _selectedParameterIds - we want to accumulate selections
        Parameters.Clear();
        FilteredParameters.Clear();
        SearchText = string.Empty;

        if (value != null)
        {
            StatusMessage = $"Collection '{value.Name}' selected.";
            // Display parameters from cache (local filtering, no network call)
            DisplayParametersForCollection();
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

            // Clear displayed parameters
            Parameters.Clear();
            FilteredParameters.Clear();

            // Add all new parameters to Parameters (this will subscribe to their PropertyChanged events)
            foreach (var param in response.Parameters)
            {
                var selectableParam = new SelectableParameter { Parameter = param };
                Parameters.Add(selectableParam);
                FilteredParameters.Add(selectableParam);
            }

            // Now restore selection state by setting IsSelected on previously selected parameters
            // This will trigger UpdateSelectedParameters via the PropertyChanged subscription
            foreach (var selectableParam in Parameters)
            {
                if (_selectedParameterIds.Contains(selectableParam.Id))
                {
                    selectableParam.IsSelected = true;
                }
            }

            var cacheStatus = response.Cached ? " (cached)" : "";
            var selectedFromCollection = Parameters.Count(p => p.IsSelected);
            StatusMessage = $"Loaded {Parameters.Count} COBie parameters{cacheStatus} from '{SelectedCollection.Name}'. " +
                           $"{SelectedParameters.Count} total selected across collections.";
            _logger?.Info($"[ViewModel] Loaded {Parameters.Count} parameters, {selectedFromCollection} were already selected");
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

    /// <summary>
    /// Load all parameters from all collections and cache them for local filtering.
    /// </summary>
    private async Task LoadAllParametersAsync()
    {
        if (Collections.Count == 0)
        {
            _logger?.Warn("[ViewModel] No collections to load parameters from");
            return;
        }

        _logger?.Info($"[ViewModel] Loading parameters for all {Collections.Count} collections");
        LoadingOverlayService.UpdateMessage("Loading COBie Parameters", "Loading parameters from all collections...");

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            // Clear existing cache
            _allParametersByCollection.Clear();

            // Fetch parameters for all collections in parallel
            var fetchTasks = Collections.Select(async collection =>
            {
                try
                {
                    var response = await _parametersService.GetParametersAsync(FixedHubId, collection.Id);
                    return (Collection: collection, Response: response);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[ViewModel] Failed to load parameters for collection '{collection.Name}'", ex);
                    return (Collection: collection, Response: null);
                }
            }).ToList();

            var results = await Task.WhenAll(fetchTasks);

            // Process results and cache by collection ID
            foreach (var result in results)
            {
                if (result.Response != null)
                {
                    var parameters = result.Response.Parameters.Select(p =>
                    {
                        var selectableParam = new SelectableParameter { Parameter = p, CollectionName = result.Collection.Name };
                        // Restore selection state if previously selected
                        if (_selectedParameterIds.Contains(selectableParam.Id))
                        {
                            selectableParam.IsSelected = true;
                        }
                        return selectableParam;
                    }).ToList();

                    _allParametersByCollection[result.Collection.Id] = parameters;
                    _logger?.Info($"[ViewModel] Cached {parameters.Count} parameters from collection '{result.Collection.Name}'");
                }
                else
                {
                    // Store empty list for failed collections
                    _allParametersByCollection[result.Collection.Id] = new List<SelectableParameter>();
                }
            }

            var totalParams = _allParametersByCollection.Values.Sum(list => list.Count);
            StatusMessage = $"Loaded {totalParams} parameters from {Collections.Count} collections.";
            _logger?.Info($"[ViewModel] Loaded total of {totalParams} parameters from all collections");
        }
        catch (RefreshTokenExpiredException)
        {
            _logger?.Warn("[ViewModel] Refresh token expired while loading all parameters");
            IsAuthenticated = false;
            StatusMessage = "Your session has expired. Please login again.";
        }
        catch (TokenRefreshException ex)
        {
            _logger?.Error("[ViewModel] Token refresh failed while loading all parameters", ex);
            StatusMessage = $"Failed to refresh token: {ex.Message}. Please try logging in again.";
            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Failed to load all parameters", ex);
            StatusMessage = $"Failed to load parameters: {ex.Message}";
        }
    }

    /// <summary>
    /// Load labels from APS and populate the Labels collection.
    /// </summary>
    private async Task LoadLabelsAsync()
    {
        _logger?.Info("[ViewModel] Loading labels from APS");

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            // Fetch labels from APS
            var labels = await _parametersService.GetLabelsAsync(FixedHubId);

            Labels.Clear();
            FilteredLabels.Clear();
            foreach (var label in labels)
            {
                // Subscribe to label property changes to update filter when selection changes
                label.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ApsLabel.IsSelected))
                    {
                        OnLabelSelectionChanged();
                    }
                };
                Labels.Add(label);
                FilteredLabels.Add(label);
            }

            _logger?.Info($"[ViewModel] Loaded {Labels.Count} labels");
        }
        catch (Exception ex)
        {
            _logger?.Error("[ViewModel] Failed to load labels", ex);
            // Don't fail the entire flow if labels fail to load
            StatusMessage = $"Failed to load labels: {ex.Message}";
        }
    }

    /// <summary>
    /// Displays parameters from the cache based on the selected collection.
    /// If "All" is selected, shows all parameters from all collections.
    /// Otherwise, shows only parameters from the specific collection.
    /// </summary>
    private void DisplayParametersForCollection()
    {
        if (SelectedCollection == null)
        {
            Parameters.Clear();
            FilteredParameters.Clear();
            return;
        }

        // Clear current display
        Parameters.Clear();
        FilteredParameters.Clear();

        // Check if this is the special "All" collection
        if (_allCollection != null && SelectedCollection.Id == _allCollection.Id)
        {
            // Display all parameters from all collections
            foreach (var kvp in _allParametersByCollection)
            {
                foreach (var param in kvp.Value)
                {
                    Parameters.Add(param);
                }
            }
            StatusMessage = $"Showing all {Parameters.Count} parameters from all collections. {SelectedParameters.Count} total selected.";
            _logger?.Info($"[ViewModel] Displaying all {Parameters.Count} parameters from all collections");
        }
        else
        {
            // Display only parameters from the selected collection
            if (_allParametersByCollection.TryGetValue(SelectedCollection.Id, out var collectionParams))
            {
                foreach (var param in collectionParams)
                {
                    Parameters.Add(param);
                }
                StatusMessage = $"Showing {Parameters.Count} parameters from '{SelectedCollection.Name}'. {SelectedParameters.Count} total selected across collections.";
                _logger?.Info($"[ViewModel] Displaying {Parameters.Count} parameters from collection '{SelectedCollection.Name}'");
            }
            else
            {
                StatusMessage = $"No parameters found for collection '{SelectedCollection.Name}'.";
                _logger?.Warn($"[ViewModel] No cached parameters found for collection '{SelectedCollection.Name}'");
            }
        }

        // Apply the current search filter
        ApplySearchFilter();

        // Notify that computed properties have changed
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoParameters));
        OnPropertyChanged(nameof(HasNoParametersAndNotLoading));
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

                // Bind parameters to categories with the VariesAcrossGroups setting
                LoadingOverlayService.UpdateMessage($"Binding {definitionsToCreate.Count} parameters to categories...", "This may take a moment...");
                await Task.Delay(30);
                bindingResult = bindingService.BindParameters(document, definitionsToCreate, variesAcrossGroups: VariesAcrossGroups);

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
        // Select only the currently filtered parameters
        foreach (var param in FilteredParameters)
        {
            param.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        // Deselect all currently visible/filtered parameters
        // Do NOT clear _selectedParameterIds - we want to keep selections from other collections
        foreach (var param in FilteredParameters)
        {
            param.IsSelected = false;
        }
        // Note: We don't clear SelectedParameters or _selectedParameterIds here
        // The UpdateSelectedParameters method will handle removing the deselected items
    }

    /// <summary>
    /// Updates the SelectedParameters collection when a parameter's IsSelected property changes.
    /// Also tracks selected parameter IDs to preserve selections when switching collections.
    /// </summary>
    private void UpdateSelectedParameters(SelectableParameter parameter)
    {
        if (parameter.IsSelected)
        {
            if (!_selectedParameterIds.Contains(parameter.Id))
            {
                _selectedParameterIds.Add(parameter.Id);
            }
            if (!SelectedParameters.Contains(parameter))
            {
                SelectedParameters.Add(parameter);
            }
        }
        else
        {
            _selectedParameterIds.Remove(parameter.Id);
            // Remove from SelectedParameters - use FirstOrDefault to find by reference
            var itemToRemove = SelectedParameters.FirstOrDefault(p => p == parameter);
            if (itemToRemove != null)
            {
                SelectedParameters.Remove(itemToRemove);
            }
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
