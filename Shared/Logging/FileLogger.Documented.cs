using System;
using System.IO;
using System.Text;

namespace COBIeManager.Shared.Logging
{
    /// <summary>
    /// File-based logger implementation that writes diagnostic messages to log files.
    ///
    /// Features:
    /// - Writes to %APPDATA%\COBIeManager\Logs\ directory
    /// - Creates one file per day with timestamp
    /// - Supports 5 log levels: Debug, Info, Warn, Error, Fatal
    /// - Thread-safe file operations
    /// - Includes exception details and stack traces
    /// - Graceful failure (doesn't crash if logging fails)
    ///
    /// Example:
    /// <code>
    /// var logger = new FileLogger();
    /// logger.Info("Application started");
    /// logger.Error("Operation failed", ex);
    /// </code>
    ///
    /// Log files are located at:
    /// %APPDATA%\COBIeManager\Logs\COBIeManager_YYYY-MM-DD.log
    /// </summary>
    public class FileLoggerDocumented : ILogger
    {
        private readonly string _logPath;
        private readonly object _lockObj = new();

        /// <summary>
        /// Initializes a new instance of the FileLogger class.
        /// </summary>
        /// <param name="logDirectory">Optional: Directory to write logs to.
        /// If null, defaults to %APPDATA%\COBIeManager\Logs\</param>
        ///
        /// <example>
        /// // Use default directory
        /// var logger = new FileLogger();
        ///
        /// // Use custom directory
        /// var logger = new FileLogger(@"C:\Logs");
        /// </example>
        public FileLoggerDocumented(string logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                // Default to appdata folder
                logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "COBIeManager",
                    "Logs");
            }

            _logPath = Path.Combine(logDirectory, $"COBIeManager_{DateTime.Now}.log");

            // Ensure directory exists
            Directory.CreateDirectory(logDirectory);
        }

        /// <summary>
        /// Logs a debug-level message.
        /// Use for detailed diagnostic information.
        /// </summary>
        /// <param name="message">The debug message to log</param>
        /// <param name="exception">Optional: Associated exception</param>
        ///
        /// <example>
        /// logger.Debug("Processing item 42 with type: Column");
        /// </example>
        public void Debug(string message, Exception exception = null)
        {
            WriteLog("DEBUG", message, exception);
        }

        /// <summary>
        /// Logs an informational message.
        /// Use for general flow information (operations started, completed, etc.).
        /// </summary>
        /// <param name="message">The information message to log</param>
        /// <param name="exception">Optional: Associated exception</param>
        ///
        /// <example>
        /// logger.Info("Column creation started for layer: Structural_01");
        /// </example>
        public void Info(string message, Exception exception = null)
        {
            WriteLog("INFO", message, exception);
        }

        /// <summary>
        /// Logs a warning message.
        /// Use for potentially problematic situations that are still handled.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="exception">Optional: Associated exception</param>
        ///
        /// <example>
        /// logger.Warn("Column family 'W14x61' not found, using default family");
        /// </example>
        public void Warn(string message, Exception exception = null)
        {
            WriteLog("WARN", message, exception);
        }

        /// <summary>
        /// Logs an error message.
        /// Use for error conditions where operation partially or fully failed.
        /// </summary>
        /// <param name="message">The error message describing what failed</param>
        /// <param name="exception">Optional: Associated exception with stack trace</param>
        ///
        /// <example>
        /// try { CreateColumn(cadColumn); }
        /// catch (Exception ex) { logger.Error("Failed to create column", ex); }
        /// </example>
        public void Error(string message, Exception exception = null)
        {
            WriteLog("ERROR", message, exception);
        }

        /// <summary>
        /// Logs a fatal error message.
        /// Use for critical failures where application cannot continue.
        /// </summary>
        /// <param name="message">The fatal error message</param>
        /// <param name="exception">Optional: Associated exception</param>
        ///
        /// <example>
        /// logger.Fatal("Critical failure in CAD geometry extraction", ex);
        /// </example>
        public void Fatal(string message, Exception exception = null)
        {
            WriteLog("FATAL", message, exception);
        }

        /// <summary>
        /// Internal method to write formatted log entries to file.
        /// Thread-safe through lock mechanism.
        /// </summary>
        /// <param name="level">Log level (DEBUG, INFO, WARN, ERROR, FATAL)</param>
        /// <param name="message">The message content</param>
        /// <param name="exception">Optional exception with full details</param>
        ///
        /// <remarks>
        /// Log format:
        /// [YYYY-MM-DD HH:mm:ss.fff] [LEVEL] Message text
        /// Exception: ExceptionType
        /// Message: Exception message
        /// StackTrace: Full stack trace
        /// InnerException: Inner exception message (if present)
        /// </remarks>
        private void WriteLog(string level, string message, Exception exception)
        {
            try
            {
                lock (_lockObj)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");

                    if (exception != null)
                    {
                        sb.AppendLine($"Exception: {exception.GetType().Name}");
                        sb.AppendLine($"Message: {exception.Message}");
                        sb.AppendLine($"StackTrace: {exception.StackTrace}");

                        if (exception.InnerException != null)
                        {
                            sb.AppendLine($"InnerException: {exception.InnerException.Message}");
                        }
                    }

                    File.AppendAllText(_logPath, sb.ToString());
                }
            }
            catch
            {
                // Silently fail if logging fails to avoid crashing the application
                // This is intentional - we don't want logging errors to break functionality
            }
        }
    }
}
