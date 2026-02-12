using Aps.Core.Models;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Service for creating Shared Parameters in Revit from APS COBie parameter definitions.
/// </summary>
public interface IParameterCreationService
{
    /// <summary>
    /// Creates Shared Parameters in the Revit document from the given COBie parameter definitions.
    /// </summary>
    /// <param name="document">The Revit document to create parameters in</param>
    /// <param name="definitions">The COBie parameter definitions to create</param>
    /// <param name="groupName">The group name in the shared parameter file (default: "COBie")</param>
    /// <returns>Result of the parameter creation operation</returns>
    ParameterCreationResult CreateParameters(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions,
        string groupName = "COBie");

    /// <summary>
    /// Ensures the shared parameter file exists and is configured in Revit.
    /// </summary>
    /// <param name="application">The Revit application</param>
    /// <param name="filePath">The path to the shared parameter file (optional, uses default if not provided)</param>
    /// <returns>True if the shared parameter file is ready to use</returns>
    bool EnsureSharedParameterFile(Application application, string? filePath = null);

    /// <summary>
    /// Gets the path to the shared parameter file.
    /// </summary>
    string SharedParameterFilePath { get; }
}

/// <summary>
/// Result of a parameter creation operation.
/// </summary>
public class ParameterCreationResult
{
    /// <summary>
    /// Number of parameters successfully created
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of parameters that were skipped (already exists)
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of parameters that failed to create
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Details of created parameters
    /// </summary>
    public List<CreatedParameterInfo> CreatedParameters { get; set; } = new();

    /// <summary>
    /// Details of skipped parameters (with reason)
    /// </summary>
    public List<SkippedParameterInfo> SkippedParameters { get; set; } = new();

    /// <summary>
    /// Error messages for failed parameters
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the operation was successful (no errors)
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;
}

/// <summary>
/// Information about a successfully created parameter
/// </summary>
public class CreatedParameterInfo
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter data type
    /// </summary>
    public ParameterDataType DataType { get; set; }

    /// <summary>
    /// Parameter type (Instance or Type)
    /// </summary>
    public ParameterType ParameterType { get; set; }

    /// <summary>
    /// Element ID of the created parameter in the document
    /// </summary>
    public int ParameterElementId { get; set; }
}

/// <summary>
/// Information about a skipped parameter
/// </summary>
public class SkippedParameterInfo
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reason for skipping
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
