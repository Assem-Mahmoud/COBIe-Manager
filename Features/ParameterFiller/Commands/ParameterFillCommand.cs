using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using System;
using System.IO;
using System.Reflection;

namespace COBIeManager.Features.ParameterFiller.Commands
{
    /// <summary>
    /// External Command for Auto-Fill Parameters
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ParameterFillCommand : IExternalCommand
    {
        private static bool _assemblyResolverInstalled = false;
        private static readonly object _lock = new object();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // MUST be called first for async support
            RevitTask.Initialize(commandData.Application);

            // Install assembly resolver to help find MaterialDesignThemes and other DLLs
            EnsureAssemblyResolverInstalled();

            var uiDoc = commandData.Application.ActiveUIDocument;
            var task = ShowParameterFillWindowAsync(uiDoc);
            task.Wait();

            return Result.Succeeded;
        }

        private async System.Threading.Tasks.Task ShowParameterFillWindowAsync(UIDocument uiDoc)
        {
            var window = new Views.ParameterFillWindow(uiDoc);
            window.ShowDialog();
        }

        private void EnsureAssemblyResolverInstalled()
        {
            if (_assemblyResolverInstalled)
                return;

            lock (_lock)
            {
                if (_assemblyResolverInstalled)
                    return;

                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    try
                    {
                        string assemblyName = new AssemblyName(args.Name).Name;

                        // Look for the assembly in the same folder as this plugin
                        string assemblyPath = Path.Combine(assemblyFolder, assemblyName + ".dll");

                        if (File.Exists(assemblyPath))
                        {
                            return Assembly.LoadFrom(assemblyPath);
                        }
                    }
                    catch
                    {
                        // Ignore resolution errors
                    }
                    return null;
                };

                _assemblyResolverInstalled = true;
            }
        }
    }
}
