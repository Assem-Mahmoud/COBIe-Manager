using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aps.Core.Logging;
using Newtonsoft.Json;

namespace Aps.Core.Services;

/// <summary>
/// File-based storage for APS categories.
/// Automatically fetches and caches categories locally, similar to token storage.
/// </summary>
public class ApsCategoryStorageService
{
    private readonly string _categoriesFile;
    private readonly ApsSessionManager _sessionManager;
    private readonly IApsLogger? _logger;
    private Dictionary<string, string>? _categoryMap;
    private readonly object _lock = new();

    // Cache expiry - categories should be refreshed periodically (e.g., every 30 days)
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(30);

    public ApsCategoryStorageService(ApsSessionManager sessionManager, IApsLogger? logger = null)
    {
        _sessionManager = sessionManager;
        _logger = logger;

        _categoriesFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "COBIeManager", "aps_categories.json");
    }

    /// <summary>
    /// Gets the category map, loading from file or fetching from API if needed.
    /// </summary>
    public async Task<Dictionary<string, string>> GetCategoryMapAsync()
    {
        lock (_lock)
        {
            // Return cached map if already loaded
            if (_categoryMap != null)
            {
                _logger?.Info($"[CategoryStorage] Using cached category map: {_categoryMap.Count} categories");
                return _categoryMap;
            }
        }

        // Try to load from file
        var loadedMap = await LoadFromFileAsync();
        if (loadedMap != null)
        {
            lock (_lock)
            {
                _categoryMap = loadedMap;
            }
            _logger?.Info($"[CategoryStorage] Loaded {loadedMap.Count} categories from file");
            return loadedMap;
        }

        // File doesn't exist or is invalid - fetch from API
        _logger?.Info("[CategoryStorage] No valid categories file found, fetching from API...");
        var fetchedMap = await FetchFromApiAsync();

        lock (_lock)
        {
            _categoryMap = fetchedMap;
        }

        return fetchedMap;
    }

    /// <summary>
    /// Forces a refresh of the categories from the API.
    /// </summary>
    public async Task<Dictionary<string, string>> RefreshCategoriesAsync()
    {
        _logger?.Info("[CategoryStorage] Force refreshing categories from API...");

        var fetchedMap = await FetchFromApiAsync();

        lock (_lock)
        {
            _categoryMap = fetchedMap;
        }

        return fetchedMap;
    }

    /// <summary>
    /// Clears the cached category map and the file.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _categoryMap = null;
        }

        try
        {
            if (File.Exists(_categoriesFile))
            {
                File.Delete(_categoriesFile);
                _logger?.Info("[CategoryStorage] Categories cache file deleted");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"[CategoryStorage] Failed to delete categories file: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the file path for diagnostic purposes.
    /// </summary>
    public string GetFilePath() => _categoriesFile;

    /// <summary>
    /// Loads categories from the local file.
    /// </summary>
    private async Task<Dictionary<string, string>?> LoadFromFileAsync()
    {
        try
        {
            if (!File.Exists(_categoriesFile))
            {
                _logger?.Info($"[CategoryStorage] Categories file not found: {_categoriesFile}");
                return null;
            }

            var json = await Task.Run(() => File.ReadAllText(_categoriesFile));
            var data = JsonConvert.DeserializeObject<CategoryStorageData>(json);

            if (data == null)
            {
                _logger?.Warn("[CategoryStorage] Failed to deserialize categories file");
                return null;
            }

            // Check if cache has expired
            if (DateTime.UtcNow - data.SavedAt > CacheExpiry)
            {
                _logger?.Info($"[CategoryStorage] Categories cache expired (saved: {data.SavedAt})");
                return null;
            }

            _logger?.Info($"[CategoryStorage] Categories file is valid ({data.Categories.Count} categories, saved: {data.SavedAt})");
            return new Dictionary<string, string>(data.Categories, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger?.Error($"[CategoryStorage] Failed to load categories from file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches all categories from the API and saves to file.
    /// </summary>
    private async Task<Dictionary<string, string>> FetchFromApiAsync()
    {
        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var baseUrl = "https://developer.api.autodesk.com/parameters/v1/classifications/categories";
            const int limit = 100;

            var url = baseUrl;
            var pageCount = 0;
            var totalResults = 0;

            _logger?.Info("[CategoryStorage] ========== FETCHING ALL CATEGORIES FROM API ==========");

            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionManager.AccessToken);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            do
            {
                pageCount++;
                _logger?.Info($"[CategoryStorage] Fetching page {pageCount}: {url}");

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch categories: {response.StatusCode} - {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var token = Newtonsoft.Json.Linq.JToken.Parse(json);

                // Check for pagination
                var pagination = token["pagination"];
                if (pagination != null)
                {
                    totalResults = pagination["totalResults"]?.ToObject<int>() ?? 0;
                    var nextUrl = pagination["nextUrl"]?.ToString();
                    var currentOffset = pagination["offset"]?.ToObject<int>() ?? 0;
                    var currentLimit = pagination["limit"]?.ToObject<int>() ?? 0;

                    _logger?.Info($"[CategoryStorage] Page {pageCount}: offset={currentOffset}, limit={currentLimit}, totalResults={totalResults}");

                    // Set next URL
                    if (!string.IsNullOrEmpty(nextUrl))
                    {
                        url = nextUrl;
                    }
                    else if (currentOffset + currentLimit < totalResults)
                    {
                        var nextOffset = currentOffset + currentLimit;
                        url = $"{baseUrl}?offset={nextOffset}&limit={limit}";
                    }
                    else
                    {
                        url = null;
                    }
                }

                // Parse categories
                var results = token["results"] as Newtonsoft.Json.Linq.JArray;
                if (results != null)
                {
                    _logger?.Info($"[CategoryStorage] Page {pageCount}: Found {results.Count} categories");

                    foreach (var category in results)
                    {
                        var id = category["id"]?.ToString();
                        var name = category["name"]?.ToString();

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            // Store ID -> Name mapping
                            // Note: Multiple versions may exist (e.g., walls-1.0.0, walls-2.0.0)
                            // We keep all versions since APS parameters may reference any version
                            categoryMap[id] = name;
                        }
                    }

                    _logger?.Info($"[CategoryStorage] Page {pageCount}: Parsed {results.Count} categories. Total so far: {categoryMap.Count}");
                }

                // Safety check
                if (pageCount > 50)
                {
                    _logger?.Warn("[CategoryStorage] Stopping pagination after 50 pages (safety limit)");
                    break;
                }

            } while (!string.IsNullOrEmpty(url));

            _logger?.Info($"[CategoryStorage] ========== FETCH COMPLETE: {categoryMap.Count} categories ==========");

            // Save to file
            await SaveToFileAsync(categoryMap);

            return categoryMap;
        }
        catch (Exception ex)
        {
            _logger?.Error($"[CategoryStorage] Failed to fetch categories from API: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Saves the category map to file.
    /// </summary>
    private async Task SaveToFileAsync(Dictionary<string, string> categoryMap)
    {
        try
        {
            var folder = Path.GetDirectoryName(_categoriesFile);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var data = new CategoryStorageData
            {
                SavedAt = DateTime.UtcNow,
                Categories = categoryMap
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(_categoriesFile, json));

            _logger?.Info($"[CategoryStorage] Saved {categoryMap.Count} categories to: {_categoriesFile}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"[CategoryStorage] Failed to save categories to file: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal data structure for category storage.
    /// </summary>
    private class CategoryStorageData
    {
        public DateTime SavedAt { get; set; }
        public Dictionary<string, string> Categories { get; set; } = new();
    }
}
