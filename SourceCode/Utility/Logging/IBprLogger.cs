using System;
using System.Collections.Generic;

namespace BprDevKit.Utility.Logging
{
    /// <summary>
    /// Core logger interface for BprDevKit
    /// </summary>
    public interface IBprLogger
    {
        void Log(
            LogLevel level,
            string source,
            string message,
            string? details = null,
            string? serverName = null,
            string? databaseName = null,
            long? executionTimeMs = null,
            Exception? exception = null);

        void LogTrace(string source, string message, string? details = null);
        void LogDebug(string source, string message, string? details = null);
        void LogInformation(string source, string message, string? details = null, string? serverName = null, string? databaseName = null, long? executionTimeMs = null);
        void LogWarning(string source, string message, string? details = null, Exception? exception = null);
        void LogError(string source, string message, Exception? exception = null, string? details = null, string? serverName = null, string? databaseName = null);
        void LogCritical(string source, string message, Exception? exception = null, string? details = null);

        // Convenient caller-friendly overloads
        void LogTrace(string message);
        void LogDebug(string message);
        void LogInformation(string message, string? serverName = null, string? databaseName = null, long? executionTimeMs = null);
        void LogWarning(string message, Exception? exception = null, string? serverName = null);
        void LogError(string message, Exception? exception = null, string? serverName = null, string? databaseName = null);

        IReadOnlyList<LogEntry> GetRecentLogs();
        void ClearRecentLogs();
        event Action<LogEntry>? OnLogEntryAdded;
        string GetLogDirectory();
    }
}
