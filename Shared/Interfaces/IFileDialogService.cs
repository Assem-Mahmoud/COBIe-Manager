namespace COBIeManager.Shared.Interfaces;

/// <summary>
/// Service for showing file dialogs (abstracts WPF from ViewModels)
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows a save file dialog
    /// </summary>
    /// <param name="defaultFileName">Default filename</param>
    /// <param name="filter">File filter (e.g., "JSON Files|*.json|All Files|*.*")</param>
    /// <returns>Selected file path or null if canceled</returns>
    string? ShowSaveFileDialog(string defaultFileName, string filter);

    /// <summary>
    /// Shows an open file dialog
    /// </summary>
    /// <param name="filter">File filter (e.g., "JSON Files|*.json|All Files|*.*")</param>
    /// <returns>Selected file path or null if canceled</returns>
    string? ShowOpenFileDialog(string filter);
}
