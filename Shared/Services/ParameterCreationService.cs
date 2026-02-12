using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Aps.Core.Models;
using COBIeManager.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COBIeManager.Shared.Services;

/// <summary>
/// Service for creating Shared Parameters in Revit from APS COBie parameter definitions.
/// </summary>
public class ParameterCreationService : IParameterCreationService
{
    private const string DefaultSharedParameterFileName = "COBIeParameters.txt";
    private readonly string _sharedParameterFilePath;

    /// <summary>
    /// Initializes a new instance of the ParameterCreationService.
    /// </summary>
    public ParameterCreationService()
    {
        // Default shared parameter file location in AppData
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cobieManagerPath = Path.Combine(appDataPath, "COBIeManager");

        if (!Directory.Exists(cobieManagerPath))
        {
            Directory.CreateDirectory(cobieManagerPath);
        }

        _sharedParameterFilePath = Path.Combine(cobieManagerPath, DefaultSharedParameterFileName);
    }

    /// <inheritdoc/>
    public string SharedParameterFilePath => _sharedParameterFilePath;

    /// <inheritdoc/>
    public bool EnsureSharedParameterFile(Application application, string? filePath = null)
    {
        var pathToUse = filePath ?? _sharedParameterFilePath;

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(pathToUse);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create file if it doesn't exist
            if (!File.Exists(pathToUse))
            {
                File.WriteAllText(pathToUse, "# COBIe Manager Shared Parameters File");
            }

            // Set the shared parameter file in Revit if not already set
            var currentSharedParamFile = application.SharedParametersFilename;
            if (string.IsNullOrEmpty(currentSharedParamFile) ||
                !Path.GetFullPath(currentSharedParamFile).Equals(Path.GetFullPath(pathToUse), StringComparison.OrdinalIgnoreCase))
            {
                application.SharedParametersFilename = pathToUse;
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to ensure shared parameter file at {pathToUse}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public ParameterCreationResult CreateParameters(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions,
        string groupName = "COBie")
    {
        var result = new ParameterCreationResult();
        var application = document.Application;

        try
        {
            // Ensure shared parameter file is configured
            if (!EnsureSharedParameterFile(application))
            {
                result.Errors.Add("Failed to configure shared parameter file.");
                return result;
            }

            // Open the shared parameter file
            var sharedParamFile = application.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                result.Errors.Add("Failed to open shared parameter file.");
                return result;
            }

            // Get or create the parameter group
            var group = sharedParamFile.Groups.get_Item(groupName);
            if (group == null)
            {
                group = sharedParamFile.Groups.Create(groupName);
            }

            // Get the shared parameters in the document
            var existingParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paramIterator = new FilteredElementCollector(document)
                .OfClass(typeof(ParameterElement))
                .Cast<ParameterElement>();

            foreach (var paramElem in paramIterator)
            {
                existingParamNames.Add(paramElem.Name);
            }

            // Process each definition
            foreach (var definition in definitions)
            {
                try
                {
                    // Check if parameter already exists in document
                    if (existingParamNames.Contains(definition.Name))
                    {
                        result.SkippedCount++;
                        result.SkippedParameters.Add(new SkippedParameterInfo
                        {
                            Name = definition.Name,
                            Reason = "Parameter already exists in document"
                        });
                        continue;
                    }

                    // Check if external definition already exists in shared parameter file
                    var externalDef = group.Definitions.get_Item(definition.Name);
                    if (externalDef == null)
                    {
                        // Create new external definition
                        var specTypeId = GetSpecTypeId(definition.DataType);

                        var options = new ExternalDefinitionCreationOptions(definition.Name, specTypeId)
                        {
                            Description = definition.Description ?? string.Empty,
                            UserModifiable = true,
                            Visible = !definition.IsHidden
                        };

                        try
                        {
                            externalDef = group.Definitions.Create(options) as ExternalDefinition;
                        }
                        catch (ArgumentException)
                        {
                            // Handle race condition where definition was created by another process/thread
                            // or if the same parameter appears twice in the input list
                            externalDef = group.Definitions.get_Item(definition.Name) as ExternalDefinition;
                        }

                        if (externalDef == null)
                        {
                            result.Errors.Add($"Failed to create external definition for '{definition.Name}'.");
                            result.FailedCount++;
                            continue;
                        }
                    }

                    // Successfully created external definition
                    // The actual parameter in the document will be created during binding
                    result.CreatedCount++;
                    result.CreatedParameters.Add(new CreatedParameterInfo
                    {
                        Name = definition.Name,
                        DataType = definition.DataType,
                        ParameterType = definition.InstanceTypeAssociation,
                        ParameterElementId = -1 // Will be set during binding
                    });

                    existingParamNames.Add(definition.Name);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create parameter '{definition.Name}': {ex.Message}");
                    result.FailedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to create parameters: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Maps APS ParameterDataType to Revit ForgeTypeId
    /// </summary>
    private ForgeTypeId GetSpecTypeId(ParameterDataType dataType)
    {
        return dataType switch
        {
            ParameterDataType.Text => SpecTypeId.String.Text,
            ParameterDataType.Integer => SpecTypeId.Int.Integer,
            ParameterDataType.Number => SpecTypeId.String.Text, // Use text for numbers
            ParameterDataType.Length => SpecTypeId.Length,
            ParameterDataType.Area => SpecTypeId.Area,
            ParameterDataType.Volume => SpecTypeId.Volume,
            ParameterDataType.Angle => SpecTypeId.Angle,
            ParameterDataType.FamilyType => SpecTypeId.String.Text, // Use text for FamilyType
            ParameterDataType.YesNo => new ForgeTypeId("autodesk.spec:boolean-1.0.0"),
            ParameterDataType.MultiValue => SpecTypeId.String.Text, // Fallback to text
            _ => SpecTypeId.String.Text // Fallback for unknown types
        };
    }
}
