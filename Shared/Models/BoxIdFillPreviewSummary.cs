using System.Collections.Generic;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Lightweight summary for box ID fill preview mode (no document modification).
    /// </summary>
    public class BoxIdFillPreviewSummary
    {
        /// <summary>
        /// Estimated number of groups to process
        /// </summary>
        public int EstimatedGroupsToProcess { get; set; }

        /// <summary>
        /// Estimated total number of group members across all groups
        /// </summary>
        public int EstimatedMembersToProcess { get; set; }

        /// <summary>
        /// Groups skipped due to missing name
        /// </summary>
        public int GroupsSkippedNoName { get; set; }

        /// <summary>
        /// Configuration issues
        /// </summary>
        public List<string> ValidationWarnings { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BoxIdFillPreviewSummary()
        {
            ValidationWarnings = new List<string>();
        }

        /// <summary>
        /// Whether the preview has any validation warnings
        /// </summary>
        public bool HasValidationWarnings => ValidationWarnings != null && ValidationWarnings.Count > 0;

        /// <summary>
        /// Whether there are any elements to process
        /// </summary>
        public bool HasElementsToProcess => EstimatedMembersToProcess > 0;

        /// <summary>
        /// Creates a formatted preview summary for display
        /// </summary>
        /// <returns>Formatted preview summary string</returns>
        public string ToFormattedString()
        {
            var summary = $"Box ID Fill Preview Summary:\n";
            summary += $"---------------------------\n";
            summary += $"Estimated Groups to Process: {EstimatedGroupsToProcess}\n";
            summary += $"Estimated Members to Process: {EstimatedMembersToProcess}\n";
            summary += $"Groups Skipped (No Name): {GroupsSkippedNoName}\n";

            if (HasValidationWarnings)
            {
                summary += $"\nValidation Warnings:\n";
                foreach (var warning in ValidationWarnings)
                {
                    summary += $"  ⚠️ {warning}\n";
                }
            }

            return summary;
        }

        /// <summary>
        /// Adds a validation warning
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void AddValidationWarning(string warning)
        {
            if (ValidationWarnings == null)
            {
                ValidationWarnings = new List<string>();
            }
            ValidationWarnings.Add(warning);
        }

        /// <summary>
        /// Gets a brief status message
        /// </summary>
        /// <returns>Brief status message</returns>
        public string GetStatusMessage()
        {
            if (HasValidationWarnings)
            {
                return $"⚠️ Configuration has {ValidationWarnings.Count} warning(s)";
            }

            if (!HasElementsToProcess)
            {
                return "ℹ️ No groups found to process";
            }

            return $"✓ Ready to process {EstimatedGroupsToProcess} group(s) with {EstimatedMembersToProcess} member(s)";
        }
    }
}
