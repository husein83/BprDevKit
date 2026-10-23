using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace BprDevKit.Utility.Logging
{
    /// <summary>
    /// Parses BprDevKit rolling log files back into LogEntry objects.
    /// </summary>
    public static class LogFileParser
    {
        private static readonly Regex HeaderRegex = new(
            @"^\[(?<time>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\]\s+\[(?<level>[^\]]+)\]\s+\[(?<source>[^\]]+)\]\s*(?<rest>.*)$",
            RegexOptions.Compiled);

        private static readonly Regex TargetRegex = new(
            @"\[Target:\s*(?<server>[^/]+)/(?<db>[^\]]+)\]\s*",
            RegexOptions.Compiled);

        private static readonly Regex DurationRegex = new(
            @"\((?<ms>\d+)ms\)\s*",
            RegexOptions.Compiled);

        public static List<LogEntry> ParseLogs(string content)
        {
            var entries = new List<LogEntry>();
            if (string.IsNullOrWhiteSpace(content))
                return entries;

            using var reader = new StringReader(content);
            LogEntry? current = null;
            var detailsBuffer = new System.Text.StringBuilder();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = HeaderRegex.Match(line);
                if (match.Success)
                {
                    if (current != null)
                    {
                        if (detailsBuffer.Length > 0)
                            current.Details = detailsBuffer.ToString().TrimEnd();
                        entries.Add(current);
                        detailsBuffer.Clear();
                    }

                    var timeStr = match.Groups["time"].Value;
                    var levelStr = match.Groups["level"].Value.Trim();
                    var sourceStr = match.Groups["source"].Value.Trim();
                    var rest = match.Groups["rest"].Value;

                    DateTime.TryParseExact(timeStr, "yyyy-MM-dd HH:mm:ss.fff",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp);

                    Enum.TryParse<LogLevel>(levelStr, true, out var level);

                    string? server = null;
                    string? db = null;
                    long? duration = null;

                    var targetMatch = TargetRegex.Match(rest);
                    if (targetMatch.Success)
                    {
                        server = targetMatch.Groups["server"].Value.Trim();
                        if (server == "—") server = null;
                        db = targetMatch.Groups["db"].Value.Trim();
                        if (db == "—") db = null;
                        rest = rest.Substring(targetMatch.Length);
                    }

                    var durMatch = DurationRegex.Match(rest);
                    if (durMatch.Success)
                    {
                        if (long.TryParse(durMatch.Groups["ms"].Value, out var ms))
                            duration = ms;
                        rest = rest.Substring(durMatch.Length);
                    }

                    current = new LogEntry
                    {
                        Timestamp = timestamp == default ? DateTime.Now : timestamp,
                        Level = level,
                        Source = sourceStr,
                        ServerName = server,
                        DatabaseName = db,
                        ExecutionTimeMs = duration,
                        Message = rest.Trim()
                    };
                }
                else if (current != null)
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("Details:", StringComparison.OrdinalIgnoreCase))
                    {
                        detailsBuffer.AppendLine(trimmed.Substring("Details:".Length).Trim());
                    }
                    else if (trimmed.StartsWith("Exception:", StringComparison.OrdinalIgnoreCase))
                    {
                        detailsBuffer.AppendLine(trimmed);
                    }
                    else if (trimmed.StartsWith("StackTrace:", StringComparison.OrdinalIgnoreCase))
                    {
                        detailsBuffer.AppendLine(trimmed);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        detailsBuffer.AppendLine(line);
                    }
                }
            }

            if (current != null)
            {
                if (detailsBuffer.Length > 0)
                    current.Details = detailsBuffer.ToString().TrimEnd();
                entries.Add(current);
            }

            return entries;
        }

        public static List<LogEntry> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<LogEntry>();

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, System.Text.Encoding.UTF8);
                return ParseLogs(sr.ReadToEnd());
            }
            catch
            {
                return new List<LogEntry>();
            }
        }
    }
}
