using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace BprDevKit.Utility
{
    /// <summary>
    /// Utility extension methods
    /// </summary>
    public static class Extension
    {
        #region Log
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

        #region Validation
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
        #endregion

        #region Cryptography
        /// <summary>
        /// Encrypt content using AES
        /// </summary>
        /// <returns>
        /// CipherText
        /// </returns>
        public static string EncryptContent(this string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(BprDevKit.Utility.Constants.EncryptionKey));
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
        /// <returns>
        /// PlainText
        /// </returns>
        public static string DecryptContent(this string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(BprDevKit.Utility.Constants.EncryptionKey));

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

        #region Collections
        public static List<TValue> ToListOrEmpty<TValue>(this TValue source)
        {
            return source is not null ? new List<TValue> { source } : new List<TValue>();
        }
        public static List<TValue> ToListOrEmpty<TValue>(this IEnumerable<TValue> source)
        {
            return source is not null && source.Count() > 0 && source.Any() ? source.ToList() : new List<TValue>();
        }
        #endregion

        #region Json
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
    }
}