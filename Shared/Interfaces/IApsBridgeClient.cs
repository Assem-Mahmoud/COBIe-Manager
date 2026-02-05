using System.Threading;
using System.Threading.Tasks;
using COBIeManager.Features.CobieParameters.Models;
using COBIeManager.Shared.APS.Models;

namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Client for communicating with the APS Bridge process
/// </summary>
public interface IApsBridgeClient
{
    /// <summary>
    /// Check if the bridge is running and healthy
    /// </summary>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current authentication status from the bridge
    /// </summary>
    Task<AuthStatusResponse> GetAuthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiate OAuth login flow (opens browser)
    /// </summary>
    Task LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh the access token
    /// </summary>
    Task<TokenRequestDto> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout and clear tokens
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve COBie parameters from APS
    /// </summary>
    Task<ApsParameterResponse.ParametersResponse> GetParametersAsync(
        string accountId,
        string? collectionId = null,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available data type specifications
    /// </summary>
    Task<ApsParameterResponse.SpecsResponse> GetSpecsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available Revit categories
    /// </summary>
    Task<ApsParameterResponse.CategoriesResponse> GetCategoriesAsync(
        string? discipline = null,
        CancellationToken cancellationToken = default);
}
