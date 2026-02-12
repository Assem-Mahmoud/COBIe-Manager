using Aps.Core.Models;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Service for binding Shared Parameters to Revit categories.
/// </summary>
public interface IParameterBindingService
{
    /// <summary>
    /// Binds Shared Parameters to the specified Revit categories.
    /// </summary>
    /// <param name="document">The Revit document</param>
    /// <param name="definitions">The COBie parameter definitions to bind</param>
    /// <param name="groupName">The group name in the shared parameter file</param>
    /// <returns>Result of the binding operation</returns>
    ParameterBindingResult BindParameters(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions,
        string groupName = "COBie");

    /// <summary>
    /// Gets all categories that a parameter is bound to in the document.
    /// </summary>
    /// <param name="document">The Revit document</param>
    /// <param name="parameterName">The parameter name</param>
    /// <returns>List of categories the parameter is bound to</returns>
    List<Category> GetParameterCategories(Document document, string parameterName);
}

/// <summary>
/// Result of a parameter binding operation.
/// </summary>
public class ParameterBindingResult
{
    /// <summary>
    /// Number of parameters successfully bound
    /// </summary>
    public int BoundCount { get; set; }

    /// <summary>
    /// Number of parameters that failed to bind
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Details of bound parameters
    /// </summary>
    public List<BoundParameterInfo> BoundParameters { get; set; } = new();

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;
}

/// <summary>
/// Information about a successfully bound parameter
/// </summary>
public class BoundParameterInfo
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Categories the parameter was bound to
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Parameter type (Instance or Type)
    /// </summary>
    public ParameterType ParameterType { get; set; }

    /// <summary>
    /// Element ID of the parameter in the document
    /// </summary>
    public int ParameterElementId { get; set; }
}
