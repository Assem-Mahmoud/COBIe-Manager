using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Logger for tracking processing results and statistics
    /// </summary>
    public class ProcessingLogger : IProcessingLogger
    {
        private readonly ILogger _logger;
        private readonly List<LogEntry> _successEntries;
        private readonly List<LogEntry> _skipEntries;
        private readonly List<LogEntry> _errorEntries;
        private readonly Dictionary<string, int> _skipReasonCounts;

        /// <summary>
        /// Creates a new processing logger
        /// </summary>
        public ProcessingLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _successEntries = new List<LogEntry>();
            _skipEntries = new List<LogEntry>();
            _errorEntries = new List<LogEntry>();
            _skipReasonCounts = new Dictionary<string, int>();
        }

        /// <summary>
        /// Logs a successfully processed element
        /// </summary>
        public void LogSuccess(ElementId elementId, string category, string details)
        {
            var entry = new LogEntry
            {
                ElementId = elementId,
                Category = category,
                Message = details,
                Timestamp = DateTime.Now
            };
            _successEntries.Add(entry);
            _logger.Debug($"[SUCCESS] ID: {elementId}, Category: {category}, Details: {details}");
        }

        /// <summary>
        /// Logs an element that was skipped
        /// </summary>
        public void LogSkip(ElementId elementId, string category, string skipReason)
        {
            var entry = new LogEntry
            {
                ElementId = elementId,
                Category = category,
                Message = skipReason,
                Timestamp = DateTime.Now
            };
            _skipEntries.Add(entry);

            // Track skip reason counts
            if (_skipReasonCounts.ContainsKey(skipReason))
            {
                _skipReasonCounts[skipReason]++;
            }
            else
            {
                _skipReasonCounts[skipReason] = 1;
            }

            _logger.Debug($"[SKIP] ID: {elementId}, Category: {category}, Reason: {skipReason}");
        }

        /// <summary>
        /// Logs an error that occurred during processing
        /// </summary>
        public void LogError(ElementId? elementId, string? category, string error, Exception? exception = null)
        {
            var entry = new LogEntry
            {
                ElementId = elementId ?? ElementId.InvalidElementId,
                Category = category,
                Message = error,
                Timestamp = DateTime.Now,
                Exception = exception
            };
            _errorEntries.Add(entry);

            if (exception != null)
            {
                _logger.Error($"[ERROR] ID: {elementId}, Category: {category}, Error: {error}", exception);
            }
            else
            {
                _logger.Error($"[ERROR] ID: {elementId}, Category: {category}, Error: {error}");
            }
        }

        /// <summary>
        /// Gets the processing summary statistics
        /// </summary>
        public ProcessingSummary GetSummary()
        {
            var summary = new ProcessingSummary
            {
                TotalElementsScanned = TotalCount,
                ElementsProcessed = SuccessCount,
                SkippedNoLocation = GetSkipCount("No Location"),
                SkippedNoRoomFound = GetSkipCount("No Room Found"),
                SkippedNoBoundingBox = GetSkipCount("No bounding box"),
                SkippedParameterMissing = GetSkipCount("Parameter missing"),
                SkippedParameterReadOnly = GetSkipCount("Parameter read-only"),
                SkippedNotInLevelBand = GetSkipCount("Position: BelowBand") + GetSkipCount("Position: AboveBand"),
                SkippedValueExists = GetSkipCount("Existing value, overwrite disabled"),
                SkippedElementIds = new Dictionary<string, List<int>>()
            };

            // Populate skipped element IDs grouped by reason
            foreach (var group in _skipEntries.GroupBy(e => e.Message))
            {
                summary.SkippedElementIds[group.Key] = group.Select(e => e.ElementId.IntegerValue).ToList();
            }

            return summary;
        }

        /// <summary>
        /// Resets all statistics and logs
        /// </summary>
        public void Reset()
        {
            _successEntries.Clear();
            _skipEntries.Clear();
            _errorEntries.Clear();
            _skipReasonCounts.Clear();
            _logger.Info("Processing logger reset");
        }

        /// <summary>
        /// Gets total number of elements processed
        /// </summary>
        public int TotalCount => SuccessCount + SkipCount + ErrorCount;

        /// <summary>
        /// Gets number of elements that were successfully processed
        /// </summary>
        public int SuccessCount => _successEntries.Count;

        /// <summary>
        /// Gets number of elements that were skipped
        /// </summary>
        public int SkipCount => _skipEntries.Count;

        /// <summary>
        /// Gets number of elements that had errors
        /// </summary>
        public int ErrorCount => _errorEntries.Count;

        /// <summary>
        /// Exports detailed processing log to a file
        /// </summary>
        /// <param name="summary">Processing summary to export</param>
        /// <param name="filePath">Path to log file</param>
        public void ExportLog(ProcessingSummary summary, string filePath)
        {
            try
            {
                var content = GenerateLogContent(summary);
                File.WriteAllText(filePath, content, Encoding.UTF8);
                _logger.Info($"Log exported to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to export log to {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates detailed log content
        /// </summary>
        /// <param name="summary">Processing summary</param>
        /// <returns>Formatted log content</returns>
        public string GenerateLogContent(ProcessingSummary summary)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("COBIe Manager - Parameter Fill Processing Log");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            // Summary section
            sb.AppendLine("SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine(summary.ToFormattedString());
            sb.AppendLine();

            // Detailed skip reasons
            if (_skipReasonCounts.Any())
            {
                sb.AppendLine("SKIP REASONS BREAKDOWN");
                sb.AppendLine("-".PadRight(80, '-'));
                foreach (var kvp in _skipReasonCounts.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"  {kvp.Key,-40} : {kvp.Value,5} elements");
                }
                sb.AppendLine();
            }

            // Success details
            if (_successEntries.Any())
            {
                sb.AppendLine("SUCCESSFULLY PROCESSED ELEMENTS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"Total: {_successEntries.Count}");
                sb.AppendLine();

                foreach (var entry in _successEntries.Take(100))
                {
                    sb.AppendLine($"  [{entry.ElementId.IntegerValue,8}] {entry.Category,-25} - {entry.Message}");
                }

                if (_successEntries.Count > 100)
                {
                    sb.AppendLine($"  ... and {_successEntries.Count - 100} more elements");
                }
                sb.AppendLine();
            }

            // Skipped elements details
            if (_skipEntries.Any())
            {
                sb.AppendLine("SKIPPED ELEMENTS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"Total: {_skipEntries.Count}");
                sb.AppendLine();

                foreach (var group in _skipEntries.GroupBy(e => e.Message).OrderByDescending(g => g.Count()))
                {
                    sb.AppendLine($"  Reason: {group.Key}");
                    sb.AppendLine($"  Count: {group.Count()}");
                    sb.AppendLine($"  Element IDs: {string.Join(", ", group.Take(20).Select(e => e.ElementId.IntegerValue))}");
                    if (group.Count() > 20)
                    {
                        sb.AppendLine($"              ... and {group.Count() - 20} more");
                    }
                    sb.AppendLine();
                }
            }

            // Error details
            if (_errorEntries.Any())
            {
                sb.AppendLine("ERRORS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"Total: {_errorEntries.Count}");
                sb.AppendLine();

                foreach (var entry in _errorEntries)
                {
                    sb.AppendLine($"  [{entry.ElementId.IntegerValue,8}] {entry.Category,-25} - {entry.Message}");
                    if (entry.Exception != null)
                    {
                        sb.AppendLine($"  Exception: {entry.Exception.Message}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("End of Log");
            sb.AppendLine("=".PadRight(80, '='));

            return sb.ToString();
        }

        /// <summary>
        /// Gets skip count for a specific reason
        /// </summary>
        private int GetSkipCount(string reason)
        {
            return _skipReasonCounts.ContainsKey(reason) ? _skipReasonCounts[reason] : 0;
        }

        /// <summary>
        /// Internal log entry structure
        /// </summary>
        private class LogEntry
        {
            public ElementId ElementId { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
