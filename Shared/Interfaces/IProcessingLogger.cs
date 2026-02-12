using System;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Logger interface for tracking processing results and statistics
    /// </summary>
    public interface IProcessingLogger
    {
        /// <summary>
        /// Logs a successfully processed element
        /// </summary>
        /// <param name="elementId">ID of the processed element</param>
        /// <param name="category">Element category name</param>
        /// <param name="details">Additional details about the processing</param>
        void LogSuccess(ElementId elementId, string category, string details);

        /// <summary>
        /// Logs an element that was skipped
        /// </summary>
        /// <param name="elementId">ID of the skipped element</param>
        /// <param name="category">Element category name</param>
        /// <param name="skipReason">Reason for skipping</param>
        void LogSkip(ElementId elementId, string category, string skipReason);

        /// <summary>
        /// Logs an error that occurred during processing
        /// </summary>
        /// <param name="elementId">ID of the element (optional)</param>
        /// <param name="category">Element category name (optional)</param>
        /// <param name="error">Error message</param>
        /// <param name="exception">Exception (optional)</param>
        void LogError(ElementId? elementId, string? category, string error, Exception? exception = null);

        /// <summary>
        /// Gets the processing summary statistics
        /// </summary>
        /// <returns>Current processing summary</returns>
        ProcessingSummary GetSummary();

        /// <summary>
        /// Resets all statistics and logs
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the total number of elements processed
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Gets the number of elements that were successfully processed
        /// </summary>
        int SuccessCount { get; }

        /// <summary>
        /// Gets the number of elements that were skipped
        /// </summary>
        int SkipCount { get; }

        /// <summary>
        /// Gets the number of elements that had errors
        /// </summary>
        int ErrorCount { get; }
    }
}