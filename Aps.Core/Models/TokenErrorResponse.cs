namespace Aps.Core.Models;

/// <summary>
/// Represents an error response from the APS token endpoint.
/// </summary>
public class TokenErrorResponse
{
    public string error { get; set; } = string.Empty;
    public string error_description { get; set; } = string.Empty;
}
