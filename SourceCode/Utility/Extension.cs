using System;
using System.IO;

namespace BprDevKit.Utility
{
    /// <summary>
    /// Utility extension methods
    /// </summary>
    public static class Extension
    {
        /// <summary>
        /// Gets the log file name based on log level and date
        /// </summary>
        public static string GetLogFileName(this LogLevel logLevel, DateTime dateTime, string? category = null)
        {
            string template = "{0}Log--{1}";
            if (!string.IsNullOrWhiteSpace(category))
            {
                template = template + $"--{category}";
            }

            return string.Format(template, logLevel.ToString(), dateTime.ToString("yyyyMMdd"));
        }

        public static bool HasAny<TSource>(this TSource? source) where TSource : class
        {
            bool result = false;

            result = source is not null;

            return result;
        }

        public static bool HasAny<TSource>(this IEnumerable<TSource>? source)
        {
            bool result = false;

            result = source is not null && source.Any() && source.Count() > 0;

            return result;
        }
    }
}