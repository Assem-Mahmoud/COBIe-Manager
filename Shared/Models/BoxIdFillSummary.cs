using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Aggregate statistics from a box ID fill operation.
    /// </summary>
    public class BoxIdFillSummary
    {
        /// <summary>
        /// Total groups evaluated
        /// </summary>
        public int TotalGroupsScanned { get; set; }

        /// <summary>
        /// Groups successfully processed (members updated)
        /// </summary>
        public int GroupsProcessed { get; set; }

        /// <summary>
        /// Total members scanned across all groups
        /// </summary>
        public int TotalMembersScanned { get; set; }

        /// <summary>
        /// Members that had the parameter successfully set
        /// </summary>
        public int MembersUpdated { get; set; }

        /// <summary>
        /// Groups skipped due to missing name
        /// </summary>
        public int GroupsSkippedNoName { get; set; }

        /// <summary>
        /// Members skipped due to missing parameter
        /// </summary>
        public int MembersSkippedParameterMissing { get; set; }

        /// <summary>
        /// Members skipped due to read-only parameter
        /// </summary>
        public int MembersSkippedParameterReadOnly { get; set; }

        /// <summary>
        /// Members skipped due to existing value (when overwrite=false)
        /// </summary>
        public int MembersSkippedValueExists { get; set; }

        /// <summary>
        /// Members skipped due to nested group membership
        /// </summary>
        public int MembersSkippedNestedGroup { get; set; }

        /// <summary>
        /// Members filled with NotAssignedValue (N/A) because they are not in any group
        /// </summary>
        public int MembersFilledWithNA { get; set; }

        /// <summary>
        /// Elements from selected categories that are not in any group
        /// </summary>
        public int MembersNotInGroup { get; set; }

        /// <summary>
        /// Group elements updated (when includeGroupElement=true)
        /// </summary>
        public int GroupElementsUpdated { get; set; }

        /// <summary>
        /// Number of group-related failures resolved by failure preprocessor
        /// </summary>
        public int FailuresResolved { get; set; }

        /// <summary>
        /// Time taken for the operation
        /// </summary>
        public TimeSpan ProcessingDuration { get; set; }

        /// <summary>
        /// Detailed skip reasons with element IDs
        /// </summary>
        public Dictionary<string, List<int>> SkippedElementIds { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BoxIdFillSummary()
        {
            SkippedElementIds = new Dictionary<string, List<int>>();
        }

        /// <summary>
        /// Calculates total number of skipped members
        /// </summary>
        public int TotalMembersSkipped =>
            MembersSkippedParameterMissing +
            MembersSkippedParameterReadOnly +
            MembersSkippedValueExists +
            MembersSkippedNestedGroup;

        /// <summary>
        /// Calculates total number of skipped groups
        /// </summary>
        public int TotalGroupsSkipped => GroupsSkippedNoName;

        /// <summary>
        /// Gets the processing success rate for members
        /// </summary>
        public double MemberSuccessRate
        {
            get
            {
                if (TotalMembersScanned == 0) return 0;
                return (double)MembersUpdated / TotalMembersScanned * 100;
            }
        }

        /// <summary>
        /// Registers a skipped element with reason
        /// </summary>
        /// <param name="elementId">Element ID</param>
        /// <param name="skipReason">Reason for skipping</param>
        public void RegisterSkippedElement(int elementId, string skipReason)
        {
            if (!SkippedElementIds.ContainsKey(skipReason))
            {
                SkippedElementIds[skipReason] = new List<int>();
            }
            SkippedElementIds[skipReason].Add(elementId);
        }

        /// <summary>
        /// Gets all processed element IDs (both updated and skipped)
        /// </summary>
        /// <returns>Set of element IDs that were processed</returns>
        public HashSet<int> GetProcessedElementIds()
        {
            var processedIds = new HashSet<int>();

            // Add all IDs from skipped elements
            foreach (var kvp in SkippedElementIds)
            {
                foreach (var id in kvp.Value)
                {
                    processedIds.Add(id);
                }
            }

            // Note: We don't have a direct list of updated elements in the summary
            // The caller will need to track updated elements separately
            // This method returns the skipped elements which we don't want to fill with N/A

            return processedIds;
        }

        /// <summary>
        /// Creates a formatted summary text for display
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public string ToFormattedString()
        {
            return $"Box ID Fill Summary:\n" +
                   $"-------------------\n" +
                   $"Total Groups Scanned: {TotalGroupsScanned}\n" +
                   $"Groups Processed: {GroupsProcessed}\n" +
                   $"Groups Skipped (No Name): {GroupsSkippedNoName}\n" +
                   $"\n" +
                   $"Total Members Scanned: {TotalMembersScanned}\n" +
                   $"Members Updated: {MembersUpdated}\n" +
                   $"Members Skipped: {TotalMembersSkipped}\n" +
                   $"  - Parameter Missing: {MembersSkippedParameterMissing}\n" +
                   $"  - Parameter Read-Only: {MembersSkippedParameterReadOnly}\n" +
                   $"  - Value Exists: {MembersSkippedValueExists}\n" +
                   $"  - Nested Group: {MembersSkippedNestedGroup}\n" +
                   $"\n" +
                   $"Group Elements Updated: {GroupElementsUpdated}\n" +
                   $"\n" +
                   $"Failures Resolved: {FailuresResolved}\n" +
                   $"\n" +
                   $"Member Success Rate: {MemberSuccessRate:F1}%\n" +
                   $"Processing Duration: {ProcessingDuration.TotalSeconds:F2} seconds";
        }

        /// <summary>
        /// Creates detailed log content with all skip reasons
        /// </summary>
        /// <returns>Detailed log content</returns>
        public string ToDetailedLogString()
        {
            var log = ToFormattedString() + "\n\n";

            if (SkippedElementIds.Any())
            {
                log += "Skipped Elements by Reason:\n";
                log += "-------------------------\n";

                foreach (var kvp in SkippedElementIds.OrderBy(x => x.Key))
                {
                    log += $"{kvp.Key}: {kvp.Value.Count} elements\n";
                    log += $"  IDs: {string.Join(", ", kvp.Value.Take(10))}";
                    if (kvp.Value.Count > 10)
                    {
                        log += $" ... (+{kvp.Value.Count - 10} more)";
                    }
                    log += "\n";
                }
            }

            return log;
        }
    }
}
