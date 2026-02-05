using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APS.Bridge.Services;

/// <summary>
/// Client for APS Parameters API
/// </summary>
public class ApsParametersService
{
    private const string BaseUrl = "https://developer.api.autodesk.com/parameters/v1";
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public ApsParametersService(HttpClient httpClient, string accessToken)
    {
        _httpClient = httpClient;
        _accessToken = accessToken;
    }

    /// <summary>
    /// Get all parameters from a COBie collection
    /// </summary>
    public async Task<ObservableCollection<CobieParameterDefinition>> GetParametersAsync(
        string accountId,
        string? collectionId = null,
        CancellationToken cancellationToken = default)
    {
        // If no collection specified, try to find the default COBie collection
        if (string.IsNullOrEmpty(collectionId))
        {
            collectionId = await GetDefaultCobieCollectionIdAsync(accountId, cancellationToken);
            if (string.IsNullOrEmpty(collectionId))
            {
                return new ObservableCollection<CobieParameterDefinition>();
            }
        }

        var url = $"{BaseUrl}/accounts/{accountId}/groups/{accountId}/collections/{collectionId}/parameters";
        var response = await GetWithAuthAsync(url, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonArray = JArray.Parse(json);

        var parameters = new ObservableCollection<CobieParameterDefinition>();
        foreach (var token in jsonArray)
        {
            parameters.Add(ParseParameterDefinition(token));
        }

        return parameters;
    }

    /// <summary>
    /// Get data type specifications
    /// </summary>
    public async Task<List<DataTypeSpec>> GetSpecsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/specs";
        var response = await GetWithAuthAsync(url, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonArray = JArray.Parse(json);

        var specs = new List<DataTypeSpec>();
        foreach (var token in jsonArray)
        {
            specs.Add(new DataTypeSpec
            {
                Id = token["id"]?.Value<string>() ?? string.Empty,
                Name = token["name"]?.Value<string>() ?? string.Empty,
                UnitType = token["unitType"]?.Value<string>()
            });
        }

        return specs;
    }

    /// <summary>
    /// Get Revit categories
    /// </summary>
    public async Task<List<CategoryInfo>> GetCategoriesAsync(
        string? discipline = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/classifications/categories";
        var response = await GetWithAuthAsync(url, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonArray = JArray.Parse(json);

        var categories = new List<CategoryInfo>();
        foreach (var token in jsonArray)
        {
            var category = new CategoryInfo
            {
                Id = token["id"]?.Value<string>() ?? string.Empty,
                Name = token["name"]?.Value<string>() ?? string.Empty,
                Discipline = token["discipline"]?.Value<string>() ?? "architectural",
                Level = token["level"]?.Value<string>() ?? "instances"
            };

            // Filter by discipline if specified
            if (string.IsNullOrEmpty(discipline) ||
                category.Discipline.Equals(discipline, StringComparison.OrdinalIgnoreCase))
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    /// <summary>
    /// Get default COBie collection ID
    /// </summary>
    private async Task<string?> GetDefaultCobieCollectionIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/accounts/{accountId}/groups/{accountId}/collections";
        var response = await GetWithAuthAsync(url, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonArray = JArray.Parse(json);

        // Look for a collection named "COBie" or similar
        foreach (var token in jsonArray)
        {
            var name = token["name"]?.Value<string>();
            if (!string.IsNullOrEmpty(name) &&
                name.Equals("COBie", StringComparison.OrdinalIgnoreCase))
            {
                return token["id"]?.Value<string>();
            }
        }

        // If no COBie collection found, return the first collection
        if (jsonArray.HasValues)
        {
            return jsonArray[0]["id"]?.Value<string>();
        }

        return null;
    }

    /// <summary>
    /// Make HTTP GET request with Bearer token authentication
    /// </summary>
    private async Task<HttpResponseMessage> GetWithAuthAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApsParametersException(
                $"APS Parameters API request failed: {response.StatusCode}",
                (int)response.StatusCode,
                error);
        }

        return response;
    }

    /// <summary>
    /// Parse APS parameter JSON to CobieParameterDefinition
    /// </summary>
    private static CobieParameterDefinition ParseParameterDefinition(JToken token)
    {
        var param = new CobieParameterDefinition
        {
            Id = token["id"]?.Value<string>() ?? string.Empty,
            Name = token["name"]?.Value<string>() ?? string.Empty,
            Description = token["description"]?.Value<string>(),
            DataTypeId = token["dataTypeId"]?.Value<string>() ?? string.Empty,
            IsHidden = token["readOnly"]?.Value<bool>() ?? false,
            IsArchived = false // Will be determined from metadata
        };

        // Parse metadata array
        var metadata = token["metadata"] as JArray;
        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                var id = item["id"]?.Value<string>();
                var value = item["value"];

                switch (id)
                {
                    case "instanceTypeAssociation":
                        if (value?.Value<string>() == "TYPE")
                            param.InstanceTypeAssociation = ParameterType.Type;
                        else
                            param.InstanceTypeAssociation = ParameterType.Instance;
                        break;

                    case "categoryBindingIds":
                        if (value is JArray categoryArray)
                        {
                            param.CategoryBindingIds = categoryArray
                                .Select(c => c.Value<string>() ?? string.Empty)
                                .ToArray();
                        }
                        break;

                    case "labelIds":
                        if (value is JArray labelArray)
                        {
                            param.Labels = labelArray
                                .Select(l => l.Value<string>() ?? string.Empty)
                                .ToArray();
                        }
                        break;

                    case "isHidden":
                        param.IsHidden = value?.Value<bool>() ?? param.IsHidden;
                        break;

                    case "isArchived":
                        param.IsArchived = value?.Value<bool>() ?? false;
                        break;

                    case "groupBindingId":
                        param.GroupBindingId = value?.Value<string>();
                        break;
                }
            }
        }

        // Parse data type from ID
        param.DataType = ParseDataType(param.DataTypeId);

        return param;
    }

    /// <summary>
    /// Parse data type from APS data type ID
    /// </summary>
    private static ParameterDataType ParseDataType(string dataTypeId)
    {
        if (string.IsNullOrEmpty(dataTypeId))
            return ParameterDataType.Unknown;

        return dataTypeId.ToLowerInvariant() switch
        {
            var t when t.Contains("text") => ParameterDataType.Text,
            var t when t.Contains("integer") => ParameterDataType.Integer,
            var t when t.Contains("length") => ParameterDataType.Length,
            var t when t.Contains("area") => ParameterDataType.Area,
            var t when t.Contains("volume") => ParameterDataType.Volume,
            var t when t.Contains("angle") => ParameterDataType.Angle,
            var t when t.Contains("familytype") => ParameterDataType.FamilyType,
            var t when t.Contains("boolean") => ParameterDataType.YesNo,
            _ => ParameterDataType.Unknown
        };
    }
}

/// <summary>
/// Models for APS Parameters Service
/// </summary>
public class CobieParameterDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataTypeId { get; set; } = string.Empty;
    public ParameterDataType DataType { get; set; }
    public ParameterType InstanceTypeAssociation { get; set; }
    public string[] CategoryBindingIds { get; set; } = Array.Empty<string>();
    public string[] Labels { get; set; } = Array.Empty<string>();
    public bool IsHidden { get; set; }
    public bool IsArchived { get; set; }
    public string? GroupBindingId { get; set; }
}

public enum ParameterDataType
{
    Text, Integer, Number, Length, Area, Volume, Angle, FamilyType, YesNo, MultiValue, Unknown
}

public enum ParameterType
{
    Instance, Type
}

public class DataTypeSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UnitType { get; set; }
}

public class CategoryInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

/// <summary>
/// Exception for APS Parameters API errors
/// </summary>
public class ApsParametersException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public ApsParametersException(string message, int statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
