using System;
using System.Threading.Tasks;
using Aps.Core.Interfaces;
using Aps.Core.Models;

namespace Aps.Core.Services;

/// <summary>
/// Manages APS authentication session with automatic token refresh.
/// </summary>
public class ApsSessionManager
{
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime TokenExpiresAt { get; private set; }

    private readonly ApsAuthService _authService;
    private readonly ITokenStorage _tokenStorage;
    private readonly IApsLogger? _logger;

    public ApsSessionManager(ApsAuthService authService, ITokenStorage tokenStorage, IApsLogger? logger = null)
    {
        _authService = authService;
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    /// <summary>
    /// Sets the authentication tokens.
    /// </summary>
    public void SetTokens(string accessToken, string refreshToken, int expiresInSeconds)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }

    /// <summary>
    /// Sets the authentication tokens with explicit expiration.
    /// </summary>
    public void SetTokens(string accessToken, string refreshToken, DateTime expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
    }

    /// <summary>
    /// Clears the current session.
    /// </summary>
    public void ClearSession()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        TokenExpiresAt = DateTime.MinValue;
        _tokenStorage.ClearToken();
    }

    /// <summary>
    /// Checks if the current token is valid (not expired).
    /// </summary>
    public bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(AccessToken) && DateTime.UtcNow < TokenExpiresAt;
    }

    /// <summary>
    /// Ensures the access token is valid, refreshing if necessary.
    /// Proactively refreshes 2 minutes before expiration, or immediately if already expired.
    /// </summary>
    public async Task<bool> EnsureTokenValidAsync()
    {
        // If we have no tokens, return false
        if (string.IsNullOrEmpty(RefreshToken))
        {
            _logger?.Warn("[ApsSessionManager] No refresh token available");
            return false;
        }

        // Check if token needs refresh: either expired OR expiring within 2 minutes
        bool needsRefresh = DateTime.UtcNow > TokenExpiresAt.AddMinutes(-2);

        if (needsRefresh)
        {
            if (DateTime.UtcNow > TokenExpiresAt)
            {
                _logger?.Info("[ApsSessionManager] Token is already expired, attempting refresh...");
            }
            else
            {
                _logger?.Info("[ApsSessionManager] Token expiring soon, attempting proactive refresh...");
            }

            try
            {
                var response = await _authService.ExchangeRefreshTokenAsync(RefreshToken);
                SetTokens(
                    response.access_token ?? throw new Exception("No access token in response"),
                    response.refresh_token ?? RefreshToken, // Use old refresh token if new one not provided
                    response.expires_in);

                // Update TokenStorage with new token data
                _tokenStorage.SaveToken(AccessToken, RefreshToken, TokenExpiresAt);

                _logger?.Info("[ApsSessionManager] Token refreshed successfully");
                return true;
            }
            catch (RefreshTokenExpiredException)
            {
                // Refresh token is expired - clear session and require re-auth
                _logger?.Warn("[ApsSessionManager] Refresh token has expired, clearing session");
                ClearSession();
                throw; // Re-throw to let caller handle re-auth prompt
            }
            catch (TokenRefreshException ex)
            {
                // Token refresh failed but may be temporary (network issue, etc.)
                _logger?.Error($"[ApsSessionManager] Token refresh failed: {ex.Message}");

                // Check if current token is still valid (not expired yet)
                if (DateTime.UtcNow < TokenExpiresAt)
                {
                    _logger?.Warn("[ApsSessionManager] Current token still valid, using existing token");
                    return true;
                }

                // Token is expired and refresh failed
                return false;
            }
            catch (Exception ex)
            {
                // Unexpected error during token refresh
                _logger?.Error("[ApsSessionManager] Unexpected error during token refresh", ex);

                // Check if current token is still valid
                if (DateTime.UtcNow < TokenExpiresAt)
                {
                    _logger?.Warn("[ApsSessionManager] Current token still valid, using existing token");
                    return true;
                }

                return false;
            }
        }

        // Token is still valid and not close to expiry
        _logger?.Debug("[ApsSessionManager] Token is still valid, no refresh needed");
        return true;
    }
}
