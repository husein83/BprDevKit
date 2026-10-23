using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BprDevKit.Utility
{
    /// <summary>
    /// Centralized extension methods for BprDevKit
    /// </summary>
    public static class DevExtensions
    {
        #region DataRow Extensions
        public static bool HasColumn(this DataRow row, string columnName)
        {
            return row?.Table?.Columns.Contains(columnName) == true;
        }

        public static string GetString(this DataRow row, string columnName, string defaultValue = "")
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? defaultValue : Convert.ToString(val) ?? defaultValue;
        }

        public static string? GetNullableString(this DataRow row, string columnName)
        {
            if (row == null || !row.HasColumn(columnName)) return null;
            var val = row[columnName];
            if (val == null || val == DBNull.Value) return null;
            var str = val.ToString();
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        public static bool GetBoolean(this DataRow row, string columnName, bool defaultValue = false)
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue;
            var val = row[columnName];
            if (val == null || val == DBNull.Value) return defaultValue;

            return val switch
            {
                bool b => b,
                int i => i != 0,
                short s => s != 0,
                byte bt => bt != 0,
                string str when bool.TryParse(str, out var parsedBool) => parsedBool,
                string str when int.TryParse(str, out var parsedInt) => parsedInt != 0,
                _ => Convert.ToBoolean(val)
            };
        }

        public static bool? GetNullableBoolean(this DataRow row, string columnName)
        {
            if (row == null || !row.HasColumn(columnName)) return null;
            var val = row[columnName];
            if (val == null || val == DBNull.Value) return null;

            return val switch
            {
                bool b => b,
                int i => i == 1,
                short s => s == 1,
                byte bt => bt == 1,
                string str when bool.TryParse(str, out var parsedBool) => parsedBool,
                string str when int.TryParse(str, out var parsedInt) => parsedInt == 1,
                _ => Convert.ToInt32(val) == 1
            };
        }

        public static int GetInt32(this DataRow row, string columnName, int defaultValue = 0)
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? defaultValue : Convert.ToInt32(val);
        }

        public static int? GetNullableInt32(this DataRow row, string columnName)
        {
            if (row == null || !row.HasColumn(columnName)) return null;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? null : Convert.ToInt32(val);
        }

        public static long GetInt64(this DataRow row, string columnName, long defaultValue = 0L)
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? defaultValue : Convert.ToInt64(val);
        }

        public static short GetInt16(this DataRow row, string columnName, short defaultValue = 0)
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? defaultValue : Convert.ToInt16(val);
        }

        public static short? GetNullableInt16(this DataRow row, string columnName)
        {
            if (row == null || !row.HasColumn(columnName)) return null;
            var val = row[columnName];
            if (val == null || val == DBNull.Value) return null;
            return val switch
            {
                short s => s,
                byte b => (short)b,
                int i when i >= short.MinValue && i <= short.MaxValue => (short)i,
                _ => Convert.ToInt16(val)
            };
        }

        public static DateTime GetDateTime(this DataRow row, string columnName, DateTime? defaultValue = null)
        {
            if (row == null || !row.HasColumn(columnName)) return defaultValue ?? DateTime.MinValue;
            var val = row[columnName];
            return val is DateTime dt ? dt : (val == null || val == DBNull.Value ? (defaultValue ?? DateTime.MinValue) : Convert.ToDateTime(val));
        }

        public static DateTime? GetNullableDateTime(this DataRow row, string columnName)
        {
            if (row == null || !row.HasColumn(columnName)) return null;
            var val = row[columnName];
            if (val == null || val == DBNull.Value) return null;
            return val is DateTime dt ? dt : Convert.ToDateTime(val);
        }
        #endregion

        #region String Extensions
        /// <summary>
        /// Cleans an expression or string to form a safe, alphanumeric SQL identifier part.
        /// </summary>
        public static string ToSafeSqlIdentifier(this string? text, string fallback = "Item", int maxLength = 30)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            var cleaned = Regex.Replace(text, @"[^a-zA-Z0-9_]", "_");
            while (cleaned.Contains("__"))
                cleaned = cleaned.Replace("__", "_");

            cleaned = cleaned.Trim('_');

            if (string.IsNullOrWhiteSpace(cleaned))
                return fallback;

            if (cleaned.Length > maxLength)
                cleaned = cleaned[..maxLength].TrimEnd('_');

            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }

        /// <summary>
        /// Truncates a string with an ellipsis if it exceeds the specified maximum length.
        /// </summary>
        public static string Truncate(this string? text, int maxLength, string suffix = "…")
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            var targetLength = Math.Max(0, maxLength - suffix.Length);
            return text[..targetLength] + suffix;
        }

        /// <summary>
        /// Formats file size in bytes to human-readable string (KB, MB, GB).
        /// </summary>
        public static string FormatFileSize(this long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }
        #endregion

        #region Cryptography Extensions
        /// <summary>
        /// Encrypt content using AES
        /// </summary>
        public static string EncryptContent(this string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(Constants.EncryptionKey));
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypt content using AES
        /// </summary>
        public static string DecryptContent(this string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(Constants.EncryptionKey));

            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        #endregion

        #region Json Extensions
        public static bool TryDevDeserialize<TValue>(this string jsonText, out TValue? result) where TValue : class
        {
            TValue? output = default;
            try
            {
                output = JsonSerializer.Deserialize<TValue>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowDuplicateProperties = false,
                });
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                result = output;
            }
            return result.HasAny();
        }

        public static bool TryDevSerialize<TValue>(this List<TValue> source, out string result) where TValue : class
        {
            result = string.Empty;
            try
            {
                if (source.HasAny())
                {
                    result = JsonSerializer.Serialize(source, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            return !string.IsNullOrWhiteSpace(result);
        }

        public static bool TryDevSerialize<TValue>(this TValue source, out string result) where TValue : class
        {
            result = string.Empty;
            try
            {
                if (source.HasAny())
                {
                    result = JsonSerializer.Serialize(source, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            return !string.IsNullOrWhiteSpace(result);
        }
        #endregion

        #region Collection & Validation Extensions
        public static bool HasAny<TSource>(this TSource? source) where TSource : class
        {
            return source is not null;
        }

        public static bool HasAny<TSource>(this IEnumerable<TSource>? source)
        {
            return source is not null && source.Any();
        }

        public static List<TValue> ToListOrEmpty<TValue>(this TValue source)
        {
            return source is not null ? new List<TValue> { source } : new List<TValue>();
        }

        public static List<TValue> ToListOrEmpty<TValue>(this IEnumerable<TValue>? source)
        {
            return source != null ? source.ToList() : new List<TValue>();
        }
        #endregion

        #region Logging & Formatting Extensions
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
        #endregion
    }
}
