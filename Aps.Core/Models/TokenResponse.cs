namespace Aps.Core.Models;

/// <summary>
/// Represents the token response from APS authentication.
/// </summary>
public class TokenResponse
{
    public string? access_token { get; set; }
    public string? refresh_token { get; set; }
    public string? token_type { get; set; }
    public int expires_in { get; set; }
}
