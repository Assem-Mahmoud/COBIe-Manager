using System.Collections.Generic;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Lightweight summary for room-only fill preview mode (no document modification).
    /// </summary>
    public class RoomFillPreviewSummary
    {
        /// <summary>
        /// Estimated elements that will be processed
        /// </summary>
        public int EstimatedElementsToProcess { get; set; }

        /// <summary>
        /// Estimated elements where a room was found
        /// </summary>
        public int EstimatedRoomsFound { get; set; }

        /// <summary>
        /// Estimated elements where no room was found
        /// </summary>
        public int EstimatedNoRoomFound { get; set; }

        /// <summary>
        /// Categories that have zero elements in model
        /// </summary>
        public List<string> CategoriesWithNoElements { get; set; } = new List<string>();

        /// <summary>
        /// Configuration issues (e.g., no categories selected, no parameters mapped)
        /// </summary>
        public List<string> ValidationWarnings { get; set; } = new List<string>();

        /// <summary>
        /// Checks if there are any validation warnings
        /// </summary>
        public bool HasValidationWarnings => ValidationWarnings != null && ValidationWarnings.Count > 0;
    }
}
