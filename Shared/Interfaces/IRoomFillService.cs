using System;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for filling room parameters based on room ownership/association
    /// </summary>
    public interface IRoomFillService
    {
        /// <summary>
        /// Analyzes elements and returns preview summary without modifying document
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration containing selected categories and parameters</param>
        /// <returns>Preview summary with estimated counts</returns>
        RoomFillPreviewSummary PreviewFill(Document document, FillConfiguration config);

        /// <summary>
        /// Executes room parameter fill operation
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Processing summary with actual results</returns>
        RoomFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);
    }
}
