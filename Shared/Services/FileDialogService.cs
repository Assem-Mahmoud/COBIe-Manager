using System;
using System.Windows;
using Microsoft.Win32;
using COBIeManager.Shared.Interfaces;

namespace COBIeManager.Shared.Services;

/// <summary>
/// WPF-based file dialog service
/// </summary>
public class FileDialogService : IFileDialogService
{
    /// <summary>
    /// Shows a save file dialog
    /// </summary>
    public string? ShowSaveFileDialog(string defaultFileName, string filter)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = "json"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        catch (Exception)
        {
            // Fallback for cases where WPF dialogs might not work (e.g., in Revit context)
            return null;
        }
    }

    /// <summary>
    /// Shows an open file dialog
    /// </summary>
    public string? ShowOpenFileDialog(string filter)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                DefaultExt = "json"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        catch (Exception)
        {
            // Fallback for cases where WPF dialogs might not work (e.g., in Revit context)
            return null;
        }
    }
}
