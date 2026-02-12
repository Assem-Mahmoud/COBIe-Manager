using Aps.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// UI wrapper for CobieParameterDefinition with selection state
/// </summary>
public partial class SelectableParameter : ObservableObject
{
    private CobieParameterDefinition? _parameter;

    /// <summary>
    /// The underlying parameter definition from APS
    /// </summary>
    public CobieParameterDefinition Parameter
    {
        get => _parameter;
        set
        {
            _parameter = value;
            // Notify that all computed properties have changed
            OnPropertyChanged(nameof(CategoryCount));
            OnPropertyChanged(nameof(DataTypeDisplay));
            OnPropertyChanged(nameof(GroupDisplay));
        }
    }

    /// <summary>
    /// UI selection state (not serialized)
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    // Expose properties for easier binding with safe null handling
    public string Id => _parameter?.Id ?? string.Empty;
    public string Name => _parameter?.Name ?? string.Empty;
    public string? Description => _parameter?.Description;
    public string DataTypeId => _parameter?.DataTypeId ?? string.Empty;
    public ParameterDataType DataType => _parameter?.DataType ?? ParameterDataType.Unknown;
    public ParameterType InstanceTypeAssociation => _parameter?.InstanceTypeAssociation ?? ParameterType.Instance;
    public string[] CategoryBindingIds => _parameter?.CategoryBindingIds ?? System.Array.Empty<string>();
    public string[] CategoryNames => _parameter?.CategoryNames ?? System.Array.Empty<string>();
    public string[] Labels => _parameter?.Labels ?? System.Array.Empty<string>();
    public bool IsHidden => _parameter?.IsHidden ?? false;
    public bool IsArchived => _parameter?.IsArchived ?? false;
    public string? GroupBindingId => _parameter?.GroupBindingId;

    /// <summary>
    /// Number of categories this parameter is bound to
    /// </summary>
    public int CategoryCount => _parameter?.CategoryBindingIds?.Length ?? 0;

    /// <summary>
    /// Display name for the data type
    /// </summary>
    public string DataTypeDisplay => _parameter?.DataType switch
    {
        ParameterDataType.Text => "Text",
        ParameterDataType.Integer => "Integer",
        ParameterDataType.Number => "Number",
        ParameterDataType.Length => "Length",
        ParameterDataType.Area => "Area",
        ParameterDataType.Volume => "Volume",
        ParameterDataType.Angle => "Angle",
        ParameterDataType.FamilyType => "Family Type",
        ParameterDataType.YesNo => "Yes/No",
        ParameterDataType.MultiValue => "Multi-Value",
        _ => "Unknown"
    };

    /// <summary>
    /// Display name for the parameter group
    /// </summary>
    public string? GroupDisplay => GroupBindingId;

    /// <summary>
    /// Formatted category count display (e.g., "5 categories")
    /// </summary>
    public string CategoryCountDisplay => CategoryCount > 0 ? $"{CategoryCount} categories" : "0 categories";
}
