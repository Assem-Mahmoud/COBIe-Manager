using Newtonsoft.Json;
using System.Collections.Generic;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Represents a COBie parameter definition retrieved from APS
/// </summary>
public class CobieParameterDefinition
{
    /// <summary>
    /// Unique identifier (32-char hex from APS)
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the parameter
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// APS data type identifier (e.g., autodesk.revit.spec:text-1.0.0)
    /// </summary>
    [JsonProperty("dataTypeId")]
    public string DataTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Normalized data type enum
    /// </summary>
    [JsonProperty("dataType")]
    public ParameterDataType DataType { get; set; }

    /// <summary>
    /// INSTANCE or TYPE
    /// </summary>
    [JsonProperty("instanceTypeAssociation")]
    public ParameterType InstanceTypeAssociation { get; set; }

    /// <summary>
    /// Array of APS category identifiers
    /// </summary>
    [JsonProperty("categoryBindingIds")]
    public string[] CategoryBindingIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Associated label names
    /// </summary>
    [JsonProperty("labels")]
    public string[] Labels { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Whether parameter is hidden
    /// </summary>
    [JsonProperty("isHidden")]
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether parameter is archived
    /// </summary>
    [JsonProperty("isArchived")]
    public bool IsArchived { get; set; }

    /// <summary>
    /// Property group identifier for Revit
    /// </summary>
    [JsonProperty("groupBindingId")]
    public string? GroupBindingId { get; set; }

    /// <summary>
    /// UI selection state (not serialized)
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; }
}
