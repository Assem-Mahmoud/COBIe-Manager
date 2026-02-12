using System;

namespace Aps.Core.Interfaces;

/// <summary>
/// Interface for persisting and retrieving authentication tokens.
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Saves the authentication token to storage.
    /// </summary>
    void SaveToken(string accessToken, string refreshToken, DateTime expiresAt);

    /// <summary>
    /// Retrieves the stored authentication token.
    /// </summary>
    /// <returns>A tuple containing access token, refresh token, and expiration date, or null if not found.</returns>
    (string accessToken, string refreshToken, DateTime expiresAt)? GetToken();

    /// <summary>
    /// Clears any stored tokens.
    /// </summary>
    void ClearToken();
}
