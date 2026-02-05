using Autodesk.Revit.DB;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Represents a single Revit category mapping
/// </summary>
public class RevitCategory
{
    /// <summary>
    /// APS category identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Revit BuiltInCategory enum value
    /// </summary>
    public BuiltInCategory BuiltInCategory { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter type (Instance or Type)
    /// </summary>
    public ParameterType ParameterType { get; set; }
}
