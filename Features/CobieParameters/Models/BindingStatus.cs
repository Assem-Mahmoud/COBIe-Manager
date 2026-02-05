namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Status of parameter binding operation
/// </summary>
public enum BindingStatus
{
    /// <summary>
    /// Parameter was created successfully
    /// </summary>
    Success,

    /// <summary>
    /// Parameter was skipped (duplicate, etc.)
    /// </summary>
    Skipped,

    /// <summary>
    /// Parameter creation failed
    /// </summary>
    Failed
}
