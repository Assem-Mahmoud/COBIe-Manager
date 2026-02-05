using System;

namespace COBIeManager.Shared.Logging
{
    /// <summary>
    /// Logger interface for application-wide logging.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        void Debug(string message, Exception exception = null);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void Info(string message, Exception exception = null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void Warn(string message, Exception exception = null);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        void Error(string message, Exception exception = null);

        /// <summary>
        /// Logs a fatal error message.
        /// </summary>
        void Fatal(string message, Exception exception = null);
    }
}
