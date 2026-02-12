namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Enumeration defining the position of an element relative to a level band
    /// </summary>
    public enum LevelBandPosition
    {
        /// <summary>
        /// Element is completely below the level band
        /// </summary>
        BelowBand,

        /// <summary>
        /// Element is completely above the level band
        /// </summary>
        AboveBand,

        /// <summary>
        /// Element intersects with or is contained within the level band
        /// </summary>
        InBand,

        /// <summary>
        /// Element has no bounding box (cannot determine position)
        /// </summary>
        NoBoundingBox
    }
}