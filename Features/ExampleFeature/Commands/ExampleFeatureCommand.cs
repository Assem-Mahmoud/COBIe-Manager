using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Revit.Async;
using System;

namespace COBIeManager.Features.ExampleFeature.Commands
{
    /// <summary>
    /// Example External Command - Template for new features
    ///
    /// To use this template:
    /// 1. Rename "ExampleFeature" to your feature name
    /// 2. Update the namespace throughout
    /// 3. Implement your feature logic in the Execute method
    /// 4. Add a ribbon button in App.cs that points to this command
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ExampleFeatureCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // NOTE: RevitTask.Initialize requires external command/application data.
            // Uncomment and pass the correct arguments when using async operations:
            // RevitTask.Initialize(commandData.Application);

            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // TODO: Implement your feature logic here
                //
                // Example patterns:
                // 1. Show a WPF window:
                //    var window = new ExampleFeatureWindow();
                //    window.ShowDialog();
                //
                // 2. Modify Revit document:
                //    using (var t = new Transaction(doc, "Example Operation"))
                //    {
                //        t.Start();
                //        // Your code here
                //        t.Commit();
                //    }
                //
                // 3. Use async/await:
                //    RevitTask.Initialize(commandData.Application);
                //    var task = ExecuteAsync(uiDoc);
                //    task.Wait();

                TaskDialog.Show("Example Feature", "Feature executed successfully!");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                return Result.Failed;
            }
        }

        // Example async method pattern
        //private async System.Threading.Tasks.Task ExecuteAsync(UIDocument uiDoc)
        //{
        //    Document doc = uiDoc.Document;
        //
        //    // Run long operations off the Revit thread
        //    await System.Threading.Tasks.Task.Run(() =>
        //    {
        //        // Heavy computation here
        //    });
        //
        //    // Modify Revit document on main thread
        //    using (var t = new Transaction(doc, "Async Operation"))
        //    {
        //        t.Start();
        //        // Your code here
        //        t.Commit();
        //    }
        //}
    }
}
