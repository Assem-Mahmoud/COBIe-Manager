using Autodesk.Revit.DB;
using Aps.Core.Models;
using COBIeManager.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Services;

/// <summary>
/// Service for detecting and resolving parameter conflicts.
/// </summary>
public class ParameterConflictService : IParameterConflictService
{
    /// <inheritdoc/>
    public List<ParameterConflict> DetectConflicts(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions)
    {
        var conflicts = new List<ParameterConflict>();
        var existingParameters = GetExistingParameters(document);
        var existingParamMap = existingParameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            var conflict = new ParameterConflict
            {
                Definition = definition,
                ConflictType = ParameterConflictType.None,
                SuggestedResolution = ConflictResolution.UseExisting
            };

            // Check if parameter already exists
            if (existingParamMap.TryGetValue(definition.Name, out var existingParam))
            {
                conflict.ExistingParameter = existingParam;

                // Check for type mismatch
                if (existingParam.DataType != definition.DataType)
                {
                    conflict.ConflictType = ParameterConflictType.TypeMismatch;
                    conflict.Description = $"Parameter '{definition.Name}' exists but with different data type. Existing: {existingParam.DataType}, New: {definition.DataType}";
                    conflict.SuggestedResolution = ConflictResolution.Skip;
                }
                // Check for instance/type mismatch
                else if (existingParam.ParameterType != definition.InstanceTypeAssociation)
                {
                    conflict.ConflictType = ParameterConflictType.InstanceTypeMismatch;
                    conflict.Description = $"Parameter '{definition.Name}' exists but as different parameter type. Existing: {existingParam.ParameterType}, New: {definition.InstanceTypeAssociation}";
                    conflict.SuggestedResolution = ConflictResolution.Skip;
                }
                // Check for category mismatch
                else if (!CategoriesMatch(definition.CategoryBindingIds, existingParam.Categories))
                {
                    conflict.ConflictType = ParameterConflictType.CategoryMismatch;
                    conflict.Description = $"Parameter '{definition.Name}' exists but is bound to different categories.";
                    conflict.SuggestedResolution = existingParam.IsShared ? ConflictResolution.Rebind : ConflictResolution.Skip;
                }
                else
                {
                    // Exact match - no conflict
                    conflict.ConflictType = ParameterConflictType.None;
                    conflict.Description = $"Parameter '{definition.Name}' already exists with matching configuration.";
                    conflict.SuggestedResolution = ConflictResolution.UseExisting;
                }
            }
            else
            {
                // No conflict - parameter doesn't exist
                conflict.ConflictType = ParameterConflictType.None;
                conflict.Description = string.Empty;
                conflict.SuggestedResolution = ConflictResolution.UseExisting;
            }

            conflicts.Add(conflict);
        }

        return conflicts;
    }

    /// <inheritdoc/>
    public List<ExistingParameterInfo> GetExistingParameters(Document document)
    {
        var parameters = new List<ExistingParameterInfo>();
        var bindingMap = document.ParameterBindings;

        var iterator = bindingMap.ForwardIterator();
        while (iterator.MoveNext())
        {
            var definition = iterator.Current as Definition;
            if (definition == null) continue;

            var binding = bindingMap.get_Item(definition);
            var categories = GetCategoriesFromBinding(binding);

            // Get parameter type from binding
            ParameterType paramType = binding is InstanceBinding ? ParameterType.Instance : ParameterType.Type;

            // Get data type from definition
            var dataType = GetDataTypeFromDefinition(definition);

            // Check if it's a shared parameter
            bool isShared = false;
            int elementId = -1;

            if (definition is ExternalDefinition externalDef)
            {
                isShared = true;
                // Try to find the parameter element
                var paramIterator = new FilteredElementCollector(document)
                    .OfClass(typeof(ParameterElement))
                    .Cast<ParameterElement>()
                    .FirstOrDefault(p => p.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase));

                if (paramIterator != null)
                {
                    elementId = paramIterator.Id.IntegerValue;
                }
            }

            parameters.Add(new ExistingParameterInfo
            {
                Name = definition.Name,
                DataType = dataType,
                ParameterType = paramType,
                Categories = categories.Select(c => c.Name).ToList(),
                ElementId = elementId,
                IsShared = isShared
            });
        }

        return parameters;
    }

    /// <summary>
    /// Checks if the category IDs match the existing categories.
    /// </summary>
    private bool CategoriesMatch(string[] categoryIds, List<string> existingCategories)
    {
        // For simplicity, just check if there's overlap
        // In a full implementation, you'd map category IDs to category names
        if (categoryIds.Length == 0 || existingCategories.Count == 0)
        {
            return false;
        }

        // Basic check - this would need proper category mapping
        return true;
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
    /// Gets the parameter data type from a Revit definition.
    /// </summary>
    private ParameterDataType GetDataTypeFromDefinition(Definition definition)
    {
        try
        {
            // In Revit API, Definition has a ParameterType property that returns a Revit ParameterType enum
            // We need to use our fully qualified alias to avoid name collision
            var defType = definition.GetDataType();

            // Map ForgeTypeId to our ParameterDataType
            if (defType == SpecTypeId.String.Text)
                return ParameterDataType.Text;
            if (defType == SpecTypeId.Int.Integer)
                return ParameterDataType.Integer;
            // Number type maps to text in this version
            if (defType == SpecTypeId.String.Text)
                return ParameterDataType.Number;
            if (defType == SpecTypeId.Length)
                return ParameterDataType.Length;
            if (defType == SpecTypeId.Area)
                return ParameterDataType.Area;
            if (defType == SpecTypeId.Volume)
                return ParameterDataType.Volume;
            if (defType == SpecTypeId.Angle)
                return ParameterDataType.Angle;
            if (defType == new ForgeTypeId("autodesk.spec:boolean-1.0.0"))
                return ParameterDataType.YesNo;

            return ParameterDataType.Unknown;
        }
        catch
        {
            return ParameterDataType.Unknown;
        }
    }
}
