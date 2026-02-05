using System;

namespace COBIeManager.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when CAD geometry extraction fails.
    /// </summary>
    public class CadExtractionException : Exception
    {
        public CadExtractionException(string message) : base(message) { }

        public CadExtractionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
