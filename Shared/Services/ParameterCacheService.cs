using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aps.Core.Models;
using Aps.Core.Services;
using Newtonsoft.Json;

namespace COBIeManager.Shared.Services;

/// <summary>
/// Service for caching COBie parameters to local file for offline support
/// </summary>
public class ParameterCacheService
{
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "COBIeManager",
        "Parameters");

    private static readonly string CacheFilePath = Path.Combine(CacheDirectory, "cache.json");
    private static readonly string CategoriesCacheFilePath = Path.Combine(CacheDirectory, "categories_cache.json");
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    /// <summary>
    /// Save parameters to cache file
    /// </summary>
    public async Task SaveCacheAsync(string accountId, ObservableCollection<CobieParameterDefinition> parameters)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(CacheDirectory);

            var cacheData = new ParameterCacheData
            {
                AccountId = accountId,
                CachedAt = DateTime.UtcNow,
                Parameters = parameters.ToList(),
                Version = "1.0"
            };

            var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(CacheFilePath, json));
        }
        catch (Exception ex)
        {
            // Log error but don't throw - caching is non-critical
            System.Diagnostics.Debug.WriteLine($"Failed to save parameter cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Load parameters from cache file
    /// </summary>
    /// <returns>
    /// Tuple of (parameters, cachedAt) or (null, null) if cache is invalid/missing
    /// </returns>
    public async Task<(List<CobieParameterDefinition>? Parameters, DateTime? CachedAt)?> LoadCacheAsync(string accountId)
    {
        try
        {
            if (!File.Exists(CacheFilePath))
                return null;

            var json = await Task.Run(() => File.ReadAllText(CacheFilePath));
            var cacheData = JsonConvert.DeserializeObject<ParameterCacheData>(json);

            if (cacheData == null)
                return null;

            // Validate account ID matches
            if (cacheData.AccountId != accountId)
                return null;

            // Validate cache hasn't expired
            if (DateTime.UtcNow - cacheData.CachedAt > CacheExpiry)
                return null;

            return (cacheData.Parameters, cacheData.CachedAt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load parameter cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if cache exists and is valid for the given account
    /// </summary>
    public bool IsCacheValid(string accountId)
    {
        try
        {
            if (!File.Exists(CacheFilePath))
                return false;

            var json = File.ReadAllText(CacheFilePath);
            var cacheData = JsonConvert.DeserializeObject<ParameterCacheData>(json);

            return cacheData != null &&
                   cacheData.AccountId == accountId &&
                   DateTime.UtcNow - cacheData.CachedAt <= CacheExpiry;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clear the cache file
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear parameter cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the cache file path for diagnostic purposes
    /// </summary>
    public string GetCacheFilePath() => CacheFilePath;

    /// <summary>
    /// Save categories to cache file
    /// </summary>
    public async Task SaveCategoriesCacheAsync(Dictionary<string, string> categoryMap)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(CacheDirectory);

            var cacheData = new CategoryCacheData
            {
                CachedAt = DateTime.UtcNow,
                CategoryMap = categoryMap,
                Version = "1.0"
            };

            var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(CategoriesCacheFilePath, json));
            System.Diagnostics.Debug.WriteLine($"[CategoryCache] Saved {categoryMap.Count} categories to cache");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CategoryCache] Failed to save category cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Load categories from cache file
    /// </summary>
    public async Task<Dictionary<string, string>?> LoadCategoriesCacheAsync()
    {
        try
        {
            if (!File.Exists(CategoriesCacheFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[CategoryCache] Cache file not found");
                return null;
            }

            var json = await Task.Run(() => File.ReadAllText(CategoriesCacheFilePath));
            var cacheData = JsonConvert.DeserializeObject<CategoryCacheData>(json);

            if (cacheData == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CategoryCache] Failed to deserialize cache data");
                return null;
            }

            // Validate cache hasn't expired
            if (DateTime.UtcNow - cacheData.CachedAt > CacheExpiry)
            {
                System.Diagnostics.Debug.WriteLine($"[CategoryCache] Cache expired (cached: {cacheData.CachedAt})");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[CategoryCache] Loaded {cacheData.CategoryMap.Count} categories from cache");
            return cacheData.CategoryMap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CategoryCache] Failed to load category cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if category cache exists and is valid
    /// </summary>
    public bool IsCategoriesCacheValid()
    {
        try
        {
            if (!File.Exists(CategoriesCacheFilePath))
                return false;

            var json = File.ReadAllText(CategoriesCacheFilePath);
            var cacheData = JsonConvert.DeserializeObject<CategoryCacheData>(json);

            return cacheData != null && DateTime.UtcNow - cacheData.CachedAt <= CacheExpiry;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clear the category cache file
    /// </summary>
    public void ClearCategoriesCache()
    {
        try
        {
            if (File.Exists(CategoriesCacheFilePath))
            {
                File.Delete(CategoriesCacheFilePath);
                System.Diagnostics.Debug.WriteLine($"[CategoryCache] Cache cleared");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CategoryCache] Failed to clear category cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal cache data structure for parameters
    /// </summary>
    private class ParameterCacheData
    {
        public string AccountId { get; set; } = string.Empty;
        public DateTime CachedAt { get; set; }
        public List<CobieParameterDefinition> Parameters { get; set; } = new();
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Internal cache data structure for categories
    /// </summary>
    private class CategoryCacheData
    {
        public DateTime CachedAt { get; set; }
        public Dictionary<string, string> CategoryMap { get; set; } = new();
        public string Version { get; set; } = "1.0";
    }
}
