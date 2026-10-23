using System;
using System.IO;
using System.Text;
using BprDevKit.Config;

namespace BprDevKit.Utility.Logging
{
    /// <summary>
    /// Thread-safe rolling file logger with size-based rotation and retention cleanup
    /// </summary>
    public class RollingFileLogger
    {
        private readonly object _lock = new();
        private DateTime _lastCleanupDate = DateTime.MinValue;

        public string GetLogDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folderName = ApplicationConfiguration.Config.Logging.LogFolder;
            if (string.IsNullOrWhiteSpace(folderName)) folderName = Constants.LogFolderName;
            string logDir = Path.Combine(baseDir, folderName);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            return logDir;
        }

        public List<string> GetAvailableLogFiles()
        {
            var list = new List<string>();
            try
            {
                var dir = GetLogDirectory();
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "bprdevkit-*.log")
                        .Select(Path.GetFileName)
                        .Where(f => !string.IsNullOrEmpty(f))
                        .OrderByDescending(f => f)
                        .ToList();
                    list.AddRange(files!);
                }
            }
            catch
            {
            }
            return list;
        }

        public string? GetTodayLogFilePath()
        {
            try
            {
                var dir = GetLogDirectory();
                var todayPath = Path.Combine(dir, $"bprdevkit-{DateTime.Now:yyyy-MM-dd}.log");
                if (File.Exists(todayPath))
                    return todayPath;
            }
            catch
            {
            }
            return null;
        }

        public void WriteLog(LogEntry entry, LoggingSettings settings)
        {
            if (!settings.EnableFileLogging) return;

            lock (_lock)
            {
                try
                {
                    string logDir = GetLogDirectory();
                    CleanupOldLogs(logDir, settings.RetentionDays);

                    string targetFile = GetCurrentLogFilePath(logDir, settings.MaxLogFileSizeMB);

                    var sb = new StringBuilder();
                    sb.Append($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
                    sb.Append($"[{entry.Level,-11}] ");
                    sb.Append($"[{entry.Source}] ");

                    if (!string.IsNullOrWhiteSpace(entry.ServerName) || !string.IsNullOrWhiteSpace(entry.DatabaseName))
                    {
                        sb.Append($"[Target: {entry.ServerName ?? "—"}/{entry.DatabaseName ?? "—"}] ");
                    }

                    if (entry.ExecutionTimeMs.HasValue)
                    {
                        sb.Append($"({entry.ExecutionTimeMs.Value}ms) ");
                    }

                    sb.AppendLine(entry.Message);

                    if (!string.IsNullOrWhiteSpace(entry.Details))
                    {
                        sb.AppendLine($"  Details: {entry.Details.Replace(Environment.NewLine, Environment.NewLine + "  ")}");
                    }

                    if (entry.Exception != null)
                    {
                        sb.AppendLine($"  Exception: {entry.Exception.GetType().FullName}: {entry.Exception.Message}");
                        if (!string.IsNullOrWhiteSpace(entry.Exception.StackTrace))
                        {
                            sb.AppendLine($"  StackTrace: {entry.Exception.StackTrace.Replace(Environment.NewLine, Environment.NewLine + "  ")}");
                        }
                    }

                    File.AppendAllText(targetFile, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // Logging failure should never crash the main application
                }
            }
        }

        private string GetCurrentLogFilePath(string logDir, int maxFileSizeMb)
        {
            long maxBytes = Math.Max(1, maxFileSizeMb) * 1024L * 1024L;
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            string baseFileName = $"bprdevkit-{dateStr}";

            string currentPath = Path.Combine(logDir, $"{baseFileName}.log");
            if (!File.Exists(currentPath))
                return currentPath;

            var fileInfo = new FileInfo(currentPath);
            if (fileInfo.Length < maxBytes)
                return currentPath;

            // Rolling: find next index .1, .2, ...
            int index = 1;
            while (true)
            {
                string candidate = Path.Combine(logDir, $"{baseFileName}.{index}.log");
                if (!File.Exists(candidate))
                    return candidate;

                var candidateInfo = new FileInfo(candidate);
                if (candidateInfo.Length < maxBytes)
                    return candidate;

                index++;
            }
        }

        private void CleanupOldLogs(string logDir, int retentionDays)
        {
            if (retentionDays <= 0) return;
            if ((DateTime.Now - _lastCleanupDate).TotalHours < 6) return;

            _lastCleanupDate = DateTime.Now;

            try
            {
                var thresholdDate = DateTime.Now.AddDays(-retentionDays);
                var files = Directory.GetFiles(logDir, "bprdevkit-*.log");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < thresholdDate)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch
            {
                // Silently ignore cleanup errors
            }
        }
    }
}
