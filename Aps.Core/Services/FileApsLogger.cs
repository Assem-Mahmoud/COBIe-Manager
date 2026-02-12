using System;
using System.IO;
using Aps.Core.Services;

namespace Aps.Core.Logging;

/// <summary>
/// File-based implementation of IApsLogger for Aps.Core
/// </summary>
public class FileApsLogger : IApsLogger
{
    private readonly string _logFile;
    private readonly object _lock = new();

    public FileApsLogger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "COBIeManager", "Logs");

        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        _logFile = Path.Combine(logDir, $"aps_auth_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null
            ? $"{message}{Environment.NewLine}Exception: {ex.GetType().Name}{Environment.NewLine}Message: {ex.Message}{Environment.NewLine}{ex.StackTrace}"
            : message;

        WriteLog("ERROR", fullMessage);
    }

    public void Debug(string message)
    {
        WriteLog("DEBUG", message);
    }

    public void Warn(string message)
    {
        WriteLog("WARN", message);
    }

    private void WriteLog(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(_logFile, logLine + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }

    public string GetLogPath() => _logFile;
}
