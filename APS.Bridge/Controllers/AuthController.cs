using APS.Bridge.Models;
using ACG.Aps.Core.Helpers;
using ACG.Aps.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace APS.Bridge.Controllers;

/// <summary>
/// Controller for APS OAuth authentication
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ApsAuthService _authService;
    private readonly ApsSessionManager _sessionManager;
    private readonly TokenStorage _tokenStorage;
    private readonly BridgeAuthStatus _authStatus;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApsAuthService authService,
        ApsSessionManager sessionManager,
        TokenStorage tokenStorage,
        BridgeAuthStatus authStatus,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _sessionManager = sessionManager;
        _tokenStorage = tokenStorage;
        _authStatus = authStatus;
        _logger = logger;
    }

    /// <summary>
    /// Initiate OAuth login - opens browser for user authentication
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login()
    {
        if (_authStatus.IsLoginInProgress)
        {
            if (_authStatus.IsLoginTimedOut)
            {
                _authStatus.IsLoginInProgress = false;
            }
            else
            {
                return Conflict(new
                {
                    error = "login_in_progress",
                    message = "A login flow is already in progress. Please wait for it to complete or timeout."
                });
            }
        }

        try
        {
            _authStatus.IsLoginInProgress = true;
            _authStatus.LoginStartedAt = DateTime.UtcNow;

            var loginUrl = _authService.BuildLoginUrl();
            _authService.OpenLoginInBrowser();

            return Ok(new
            {
                loginUrl,
                message = "Login initiated - browser should open automatically"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate login");
            _authStatus.IsLoginInProgress = false;
            return StatusCode(500, new
            {
                error = "login_failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Process OAuth callback - called by the bridge's HTTP listener
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] CallbackRequest request)
    {
        if (!_authStatus.IsLoginInProgress)
        {
            return BadRequest(new
            {
                error = "no_login_in_progress",
                message = "No login flow is currently in progress"
            });
        }

        try
        {
            // Exchange code for tokens
            var tokenResponse = await _authService.ExchangeCodeForTokenAsync(request.Code);

            // Set tokens in session manager
            _sessionManager.SetTokens(
                tokenResponse.access_token ?? string.Empty,
                tokenResponse.refresh_token ?? string.Empty,
                tokenResponse.expires_in);

            // Store account ID for later retrieval
            _authStatus.AccountId = tokenResponse.AccountId;

            // Save tokens to storage
            _tokenStorage.SaveToken(
                tokenResponse.access_token ?? string.Empty,
                tokenResponse.refresh_token ?? string.Empty,
                DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));

            // Clear login in progress state
            _authStatus.IsLoginInProgress = false;

            return Ok(new
            {
                message = "Authentication successful",
                accountId = tokenResponse.AccountId // Will be populated if ACG.APS.Core returns it
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OAuth callback");
            _authStatus.IsLoginInProgress = false;
            return BadRequest(new
            {
                error = "callback_failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get current authentication status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var token = _tokenStorage.GetToken();

        if (token == null)
        {
            return Ok(new
            {
                isAuthenticated = false,
                accountId = (string?)null,
                userId = (string?)null,
                expiresAt = (DateTime?)null
            });
        }

        // Check if token is expired
        var isExpired = DateTime.UtcNow > token.expiresAt;

        return Ok(new
        {
            isAuthenticated = !isExpired,
            accountId = _authStatus.AccountId,
            userId = (string?)null,
            expiresAt = token.expiresAt
        });
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var success = await _sessionManager.EnsureTokenValidAsync();

            if (!success)
            {
                return Unauthorized(new
                {
                    error = "refresh_failed",
                    message = "Token refresh failed. Please login again."
                });
            }

            return Ok(new
            {
                accessToken = _sessionManager.AccessToken,
                refreshToken = _sessionManager.RefreshToken,
                expiresAt = _sessionManager.TokenExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
            return StatusCode(500, new
            {
                error = "refresh_error",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Logout and clear tokens
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            _tokenStorage.ClearToken();
            _authStatus.IsLoginInProgress = false;

            return Ok(new
            {
                message = "Logged out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout");
            return StatusCode(500, new
            {
                error = "logout_failed",
                message = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for OAuth callback
/// </summary>
public class CallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string? State { get; set; }
}
