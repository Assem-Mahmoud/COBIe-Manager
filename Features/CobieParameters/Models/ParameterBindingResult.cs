using Autodesk.Revit.DB;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Represents the outcome of a parameter creation operation
/// </summary>
public class ParameterBindingResult
{
    /// <summary>
    /// The parameter being created
    /// </summary>
    public CobieParameterDefinition ParameterDefinition { get; set; } = null!;

    /// <summary>
    /// Success, Skipped, or Failed
    /// </summary>
    public BindingStatus Status { get; set; }

    /// <summary>
    /// Reason for skipping (if applicable)
    /// </summary>
    public string SkipReason { get; set; } = string.Empty;

    /// <summary>
    /// Error details (if failed)
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Revit ElementId of created parameter
    /// </summary>
    public ElementId? CreatedParameterId { get; set; }
}
