using APS.Bridge.Services;
using ACG.Aps.Core.Helpers;
using ACG.Aps.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace APS.Bridge.Controllers;

/// <summary>
/// Controller for APS Parameters API operations
/// </summary>
[ApiController]
[Route("parameters")]
public class ParametersController : ControllerBase
{
    private readonly ApsSessionManager _sessionManager;
    private readonly TokenStorage _tokenStorage;
    private readonly ILogger<ParametersController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Cache for parameters (in-memory for simplicity)
    private static readonly Dictionary<string, (List<CobieParameterDefinition> Parameters, DateTime CachedAt)> _parameterCache = new();

    public ParametersController(
        ApsSessionManager sessionManager,
        TokenStorage tokenStorage,
        ILogger<ParametersController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _sessionManager = sessionManager;
        _tokenStorage = tokenStorage;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Get COBie parameters from APS
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetParameters(
        [FromQuery] string accountId,
        [FromQuery] string? collectionId = null,
        [FromQuery] bool forceRefresh = false)
    {
        var token = _tokenStorage.GetToken();
        if (token == null || DateTime.UtcNow > token.expiresAt)
        {
            return Unauthorized(new
            {
                error = "not_authenticated",
                message = "Please authenticate first using /auth/login"
            });
        }

        try
        {
            // Check cache first (unless force refresh)
            var cacheKey = $"{accountId}_{collectionId ?? "default"}";
            if (!forceRefresh && _parameterCache.TryGetValue(cacheKey, out var cached))
            {
                // Return cached if less than 24 hours old
                if (DateTime.UtcNow - cached.CachedAt < TimeSpan.FromHours(24))
                {
                    return Ok(new
                    {
                        parameters = cached.Parameters,
                        cached = true,
                        cachedAt = cached.CachedAt
                    });
                }
            }

            // Ensure token is valid
            await _sessionManager.EnsureTokenValidAsync();

            // Fetch from APS
            var httpClient = _httpClientFactory.CreateClient();
            var service = new ApsParametersService(httpClient, _sessionManager.AccessToken);
            var parameters = await service.GetParametersAsync(accountId, collectionId);

            // Update cache
            _parameterCache[cacheKey] = (parameters.ToList(), DateTime.UtcNow);

            return Ok(new
            {
                parameters = parameters,
                cached = false,
                cachedAt = (DateTime?)null
            });
        }
        catch (ApsParametersException ex)
        {
            _logger.LogError(ex, "Failed to retrieve parameters from APS");
            return StatusCode(ex.StatusCode, new
            {
                error = "aps_request_failed",
                message = ex.Message,
                code = ex.StatusCode.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving parameters");
            return StatusCode(503, new
            {
                error = "service_unavailable",
                message = "Failed to retrieve parameters. Please try again."
            });
        }
    }

    /// <summary>
    /// Get available data type specifications
    /// </summary>
    [HttpGet("specs")]
    public async Task<IActionResult> GetSpecs()
    {
        var token = _tokenStorage.GetToken();
        if (token == null || DateTime.UtcNow > token.expiresAt)
        {
            return Unauthorized(new
            {
                error = "not_authenticated",
                message = "Please authenticate first using /auth/login"
            });
        }

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            var httpClient = _httpClientFactory.CreateClient();
            var service = new ApsParametersService(httpClient, _sessionManager.AccessToken);
            var specs = await service.GetSpecsAsync();

            return Ok(new { specs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve specs from APS");
            return StatusCode(503, new
            {
                error = "service_unavailable",
                message = "Failed to retrieve data type specifications."
            });
        }
    }

    /// <summary>
    /// Get Revit categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? discipline = null)
    {
        var token = _tokenStorage.GetToken();
        if (token == null || DateTime.UtcNow > token.expiresAt)
        {
            return Unauthorized(new
            {
                error = "not_authenticated",
                message = "Please authenticate first using /auth/login"
            });
        }

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            var httpClient = _httpClientFactory.CreateClient();
            var service = new ApsParametersService(httpClient, _sessionManager.AccessToken);
            var categories = await service.GetCategoriesAsync(discipline);

            return Ok(new { categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve categories from APS");
            return StatusCode(503, new
            {
                error = "service_unavailable",
                message = "Failed to retrieve categories."
            });
        }
    }

    /// <summary>
    /// Search parameters with filters
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchParameters([FromBody] SearchRequest request)
    {
        var token = _tokenStorage.GetToken();
        if (token == null || DateTime.UtcNow > token.expiresAt)
        {
            return Unauthorized(new
            {
                error = "not_authenticated",
                message = "Please authenticate first using /auth/login"
            });
        }

        try
        {
            await _sessionManager.EnsureTokenValidAsync();

            var httpClient = _httpClientFactory.CreateClient();
            var service = new ApsParametersService(httpClient, _sessionManager.AccessToken);
            var allParameters = await service.GetParametersAsync(request.AccountId, request.CollectionId);

            // Apply client-side filtering (simplified - could be done server-side with APS API)
            var filtered = allParameters.AsEnumerable();

            if (!string.IsNullOrEmpty(request.SearchedText))
            {
                var searchLower = request.SearchedText.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    p.Name.ToLowerInvariant().Contains(searchLower) ||
                    (p.Description?.ToLowerInvariant().Contains(searchLower) ?? false));
            }

            if (request.DataTypeIds != null && request.DataTypeIds.Length > 0)
            {
                filtered = filtered.Where(p =>
                    request.DataTypeIds.Contains(p.DataTypeId));
            }

            if (request.IsArchived.HasValue)
            {
                filtered = filtered.Where(p => p.IsArchived == request.IsArchived.Value);
            }

            // Apply sorting
            filtered = request.Sort?.ToUpperInvariant() == "NAME_DESCENDING"
                ? filtered.OrderByDescending(p => p.Name)
                : filtered.OrderBy(p => p.Name);

            return Ok(new
            {
                results = filtered.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search parameters");
            return StatusCode(503, new
            {
                error = "service_unavailable",
                message = "Failed to search parameters."
            });
        }
    }
}

/// <summary>
/// Request model for parameter search
/// </summary>
public class SearchRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string? CollectionId { get; set; }
    public string? SearchedText { get; set; }
    public string[]? DataTypeIds { get; set; }
    public bool? IsArchived { get; set; }
    public string? Sort { get; set; } = "NAME_ASCENDING";
}
