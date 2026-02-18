using System;
using Autodesk.Revit.DB;
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
        /// <param name="document">Revit document</param>
        /// <param name="parameterName">Parameter name to fill</param>
        /// <param name="overwriteExisting">Whether to overwrite existing values</param>
        /// <returns>Preview summary with estimated counts</returns>
        BoxIdFillPreviewSummary PreviewFill(Document document, string parameterName, bool overwriteExisting);

        /// <summary>
        /// Executes box ID fill operation
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="parameterName">Parameter name to fill</param>
        /// <param name="overwriteExisting">Whether to overwrite existing values</param>
        /// <param name="includeGroupElement">Whether to also fill the group element itself</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Processing summary with actual results</returns>
        BoxIdFillSummary ExecuteFill(
            Document document,
            string parameterName,
            bool overwriteExisting,
            bool includeGroupElement,
            Action<int, string> progressAction = null);
    }
}
