using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Full snapshot for import/export - includes parameter definitions
/// </summary>
public class ParameterSelectionSnapshot
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; }
    public string? ExportedBy { get; set; }
    public string SnapshotName { get; set; } = "COBie Parameters Snapshot";

    /// <summary>
    /// Full parameter definitions for portability
    /// </summary>
    public List<ExportedParameter> Parameters { get; set; } = new();

    /// <summary>
    /// The VariesAcrossGroups setting at export time
    /// </summary>
    public bool VariesAcrossGroups { get; set; }
}

/// <summary>
/// Exported parameter with full definition for portability
/// </summary>
public class ExportedParameter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataTypeId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string InstanceTypeAssociation { get; set; } = string.Empty;
    public string[] CategoryBindingIds { get; set; } = Array.Empty<string>();
    public string[] CategoryNames { get; set; } = Array.Empty<string>();
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string? GroupBindingId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
}
