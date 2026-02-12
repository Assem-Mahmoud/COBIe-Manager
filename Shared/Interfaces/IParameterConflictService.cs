using Aps.Core.Models;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Service for detecting and resolving parameter conflicts.
/// </summary>
public interface IParameterConflictService
{
    /// <summary>
    /// Detects conflicts for the given parameter definitions.
    /// </summary>
    /// <param name="document">The Revit document</param>
    /// <param name="definitions">The parameter definitions to check</param>
    /// <returns>List of detected conflicts</returns>
    List<ParameterConflict> DetectConflicts(
        Document document,
        IEnumerable<CobieParameterDefinition> definitions);

    /// <summary>
    /// Gets all shared parameters in the document.
    /// </summary>
    /// <param name="document">The Revit document</param>
    /// <returns>List of existing shared parameters</returns>
    List<ExistingParameterInfo> GetExistingParameters(Document document);
}

/// <summary>
/// Represents a parameter conflict.
/// </summary>
public class ParameterConflict
{
    /// <summary>
    /// The parameter definition from APS
    /// </summary>
    public CobieParameterDefinition Definition { get; set; } = null!;

    /// <summary>
    /// The existing parameter in Revit (if any)
    /// </summary>
    public ExistingParameterInfo? ExistingParameter { get; set; }

    /// <summary>
    /// Type of conflict
    /// </summary>
    public ParameterConflictType ConflictType { get; set; }

    /// <summary>
    /// Description of the conflict
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Suggested resolution
    /// </summary>
    public ConflictResolution SuggestedResolution { get; set; }
}

/// <summary>
/// Type of parameter conflict.
/// </summary>
public enum ParameterConflictType
{
    /// <summary>
    /// Parameter with same name already exists
    /// </summary>
    DuplicateName,

    /// <summary>
    /// Parameter exists but with different data type
    /// </summary>
    TypeMismatch,

    /// <summary>
    /// Parameter exists but is bound to different categories
    /// </summary>
    CategoryMismatch,

    /// <summary>
    /// Parameter exists but is a different type (instance vs type)
    /// </summary>
    InstanceTypeMismatch,

    /// <summary>
    /// No conflict
    /// </summary>
    None
}

/// <summary>
/// Suggested resolution for a conflict.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Skip this parameter (don't create)
    /// </summary>
    Skip,

    /// <summary>
    /// Use existing parameter as-is
    /// </summary>
    UseExisting,

    /// <summary>
    /// Rebind existing parameter to new categories
    /// </summary>
    Rebind,

    /// <summary>
    /// Create parameter with a suffix (e.g., "ParameterName (2)")
    /// </summary>
    CreateWithSuffix
}

/// <summary>
/// Information about an existing parameter in Revit.
/// </summary>
public class ExistingParameterInfo
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
    /// Categories the parameter is bound to
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Element ID of the parameter
    /// </summary>
    public int ElementId { get; set; }

    /// <summary>
    /// Whether this is a shared parameter
    /// </summary>
    public bool IsShared { get; set; }
}
