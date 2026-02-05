using System;

namespace COBIeManager.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when Revit element creation fails.
    /// </summary>
    public class RevitElementCreationException : Exception
    {
        public RevitElementCreationException(string message) : base(message) { }

        public RevitElementCreationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
