using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Aps.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aps.Core.Services;

/// <summary>
/// Service for managing APS hubs and projects.
/// Based on ACG.Aps.Core ApsDataService pattern.
/// </summary>
public class ApsHubService
{
    private const string BaseUrl = "https://developer.api.autodesk.com";
    private readonly ApsSessionManager _sessionManager;

    public ApsHubService(ApsSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Gets all hubs accessible to the authenticated user.
    /// </summary>
    public async Task<List<ApsHub>> GetHubsAsync()
    {
        await _sessionManager.EnsureTokenValidAsync();

        using var client = CreateHttpClient();
        var response = await client.GetAsync($"{BaseUrl}/project/v1/hubs");
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(json);

        var hubs = new List<ApsHub>();
        var data = result["data"] as JArray;
        if (data != null)
        {
            foreach (var item in data)
            {
                var hubType = item["attributes"]?["extension"]?["type"]?.ToString();
                hubs.Add(new ApsHub
                {
                    Id = item["id"]?.ToString() ?? string.Empty,
                    Name = item["attributes"]?["name"]?.ToString() ?? "Unnamed Hub",
                    HubType = hubType
                });
            }
        }

        return hubs;
    }

    /// <summary>
    /// Gets all projects for a specific hub.
    /// </summary>
    public async Task<List<ApsProject>> GetProjectsAsync(string hubId)
    {
        await _sessionManager.EnsureTokenValidAsync();

        using var client = CreateHttpClient();
        var response = await client.GetAsync($"{BaseUrl}/project/v1/hubs/{hubId}/projects");
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(json);

        var projects = new List<ApsProject>();
        var data = result["data"] as JArray;
        if (data != null)
        {
            foreach (var item in data)
            {
                var rootFolderId = item["relationships"]?["rootFolder"]?["data"]?["id"]?.ToString() ?? string.Empty;
                projects.Add(new ApsProject
                {
                    Id = item["id"]?.ToString() ?? string.Empty,
                    Name = item["attributes"]?["name"]?.ToString() ?? "Unnamed Project",
                    RootFolderId = rootFolderId,
                    HubId = hubId
                });
            }
        }

        return projects;
    }

    /// <summary>
    /// Gets the user's account ID from APS (returns first hub ID).
    /// </summary>
    public async Task<string> GetAccountIdAsync()
    {
        var hubs = await GetHubsAsync();
        return hubs.FirstOrDefault()?.Id ?? string.Empty;
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _sessionManager.AccessToken);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"APS API request failed: {response.StatusCode} - {errorContent}");
        }
    }
}
