using System;

namespace Aps.Core.Models;

/// <summary>
/// Represents stored authentication tokens.
/// </summary>
public class StoredTokenModel
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
