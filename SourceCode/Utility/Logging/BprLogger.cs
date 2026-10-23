using System;
using System.Collections.Generic;
using System.Linq;
using BprDevKit.Config;

namespace BprDevKit.Utility.Logging
{
    /// <summary>
    /// Thread-safe central logger service for BprDevKit
    /// </summary>
    public class BprLogger : IBprLogger
    {
        private const int MaxInMemoryLogs = 1000;
        private readonly List<LogEntry> _inMemoryLogs = new();
        private readonly object _lock = new();
        private readonly RollingFileLogger _fileLogger = new();

        public static BprLogger Instance { get; } = new();

        public event Action<LogEntry>? OnLogEntryAdded;

        public string GetLogDirectory() => _fileLogger.GetLogDirectory();

        public void Log(
            LogLevel level,
            string source,
            string message,
            string? details = null,
            string? serverName = null,
            string? databaseName = null,
            long? executionTimeMs = null,
            Exception? exception = null)
        {
            var settings = ApplicationConfiguration.Config.Logging;

            // Check if log level meets configured threshold
            if (level < settings.LogLevel)
                return;

            var entry = new LogEntry
            {
                Level = level,
                Source = string.IsNullOrWhiteSpace(source) ? "System" : source,
                Message = message,
                Details = details,
                ServerName = serverName,
                DatabaseName = databaseName,
                ExecutionTimeMs = executionTimeMs,
                Exception = exception,
                Timestamp = DateTime.Now
            };

            // 1. Console logging
            if (settings.EnableConsoleLogging)
            {
                WriteToConsole(entry);
            }

            // 2. File logging
            _fileLogger.WriteLog(entry, settings);

            // 3. In-memory buffer for UI
            lock (_lock)
            {
                _inMemoryLogs.Add(entry);
                if (_inMemoryLogs.Count > MaxInMemoryLogs)
                {
                    _inMemoryLogs.RemoveAt(0);
                }
            }

            // 4. Notify UI subscribers
            try
            {
                OnLogEntryAdded?.Invoke(entry);
            }
            catch
            {
                // UI subscriber errors should not affect logging
            }
        }

        public void LogTrace(string source, string message, string? details = null)
            => Log(LogLevel.Trace, source, message, details);

        public void LogDebug(string source, string message, string? details = null)
            => Log(LogLevel.Debug, source, message, details);

        public void LogInformation(string source, string message, string? details = null, string? serverName = null, string? databaseName = null, long? executionTimeMs = null)
            => Log(LogLevel.Information, source, message, details, serverName, databaseName, executionTimeMs);

        public void LogWarning(string source, string message, string? details = null, Exception? exception = null)
            => Log(LogLevel.Warning, source, message, details, exception: exception);

        public void LogError(string source, string message, Exception? exception = null, string? details = null, string? serverName = null, string? databaseName = null)
            => Log(LogLevel.Error, source, message, details, serverName, databaseName, exception: exception);

        public void LogCritical(string source, string message, Exception? exception = null, string? details = null)
            => Log(LogLevel.Critical, source, message, details, exception: exception);

        public void LogTrace(string message)
            => Log(LogLevel.Trace, "General", message);

        public void LogDebug(string message)
            => Log(LogLevel.Debug, "General", message);

        public void LogInformation(string message, string? serverName = null, string? databaseName = null, long? executionTimeMs = null)
            => Log(LogLevel.Information, "General", message, serverName: serverName, databaseName: databaseName, executionTimeMs: executionTimeMs);

        public void LogWarning(string message, Exception? exception = null, string? serverName = null)
            => Log(LogLevel.Warning, "General", message, serverName: serverName, exception: exception);

        public void LogError(string message, Exception? exception = null, string? serverName = null, string? databaseName = null)
            => Log(LogLevel.Error, "General", message, serverName: serverName, databaseName: databaseName, exception: exception);

        public IReadOnlyList<LogEntry> GetRecentLogs()
        {
            lock (_lock)
            {
                if (_inMemoryLogs.Count == 0)
                {
                    var todayFile = GetTodayLogFilePath();
                    if (!string.IsNullOrEmpty(todayFile))
                    {
                        var fromDisk = LogFileParser.ParseFile(todayFile);
                        if (fromDisk.Count > 0)
                        {
                            var slice = fromDisk.Skip(Math.Max(0, fromDisk.Count - MaxInMemoryLogs)).ToList();
                            _inMemoryLogs.AddRange(slice);
                        }
                    }
                }
                return _inMemoryLogs.ToList();
            }
        }

        public void ClearRecentLogs()
        {
            lock (_lock)
            {
                _inMemoryLogs.Clear();
            }
        }

        public List<string> GetAvailableLogFiles() => _fileLogger.GetAvailableLogFiles();

        public string? GetTodayLogFilePath() => _fileLogger.GetTodayLogFilePath();

        public List<LogEntry> LoadLogsFromFile(string filePath) => LogFileParser.ParseFile(filePath);

        public List<LogEntry> LoadLogsFromContent(string content) => LogFileParser.ParseLogs(content);

        private static void WriteToConsole(LogEntry entry)
        {
            var oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = entry.Level switch
                {
                    LogLevel.Trace => ConsoleColor.Gray,
                    LogLevel.Debug => ConsoleColor.DarkCyan,
                    LogLevel.Information => ConsoleColor.Cyan,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Critical => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.Source}] {entry.Message}");
                if (!string.IsNullOrWhiteSpace(entry.Details))
                {
                    Console.WriteLine($"  {entry.Details}");
                }
                if (entry.Exception != null)
                {
                    Console.WriteLine($"  Exception: {entry.Exception.Message}");
                }
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }
    }
}
