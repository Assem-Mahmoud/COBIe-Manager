using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Tracks parameter usage for recently used feature
/// </summary>
public class ParameterUsageData
{
    public string Version { get; set; } = "1.0";
    public DateTime LastUpdated { get; set; }
    public List<ParameterUsageEntry> RecentParameters { get; set; } = new();
}

/// <summary>
/// Entry for a recently used parameter
/// </summary>
public class ParameterUsageEntry
{
    /// <summary>
    /// Unique parameter ID from APS
    /// </summary>
    public string ParameterId { get; set; } = string.Empty;

    /// <summary>
    /// Parameter name
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Collection name this parameter belongs to
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// When this parameter was last used
    /// </summary>
    public DateTime LastUsed { get; set; }

    /// <summary>
    /// Number of times this parameter has been used
    /// </summary>
    public int UseCount { get; set; }

    /// <summary>
    /// Data type for display purposes
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Labels associated with this parameter
    /// </summary>
    public string[] Labels { get; set; } = Array.Empty<string>();
}
