using Autodesk.Revit.DB;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Handles Revit warnings during transactions by suppressing or resolving them.
    /// Implements IFailuresPreprocessor to intercept failures before they're displayed.
    /// </summary>
    public class WarningSuppressionService : IWarningSuppressionService, IFailuresPreprocessor
    {
        private readonly ILogger _logger;
        private readonly HashSet<FailureDefinitionId> _suppressedWarnings;
        private int _lastSuppressedCount;
        private int _lastResolvedCount;
        private int _lastErrorCount;

        public WarningSuppressionService(ILogger logger = null)
        {
            _logger = logger ?? new FileLogger();
            _suppressedWarnings = new HashSet<FailureDefinitionId>();
            _lastSuppressedCount = 0;
            _lastResolvedCount = 0;
            _lastErrorCount = 0;

            // Configure default suppressions
            ConfigureDefaultSuppressions();
            _logger?.Info("WarningSuppressionService initialized with default suppressions");
        }

        /// <summary>
        /// Configures default warnings that should be suppressed.
        /// Called automatically during construction.
        /// </summary>
        private void ConfigureDefaultSuppressions()
        {
            // Suppress common warnings that occur during wall merging and door placement
            _suppressedWarnings.Add(BuiltInFailures.OverlapFailures.WallsOverlap);
            _suppressedWarnings.Add(BuiltInFailures.JoinElementsFailures.CannotJoinElementsError);

            // Note: "Wall slightly off axis" warnings are handled automatically
            // as part of general warning suppression for placement operations
        }

        public void ConfigureSuppressions(params FailureDefinitionId[] warnings)
        {
            if (warnings == null || warnings.Length == 0)
            {
                _logger?.Warn("ConfigureSuppressions called with no warnings");
                return;
            }

            _suppressedWarnings.Clear();
            foreach (var warning in warnings)
            {
                _suppressedWarnings.Add(warning);
            }

            _logger?.Info($"Configured {warnings.Length} custom warning suppressions");
        }

        public void EnableWarningSuppressionForTransaction(Transaction transaction)
        {
            if (transaction == null)
            {
                _logger?.Error("Cannot enable warning suppression: transaction is null");
                return;
            }

            try
            {
                // Reset statistics for new transaction
                _lastSuppressedCount = 0;
                _lastResolvedCount = 0;
                _lastErrorCount = 0;

                // Get current failure handling options
                var options = transaction.GetFailureHandlingOptions();

                // Set this service as the failures preprocessor
                options.SetFailuresPreprocessor(this);

                // Apply the options back to the transaction
                transaction.SetFailureHandlingOptions(options);

                _logger?.Debug($"Warning suppression enabled for transaction: '{transaction.GetName()}'");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to enable warning suppression: {ex.Message}", ex);
            }
        }

        public string GetLastWarningStatistics()
        {
            if (_lastSuppressedCount == 0 && _lastResolvedCount == 0 && _lastErrorCount == 0)
            {
                return "No warnings encountered";
            }

            return $"Suppressed: {_lastSuppressedCount}, Resolved: {_lastResolvedCount}, Errors: {_lastErrorCount}";
        }

        /// <summary>
        /// IFailuresPreprocessor implementation - called by Revit when failures occur.
        /// </summary>
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            try
            {
                if (failuresAccessor == null)
                {
                    _logger?.Error("FailuresAccessor is null in PreprocessFailures");
                    return FailureProcessingResult.Continue;
                }

                // Get all failure messages
                var failures = failuresAccessor.GetFailureMessages();

                if (failures.Count == 0)
                {
                    return FailureProcessingResult.Continue;
                }

                _logger?.Debug($"Processing {failures.Count} failure(s)");

                foreach (FailureMessageAccessor failure in failures)
                {
                    try
                    {
                        var severity = failure.GetSeverity();
                        var failureId = failure.GetFailureDefinitionId();
                        var failureText = failure.GetDescriptionText();

                        _logger?.Debug($"Failure: {failureText} | Severity: {severity} | ID: {failureId?.Guid}");

                        // Handle based on severity
                        if (severity == FailureSeverity.Warning)
                        {
                            // Check if this warning should be suppressed
                            if (failureId != null && _suppressedWarnings.Contains(failureId))
                            {
                                // Delete the warning (suppress it)
                                failuresAccessor.DeleteWarning(failure);
                                _lastSuppressedCount++;
                                _logger?.Debug($"✓ Suppressed warning: {failureText}");
                            }
                            else
                            {
                                // For warnings not in our suppression list, try to resolve them
                                // This handles warnings like "wall slightly off axis" generically
                                try
                                {
                                    failuresAccessor.DeleteWarning(failure);
                                    _lastSuppressedCount++;
                                    _logger?.Debug($"✓ Suppressed generic warning: {failureText}");
                                }
                                catch
                                {
                                    // If we can't delete, try to resolve
                                    failuresAccessor.ResolveFailure(failure);
                                    _lastResolvedCount++;
                                    _logger?.Debug($"✓ Resolved warning: {failureText}");
                                }
                            }
                        }
                        else if (severity == FailureSeverity.Error)
                        {
                            // NEVER suppress errors - let them through to user
                            _lastErrorCount++;
                            _logger?.Error($"Error failure (NOT suppressed): {failureText}");

                            // Let Revit handle the error
                            // Do not delete or resolve - user needs to see this
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"Error processing individual failure: {ex.Message}", ex);
                    }
                }

                // Log summary if any failures were processed
                if (_lastSuppressedCount > 0 || _lastResolvedCount > 0 || _lastErrorCount > 0)
                {
                    _logger?.Info($"Failure handling complete: {GetLastWarningStatistics()}");
                }

                // Continue with transaction
                return FailureProcessingResult.Continue;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Critical error in PreprocessFailures: {ex.Message}", ex);

                // Even if our handler fails, continue with the transaction
                return FailureProcessingResult.Continue;
            }
        }
    }
}
