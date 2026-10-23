using System;

namespace BprDevKit.Utility.Logging
{
    /// <summary>
    /// Represents an individual structured log event
    /// </summary>
    public class LogEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; } = LogLevel.Information;
        public string Source { get; set; } = "Application";
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public Exception? Exception { get; set; }

        public string LevelBadgeClass => Level switch
        {
            LogLevel.Trace => "bg-secondary text-white",
            LogLevel.Debug => "bg-dark text-white",
            LogLevel.Information => "bg-info text-dark",
            LogLevel.Warning => "bg-warning text-dark",
            LogLevel.Error => "bg-danger text-white",
            LogLevel.Critical => "bg-danger text-white border border-light",
            _ => "bg-secondary text-white"
        };

        public string LevelIconClass => Level switch
        {
            LogLevel.Trace => "bi-dash-circle",
            LogLevel.Debug => "bi-bug",
            LogLevel.Information => "bi-info-circle-fill",
            LogLevel.Warning => "bi-exclamation-triangle-fill",
            LogLevel.Error => "bi-x-circle-fill",
            LogLevel.Critical => "bi-radioactive",
            _ => "bi-circle"
        };
    }
}
