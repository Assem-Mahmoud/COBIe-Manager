using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aps.Core.Models;

/// <summary>
/// Represents a label from the APS Parameters API.
/// Labels are used to categorize and group parameters.
/// </summary>
public class ApsLabel : INotifyPropertyChanged
{
    private bool _isSelected;

    /// <summary>
    /// Unique identifier for the label
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the label
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the label
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Account ID that this label belongs to
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// The color assigned to the label (for UI display)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Whether this label is selected for filtering
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Override ToString for better display in UI
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
