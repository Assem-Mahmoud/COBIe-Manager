using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for filling ACG-BOX-ID parameters from Model GroupType names.
    /// Processes ALL instances of each group type.
    /// </summary>
    public class BoxIdFillService : IBoxIdFillService
    {
        private readonly ILogger _logger;

        public BoxIdFillService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes groups and returns preview summary without modifying document.
        /// Counts all instances across all group types.
        /// </summary>
        public BoxIdFillPreviewSummary PreviewFill(Document document, string parameterName, bool overwriteExisting)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Parameter name cannot be empty", nameof(parameterName));
            }

            _logger.Info("Starting box ID fill preview (GroupType-based)");

            var summary = new BoxIdFillPreviewSummary();

            // Collect all model groups
            var groups = CollectModelGroups(document);

            if (!groups.Any())
            {
                _logger.Info("No model groups found in document");
                return summary;
            }

            _logger.Info($"Found {groups.Count} model group instances");

            // Group by GroupType.Id to count unique group types
            var groupsByType = groups
                .Where(g => g.GroupType != null)
                .GroupBy(g => g.GroupType.Id)
                .ToList();

            _logger.Info($"Found {groupsByType.Count} unique group types");

            int totalMembers = 0;
            int skippedNoName = 0;
            int typesWithNoName = 0;

            foreach (var typeGroup in groupsByType)
            {
                var representativeGroup = typeGroup.First();
                var groupType = representativeGroup.GroupType;
                string typeName = groupType.Name?.Trim();

                if (string.IsNullOrEmpty(typeName))
                {
                    typesWithNoName++;
                    skippedNoName += typeGroup.Count();
                    _logger.Debug($"GroupType {groupType.Id} has no name, skipping {typeGroup.Count()} instances");
                    continue;
                }

                // Count members from ALL instances of this type
                var memberIds = representativeGroup.GetMemberIds();
                int membersPerInstance = memberIds.Count;
                int totalMembersForType = membersPerInstance * typeGroup.Count();
                totalMembers += totalMembersForType;

                _logger.Debug($"GroupType '{typeName}' ({typeGroup.Count()} instances) has {membersPerInstance} members per instance = {totalMembersForType} total members");
            }

            // Count all instances (not just types) excluding those without names
            int totalInstances = groupsByType.Sum(g => g.Count());
            int namedInstances = totalInstances - skippedNoName;

            summary.EstimatedGroupsToProcess = namedInstances;
            summary.EstimatedMembersToProcess = totalMembers;
            summary.GroupsSkippedNoName = skippedNoName;

            _logger.Info($"Preview complete: {summary.EstimatedGroupsToProcess} group instances, {summary.EstimatedMembersToProcess} total members");

            return summary;
        }

        /// <summary>
        /// Executes box ID fill operation using GroupType.Name as the value.
        /// Processes ALL instances of each group type.
        /// </summary>
        public BoxIdFillSummary ExecuteFill(
            Document document,
            string parameterName,
            bool overwriteExisting,
            bool includeGroupElement,
            Action<int, string> progressAction = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Parameter name cannot be empty", nameof(parameterName));
            }

            _logger.Info($"Starting box ID fill operation (parameter: {parameterName}, overwrite: {overwriteExisting})");

            var stopwatch = Stopwatch.StartNew();
            var summary = new BoxIdFillSummary();

            // Collect all model groups
            var groups = CollectModelGroups(document);

            if (!groups.Any())
            {
                _logger.Info("No model groups found in document");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            // Group by GroupType.Id - we'll process only ONE instance per type
            var groupsByType = groups
                .Where(g => g.GroupType != null)
                .GroupBy(g => g.GroupType.Id)
                .ToList();

            summary.TotalGroupsScanned = groups.Count;
            _logger.Info($"Processing {groups.Count} model group instances ({groupsByType.Count} unique types)");

            // Create failure preprocessor for group-related warnings
            var failurePreprocessor = new GroupFailurePreprocessor(_logger);

            // Use transaction with failure preprocessor
            using (var transaction = new Transaction(document, "Fill ACG-BOX-ID from GroupTypes"))
            {
                // Configure failure handling
                var failureOptions = transaction.GetFailureHandlingOptions();
                failureOptions.SetFailuresPreprocessor(failurePreprocessor);
                transaction.SetFailureHandlingOptions(failureOptions);

                transaction.Start();

                try
                {
                    int processedTypes = 0;
                    int processedInstances = 0;
                    int totalMembersProcessed = 0;
                    int totalInstances = groupsByType.Sum(g => g.Count());

                    foreach (var typeGroup in groupsByType)
                    {
                        processedTypes++;

                        // Get the group type from the first instance
                        var firstInstance = typeGroup.First();
                        var groupType = firstInstance.GroupType;

                        // Use GroupType.Name as the value (not Group.Name)
                        string typeName = groupType.Name?.Trim();

                        if (string.IsNullOrEmpty(typeName))
                        {
                            summary.GroupsSkippedNoName += typeGroup.Count();
                            _logger.Debug($"GroupType {groupType.Id} has no name, skipping {typeGroup.Count()} instances");
                            continue;
                        }

                        _logger.Debug($"Processing GroupType '{typeName}' (type {processedTypes}/{groupsByType.Count}) - " +
                                    $"Type has {typeGroup.Count()} instances");

                        // Report progress
                        if (processedInstances % 5 == 0 || processedInstances + typeGroup.Count() >= totalInstances)
                        {
                            var message = $"Processing group instances: {processedInstances}/{totalInstances} - '{typeName}'";
                            _logger.Debug(message);
                            progressAction?.Invoke(processedInstances, message);
                        }

                        // Process ALL instances of this GroupType
                        foreach (var groupInstance in typeGroup)
                        {
                            processedInstances++;

                            // Get members from this specific instance
                            var memberIds = groupInstance.GetMemberIds();
                            totalMembersProcessed += memberIds.Count;

                            // Update each member of this specific group instance
                            foreach (var memberId in memberIds)
                            {
                                summary.TotalMembersScanned++;

                                var member = document.GetElement(memberId);
                                if (member == null)
                                {
                                    _logger.Warn($"Member {memberId} not found in document");
                                    continue;
                                }

                                // Skip nested groups
                                if (member is Group)
                                {
                                    summary.MembersSkippedNestedGroup++;
                                    summary.RegisterSkippedElement(member.Id.IntegerValue, SkipReasons.NestedGroup);
                                    _logger.Debug($"Skipping nested group {member.Id}");
                                    continue;
                                }

                                // Try to set the parameter using GroupType.Name as value
                                var result = TrySetParameter(member, parameterName, typeName, overwriteExisting);

                                if (result.Success)
                                {
                                    summary.MembersUpdated++;
                                }
                                else if (result.Skipped)
                                {
                                    summary.RegisterSkippedElement(member.Id.IntegerValue, result.SkipReason);

                                    switch (result.SkipReason)
                                    {
                                        case SkipReasons.ParameterMissing:
                                            summary.MembersSkippedParameterMissing++;
                                            break;
                                        case SkipReasons.ParameterReadOnly:
                                            summary.MembersSkippedParameterReadOnly++;
                                            break;
                                        case SkipReasons.ValueExists:
                                            summary.MembersSkippedValueExists++;
                                            break;
                                        case SkipReasons.NestedGroup:
                                            summary.MembersSkippedNestedGroup++;
                                            break;
                                    }
                                }
                            }

                            // Optionally set parameter on the group element itself
                            if (includeGroupElement)
                            {
                                var groupResult = TrySetParameter(groupInstance, parameterName, typeName, overwriteExisting);
                                if (groupResult.Success)
                                {
                                    summary.GroupElementsUpdated++;
                                }
                            }
                        }

                        // Count each instance processed, not just the type
                        summary.GroupsProcessed += typeGroup.Count();
                    }

                    transaction.Commit();

                    // Add statistics from failure preprocessor
                    summary.FailuresResolved = failurePreprocessor.ResolvedCount;

                    summary.ProcessingDuration = stopwatch.Elapsed;

                    _logger.Info($"Box ID fill complete in {summary.ProcessingDuration.TotalSeconds:F2} seconds");
                    _logger.Info($"Processed {summary.GroupsProcessed} group instances, {summary.MembersUpdated} members updated");
                    if (failurePreprocessor.ResolvedCount > 0)
                    {
                        _logger.Info($"Resolved {failurePreprocessor.ResolvedCount} group-related failure(s)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during box ID fill operation", ex);
                    transaction.RollBack();
                    throw;
                }
            }

            return summary;
        }

        /// <summary>
        /// Collects all Model Group instances from the document
        /// </summary>
        private IList<Group> CollectModelGroups(Document document)
        {
            var groups = new List<Group>();

            try
            {
                // Use FilteredElementCollector for model groups
                var collector = new FilteredElementCollector(document)
                    .OfCategory(BuiltInCategory.OST_IOSModelGroups)
                    .WhereElementIsNotElementType();

                foreach (var element in collector)
                {
                    if (element is Group group)
                    {
                        groups.Add(group);
                    }
                }

                _logger.Debug($"Collected {groups.Count} model groups");
            }
            catch (Exception ex)
            {
                _logger.Error("Error collecting model groups", ex);
            }

            return groups;
        }

        /// <summary>
        /// Attempts to set a parameter value on an element
        /// </summary>
        private ParameterSetResult TrySetParameter(Element element, string parameterName, string value, bool overwriteExisting)
        {
            // Skip nested groups
            if (element is Group)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.NestedGroup
                };
            }

            // Lookup parameter by name
            var parameter = element.LookupParameter(parameterName);

            if (parameter == null)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.ParameterMissing
                };
            }

            // Check if parameter is read-only
            if (parameter.IsReadOnly)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.ParameterReadOnly
                };
            }

            // Check if parameter has a value and we're not overwriting
            if (!overwriteExisting)
            {
                var currentValue = parameter.AsString();
                if (!string.IsNullOrEmpty(currentValue))
                {
                    return new ParameterSetResult
                    {
                        Success = false,
                        Skipped = true,
                        SkipReason = SkipReasons.ValueExists
                    };
                }
            }

            // Set the parameter value
            try
            {
                parameter.Set(value);
                return new ParameterSetResult
                {
                    Success = true,
                    Skipped = false
                };
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to set parameter '{parameterName}' on element {element.Id}: {ex.Message}");
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = false,
                    SkipReason = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Result of attempting to set a parameter
        /// </summary>
        private class ParameterSetResult
        {
            public bool Success { get; set; }
            public bool Skipped { get; set; }
            public string SkipReason { get; set; }
        }
    }
}
