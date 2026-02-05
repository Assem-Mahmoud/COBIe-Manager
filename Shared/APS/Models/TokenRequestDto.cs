using System;
using Newtonsoft.Json;

namespace COBIeManager.Shared.APS.Models;

/// <summary>
/// Data transfer object for token operations
/// </summary>
public class TokenRequestDto
{
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("accountId")]
    public string? AccountId { get; set; }

    [JsonProperty("userId")]
    public string? UserId { get; set; }
}
