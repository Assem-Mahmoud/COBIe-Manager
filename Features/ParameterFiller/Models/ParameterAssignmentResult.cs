namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Result of attempting to assign a single parameter.
    /// </summary>
    public class ParameterAssignmentResult
    {
        /// <summary>
        /// Whether the parameter was successfully set
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether the assignment was skipped (parameter missing, read-only, or value exists)
        /// </summary>
        public bool Skipped { get; set; }

        /// <summary>
        /// Reason for skipping (if applicable)
        /// </summary>
        public string SkipReason { get; set; }

        /// <summary>
        /// The element's ID for logging
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <param name="elementId">Element ID</param>
        /// <returns>Successful parameter assignment result</returns>
        public static ParameterAssignmentResult CreateSuccess(int elementId)
        {
            return new ParameterAssignmentResult
            {
                Success = true,
                Skipped = false,
                ElementId = elementId
            };
        }

        /// <summary>
        /// Creates a skipped result with reason
        /// </summary>
        /// <param name="elementId">Element ID</param>
        /// <param name="skipReason">Reason for skipping</param>
        /// <returns>Skipped parameter assignment result</returns>
        public static ParameterAssignmentResult CreateSkipped(int elementId, string skipReason)
        {
            return new ParameterAssignmentResult
            {
                Success = false,
                Skipped = true,
                SkipReason = skipReason,
                ElementId = elementId
            };
        }

        /// <summary>
        /// Creates a failure result (not skipped, but failed)
        /// </summary>
        /// <param name="elementId">Element ID</param>
        /// <param name="errorReason">Reason for failure</param>
        /// <returns>Failed parameter assignment result</returns>
        public static ParameterAssignmentResult CreateFailure(int elementId, string errorReason)
        {
            return new ParameterAssignmentResult
            {
                Success = false,
                Skipped = false,
                SkipReason = errorReason,
                ElementId = elementId
            };
        }
    }
}
