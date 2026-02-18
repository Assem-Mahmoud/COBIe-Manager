using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using COBIeManager.Shared.Models;

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
        /// Box ID fill summary (when FillMode includes group fill)
        /// </summary>
        public BoxIdFillSummary BoxIdFillSummary { get; set; }

        /// <summary>
        /// Room fill summary (when FillMode is RoomOnly)
        /// </summary>
        public RoomFillSummary RoomFillSummary { get; set; }

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
            var result = $"Processing Summary:\n" +
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

            // Add box ID fill summary if available
            if (BoxIdFillSummary != null)
            {
                result += $"\n\nBox ID Fill Summary:\n" +
                         $"-------------------\n" +
                         $"Groups Processed: {BoxIdFillSummary.GroupsProcessed}/{BoxIdFillSummary.TotalGroupsScanned}\n" +
                         $"Members Updated: {BoxIdFillSummary.MembersUpdated}/{BoxIdFillSummary.TotalMembersScanned}\n" +
                         $"Members Skipped: {BoxIdFillSummary.TotalMembersSkipped}\n" +
                         $"Group Elements Updated: {BoxIdFillSummary.GroupElementsUpdated}";
            }

            // Add room fill summary if available
            if (RoomFillSummary != null)
            {
                result += $"\n\nRoom Fill Summary:\n" +
                         $"-----------------\n" +
                         $"Total Elements Scanned: {RoomFillSummary.TotalElementsScanned}\n" +
                         $"Elements Updated: {RoomFillSummary.ElementsUpdated}\n" +
                         $"Unique Rooms Found: {RoomFillSummary.UniqueRoomsFound}\n" +
                         $"Room Number Parameters Filled: {RoomFillSummary.RoomNumberParametersFilled}\n" +
                         $"Room Name Parameters Filled: {RoomFillSummary.RoomNameParametersFilled}\n" +
                         $"Room Ref Parameters Filled: {RoomFillSummary.RoomRefParametersFilled}\n" +
                         $"Elements Skipped:\n" +
                         $"  - No Location: {RoomFillSummary.SkippedNoLocation}\n" +
                         $"  - No Room Found: {RoomFillSummary.SkippedNoRoomFound}\n" +
                         $"  - Parameter Missing: {RoomFillSummary.SkippedParameterMissing}\n" +
                         $"  - Parameter Read-Only: {RoomFillSummary.SkippedParameterReadOnly}\n" +
                         $"  - Value Exists: {RoomFillSummary.SkippedValueExists}\n" +
                         $"Processing Duration: {RoomFillSummary.ProcessingDuration.TotalSeconds:F2} seconds";
            }

            return result;
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
