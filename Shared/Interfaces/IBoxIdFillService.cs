using System;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for filling ACG-BOX-ID parameters from Model Group names
    /// </summary>
    public interface IBoxIdFillService
    {
        /// <summary>
        /// Analyzes groups and returns preview summary without modifying document
        /// </summary>
        /// <param name="document">Revit document (host/target document)</param>
        /// <param name="config">Fill configuration containing linked document selection</param>
        /// <returns>Preview summary with estimated counts</returns>
        BoxIdFillPreviewSummary PreviewFill(Document document, FillConfiguration config);

        /// <summary>
        /// Executes box ID fill operation
        /// </summary>
        /// <param name="document">Revit document (host/target document)</param>
        /// <param name="config">Fill configuration containing linked document selection</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Processing summary with actual results</returns>
        BoxIdFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);
    }
}
