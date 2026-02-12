using Autodesk.Revit.DB;
using Aps.Core.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Services;

/// <summary>
/// Service for binding Shared Parameters to Revit categories.
/// </summary>
public class ParameterBindingService : IParameterBindingService
{
    private readonly ILogger? _logger;

    public ParameterBindingService(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Maps APS category IDs to Revit BuiltInCategories.
    /// Key: APS category ID (lowercase), Value: Revit BuiltInCategory
    /// Handles both simple names like "walls" and full APS IDs like "autodesk.revit.category.local:walls-1.0.0"
    /// </summary>
    private static readonly Dictionary<string, BuiltInCategory> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common Revit categories
        { "walls", BuiltInCategory.OST_Walls },
        { "wall", BuiltInCategory.OST_Walls },
        { "floors", BuiltInCategory.OST_Floors },
        { "floor", BuiltInCategory.OST_Floors },
        { "roofs", BuiltInCategory.OST_Roofs },
        { "roof", BuiltInCategory.OST_Roofs },
        { "ceilings", BuiltInCategory.OST_Ceilings },
        { "ceiling", BuiltInCategory.OST_Ceilings },
        { "columns", BuiltInCategory.OST_Columns },
        { "column", BuiltInCategory.OST_Columns },
        { "structuralcolumns", BuiltInCategory.OST_StructuralColumns },
        { "structuralcolumn", BuiltInCategory.OST_StructuralColumns },
        { "structuralframing", BuiltInCategory.OST_StructuralFraming },
        { "beams", BuiltInCategory.OST_StructuralFraming },
        { "beam", BuiltInCategory.OST_StructuralFraming },
        { "doors", BuiltInCategory.OST_Doors },
        { "door", BuiltInCategory.OST_Doors },
        { "windows", BuiltInCategory.OST_Windows },
        { "window", BuiltInCategory.OST_Windows },
        { "curtainwallpanels", BuiltInCategory.OST_CurtainWallPanels },
        { "curtainpanels", BuiltInCategory.OST_CurtainWallPanels },
        { "curtainwallmullions", BuiltInCategory.OST_CurtainWallMullions },
        { "curtainmullions", BuiltInCategory.OST_CurtainWallMullions },
        { "stairs", BuiltInCategory.OST_Stairs },
        { "stair", BuiltInCategory.OST_Stairs },
        { "railings", BuiltInCategory.OST_StairsRailing },
        { "railing", BuiltInCategory.OST_StairsRailing },
        { "stairrailing", BuiltInCategory.OST_StairsRailing },
        { "stairsrailing", BuiltInCategory.OST_StairsRailing },
        { "ramps", BuiltInCategory.OST_Ramps },
        { "ramp", BuiltInCategory.OST_Ramps },
        { "rooms", BuiltInCategory.OST_Rooms },
        { "room", BuiltInCategory.OST_Rooms },
        { "areas", BuiltInCategory.OST_Areas },
        { "area", BuiltInCategory.OST_Areas },

        // MEP Categories
        { "pipes", BuiltInCategory.OST_PipeSegments },
        { "pipe", BuiltInCategory.OST_PipeSegments },
        { "pipesegments", BuiltInCategory.OST_PipeSegments },
        { "pipeaccessories", BuiltInCategory.OST_PipeAccessory },
        { "pipefittings", BuiltInCategory.OST_PipeFitting },
        { "pipefitting", BuiltInCategory.OST_PipeFitting },
        { "ducts", BuiltInCategory.OST_DuctCurves },
        { "duct", BuiltInCategory.OST_DuctCurves },
        { "ductcurves", BuiltInCategory.OST_DuctCurves },
        { "ductaccessories", BuiltInCategory.OST_DuctAccessory },
        { "ductfittings", BuiltInCategory.OST_DuctFitting },
        { "ductfitting", BuiltInCategory.OST_DuctFitting },
        { "ductterminal", BuiltInCategory.OST_DuctTerminal },
        { "ductterminals", BuiltInCategory.OST_DuctTerminal },
        { "cabletray", BuiltInCategory.OST_CableTray },
        { "cabletrays", BuiltInCategory.OST_CableTray },
        { "cabletrayfitting", BuiltInCategory.OST_CableTrayFitting },
        { "conduit", BuiltInCategory.OST_Conduit },
        { "conduits", BuiltInCategory.OST_Conduit },
        { "conduitfitting", BuiltInCategory.OST_ConduitFitting },
        { "electricalcircuit", BuiltInCategory.OST_ElectricalCircuit },
        { "wire", BuiltInCategory.OST_Wire },
        { "flexpipecurves", BuiltInCategory.OST_FlexPipeCurves },
        { "flexductcurves", BuiltInCategory.OST_FlexDuctCurves },
        { "zoneequipment", BuiltInCategory.OST_ZoneEquipment },
        { "electricalfixtures", BuiltInCategory.OST_ElectricalFixtures },
        { "lightingfixtures", BuiltInCategory.OST_LightingFixtures },
        { "lightingdevices", BuiltInCategory.OST_LightingDevices },
        { "mechanicalequipment", BuiltInCategory.OST_MechanicalEquipment },
        { "mechanicalequipmentset", BuiltInCategory.OST_MechanicalEquipment },
        { "plumbingfixtures", BuiltInCategory.OST_PlumbingFixtures },
        { "firealarmdevices", BuiltInCategory.OST_FireAlarmDevices },
        { "communicationsdevices", BuiltInCategory.OST_CommunicationDevices },
        { "datadevices", BuiltInCategory.OST_DataDevices },
        { "nursecalldevices", BuiltInCategory.OST_NurseCallDevices },
        { "securitydevices", BuiltInCategory.OST_SecurityDevices },
        { "telephonedevices", BuiltInCategory.OST_TelephoneDevices },
        { "sprinklers", BuiltInCategory.OST_Sprinklers },

        // Equipment
        { "electricalequipment", BuiltInCategory.OST_ElectricalEquipment },
        { "foodserviceequipment", BuiltInCategory.OST_FoodServiceEquipment },
        { "medicalequipment", BuiltInCategory.OST_MedicalEquipment },
        { "fireprotection", BuiltInCategory.OST_FireProtection },
        { "signage", BuiltInCategory.OST_Signage },
        { "audiovisualdevices", BuiltInCategory.OST_AudioVisualDevices },
        { "genericequipment", BuiltInCategory.OST_GenericModel },
        { "temporarystructure", BuiltInCategory.OST_GenericModel },
        { "specialequipment", BuiltInCategory.OST_SpecialityEquipment },
        { "specialityequipment", BuiltInCategory.OST_SpecialityEquipment },

        // Structural Foundation
        { "structuralfoundations", BuiltInCategory.OST_StructuralFoundation },
        { "structuralfoundation", BuiltInCategory.OST_StructuralFoundation },
        { "foundation", BuiltInCategory.OST_StructuralFoundation },
      

        // Furniture
        { "furniture", BuiltInCategory.OST_Furniture },
        { "furnituresystems", BuiltInCategory.OST_Furniture },
        { "parking", BuiltInCategory.OST_Parking },
        { "site", BuiltInCategory.OST_Site },
        { "planting", BuiltInCategory.OST_Planting },
        { "entourage", BuiltInCategory.OST_Site },
        { "casework", BuiltInCategory.OST_Casework },

        // Annotation
        { "dimensions", BuiltInCategory.OST_Dimensions },
        { "tags", BuiltInCategory.OST_Tags },

        // Generic fallback
        { "generic", BuiltInCategory.OST_GenericModel },
        { "genericmodel", BuiltInCategory.OST_GenericModel },
        { "genericmodels", BuiltInCategory.OST_GenericModel },

        // Additional common categories
        { "mass", BuiltInCategory.OST_Mass },
        { "masses", BuiltInCategory.OST_Mass },
        { "massfloor", BuiltInCategory.OST_MassFloor },
        { "massfloors", BuiltInCategory.OST_MassFloor },
        { "siteproperty", BuiltInCategory.OST_SiteProperty },
        { "siteproperties", BuiltInCategory.OST_SiteProperty },
        { "projectinformation", BuiltInCategory.OST_ProjectInformation },
        { "projectbasepoint", BuiltInCategory.OST_ProjectBasePoint },
        { "survey point", BuiltInCategory.OST_SpotCoordinates },
        { "spotcoordinates", BuiltInCategory.OST_SpotCoordinates },
        { "spotelevations", BuiltInCategory.OST_SpotElevations },

        // Structural analytical categories (only those available in older Revit versions)
        { "analyticalnodes", BuiltInCategory.OST_AnalyticalNodes },
        { "analyticalnode", BuiltInCategory.OST_AnalyticalNodes },
        { "analyticalpanels", BuiltInCategory.OST_AnalyticalPanel },
        { "analyticalpanel", BuiltInCategory.OST_AnalyticalPanel },
        { "structuralrebar", BuiltInCategory.OST_Rebar },
        { "rebar", BuiltInCategory.OST_Rebar },
        { "fabricreinforcement", BuiltInCategory.OST_FabricAreas },
        { "fabricareas", BuiltInCategory.OST_FabricAreas },

        // Room tags
        { "roomtags", BuiltInCategory.OST_RoomTags },
        { "roomtag", BuiltInCategory.OST_RoomTags },

        // Area tags
        { "areatags", BuiltInCategory.OST_AreaTags },
        { "areatag", BuiltInCategory.OST_AreaTags },

        // Curtain wall system components
        { "curtaingrid", BuiltInCategory.OST_CurtainGrids },
        { "curtaingrids", BuiltInCategory.OST_CurtainGrids },
        { "curtainmullion", BuiltInCategory.OST_CurtainWallMullions },

        // Parts and Assemblies
        { "parts", BuiltInCategory.OST_Parts },
        { "part", BuiltInCategory.OST_Parts },
        { "assemblies", BuiltInCategory.OST_Assemblies },
        { "assembly", BuiltInCategory.OST_Assemblies },

        // Divided surface
        { "dividedsurface", BuiltInCategory.OST_DividedSurface },
        { "dividedsurfaces", BuiltInCategory.OST_DividedSurface },

        // Grid
        { "grids", BuiltInCategory.OST_Grids },
        { "grid", BuiltInCategory.OST_Grids },

        // Levels
        { "levels", BuiltInCategory.OST_Levels },
        { "level", BuiltInCategory.OST_Levels },

        // Family instances (fallback)
        { "familyinstance", BuiltInCategory.OST_GenericModel },
        { "familyinstances", BuiltInCategory.OST_GenericModel },

        // Revit specific model elements
        { "modelelements", BuiltInCategory.OST_ModelText },
        { "modelelement", BuiltInCategory.OST_GenericModel },
    };

    /// <inheritdoc/>
    public ParameterBindingResult BindParameters(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions,
        string groupName = "COBie")
    {
        var result = new ParameterBindingResult();
        var application = document.Application;

        try
        {
            // Open shared parameter file
            var sharedParamFile = application.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                result.Errors.Add("Failed to open shared parameter file.");
                return result;
            }

            // Get the parameter group
            var group = sharedParamFile.Groups.get_Item(groupName);
            if (group == null)
            {
                result.Errors.Add($"Parameter group '{groupName}' not found in shared parameter file.");
                return result;
            }

            // Get binding map
            var bindingMap = document.ParameterBindings;
            var bindingMapIterator = bindingMap.ForwardIterator();

            foreach (var definition in definitions)
            {
                try
                {
                    // Get external definition
                    var externalDef = group.Definitions.get_Item(definition.Name);
                    if (externalDef == null)
                    {
                        result.Errors.Add($"External definition '{definition.Name}' not found in shared parameter file.");
                        result.FailedCount++;
                        continue;
                    }

                    // Check if already bound - use ReInsert for idempotent update
                    bool alreadyBound = bindingMap.Contains(externalDef);
                    Binding? existingBinding = alreadyBound ? bindingMap.get_Item(externalDef) : null;

                    // Determine the parameter group based on definition's GroupBindingId
                    BuiltInParameterGroup paramGroup = GetParameterGroup(definition.GroupBindingId);

                    // Get categories to bind to - prefer category names, fall back to IDs
                    var categoriesToBind = GetCategoriesFromNames(document, definition.CategoryNames);

                    // Log for debugging - include all category info
                    _logger?.Info($"[Binding] Parameter '{definition.Name}'");
                    _logger?.Info($"  CategoryNames: [{string.Join(", ", definition.CategoryNames)}]");
                    _logger?.Info($"  CategoryBindingIds: [{string.Join(", ", definition.CategoryBindingIds)}]");

                    if (categoriesToBind.Count == 0 && definition.CategoryBindingIds.Length > 0)
                    {
                        // Fall back to using IDs if names didn't work
                        _logger?.Info($"[Binding] No categories found by name, trying IDs: {string.Join(", ", definition.CategoryBindingIds)}");
                        categoriesToBind = GetCategoriesFromIds(document, definition.CategoryBindingIds);
                    }

                    if (categoriesToBind.Count == 0)
                    {
                        var categoryNames = definition.CategoryNames.Length > 0
                            ? string.Join(", ", definition.CategoryNames)
                            : "(none)";
                        var categoryIds = definition.CategoryBindingIds.Length > 0
                            ? string.Join(", ", definition.CategoryBindingIds)
                            : "(none)";

                        // Provide helpful diagnostic information
                        var errorMsg = $"No valid categories found for parameter '{definition.Name}'. " +
                                     $"Category Names: {categoryNames}, Category IDs: {categoryIds}. " +
                                     $"Available categories in document: {GetAvailableCategoriesInDocument(document)}";
                        result.Errors.Add(errorMsg);
                        result.FailedCount++;
                        _logger?.Error($"[Binding] {errorMsg}");
                        continue;
                    }

                    _logger?.Info($"[Binding] Parameter '{definition.Name}' will be bound to: {string.Join(", ", categoriesToBind.Select(c => c.Name))}");

                    // Create binding based on parameter type
                    Binding binding;
                    var categorySet = document.Application.Create.NewCategorySet();
                    foreach (var category in categoriesToBind)
                    {
                        categorySet.Insert(category);
                    }

                    if (definition.InstanceTypeAssociation == ParameterType.Instance)
                    {
                        binding = document.Application.Create.NewInstanceBinding(categorySet);
                    }
                    else
                    {
                        binding = document.Application.Create.NewTypeBinding(categorySet);
                    }

                    // Bind the parameter - use ReInsert for idempotent update (per workflow)
                    bool boundSuccess;
                    if (alreadyBound)
                    {
                        // Use ReInsert to update existing parameter binding
                        boundSuccess = bindingMap.ReInsert(externalDef, binding, paramGroup);
                        _logger?.Info($"[Binding] ReInsert for '{definition.Name}': {boundSuccess}");
                    }
                    else
                    {
                        // Use Insert for new parameter
                        boundSuccess = bindingMap.Insert(externalDef, binding, paramGroup);
                        _logger?.Info($"[Binding] Insert for '{definition.Name}': {boundSuccess}");
                    }

                    if (boundSuccess)
                    {
                        result.BoundCount++;
                        result.BoundParameters.Add(new BoundParameterInfo
                        {
                            Name = definition.Name,
                            Categories = categoriesToBind.Select(c => c.Name).ToList(),
                            ParameterType = definition.InstanceTypeAssociation,
                            ParameterElementId = -1
                        });
                    }
                    else
                    {
                        result.Errors.Add($"Failed to bind parameter '{definition.Name}'.");
                        result.FailedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to bind parameter '{definition.Name}': {ex.Message}");
                    result.FailedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to bind parameters: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc/>
    public List<Category> GetParameterCategories(Document document, string parameterName)
    {
        var categories = new List<Category>();
        var bindingMap = document.ParameterBindings;

        var iterator = bindingMap.ForwardIterator();
        while (iterator.MoveNext())
        {
            var definition = iterator.Current as Definition;
            if (definition != null && definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
            {
                var binding = bindingMap.get_Item(definition);
                categories.AddRange(GetCategoriesFromBinding(binding));
                break;
            }
        }

        return categories;
    }

    /// <summary>
    /// Gets Revit categories from APS category IDs.
    /// APS category IDs have format like: "autodesk.revit.category.family:walls-1.0.0"
    /// We extract the category name part (e.g., "walls") and map it to Revit BuiltInCategory.
    /// </summary>
    private List<Category> GetCategoriesFromIds(Document document, string[] categoryIds)
    {
        var categories = new List<Category>();

        foreach (var categoryId in categoryIds)
        {
            // Try direct lookup first (for backward compatibility)
            if (CategoryMap.TryGetValue(categoryId, out var builtInCategory))
            {
                var category = Category.GetCategory(document, builtInCategory);
                if (category != null)
                {
                    categories.Add(category);
                    _logger?.Info($"[Binding] Found category by direct ID lookup: {categoryId}");
                    continue;
                }
            }

            // Parse APS category ID format: "autodesk.revit.category.xxx:CategoryName-1.0.0"
            // Extract the category name between the colon and the version number
            var extractedName = ExtractCategoryNameFromApsId(categoryId);
            if (!string.IsNullOrEmpty(extractedName))
            {
                _logger?.Info($"[Binding] Extracted category name '{extractedName}' from APS ID '{categoryId}'");

                if (CategoryMap.TryGetValue(extractedName, out builtInCategory))
                {
                    var category = Category.GetCategory(document, builtInCategory);
                    if (category != null)
                    {
                        categories.Add(category);
                        _logger?.Info($"[Binding] Found category by extracted name: {extractedName}");
                        continue;
                    }
                }
            }

            _logger?.Warn($"[Binding] Could not map category ID '{categoryId}' to Revit category");
        }

        return categories;
    }

    /// <summary>
    /// Extracts the category name from an APS category ID.
    /// APS format: "autodesk.revit.category.xxx:CategoryName-1.0.0"
    /// Returns: "categoryname" (lowercase)
    /// </summary>
    private string ExtractCategoryNameFromApsId(string apsCategoryId)
    {
        if (string.IsNullOrEmpty(apsCategoryId))
            return string.Empty;

        // Find the colon separator
        var colonIndex = apsCategoryId.IndexOf(':');
        if (colonIndex < 0 || colonIndex >= apsCategoryId.Length - 1)
            return string.Empty;

        // Extract everything after the colon
        var afterColon = apsCategoryId.Substring(colonIndex + 1);

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
    /// Gets Revit categories from APS category names.
    /// This is the preferred method as category names are more stable than IDs.
    /// </summary>
    private List<Category> GetCategoriesFromNames(Document document, string[] categoryNames)
    {
        var categories = new List<Category>();

        foreach (var categoryName in categoryNames)
        {
            // Try to find the category by name in the document first (exact match)
            var category = FindCategoryByName(document, categoryName);
            if (category != null)
            {
                categories.Add(category);
                _logger?.Info($"[Binding] Found category '{categoryName}' by exact name match");
                continue;
            }

            // Fall back to the category map with normalized key
            // Remove spaces and convert to lowercase for map lookup
            var normalizedKey = categoryName.Replace(" ", "").ToLowerInvariant();
            if (CategoryMap.TryGetValue(normalizedKey, out var builtInCategory))
            {
                category = Category.GetCategory(document, builtInCategory);
                if (category != null)
                {
                    categories.Add(category);
                    _logger?.Info($"[Binding] Found category '{categoryName}' via normalized key '{normalizedKey}'");
                    continue;
                }
            }

            // Try partial matching for common categories
            category = FindCategoryByPartialName(document, categoryName);
            if (category != null)
            {
                categories.Add(category);
                _logger?.Info($"[Binding] Found category '{categoryName}' via partial match as '{category.Name}'");
                continue;
            }

            _logger?.Warn($"[Binding] Could not find category for '{categoryName}' (normalized: '{normalizedKey}')");
        }

        return categories;
    }

    /// <summary>
    /// Finds a category in the document by name.
    /// </summary>
    private Category? FindCategoryByName(Document document, string categoryName)
    {
        // Try to match against all categories in the document
        // Use document.Settings.Categories to get all categories
        var categories = document.Settings.Categories;
        foreach (Category category in categories)
        {
            if (category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a category by partial name match (e.g., "Column" matches "Structural Columns")
    /// </summary>
    private Category? FindCategoryByPartialName(Document document, string categoryName)
    {
        var categories = document.Settings.Categories;
        var searchName = categoryName.Replace(" ", "").ToLowerInvariant();

        foreach (Category category in categories)
        {
            var catName = category.Name.Replace(" ", "").ToLowerInvariant();
            // Check if either name contains the other
            if (catName.Contains(searchName) || searchName.Contains(catName))
            {
                return category;
            }
        }

        return null;
    }

    /// <summary>
    /// Lists all available categories in the document for debugging purposes.
    /// </summary>
    public static string GetAvailableCategoriesInDocument(Document document)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Available Categories in Document:");
        var categories = document.Settings.Categories;
        foreach (Category category in categories)
        {
            var bic = category.Id.IntegerValue;
            sb.AppendLine($"  - {category.Name} (ID: {bic})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets all available category mappings for diagnostic purposes.
    /// </summary>
    public static string GetAvailableCategoryMappings()
    {
        var mappings = new System.Text.StringBuilder();
        mappings.AppendLine("Available APS to Revit Category Mappings:");
        foreach (var kvp in CategoryMap)
        {
            mappings.AppendLine($"  '{kvp.Key}' -> {kvp.Value}");
        }
        return mappings.ToString();
    }

    /// <summary>
    /// Extracts categories from a binding.
    /// </summary>
    private List<Category> GetCategoriesFromBinding(Binding binding)
    {
        var categories = new List<Category>();

        if (binding is InstanceBinding instanceBinding)
        {
            foreach (Category category in instanceBinding.Categories)
            {
                categories.Add(category);
            }
        }
        else if (binding is TypeBinding typeBinding)
        {
            foreach (Category category in typeBinding.Categories)
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    /// <summary>
    /// Maps APS group binding ID to Revit BuiltInParameterGroup.
    /// APS format: autodesk.parameter.group:data-1.0.0
    /// </summary>
    private static BuiltInParameterGroup GetParameterGroup(string? apsGroupId)
    {
        if (string.IsNullOrEmpty(apsGroupId))
            return BuiltInParameterGroup.PG_DATA;

        var lowerGroup = apsGroupId.ToLowerInvariant();

        return lowerGroup switch
        {
            // Data group (default for COBie)
            var g when g.Contains("data") => BuiltInParameterGroup.PG_DATA,

            // Identity/Data
            var g when g.Contains("identity") || g.Contains("constraint") => BuiltInParameterGroup.PG_IDENTITY_DATA,

            // Geometry
            var g when g.Contains("dimension") || g.Contains("geometry") => BuiltInParameterGroup.PG_GEOMETRY,

            // Text
            var g when g.Contains("text") || g.Contains("title") || g.Contains("description") => BuiltInParameterGroup.PG_TEXT,

            // Structural
            var g when g.Contains("structural") || g.Contains("physics") => BuiltInParameterGroup.PG_GENERAL,

            // Materials
            var g when g.Contains("material") => BuiltInParameterGroup.PG_MATERIALS,

            // Electrical
            var g when g.Contains("electrical") => BuiltInParameterGroup.PG_ELECTRICAL,

            // Mechanical
            var g when g.Contains("mechanical") || g.Contains("hvac") => BuiltInParameterGroup.PG_MECHANICAL,

            // Plumbing
            var g when g.Contains("plumbing") => BuiltInParameterGroup.PG_PLUMBING,

            // Fire protection
            var g when g.Contains("fire") || g.Contains("security") => BuiltInParameterGroup.PG_FIRE_PROTECTION,

            // Energy
            var g when g.Contains("energy") => BuiltInParameterGroup.PG_ENERGY_ANALYSIS,

            // Default fallback
            _ => BuiltInParameterGroup.PG_DATA
        };
    }
}
