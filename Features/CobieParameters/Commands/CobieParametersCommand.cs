using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;

namespace COBIeManager.Features.CobieParameters.Commands;

/// <summary>
/// External Command for COBie Parameter Management
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CobieParametersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // MUST be called first for async support
        RevitTask.Initialize(commandData.Application);

        var uiDoc = commandData.Application.ActiveUIDocument;
        var task = ShowCobieParametersWindowAsync(uiDoc);
        task.Wait();

        return Result.Succeeded;
    }

    private async System.Threading.Tasks.Task ShowCobieParametersWindowAsync(UIDocument uiDoc)
    {
        var window = new Views.CobieParametersWindow();
        // Set the UI document on the window's existing ViewModel
        window.SetUiDocument(uiDoc);
        window.ShowDialog();
    }
}
