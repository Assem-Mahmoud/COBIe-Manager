using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for filling Revit parameters with level and room information
    /// </summary>
    public interface IParameterFillService
    {
        /// <summary>
        /// Analyzes elements and returns preview summary without modifying document
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <returns>Preview summary with estimated counts</returns>
        PreviewSummary PreviewFill(Document document, FillConfiguration config);

        /// <summary>
        /// Executes parameter fill operation with progress reporting
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="config">Fill configuration</param>
        /// <param name="progressAction">Progress callback with current count and message</param>
        /// <returns>Processing summary with actual results</returns>
        ProcessingSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);
    }
}
