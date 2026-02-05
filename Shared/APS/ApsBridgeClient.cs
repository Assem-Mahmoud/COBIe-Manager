using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COBIeManager.Shared.APS.Models;
using COBIeManager.Shared.Interfaces;
using Newtonsoft.Json;

namespace COBIeManager.Shared.APS;

/// <summary>
/// HttpClient implementation for communicating with the APS Bridge process
/// </summary>
public class ApsBridgeClient : IApsBridgeClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000";
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    /// <summary>
    /// Creates a new bridge client instance
    /// </summary>
    public ApsBridgeClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Creates a new bridge client with custom timeout
    /// </summary>
    public ApsBridgeClient(TimeSpan timeout)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = timeout
        };
    }

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var health = JsonConvert.DeserializeObject<ApsParameterResponse.HealthResponse>(json);

            return health?.Status == "healthy";
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<AuthStatusResponse> GetAuthStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/auth/status", cancellationToken);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthStatusResponse>(json)!;
    }

    /// <inheritdoc/>
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/auth/login", null, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    /// <inheritdoc/>
    public async Task<TokenRequestDto> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/auth/token/refresh", null, cancellationToken);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TokenRequestDto>(json)!;
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/auth/logout", null, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    /// <inheritdoc/>
    public async Task<ApsParameterResponse.ParametersResponse> GetParametersAsync(
        string accountId,
        string? collectionId = null,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["accountId"] = accountId,
            ["collectionId"] = collectionId,
            ["forceRefresh"] = forceRefresh.ToString().ToLowerInvariant()
        };

        var url = BuildQueryString("/parameters", queryParams);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ApsParameterResponse.ParametersResponse>(json)!;
    }

    /// <inheritdoc/>
    public async Task<ApsParameterResponse.SpecsResponse> GetSpecsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/parameters/specs", cancellationToken);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ApsParameterResponse.SpecsResponse>(json)!;
    }

    /// <inheritdoc/>
    public async Task<ApsParameterResponse.CategoriesResponse> GetCategoriesAsync(
        string? discipline = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(discipline))
        {
            queryParams["discipline"] = discipline;
        }

        var url = BuildQueryString("/parameters/categories", queryParams);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ApsParameterResponse.CategoriesResponse>(json)!;
    }

    /// <summary>
    /// Builds query string from dictionary
    /// </summary>
    private static string BuildQueryString(string baseUrl, Dictionary<string, string?> queryParams)
    {
        if (queryParams.Count == 0)
            return baseUrl;

        var filteredParams = queryParams
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");

        return $"{baseUrl}?{string.Join("&", filteredParams)}";
    }

    /// <summary>
    /// Ensures HTTP response is successful, throwing detailed exception if not
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            ApsParameterResponse.ErrorResponse? error = null;

            try
            {
                error = JsonConvert.DeserializeObject<ApsParameterResponse.ErrorResponse>(errorContent);
            }
            catch
            {
                // If error content isn't valid error response JSON, use status code
            }

            throw new ApsBridgeClientException(
                error?.Message ?? $"Bridge request failed: {response.StatusCode}",
                error?.Code,
                response.StatusCode);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Exception thrown when APS Bridge client encounters an error
/// </summary>
public class ApsBridgeClientException : Exception
{
    /// <summary>
    /// Application-specific error code
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public System.Net.HttpStatusCode StatusCode { get; }

    public ApsBridgeClientException(string message, string? errorCode = null, System.Net.HttpStatusCode statusCode = 0)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
