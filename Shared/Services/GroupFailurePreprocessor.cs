using Autodesk.Revit.DB;
using COBIeManager.Shared.Logging;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Failure preprocessor specifically for resolving group-related errors during parameter fill operations.
    /// Resolves AtomViolation and ModifyingMultiGroups failures when editing group members by GroupType.
    ///
    /// This allows safe modification of group members when we're intentionally updating the GroupType definition
    /// by processing one representative instance per type.
    /// </summary>
    public class GroupFailurePreprocessor : IFailuresPreprocessor
    {
        private readonly ILogger _logger;
        private readonly HashSet<FailureDefinitionId> _resolvableFailures;
        private int _resolvedCount;

        public int ResolvedCount => _resolvedCount;

        public GroupFailurePreprocessor(ILogger logger = null)
        {
            _logger = logger;
            _resolvableFailures = new HashSet<FailureDefinitionId>();
            _resolvedCount = 0;

            ConfigureResolvableFailures();
        }

        /// <summary>
        /// Configure group-related failures that we can safely resolve
        /// </summary>
        private void ConfigureResolvableFailures()
        {
            // These failures occur when modifying group members outside group edit mode
            // We can safely resolve them because we're processing by GroupType (one instance per type)
            // and updating all instances consistently

            // Atom violation when editing single group instance
            _resolvableFailures.Add(BuiltInFailures.GroupFailures.AtomViolationWhenOnePlaceInstance);

            // Atom violation when editing multiple group instances
            _resolvableFailures.Add(BuiltInFailures.GroupFailures.AtomViolationWhenMultiPlacedInstances);

            // Warning when modifying multiple groups
            _resolvableFailures.Add(BuiltInFailures.GroupFailures.ModifyingMultiGroups);

            _logger?.Debug($"GroupFailurePreprocessor configured to resolve {_resolvableFailures.Count} group failure types");
        }

        /// <summary>
        /// IFailuresPreprocessor implementation - called by Revit when failures occur
        /// </summary>
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            try
            {
                if (failuresAccessor == null)
                {
                    _logger?.Error("FailuresAccessor is null in GroupFailurePreprocessor.PreprocessFailures");
                    return FailureProcessingResult.Continue;
                }

                var failures = failuresAccessor.GetFailureMessages();

                if (failures.Count == 0)
                {
                    return FailureProcessingResult.Continue;
                }

                _logger?.Debug($"GroupFailurePreprocessor: Processing {failures.Count} failure(s)");

                foreach (FailureMessageAccessor failure in failures)
                {
                    try
                    {
                        var severity = failure.GetSeverity();
                        var failureId = failure.GetFailureDefinitionId();
                        var failureText = failure.GetDescriptionText();

                        // Check if this is a resolvable group failure
                        if (failureId != null && _resolvableFailures.Contains(failureId))
                        {
                            // Resolve the failure to allow the transaction to proceed
                            // This tells Revit we acknowledge the group edit and want to proceed
                            try
                            {
                                failuresAccessor.ResolveFailure(failure);
                                _resolvedCount++;
                                _logger?.Debug($"✓ Resolved group failure: {failureText} (ID: {failureId})");
                            }
                            catch (Exception resolveEx)
                            {
                                _logger?.Warn($"Failed to resolve group failure: {failureText} - {resolveEx.Message}");
                            }
                        }
                        else
                        {
                            // Log other failures that we're not resolving
                            _logger?.Debug($"Non-resolvable failure - {severity}: {failureText}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"Error processing individual failure: {ex.Message}", ex);
                    }
                }

                if (_resolvedCount > 0)
                {
                    _logger?.Info($"GroupFailurePreprocessor: Resolved {_resolvedCount} group-related failure(s)");
                }

                return FailureProcessingResult.Continue;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Critical error in GroupFailurePreprocessor.PreprocessFailures: {ex.Message}", ex);
                return FailureProcessingResult.Continue;
            }
        }

        /// <summary>
        /// Resets the resolution counter
        /// </summary>
        public void Reset()
        {
            _resolvedCount = 0;
        }
    }
}
