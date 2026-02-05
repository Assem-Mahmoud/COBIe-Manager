using Newtonsoft.Json;

namespace COBIeManager.Shared.APS.Models;

/// <summary>
/// Request DTOs for APS Parameters API operations
/// </summary>
public static class ApsParameterRequest
{
    /// <summary>
    /// Request to retrieve COBie parameters from APS
    /// </summary>
    public class GetParameters
    {
        [JsonProperty("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonProperty("collectionId")]
        public string? CollectionId { get; set; }

        [JsonProperty("forceRefresh")]
        public bool ForceRefresh { get; set; }
    }

    /// <summary>
    /// Request to search parameters with filters
    /// </summary>
    public class SearchParameters
    {
        [JsonProperty("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonProperty("collectionId")]
        public string? CollectionId { get; set; }

        [JsonProperty("searchedText")]
        public string? SearchedText { get; set; }

        [JsonProperty("dataTypeIds")]
        public string[]? DataTypeIds { get; set; }

        [JsonProperty("isArchived")]
        public bool IsArchived { get; set; }

        [JsonProperty("sort")]
        public string Sort { get; set; } = "NAME_ASCENDING";
    }
}
