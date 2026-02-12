using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aps.Core.Models;

namespace Aps.Core.Services;

/// <summary>
/// Simple logger interface for Aps.Core
/// </summary>
public interface IApsLogger
{
    void Info(string message);
    void Error(string message, Exception? ex = null);
    void Debug(string message);
    void Warn(string message);
}

/// <summary>
/// Handles OAuth 2.0 authentication with Autodesk Platform Services using PKCE flow.
/// Compatible with .NET Framework 4.8 and .NET 8.0.
/// </summary>
public class ApsAuthService
{
    private const string ClientId = "ydziuGXs58FCuGwGBog2rc0jH2N8PxZHdIv7pLnAK9HKGu9b";
    private const string RedirectUri = "http://localhost:3000/callback";
    private const string Scopes = "data:read data:write data:create account:read";
    private string? _codeVerifier;
    private string? _codeChallenge;
    private readonly IApsLogger? _logger;

    public ApsAuthService() : this(null) { }

    public ApsAuthService(IApsLogger? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds the login URL for OAuth authorization.
    /// </summary>
    public string BuildLoginUrl()
    {
        GeneratePkceParameters();

        var scopeEncoded = Uri.EscapeDataString(Scopes);
        var url = $"https://developer.api.autodesk.com/authentication/v2/authorize?response_type=code" +
               $"&client_id={ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&scope={scopeEncoded}" +
               $"&code_challenge={_codeChallenge}&code_challenge_method=S256";

        _logger?.Info($"[ApsAuthService] Login URL generated: {url}");
        _logger?.Debug($"[ApsAuthService] Code challenge: {_codeChallenge}");

        return url;
    }

    /// <summary>
    /// Generates PKCE parameters for secure OAuth flow.
    /// </summary>
    public void GeneratePkceParameters()
    {
        byte[] bytes;
#if NET8_0
        bytes = RandomNumberGenerator.GetBytes(64);
#else
        using (var rng = new RNGCryptoServiceProvider())
        {
            bytes = new byte[64];
            rng.GetBytes(bytes);
        }
#endif

        _codeVerifier = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        using var sha256 = SHA256.Create();
        byte[] challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(_codeVerifier));
        _codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        _logger?.Debug("[ApsAuthService] PKCE parameters generated");
    }

    /// <summary>
    /// Opens the login page in the default browser.
    /// </summary>
    public void OpenLoginInBrowser()
    {
        var url = BuildLoginUrl();
        _logger?.Info("[ApsAuthService] Opening browser for authentication...");

#if NET8_0
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#else
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#endif
    }

    /// <summary>
    /// Exchanges the authorization code for access tokens.
    /// </summary>
    public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        var tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";

        _logger?.Info("[ApsAuthService] Exchanging authorization code for token...");
        _logger?.Debug($"[ApsAuthService] Code: {code.Substring(0, Math.Min(10, code.Length))}...");
        _logger?.Debug($"[ApsAuthService] Code Verifier: {_codeVerifier?.Substring(0, Math.Min(10, _codeVerifier?.Length ?? 0))}...");

        using var client = new System.Net.Http.HttpClient();
        var content = new System.Net.Http.FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
            new KeyValuePair<string, string>("code_verifier", _codeVerifier ?? string.Empty)
        });

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, tokenUrl)
        {
            Content = content
        };

        request.Headers.Add("Accept", "application/json");

        _logger?.Debug($"[ApsAuthService] Sending POST request to: {tokenUrl}");

        System.Net.Http.HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
            _logger?.Debug($"[ApsAuthService] Response status: {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger?.Error("[ApsAuthService] HTTP request failed", ex);
            throw new Exception($"Failed to connect to APS token endpoint: {ex.Message}", ex);
        }

        var json = await response.Content.ReadAsStringAsync();
        _logger?.Debug($"[ApsAuthService] Response content: {json}");

        if (!response.IsSuccessStatusCode)
        {
            _logger?.Error($"[ApsAuthService] Token exchange failed: {(int)response.StatusCode} {response.StatusCode}");
            throw new Exception($"Token exchange failed: {(int)response.StatusCode} - {json}");
        }

        var tokenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(json);

        if (tokenResponse == null)
        {
            _logger?.Error("[ApsAuthService] Failed to deserialize token response");
            throw new Exception("Failed to deserialize token response");
        }

        if (string.IsNullOrEmpty(tokenResponse.access_token))
        {
            _logger?.Error("[ApsAuthService] No access token in response");
            throw new Exception("No access token received in response");
        }

        _logger?.Info("[ApsAuthService] Token exchange successful");
        _logger?.Debug($"[ApsAuthService] Token type: {tokenResponse.token_type}, Expires in: {tokenResponse.expires_in}s");
        _logger?.Debug($"[ApsAuthService] Access token: {tokenResponse.access_token?.Substring(0, Math.Min(20, tokenResponse.access_token?.Length ?? 0))}...");
        _logger?.Debug($"[ApsAuthService] Refresh token: {tokenResponse.refresh_token?.Substring(0, Math.Min(20, tokenResponse.refresh_token?.Length ?? 0))}...");

        return tokenResponse;
    }

    /// <summary>
    /// Waits for the authorization code via local HTTP listener.
    /// </summary>
    public async Task<string> WaitForAuthorizationCodeAsync()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:3000/callback/");

        _logger?.Info("[ApsAuthService] Starting HTTP listener on http://localhost:3000/callback/");

        listener.Start();

        try
        {
            _logger?.Info("[ApsAuthService] Waiting for authorization callback...");
            var context = await listener.GetContextAsync();

            _logger?.Info($"[ApsAuthService] Received request from: {context.Request.RemoteEndPoint}");
            _logger?.Debug($"[ApsAuthService] Request URL: {context.Request.Url}");

            var query = context.Request.QueryString;
            _logger?.Debug($"[ApsAuthService] Query string: {query}");

            string? error = query["error"];
            if (!string.IsNullOrEmpty(error))
            {
                var errorDescription = query["error_description"];
                _logger?.Error($"[ApsAuthService] OAuth error returned: {error} - {errorDescription}");
                throw new Exception($"OAuth error: {error} - {errorDescription}");
            }

            string code = query["code"] ?? throw new Exception("No authorization code received.");
            _logger?.Info("[ApsAuthService] Authorization code received successfully");

            string responseString = @"<html><head><title>Login Complete</title><script>setTimeout(function(){window.open('','_self','');window.close();},1000);</script></head><body><h2>You are logged in. You can now return to the app.</h2></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();

            return code;
        }
        finally
        {
            listener.Stop();
            _logger?.Info("[ApsAuthService] HTTP listener stopped");
        }
    }

    /// <summary>
    /// Exchanges a refresh token for new access tokens.
    /// </summary>
    public async Task<TokenResponse> ExchangeRefreshTokenAsync(string refreshToken)
    {
        _logger?.Info("[ApsAuthService] Exchanging refresh token for new access token...");

        using var client = new System.Net.Http.HttpClient();

        // For refresh_token grant type, scope should NOT be included.
        // The refresh token already has the scopes embedded from original authorization.
        var values = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", ClientId}
        };

        var content = new System.Net.Http.FormUrlEncodedContent(values);

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://developer.api.autodesk.com/authentication/v2/token")
        {
            Content = content
        };

        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        _logger?.Debug($"[ApsAuthService] Sending refresh token request to APS...");

        System.Net.Http.HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
            _logger?.Debug($"[ApsAuthService] Response status: {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger?.Error("[ApsAuthService] HTTP request failed during token refresh", ex);
            throw new Exception($"Failed to connect to APS token endpoint: {ex.Message}", ex);
        }

        var responseString = await response.Content.ReadAsStringAsync();
        _logger?.Debug($"[ApsAuthService] Refresh token response: {responseString}");

        if (!response.IsSuccessStatusCode)
        {
            _logger?.Error($"[ApsAuthService] Failed to refresh token: {(int)response.StatusCode} - {responseString}");

            // Parse error to determine if refresh token is expired
            var errorInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenErrorResponse>(responseString);
            if (errorInfo != null && IsRefreshTokenExpired(errorInfo))
            {
                throw new RefreshTokenExpiredException("The refresh token has expired. Please re-authenticate.", errorInfo);
            }

            throw new TokenRefreshException($"Failed to refresh token: {(int)response.StatusCode} - {responseString}", errorInfo);
        }

        var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(responseString);

        if (tokenData == null || string.IsNullOrEmpty(tokenData.access_token))
        {
            _logger?.Error("[ApsAuthService] Invalid token response from refresh");
            throw new Exception("Failed to deserialize token response");
        }

        _logger?.Info("[ApsAuthService] Token refreshed successfully");
        _logger?.Debug($"[ApsAuthService] New access token expires in: {tokenData.expires_in}s");

        return tokenData;
    }

    /// <summary>
    /// Determines if the error indicates an expired refresh token.
    /// </summary>
    private bool IsRefreshTokenExpired(TokenErrorResponse error)
    {
        // APS returns invalid_grant when refresh token is expired or invalid
        return error.error == "invalid_grant";
    }
}
