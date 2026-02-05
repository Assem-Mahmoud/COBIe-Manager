using System;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Represents the current APS authentication state
/// </summary>
public class AuthenticationSession
{
    /// <summary>
    /// OAuth access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// OAuth refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// APS user identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// APS account ID (hub ID)
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Whether session is valid
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && DateTime.UtcNow < ExpiresAt;
}
