namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Standardized skip reason constants for consistent logging across services
    /// </summary>
    public static class SkipReasons
    {
        // Level assignment skip reasons
        public const string NoBoundingBox = "No bounding box";
        public const string BelowBand = "Position: BelowBand";
        public const string AboveBand = "Position: AboveBand";

        // Room assignment skip reasons
        public const string NoLocation = "No Location";
        public const string NoRoomFound = "No Room Found";

        // Parameter skip reasons
        public const string ParameterMissing = "Parameter missing";
        public const string ParameterReadOnly = "Parameter read-only";
        public const string ExistingValueNoOverwrite = "Existing value, overwrite disabled";
        public const string ValueExists = ExistingValueNoOverwrite;

        // Group skip reasons
        public const string NestedGroup = "Nested group member";

        // Other skip reasons
        public const string FailedToAssignParameter = "Failed to assign level parameter";
    }
}
