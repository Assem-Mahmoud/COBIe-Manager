using System;
using System.IO;
using COBIeManager.Features.CobieParameters.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using Newtonsoft.Json;

namespace COBIeManager.Shared.Services;

/// <summary>
/// File-based storage for parameter selection persistence
/// </summary>
public class ParameterSelectionStorage : IParameterSelectionStorage
{
    private readonly string _usageDataFile;
    private readonly ILogger? _logger;

    public ParameterSelectionStorage()
    {
        _usageDataFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "COBIeManager", "parameter_usage.json");

        // Try to get the logger (optional)
        try
        {
            _logger = Shared.DependencyInjection.ServiceLocator.GetService<ILogger>();
        }
        catch
        {
            _logger = null;
        }
    }

    /// <summary>
    /// Saves the recent parameters data
    /// </summary>
    public void SaveRecentParameters(ParameterUsageData data)
    {
        try
        {
            var folder = Path.GetDirectoryName(_usageDataFile);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            data.LastUpdated = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_usageDataFile, json);

            _logger?.Info($"[ParameterSelectionStorage] Saved {data.RecentParameters.Count} recent parameters");
        }
        catch (Exception ex)
        {
            _logger?.Error($"[ParameterSelectionStorage] Failed to save recent parameters", ex);
        }
    }

    /// <summary>
    /// Loads the recent parameters data
    /// </summary>
    public ParameterUsageData LoadRecentParameters()
    {
        try
        {
            if (!File.Exists(_usageDataFile))
            {
                _logger?.Info("[ParameterSelectionStorage] No recent parameters file found, returning empty data");
                return new ParameterUsageData { LastUpdated = DateTime.UtcNow };
            }

            var json = File.ReadAllText(_usageDataFile);
            var data = JsonConvert.DeserializeObject<ParameterUsageData>(json);

            if (data == null)
            {
                _logger?.Warn("[ParameterSelectionStorage] Failed to deserialize recent parameters, returning empty data");
                return new ParameterUsageData { LastUpdated = DateTime.UtcNow };
            }

            _logger?.Info($"[ParameterSelectionStorage] Loaded {data.RecentParameters.Count} recent parameters");
            return data;
        }
        catch (Exception ex)
        {
            _logger?.Error("[ParameterSelectionStorage] Failed to load recent parameters", ex);
            return new ParameterUsageData { LastUpdated = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// Exports a snapshot to a file
    /// </summary>
    public void ExportSnapshot(string filePath, ParameterSelectionSnapshot snapshot)
    {
        try
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            snapshot.ExportedAt = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(filePath, json);

            _logger?.Info($"[ParameterSelectionStorage] Exported {snapshot.Parameters.Count} parameters to {filePath}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"[ParameterSelectionStorage] Failed to export snapshot to {filePath}", ex);
            throw;
        }
    }

    /// <summary>
    /// Imports a snapshot from a file
    /// </summary>
    public ParameterSelectionSnapshot ImportSnapshot(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Snapshot file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var snapshot = JsonConvert.DeserializeObject<ParameterSelectionSnapshot>(json);

            if (snapshot == null)
                throw new InvalidOperationException("Failed to deserialize snapshot file");

            _logger?.Info($"[ParameterSelectionStorage] Imported {snapshot.Parameters.Count} parameters from {filePath}");
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger?.Error($"[ParameterSelectionStorage] Failed to import snapshot from {filePath}", ex);
            throw;
        }
    }
}
