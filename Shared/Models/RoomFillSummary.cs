using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Aggregate statistics from a room-only parameter fill operation.
    /// </summary>
    public class RoomFillSummary
    {
        /// <summary>
        /// Total elements evaluated for processing
        /// </summary>
        public int TotalElementsScanned { get; set; }

        /// <summary>
        /// Elements that had at least one parameter set
        /// </summary>
        public int ElementsUpdated { get; set; }

        /// <summary>
        /// Count of room number parameters successfully set
        /// </summary>
        public int RoomNumberParametersFilled { get; set; }

        /// <summary>
        /// Count of room name parameters successfully set
        /// </summary>
        public int RoomNameParametersFilled { get; set; }

        /// <summary>
        /// Count of room reference parameters successfully set
        /// </summary>
        public int RoomRefParametersFilled { get; set; }

        /// <summary>
        /// Unique rooms found during processing
        /// </summary>
        public int UniqueRoomsFound { get; set; }

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
        public Dictionary<string, List<int>> SkippedElementIds { get; set; } = new Dictionary<string, List<int>>();
    }
}
