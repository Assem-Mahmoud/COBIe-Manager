using Autodesk.Revit.DB;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for handling and suppressing Revit warnings during transactions.
    /// Implements IFailuresPreprocessor to intercept warnings before they're shown to the user.
    /// </summary>
    public interface IWarningSuppressionService
    {
        /// <summary>
        /// Enables warning suppression for the specified transaction.
        /// Warnings will be intercepted and handled according to configured rules.
        /// </summary>
        /// <param name="transaction">The transaction to enable warning suppression for</param>
        void EnableWarningSuppressionForTransaction(Transaction transaction);

        /// <summary>
        /// Configures which specific warning types should be suppressed.
        /// If not called, default suppressions are used.
        /// </summary>
        /// <param name="warnings">Array of FailureDefinitionId representing warnings to suppress</param>
        void ConfigureSuppressions(params FailureDefinitionId[] warnings);

        /// <summary>
        /// Gets statistics about warnings handled in the last transaction.
        /// </summary>
        /// <returns>String containing warning statistics</returns>
        string GetLastWarningStatistics();
    }
}
