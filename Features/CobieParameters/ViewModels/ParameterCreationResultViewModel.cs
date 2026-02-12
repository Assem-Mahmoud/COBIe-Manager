using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace COBIeManager.Features.CobieParameters.ViewModels;

/// <summary>
/// ViewModel for the parameter creation result dialog.
/// </summary>
public partial class ParameterCreationResultViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Parameter Creation Results";

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private bool _hasWarnings;

    // Count properties for UI binding
    private int _createdCount;
    private int _boundCount;
    private int _skippedCount;

    public int CreatedCount
    {
        get => _createdCount;
        set
        {
            _createdCount = value;
            OnPropertyChanged();
        }
    }

    public int BoundCount
    {
        get => _boundCount;
        set
        {
            _boundCount = value;
            OnPropertyChanged();
        }
    }

    public int SkippedCount
    {
        get => _skippedCount;
        set
        {
            _skippedCount = value;
            OnPropertyChanged();
        }
    }

    public bool HasNoErrors => Errors.Count == 0;

    public ObservableCollection<ParameterResultItem> CreatedParameters { get; }
    public ObservableCollection<ParameterResultItem> SkippedParameters { get; }
    public ObservableCollection<ParameterResultItem> BoundParameters { get; }
    public ObservableCollection<string> Errors { get; }

    public ParameterCreationResultViewModel()
    {
        CreatedParameters = new ObservableCollection<ParameterResultItem>();
        SkippedParameters = new ObservableCollection<ParameterResultItem>();
        BoundParameters = new ObservableCollection<ParameterResultItem>();
        Errors = new ObservableCollection<string>();
    }

    /// <summary>
    /// Sets the results from a parameter creation operation.
    /// </summary>
    public void SetResults(
        int createdCount,
        int skippedCount,
        int errorCount,
        System.Collections.Generic.IEnumerable<COBIeManager.Shared.Interfaces.CreatedParameterInfo>? created = null,
        System.Collections.Generic.IEnumerable<COBIeManager.Shared.Interfaces.SkippedParameterInfo>? skipped = null,
        System.Collections.Generic.IEnumerable<string>? errors = null,
        System.Collections.Generic.IEnumerable<COBIeManager.Shared.Interfaces.BoundParameterInfo>? bound = null)
    {
        // Set count properties
        CreatedCount = createdCount;
        SkippedCount = skippedCount;

        // Track bound count separately
        var boundCount = 0;

        CreatedParameters.Clear();
        SkippedParameters.Clear();
        BoundParameters.Clear();
        Errors.Clear();

        if (created != null)
        {
            foreach (var param in created)
            {
                CreatedParameters.Add(new ParameterResultItem
                {
                    Name = param.Name,
                    Detail = $"{param.DataType} - {param.ParameterType}"
                });
            }
        }

        if (skipped != null)
        {
            foreach (var param in skipped)
            {
                SkippedParameters.Add(new ParameterResultItem
                {
                    Name = param.Name,
                    Detail = param.Reason
                });
            }
        }

        if (bound != null)
        {
            foreach (var param in bound)
            {
                boundCount++;
                var categoriesStr = param.Categories.Count > 0
                    ? string.Join(", ", param.Categories)
                    : "No categories";
                BoundParameters.Add(new ParameterResultItem
                {
                    Name = param.Name,
                    Detail = $"{param.ParameterType} - Categories: {categoriesStr}"
                });
            }
        }

        // Update BoundCount
        BoundCount = boundCount;

        if (errors != null)
        {
            foreach (var error in errors)
            {
                Errors.Add(error);
            }
        }

        // Use actual error count from the errors list
        var actualErrorCount = Errors.Count;

        // Build summary
        var summaryParts = new System.Text.StringBuilder();
        summaryParts.Append($"Created: {createdCount} parameters");
        if (skippedCount > 0)
        {
            summaryParts.Append($" | Skipped: {skippedCount} parameters");
            HasWarnings = true;
        }
        if (actualErrorCount > 0)
        {
            summaryParts.Append($" | Errors: {actualErrorCount}");
            HasErrors = true;
        }

        if (actualErrorCount == 0 && skippedCount == 0 && createdCount > 0)
        {
            summaryParts.Append(" - All parameters created successfully!");
        }
        else if (actualErrorCount > 0)
        {
            summaryParts.Append(" - Please check the Errors tab for details.");
        }

        Summary = summaryParts.ToString();
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    public void Close()
    {
        // This will be handled by the view's Close button
    }
}

/// <summary>
/// Represents a parameter result item for display.
/// </summary>
public class ParameterResultItem
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Additional detail (type, reason, etc.)
    /// </summary>
    public string Detail { get; set; } = string.Empty;
}
