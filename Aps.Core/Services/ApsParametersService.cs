using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Aps.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aps.Core.Services;

/// <summary>
/// Service for accessing APS COBie Parameters API directly.
/// </summary>
public class ApsParametersService
{
    private const string BaseUrl = "https://developer.api.autodesk.com/parameters/v1";
    private readonly ApsSessionManager _sessionManager;
    private readonly ApsHubService _hubService;
    private readonly IApsLogger? _logger;
    private readonly ApsCategoryStorageService _categoryStorage;

    public ApsParametersService(ApsSessionManager sessionManager) : this(sessionManager, null, null) { }

    public ApsParametersService(ApsSessionManager sessionManager, IApsLogger? logger) : this(sessionManager, logger, null) { }

    public ApsParametersService(ApsSessionManager sessionManager, IApsLogger? logger, ApsCategoryStorageService? categoryStorage)
    {
        _sessionManager = sessionManager;
        _hubService = new ApsHubService(sessionManager);
        _logger = logger;
        _categoryStorage = categoryStorage ?? new ApsCategoryStorageService(sessionManager, logger);
    }

    /// <summary>
    /// Gets COBie parameters from APS for the specified account.
    /// </summary>
    public async Task<ApsParameterResponse.ParametersResponse> GetParametersAsync(
        string accountId,
        string? collectionId = null,
        bool forceRefresh = false)
    {
        await _sessionManager.EnsureTokenValidAsync();

        // If no collection specified, try to find the default COBie collection
        if (string.IsNullOrEmpty(collectionId))
        {
            collectionId = await GetDefaultCobieCollectionIdAsync(accountId);
            if (string.IsNullOrEmpty(collectionId))
            {
                _logger?.Warn($"[ApsParametersService] No COBie collection found for account {accountId}");
                return new ApsParameterResponse.ParametersResponse
                {
                    Parameters = new List<CobieParameterDefinition>(),
                    Cached = false
                };
            }
        }

        // Get category map from storage (handles file loading/API fetching automatically)
        Dictionary<string, string> categoryMap;
        try
        {
            categoryMap = await _categoryStorage.GetCategoryMapAsync();
            _logger?.Info($"[ApsParametersService] Using category map with {categoryMap.Count} categories");
        }
        catch (Exception ex)
        {
            _logger?.Error("[ApsParametersService] Failed to get category map, continuing without categories", ex);
            categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using var client = CreateHttpClient();
        var url = $"{BaseUrl}/accounts/{accountId}/groups/{accountId}/collections/{collectionId}/parameters";

        _logger?.Info($"[ApsParametersService] Fetching parameters from: {url}");

        var response = await client.GetAsync(url);

        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();

        // Log full response for analysis
        _logger?.Info($"[ApsParametersService] ========== RAW PARAMETERS RESPONSE START ==========");
        _logger?.Info($"[ApsParametersService] Full JSON response ({json.Length} chars): {json}");
        _logger?.Info($"[ApsParametersService] ========== RAW PARAMETERS RESPONSE END ==========");

        _logger?.Info($"[ApsParametersService] Parameters response (first 2000 chars): {json.Substring(0, Math.Min(2000, json.Length))}");

        // Handle different response structures
        JToken token;
        try
        {
            token = JToken.Parse(json);
        }
        catch (Exception ex)
        {
            _logger?.Error($"[ApsParametersService] Failed to parse JSON response: {ex.Message}. Response: {json}");
            return new ApsParameterResponse.ParametersResponse
            {
                Parameters = new List<CobieParameterDefinition>(),
                Cached = false
            };
        }

        _logger?.Info($"[ApsParametersService] Parsed JSON as {token.Type}");

        JArray? jsonArray = null;

        if (token is JArray arr)
        {
            _logger?.Info($"[ApsParametersService] JSON is an array with {arr.Count} items");
            jsonArray = arr;
        }
        else if (token is JObject obj)
        {
            _logger?.Info($"[ApsParametersService] JSON is an object. Properties: {string.Join(", ", obj.Properties().Select(p => $"{p.Name}:{p.Type}"))}");

            // Try different possible property names
            var possibleNames = new[] { "parameters", "data", "results", "items", "specifications", "attributes", "definitions", "specs" };

            foreach (var name in possibleNames)
            {
                if (obj[name] is JArray paramArray)
                {
                    _logger?.Info($"[ApsParametersService] Found array in property '{name}' with {paramArray.Count} items");
                    jsonArray = paramArray;
                    break;
                }
            }

            // Also check for nested structures
            if (jsonArray == null)
            {
                // Check for nested object
                foreach (var prop in obj.Properties())
                {
                    if (prop.Value is JObject nestedObj && nestedObj["items"] is JArray nestedArray)
                    {
                        _logger?.Info($"[ApsParametersService] Found nested array in '{prop.Name}' with {nestedArray.Count} items");
                        jsonArray = nestedArray;
                        break;
                    }
                }
            }
        }
        else
        {
            _logger?.Warn($"[ApsParametersService] Unexpected JSON token type: {token.Type}");
        }

        var parameters = new List<CobieParameterDefinition>();

        if (jsonArray != null)
        {
            _logger?.Info($"[ApsParametersService] ========== PROCESSING {jsonArray.Count} PARAMETERS ==========");

            for (int i = 0; i < jsonArray.Count; i++)
            {
                try
                {
                    var item = jsonArray[i];
                    _logger?.Info($"[ApsParametersService] --- Processing parameter #{i} ---");
                    _logger?.Info($"[ApsParametersService] Raw JSON: {item.ToString(Formatting.None)}");
                    var parsedParam = ParseParameterDefinition(item, categoryMap, _logger, i);
                    parameters.Add(parsedParam);

                    // Log the parsed result
                    _logger?.Info($"[ApsParametersService] Parsed Result:");
                    _logger?.Info($"  - ID: {parsedParam.Id}");
                    _logger?.Info($"  - Name: {parsedParam.Name}");
                    _logger?.Info($"  - DataTypeId: {parsedParam.DataTypeId}");
                    _logger?.Info($"  - DataType: {parsedParam.DataType}");
                    _logger?.Info($"  - InstanceTypeAssociation: {parsedParam.InstanceTypeAssociation}");
                    _logger?.Info($"  - CategoryBindingIds ({parsedParam.CategoryBindingIds.Length}): [{string.Join(", ", parsedParam.CategoryBindingIds)}]");
                    _logger?.Info($"  - CategoryNames ({parsedParam.CategoryNames.Length}): [{string.Join(", ", parsedParam.CategoryNames)}]");
                    _logger?.Info($"  - GroupBindingId: {parsedParam.GroupBindingId ?? "(none)"}");
                    _logger?.Info($"  - IsHidden: {parsedParam.IsHidden}");
                    _logger?.Info($"  - IsArchived: {parsedParam.IsArchived}");
                    _logger?.Info($"[ApsParametersService] --- End of parameter #{i} ---");
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[ApsParametersService] Failed to parse parameter at index {i}: {ex.Message}");
                    _logger?.Error($"[ApsParametersService] Stack trace: {ex.StackTrace}");
                    _logger?.Error($"[ApsParametersService] Item JSON: {jsonArray[i].ToString(Formatting.Indented)}");
                }
            }

            _logger?.Info($"[ApsParametersService] ========== COMPLETED PROCESSING. Successfully parsed {parameters.Count}/{jsonArray.Count} parameters ==========");
        }
        else
        {
            _logger?.Error("[ApsParametersService] No parameters array found in response");
        }

        return new ApsParameterResponse.ParametersResponse
        {
            Parameters = parameters,
            Cached = false // API doesn't return cache status directly
        };
    }

    /// <summary>
    /// Gets groups for the specified account from APS.
    /// Currently only one group is supported per account, with matching group ID to the account ID.
    /// </summary>
    public async Task<List<ApsGroup>> GetGroupsAsync(string accountId)
    {
        await _sessionManager.EnsureTokenValidAsync();

        using var client = CreateHttpClient();
        var url = $"{BaseUrl}/accounts/{accountId}/groups";

        _logger?.Info($"[ApsParametersService] Fetching groups from: {url}");

        var response = await client.GetAsync(url);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();

        _logger?.Info($"[ApsParametersService] Groups response: {json}");

        // Handle different response structures
        JToken token = JToken.Parse(json);
        JArray? jsonArray = null;

        if (token is JArray arr)
        {
            jsonArray = arr;
        }
        else if (token is JObject obj)
        {
            // Check for common wrapper properties
            jsonArray = obj["groups"] as JArray
                      ?? obj["data"] as JArray
                      ?? obj["results"] as JArray
                      ?? obj["items"] as JArray;
        }

        var groups = new List<ApsGroup>();

        if (jsonArray != null)
        {
            foreach (var item in jsonArray)
            {
                var name = item["title"]?.ToString()
                         ?? item["name"]?.ToString()
                         ?? item["displayName"]?.ToString()
                         ?? "Unnamed Group";

                var group = new ApsGroup
                {
                    Id = item["id"]?.ToString() ?? string.Empty,
                    Name = name,
                    AccountId = item["accountId"]?.ToString() ?? accountId,
                    Description = item["description"]?.ToString()
                };

                _logger?.Info($"[ApsParametersService] Parsed group: Id={group.Id}, Name={group.Name}");
                groups.Add(group);
            }
        }

        _logger?.Info($"[ApsParametersService] Loaded {groups.Count} group(s)");
        return groups;
    }

    /// <summary>
    /// Gets collections for the specified group from APS.
    /// </summary>
    public async Task<List<ApsCollection>> GetCollectionsAsync(string accountId, string groupId)
    {
        await _sessionManager.EnsureTokenValidAsync();

        using var client = CreateHttpClient();
        var url = $"{BaseUrl}/accounts/{accountId}/groups/{groupId}/collections";

        _logger?.Info($"[ApsParametersService] Fetching collections from: {url}");

        var response = await client.GetAsync(url);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();

        _logger?.Info($"[ApsParametersService] Collections response: {json}");

        // Handle different response structures
        JToken token = JToken.Parse(json);
        JArray? jsonArray = null;

        if (token is JArray arr)
        {
            jsonArray = arr;
        }
        else if (token is JObject obj)
        {
            // Check for common wrapper properties
            jsonArray = obj["collections"] as JArray
                      ?? obj["data"] as JArray
                      ?? obj["results"] as JArray
                      ?? obj["items"] as JArray;
        }

        var collections = new List<ApsCollection>();

        if (jsonArray != null)
        {
            foreach (var item in jsonArray)
            {
                var name = item["name"]?.ToString()
                         ?? item["title"]?.ToString()
                         ?? item["displayName"]?.ToString()
                         ?? string.Empty;

                var collection = new ApsCollection
                {
                    Id = item["id"]?.ToString() ?? string.Empty,
                    Name = name,
                    GroupId = item["groupId"]?.ToString() ?? groupId,
                    AccountId = item["accountId"]?.ToString() ?? accountId,
                    Description = item["description"]?.ToString(),
                    IsDefaultCobieCollection = name.Equals("COBie", StringComparison.OrdinalIgnoreCase)
                };

                _logger?.Info($"[ApsParametersService] Parsed collection: Id={collection.Id}, Name={collection.Name}");
                collections.Add(collection);
            }
        }

        _logger?.Info($"[ApsParametersService] Loaded {collections.Count} collection(s)");
        return collections;
    }

    /// <summary>
    /// Gets parameter specifications from APS.
    /// </summary>
    public async Task<ApsParameterResponse.SpecsResponse> GetSpecsAsync()
    {
        await _sessionManager.EnsureTokenValidAsync();

        using var client = CreateHttpClient();
        var response = await client.GetAsync($"{BaseUrl}/specs");

        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();

        var jsonArray = JArray.Parse(json);
        var specs = new List<ApsParameterResponse.DataTypeSpec>();

        foreach (var token in jsonArray)
        {
            specs.Add(new ApsParameterResponse.DataTypeSpec
            {
                Id = token["id"]?.ToString() ?? string.Empty,
                Name = token["name"]?.ToString() ?? string.Empty,
                UnitType = token["unitType"]?.ToString()
            });
        }

        return new ApsParameterResponse.SpecsResponse { Specs = specs };
    }

    /// <summary>
    /// Get default COBie collection ID for an account.
    /// </summary>
    private async Task<string?> GetDefaultCobieCollectionIdAsync(string accountId)
    {
        _logger?.Info($"[ApsParametersService] Looking for default COBie collection for account {accountId}");

        using var client = CreateHttpClient();
        var url = $"{BaseUrl}/accounts/{accountId}/groups/{accountId}/collections";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger?.Error($"[ApsParametersService] Failed to get collections: {response.StatusCode}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();

        // Handle different response structures
        JToken token = JToken.Parse(json);
        JArray? jsonArray = null;

        if (token is JArray arr)
        {
            jsonArray = arr;
        }
        else if (token is JObject obj)
        {
            // Check for common wrapper properties
            jsonArray = obj["collections"] as JArray
                      ?? obj["data"] as JArray
                      ?? obj["results"] as JArray
                      ?? obj["items"] as JArray;
        }

        if (jsonArray == null)
        {
            _logger?.Warn("[ApsParametersService] No collections found or invalid response format");
            return null;
        }

        // Look for a collection named "COBie" or similar
        foreach (var item in jsonArray)
        {
            var name = item["name"]?.ToString();
            if (!string.IsNullOrEmpty(name) &&
                string.Equals(name, "COBie", StringComparison.OrdinalIgnoreCase))
            {
                var id = item["id"]?.ToString() ?? string.Empty;
                _logger?.Info($"[ApsParametersService] Found COBie collection: {id}");
                return id;
            }
        }

        // If no COBie collection found, return the first collection
        if (jsonArray.HasValues)
        {
            var firstId = jsonArray[0]["id"]?.ToString();
            _logger?.Info($"[ApsParametersService] Using first collection: {firstId}");
            return firstId;
        }

        _logger?.Warn("[ApsParametersService] No collections found");
        return null;
    }

    /// <summary>
    /// Parse APS parameter JSON to CobieParameterDefinition.
    /// Handles both the documented APS API format and variations.
    /// </summary>
    private static CobieParameterDefinition ParseParameterDefinition(
        JToken token,
        System.Collections.Generic.Dictionary<string, string> categoryMap,
        IApsLogger? logger = null,
        int paramIndex = -1)
    {
        var prefix = paramIndex >= 0 ? $"[Param#{paramIndex}]" : "[ParseParam]";

        logger?.Info($"{prefix} ========== PARSING PARAMETER DEFINITION ==========");

        // Extract basic properties
        var paramId = token["id"]?.ToString() ?? string.Empty;
        var paramName = token["name"]?.ToString() ?? string.Empty;
        var paramDesc = token["description"]?.ToString();
        var readOnly = token["readOnly"]?.ToObject<bool>() ?? false;

        logger?.Info($"{prefix} Basic properties extracted:");
        logger?.Info($"  - id: '{paramId}'");
        logger?.Info($"  - name: '{paramName}'");
        logger?.Info($"  - description: '{paramDesc ?? "(none)"}'");
        logger?.Info($"  - readOnly: {readOnly}");

        var param = new CobieParameterDefinition
        {
            Id = paramId,
            Name = paramName,
            Description = paramDesc,
            IsHidden = readOnly,
            IsArchived = false // Will be determined from metadata
        };

        // Try to get valueTypeId (preferred per workflow document) or dataTypeId (legacy)
        var valueTypeId = token["valueTypeId"]?.ToString();
        var dataTypeId = token["dataTypeId"]?.ToString();
        var specId = token["specId"]?.ToString();

        logger?.Info($"{prefix} Data type fields:");
        logger?.Info($"  - valueTypeId: '{valueTypeId ?? "(not found)"}'");
        logger?.Info($"  - dataTypeId: '{dataTypeId ?? "(not found)"}'");
        logger?.Info($"  - specId: '{specId ?? "(not found)"}'");

        var finalTypeId = valueTypeId ?? dataTypeId ?? specId ?? string.Empty;
        param.DataTypeId = finalTypeId;
        logger?.Info($"{prefix} Using DataTypeId: '{finalTypeId}'");

        // Parse metadata - can be an array or object
        var metadataToken = token["metadata"];
        var metadataStatus = metadataToken != null ? $"FOUND (type: {metadataToken.Type})" : "NOT FOUND";
        logger?.Info($"{prefix} metadata field: {metadataStatus}");

        if (metadataToken != null)
        {
            if (metadataToken is JArray metadataArray)
            {
                logger?.Info($"{prefix} metadata is ARRAY with {metadataArray.Count} items");
                ParseMetadataArray(param, metadataArray, categoryMap, logger, prefix);
            }
            else if (metadataToken is JObject metadataObj)
            {
                var props = string.Join(", ", metadataObj.Properties().Select(p => p.Name));
                logger?.Info($"{prefix} metadata is OBJECT with properties: {props}");
                ParseMetadataObject(param, metadataObj, categoryMap, logger, prefix);
            }
        }

        // If no categories found in metadata, try direct categories array
        if (param.CategoryBindingIds.Length == 0)
        {
            logger?.Info($"{prefix} No categories found in metadata, checking for direct categories array...");
            var categoriesToken = token["categories"];
            var categoriesStatus = categoriesToken != null ? $"FOUND (type: {categoriesToken.Type})" : "NOT FOUND";
            logger?.Info($"{prefix} categories field: {categoriesStatus}");

            if (categoriesToken is JArray categoriesArray)
            {
                logger?.Info($"{prefix} categories is ARRAY with {categoriesArray.Count} items");

                var categoryIdsList = new System.Collections.Generic.List<string>();
                var categoryNamesList = new System.Collections.Generic.List<string>();

                for (int catIdx = 0; catIdx < categoriesArray.Count; catIdx++)
                {
                    var catToken = categoriesArray[catIdx];
                    logger?.Info($"{prefix}   Category[{catIdx}]: {catToken.ToString(Formatting.None)}");

                    var catId = catToken["id"]?.ToString() ?? catToken.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(catId))
                    {
                        categoryIdsList.Add(catId);
                        logger?.Info($"{prefix}     - Category ID: '{catId}'");

                        // Try to get name from map or from the token
                        if (categoryMap.TryGetValue(catId, out var catName))
                        {
                            categoryNamesList.Add(catName);
                            logger?.Info($"{prefix}     - Category Name from map: '{catName}'");
                        }
                        else
                        {
                            var tokenName = catToken["name"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(tokenName))
                            {
                                categoryNamesList.Add(tokenName);
                                logger?.Info($"{prefix}     - Category Name from token: '{tokenName}'");
                            }
                            else
                            {
                                logger?.Warn($"{prefix}     - Category Name NOT FOUND in map or token");
                            }
                        }
                    }
                }

                param.CategoryBindingIds = categoryIdsList.ToArray();
                param.CategoryNames = categoryNamesList.ToArray();
                logger?.Info($"{prefix} Direct categories parsing complete. Found {categoryIdsList.Count} categories.");
            }
        }
        else
        {
            logger?.Info($"{prefix} Categories found in metadata: {param.CategoryBindingIds.Length} IDs, {param.CategoryNames.Length} names");
        }

        // Parse data type from ID
        param.DataType = ParseDataType(param.DataTypeId);
        logger?.Info($"{prefix} Parsed DataType: {param.DataType}");

        logger?.Info($"{prefix} ========== PARAMETER DEFINITION PARSING COMPLETE ==========");

        return param;
    }

    /// <summary>
    /// Parse metadata from array format.
    /// </summary>
    private static void ParseMetadataArray(
        CobieParameterDefinition param,
        JArray metadata,
        System.Collections.Generic.Dictionary<string, string> categoryMap,
        IApsLogger? logger = null,
        string? prefix = null)
    {
        logger?.Info($"{prefix} Parsing metadata ARRAY ({metadata.Count} items):");

        foreach (var item in metadata)
        {
            var id = item["id"]?.ToString();
            var value = item["value"];

            logger?.Info($"{prefix}   Metadata item: id='{id}', value type={(value?.Type.ToString() ?? "null")}");

            switch (id)
            {
                case "instanceTypeAssociation":
                    var instanceType = value?.ToString();
                    logger?.Info($"{prefix}     - instanceTypeAssociation: '{instanceType}'");
                    if (instanceType == "TYPE")
                        param.InstanceTypeAssociation = ParameterType.Type;
                    else
                        param.InstanceTypeAssociation = ParameterType.Instance;
                    logger?.Info($"{prefix}     - Set InstanceTypeAssociation to: {param.InstanceTypeAssociation}");
                    break;

                case "categoryBindingIds":
                    if (value is JArray categoryArray)
                    {
                        var ids = categoryArray.Select(c => c.ToString() ?? string.Empty).ToArray();
                        param.CategoryBindingIds = ids;
                        logger?.Info($"{prefix}     - categoryBindingIds: [{string.Join(", ", ids)}] ({ids.Length} items)");

                        // Also populate category names from the category map
                        var categoryNamesList = new System.Collections.Generic.List<string>();
                        foreach (var catId in param.CategoryBindingIds)
                        {
                            if (categoryMap.TryGetValue(catId, out var catName))
                            {
                                categoryNamesList.Add(catName);
                                logger?.Info($"{prefix}       - '{catId}' -> '{catName}' (mapped)");
                            }
                            else
                            {
                                // Extract category name from ID
                                var extractedName = ExtractCategoryNameFromId(catId);
                                if (!string.IsNullOrEmpty(extractedName))
                                {
                                    categoryNamesList.Add(extractedName);
                                    logger?.Info($"{prefix}       - '{catId}' -> '{extractedName}' (extracted from ID)");
                                }
                                else
                                {
                                    logger?.Warn($"{prefix}       - '{catId}' -> NOT FOUND in category map and could not extract from ID");
                                }
                            }
                        }
                        param.CategoryNames = categoryNamesList.ToArray();
                        logger?.Info($"{prefix}     - Resolved {categoryNamesList.Count}/{param.CategoryBindingIds.Length} category names");
                    }
                    else
                    {
                        logger?.Warn($"{prefix}     - categoryBindingIds value is not an array (type: {value?.Type})");
                    }
                    break;

                case "categories":
                    // Handle categories in metadata array format: value is directly an array of category objects
                    // Format from APS: {"id":"categories","value":[{"id":"autodesk.revit.category.xxx","bindingId":"..."}]}
                    var catIdsList = new System.Collections.Generic.List<string>();
                    var catNamesList = new System.Collections.Generic.List<string>();

                    if (value is JArray categoriesArray)
                    {
                        logger?.Info($"{prefix}     - categories value is direct array with {categoriesArray.Count} items");

                        foreach (var catToken in categoriesArray)
                        {
                            var catId = catToken["id"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(catId))
                            {
                                catIdsList.Add(catId);
                                logger?.Info($"{prefix}       - Category ID: '{catId}'");

                                if (categoryMap.TryGetValue(catId, out var catName))
                                {
                                    catNamesList.Add(catName);
                                    logger?.Info($"{prefix}         - Mapped to: '{catName}'");
                                }
                                else
                                {
                                    // Try to get name from the category token itself
                                    var catNameFromToken = catToken["name"]?.ToString();
                                    if (!string.IsNullOrEmpty(catNameFromToken))
                                    {
                                        catNamesList.Add(catNameFromToken);
                                        logger?.Info($"{prefix}         - Using name from token: '{catNameFromToken}'");
                                    }
                                    else
                                    {
                                        // Extract category name from ID (e.g., "autodesk.revit.category.local:walls-1.0.0" → "walls")
                                        var extractedName = ExtractCategoryNameFromId(catId);
                                        if (!string.IsNullOrEmpty(extractedName))
                                        {
                                            catNamesList.Add(extractedName);
                                            logger?.Info($"{prefix}         - Extracted name from ID: '{extractedName}'");
                                        }
                                        else
                                        {
                                            logger?.Warn($"{prefix}         - NOT FOUND in category map and could not extract name from ID");
                                        }
                                    }
                                }
                            }
                        }

                        param.CategoryBindingIds = catIdsList.ToArray();
                        param.CategoryNames = catNamesList.ToArray();
                        logger?.Info($"{prefix}     - Total categories parsed: {catIdsList.Count} IDs, {catNamesList.Count} names");
                    }
                    else if (value is JObject categoriesObj && categoriesObj["value"] is JArray categoriesValue)
                    {
                        logger?.Info($"{prefix}     - categories.value array found with {categoriesValue.Count} items");

                        foreach (var catToken in categoriesValue)
                        {
                            var catId = catToken["id"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(catId))
                            {
                                catIdsList.Add(catId);
                                logger?.Info($"{prefix}       - Category ID: '{catId}'");

                                if (categoryMap.TryGetValue(catId, out var catName))
                                {
                                    catNamesList.Add(catName);
                                    logger?.Info($"{prefix}         - Mapped to: '{catName}'");
                                }
                                else
                                {
                                    // Extract category name from ID
                                    var extractedName = ExtractCategoryNameFromId(catId);
                                    if (!string.IsNullOrEmpty(extractedName))
                                    {
                                        catNamesList.Add(extractedName);
                                        logger?.Info($"{prefix}         - Extracted name from ID: '{extractedName}'");
                                    }
                                    else
                                    {
                                        logger?.Warn($"{prefix}         - NOT FOUND in category map and could not extract name from ID");
                                    }
                                }
                            }
                        }

                        param.CategoryBindingIds = catIdsList.ToArray();
                        param.CategoryNames = catNamesList.ToArray();
                    }
                    else
                    {
                        logger?.Warn($"{prefix}     - categories value is not in expected format (type: {value?.Type})");
                    }
                    break;

                case "labelIds":
                    if (value is JArray labelArray)
                    {
                        var labels = labelArray.Select(l => l.ToString() ?? string.Empty).ToArray();
                        param.Labels = labels;
                        logger?.Info($"{prefix}     - labelIds: [{string.Join(", ", labels)}] ({labels.Length} items)");
                    }
                    break;

                case "isHidden":
                    var isHidden = value?.ToObject<bool>() ?? param.IsHidden;
                    param.IsHidden = isHidden;
                    logger?.Info($"{prefix}     - isHidden: {isHidden}");
                    break;

                case "isArchived":
                    var isArchived = value?.ToObject<bool>() ?? false;
                    param.IsArchived = isArchived;
                    logger?.Info($"{prefix}     - isArchived: {isArchived}");
                    break;

                case "groupBindingId":
                case "group":
                    var groupId = value?.ToString();
                    param.GroupBindingId = groupId;
                    logger?.Info($"{prefix}     - groupBindingId: '{groupId ?? "(null)"}'");
                    break;

                default:
                    logger?.Info($"{prefix}     - Unknown metadata id: '{id}'");
                    break;
            }
        }
    }

    /// <summary>
    /// Parse metadata from object format.
    /// </summary>
    private static void ParseMetadataObject(
        CobieParameterDefinition param,
        JObject metadata,
        System.Collections.Generic.Dictionary<string, string> categoryMap,
        IApsLogger? logger = null,
        string? prefix = null)
    {
        logger?.Info($"{prefix} Parsing metadata OBJECT with properties: {string.Join(", ", metadata.Properties().Select(p => p.Name))}");

        // Parse instanceTypeAssociation
        var instanceTypeToken = metadata["instanceTypeAssociation"];
        if (instanceTypeToken != null)
        {
            var instanceType = instanceTypeToken.ToString();
            logger?.Info($"{prefix}   - instanceTypeAssociation: '{instanceType}'");
            if (instanceType == "TYPE")
                param.InstanceTypeAssociation = ParameterType.Type;
            else
                param.InstanceTypeAssociation = ParameterType.Instance;
            logger?.Info($"{prefix}     - Set to: {param.InstanceTypeAssociation}");
        }

        // Parse categories from metadata.categories.value[].id format
        var categoriesToken = metadata["categories"];
        if (categoriesToken != null)
        {
            logger?.Info($"{prefix}   - categories field FOUND (type: {categoriesToken.Type})");

            var categoryIdsList = new System.Collections.Generic.List<string>();
            var categoryNamesList = new System.Collections.Generic.List<string>();

            // Handle both categories.value[] array and categories as direct array
            JArray? categoriesArray = null;
            if (categoriesToken is JObject categoriesObj && categoriesObj["value"] is JArray valueArray)
            {
                categoriesArray = valueArray;
                logger?.Info($"{prefix}     - categories.value array found with {valueArray.Count} items");
            }
            else if (categoriesToken is JArray directArray)
            {
                categoriesArray = directArray;
                logger?.Info($"{prefix}     - categories is direct array with {directArray.Count} items");
            }

            if (categoriesArray != null)
            {
                for (int catIdx = 0; catIdx < categoriesArray.Count; catIdx++)
                {
                    var catToken = categoriesArray[catIdx];
                    var catId = catToken["id"]?.ToString() ?? catToken.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(catId))
                    {
                        categoryIdsList.Add(catId);
                        logger?.Info($"{prefix}       - Category[{catIdx}] ID: '{catId}'");

                        if (categoryMap.TryGetValue(catId, out var catName))
                        {
                            categoryNamesList.Add(catName);
                            logger?.Info($"{prefix}         - Mapped to: '{catName}'");
                        }
                        else
                        {
                            var tokenName = catToken["name"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(tokenName))
                            {
                                categoryNamesList.Add(tokenName);
                                logger?.Info($"{prefix}         - Using token name: '{tokenName}'");
                            }
                            else
                            {
                                // Extract category name from ID
                                var extractedName = ExtractCategoryNameFromId(catId);
                                if (!string.IsNullOrEmpty(extractedName))
                                {
                                    categoryNamesList.Add(extractedName);
                                    logger?.Info($"{prefix}         - Extracted name from ID: '{extractedName}'");
                                }
                                else
                                {
                                    logger?.Warn($"{prefix}         - Category name NOT FOUND in map, token, or could not extract from ID");
                                }
                            }
                        }
                    }
                }

                param.CategoryBindingIds = categoryIdsList.ToArray();
                param.CategoryNames = categoryNamesList.ToArray();
                logger?.Info($"{prefix}     - Total categories: {categoryIdsList.Count} IDs, {categoryNamesList.Count} names");
            }
        }
        else
        {
            logger?.Info($"{prefix}   - categories field NOT FOUND");
        }

        // Parse group
        var groupToken = metadata["group"];
        if (groupToken != null)
        {
            var groupId = groupToken["id"]?.ToString() ?? groupToken.ToString();
            param.GroupBindingId = groupId;
            logger?.Info($"{prefix}   - group: '{groupId ?? "(null)"}'");
        }

        // Parse flags
        var isHidden = metadata["isHidden"]?.ToObject<bool>();
        var isArchived = metadata["isArchived"]?.ToObject<bool>();

        if (isHidden.HasValue)
        {
            param.IsHidden = isHidden.Value;
            logger?.Info($"{prefix}   - isHidden: {isHidden.Value}");
        }

        if (isArchived.HasValue)
        {
            param.IsArchived = isArchived.Value;
            logger?.Info($"{prefix}   - isArchived: {isArchived.Value}");
        }
    }

    /// <summary>
    /// Parse data type from APS value type ID.
    /// Handles both simple types (String, Integer, Boolean, Number) and spec IDs (autodesk.revit.spec:text-1.0.0).
    /// </summary>
    private static ParameterDataType ParseDataType(string valueTypeId)
    {
        if (string.IsNullOrEmpty(valueTypeId))
            return ParameterDataType.Unknown;

        var lowerType = valueTypeId.ToLowerInvariant();

        // First check for simple type names (from workflow document)
        return lowerType switch
        {
            "string" => ParameterDataType.Text,
            "text" => ParameterDataType.Text,
            "integer" => ParameterDataType.Integer,
            "int" => ParameterDataType.Integer,
            "int32" => ParameterDataType.Integer,
            "int64" => ParameterDataType.Integer,
            "boolean" => ParameterDataType.YesNo,
            "bool" => ParameterDataType.YesNo,
            "number" => ParameterDataType.Number,
            "double" => ParameterDataType.Number,
            "float" => ParameterDataType.Number,
            "float64" => ParameterDataType.Number,
            "float32" => ParameterDataType.Number,

            // Then check for spec ID format
            var t when t.Contains("text") => ParameterDataType.Text,
            var t when t.Contains("integer") => ParameterDataType.Integer,
            var t when t.Contains("length") => ParameterDataType.Length,
            var t when t.Contains("area") => ParameterDataType.Area,
            var t when t.Contains("volume") => ParameterDataType.Volume,
            var t when t.Contains("angle") => ParameterDataType.Angle,
            var t when t.Contains("familytype") => ParameterDataType.FamilyType,
            var t when t.Contains("boolean") => ParameterDataType.YesNo,
            var t when t.Contains("yesno") => ParameterDataType.YesNo,
            _ => ParameterDataType.Unknown
        };
    }

    /// <summary>
    /// Extracts the category name from an APS category ID.
    /// APS format: autodesk.revit.category.xxx:CategoryName-1.0.0
    /// Returns: categoryname (lowercase) for use in category mapping
    /// Examples:
    ///   - autodesk.revit.category.local:walls-1.0.0 → walls
    ///   - autodesk.revit.category.family:genericModel-1.0.0 → genericmodel
    ///   - autodesk.revit.category.local:rooms-1.0.0 → rooms
    /// </summary>
    private static string ExtractCategoryNameFromId(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
            return string.Empty;

        // Find the colon separator
        var colonIndex = categoryId.IndexOf(':');
        if (colonIndex < 0 || colonIndex >= categoryId.Length - 1)
            return string.Empty;

        // Extract everything after the colon
        var afterColon = categoryId.Substring(colonIndex + 1);

        // Find the version number separator (first dash)
        var dashIndex = afterColon.IndexOf('-');
        if (dashIndex > 0)
        {
            // Extract the category name (between colon and dash)
            var categoryName = afterColon.Substring(0, dashIndex);
            return categoryName.ToLowerInvariant();
        }

        // No version found, return the lowercase string
        return afterColon.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the user's account ID from APS (delegates to ApsHubService).
    /// </summary>
    public async Task<string> GetAccountIdAsync()
    {
        return await _hubService.GetAccountIdAsync();
    }

    /// <summary>
    /// Gets the user's hubs (accounts) from APS (delegates to ApsHubService).
    /// </summary>
    public async Task<List<ApsHub>> GetHubsAsync()
    {
        return await _hubService.GetHubsAsync();
    }

    /// <summary>
    /// Gets projects for a specific hub (delegates to ApsHubService).
    /// </summary>
    public async Task<List<ApsProject>> GetProjectsAsync(string hubId)
    {
        return await _hubService.GetProjectsAsync(hubId);
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

    private string BuildQueryString(string baseUrl, Dictionary<string, string?> queryParams)
    {
        if (queryParams.Count == 0)
            return baseUrl;

        var filteredParams = queryParams
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");

        return $"{baseUrl}?{string.Join("&", filteredParams)}";
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
