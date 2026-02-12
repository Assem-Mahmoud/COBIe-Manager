namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Enumeration defining different methods for detecting rooms associated with elements
    /// </summary>
    public enum RoomDetectionMethod
    {
        /// <summary>
        /// Use the element's Room parameter directly
        /// </summary>
        DirectRoomProperty,

        /// <summary>
        /// Use the From Room parameter (for doors, windows)
        /// </summary>
        FromRoomProperty,

        /// <summary>
        /// Use the To Room parameter (for doors, windows)
        /// </summary>
        ToRoomProperty,

        /// <summary>
        /// Use GetRoomAtPoint with the element's center point
        /// </summary>
        PointInRoom,

        /// <summary>
        /// Element has no location information (cannot detect room)
        /// </summary>
        NoLocation,

        /// <summary>
        /// No room found for the element
        /// </summary>
        NoRoomFound
    }
}