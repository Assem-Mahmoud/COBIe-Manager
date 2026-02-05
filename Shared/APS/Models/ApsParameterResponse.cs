using System;
using System.Collections.Generic;
using COBIeManager.Features.CobieParameters.Models;
using Newtonsoft.Json;

namespace COBIeManager.Shared.APS.Models;

/// <summary>
/// Response DTOs from APS Parameters API
/// </summary>
public static class ApsParameterResponse
{
    /// <summary>
    /// Response containing retrieved parameters
    /// </summary>
    public class ParametersResponse
    {
        [JsonProperty("parameters")]
        public List<CobieParameterDefinition> Parameters { get; set; } = new();

        [JsonProperty("cached")]
        public bool Cached { get; set; }

        [JsonProperty("cachedAt")]
        public DateTime? CachedAt { get; set; }
    }

    /// <summary>
    /// Response containing data type specifications
    /// </summary>
    public class SpecsResponse
    {
        [JsonProperty("specs")]
        public List<DataTypeSpec> Specs { get; set; } = new();
    }

    /// <summary>
    /// Response containing Revit categories
    /// </summary>
    public class CategoriesResponse
    {
        [JsonProperty("categories")]
        public List<CategoryInfo> Categories { get; set; } = new();
    }

    /// <summary>
    /// Data type specification from APS
    /// </summary>
    public class DataTypeSpec
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("unitType")]
        public string? UnitType { get; set; }
    }

    /// <summary>
    /// Category information from APS
    /// </summary>
    public class CategoryInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("discipline")]
        public string Discipline { get; set; } = string.Empty;

        [JsonProperty("level")]
        public string Level { get; set; } = string.Empty; // "instances" or "types"
    }

    /// <summary>
    /// Health check response
    /// </summary>
    public class HealthResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "healthy";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Error response
    /// </summary>
    public class ErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("code")]
        public string? Code { get; set; }
    }
}
