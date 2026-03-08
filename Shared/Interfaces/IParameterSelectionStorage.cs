using COBIeManager.Features.CobieParameters.Models;

namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Storage interface for parameter selection persistence
/// </summary>
public interface IParameterSelectionStorage
{
    /// <summary>
    /// Saves the recent parameters data
    /// </summary>
    void SaveRecentParameters(ParameterUsageData data);

    /// <summary>
    /// Loads the recent parameters data
    /// </summary>
    ParameterUsageData LoadRecentParameters();

    /// <summary>
    /// Exports a snapshot to a file
    /// </summary>
    void ExportSnapshot(string filePath, ParameterSelectionSnapshot snapshot);

    /// <summary>
    /// Imports a snapshot from a file
    /// </summary>
    ParameterSelectionSnapshot ImportSnapshot(string filePath);
}
