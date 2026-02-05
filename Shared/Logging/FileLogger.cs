using System;
using System.IO;
using System.Text;

namespace COBIeManager.Shared.Logging
{
    /// <summary>
    /// File-based logger that writes logs to a text file.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly object _lockObj = new();

        public FileLogger(string logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                // Default to appdata folder
                logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "COBIeManager",
                    "Logs");
            }

            _logPath = Path.Combine(logDirectory, $"COBIeManager_{DateTime.Now:yyyy-MM-dd}.log");

            // Ensure directory exists
            Directory.CreateDirectory(logDirectory);
        }

        public void Debug(string message, Exception exception = null)
        {
            WriteLog("DEBUG", message, exception);
        }

        public void Info(string message, Exception exception = null)
        {
            WriteLog("INFO", message, exception);
        }

        public void Warn(string message, Exception exception = null)
        {
            WriteLog("WARN", message, exception);
        }

        public void Error(string message, Exception exception = null)
        {
            WriteLog("ERROR", message, exception);
        }

        public void Fatal(string message, Exception exception = null)
        {
            WriteLog("FATAL", message, exception);
        }

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
            }
        }
    }
}
