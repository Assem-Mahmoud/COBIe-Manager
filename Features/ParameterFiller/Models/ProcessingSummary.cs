using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Aggregate statistics from a parameter fill operation.
    /// </summary>
    public class ProcessingSummary
    {
        /// <summary>
        /// Total elements evaluated for processing
        /// </summary>
        public int TotalElementsScanned { get; set; }

        /// <summary>
        /// Elements that had at least one parameter set
        /// </summary>
        public int ElementsProcessed { get; set; }

        /// <summary>
        /// Count of level parameters successfully set
        /// </summary>
        public int LevelParametersFilled { get; set; }

        /// <summary>
        /// Count of room parameters successfully set
        /// </summary>
        public int RoomParametersFilled { get; set; }

        /// <summary>
        /// Elements skipped due to missing bounding box
        /// </summary>
        public int SkippedNoBoundingBox { get; set; }

        /// <summary>
        /// Elements skipped due to missing location for room detection
        /// </summary>
        public int SkippedNoLocation { get; set; }

        /// <summary>
        /// Elements skipped because no room was found
        /// </summary>
        public int SkippedNoRoomFound { get; set; }

        /// <summary>
        /// Elements skipped because target parameter doesn't exist
        /// </summary>
        public int SkippedParameterMissing { get; set; }

        /// <summary>
        /// Elements skipped because parameter is read-only
        /// </summary>
        public int SkippedParameterReadOnly { get; set; }

        /// <summary>
        /// Elements outside the selected level band
        /// </summary>
        public int SkippedNotInLevelBand { get; set; }

        /// <summary>
        /// Elements skipped due to existing value (when overwrite=false)
        /// </summary>
        public int SkippedValueExists { get; set; }

        /// <summary>
        /// Time taken for the operation
        /// </summary>
        public TimeSpan ProcessingDuration { get; set; }

        /// <summary>
        /// Element IDs grouped by skip reason (for detailed log)
        /// </summary>
        public Dictionary<string, List<int>> SkippedElementIds { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ProcessingSummary()
        {
            SkippedElementIds = new Dictionary<string, List<int>>();
        }

        /// <summary>
        /// Calculates total number of skipped elements
        /// </summary>
        public int TotalSkipped =>
            SkippedNoBoundingBox +
            SkippedNoLocation +
            SkippedNoRoomFound +
            SkippedParameterMissing +
            SkippedParameterReadOnly +
            SkippedNotInLevelBand +
            SkippedValueExists;

        /// <summary>
        /// Gets the processing success rate
        /// </summary>
        public double SuccessRate
        {
            get
            {
                if (TotalElementsScanned == 0) return 0;
                return (double)ElementsProcessed / TotalElementsScanned * 100;
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
        /// Creates a formatted summary text for display
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public string ToFormattedString()
        {
            return $"Processing Summary:\n" +
                   $"-----------------\n" +
                   $"Total Elements Scanned: {TotalElementsScanned}\n" +
                   $"Elements Processed: {ElementsProcessed}\n" +
                   $"Level Parameters Filled: {LevelParametersFilled}\n" +
                   $"Room Parameters Filled: {RoomParametersFilled}\n" +
                   $"Total Skipped: {TotalSkipped}\n" +
                   $"  - No Bounding Box: {SkippedNoBoundingBox}\n" +
                   $"  - No Location: {SkippedNoLocation}\n" +
                   $"  - No Room Found: {SkippedNoRoomFound}\n" +
                   $"  - Parameter Missing: {SkippedParameterMissing}\n" +
                   $"  - Parameter Read-Only: {SkippedParameterReadOnly}\n" +
                   $"  - Not In Level Band: {SkippedNotInLevelBand}\n" +
                   $"  - Value Exists: {SkippedValueExists}\n" +
                   $"Success Rate: {SuccessRate:F1}%\n" +
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
