using System;
using Newtonsoft.Json;

namespace COBIeManager.Shared.APS.Models;

/// <summary>
/// Response containing current authentication status
/// </summary>
public class AuthStatusResponse
{
    [JsonProperty("isAuthenticated")]
    public bool IsAuthenticated { get; set; }

    [JsonProperty("accountId")]
    public string? AccountId { get; set; }

    [JsonProperty("userId")]
    public string? UserId { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
