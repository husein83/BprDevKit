using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Config
{
    /// <summary>
    /// Configuration management for database connections and settings
    /// </summary>
    public class ServerConfiguration
    {
        public Guid? Id { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        // Connection settings
        public ServerSettings Server { get; set; } = new();

        // Database settings
        public DatabaseSettings Database { get; set; } = new();

        // Security settings
        public SecuritySettings Security { get; set; } = new();

        // Logging settings
        public LoggingSettings Logging { get; set; } = new();

        /// <summary>
        /// Load all server configurations from encrypted JSON file
        /// </summary>
        public static List<ServerConfiguration> LoadAllFromEncryptedFile(bool sendErrorIfConfigFileNoeExists = true)
        {
            List<ServerConfiguration> result = new List<ServerConfiguration>();

            string filePath = GetServerConfigPath();
            bool isExistsFile = File.Exists(filePath);

            if (!isExistsFile && sendErrorIfConfigFileNoeExists)
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }
            else if (isExistsFile)
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = DecryptContent(encryptedContent);

                // Parse the JSON array of configurations
                result = JsonSerializer.Deserialize<List<ServerConfiguration>>(decryptedContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ServerConfiguration>();
            }

            return result;
        }

        /// <summary>
        /// Save all server configurations to encrypted JSON file
        /// </summary>
        public static void SaveAllToEncryptedFile(List<ServerConfiguration> configurations)
        {
            VerifyData(configurations);

            string jsonContent = JsonSerializer.Serialize(configurations, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            string filePath = GetServerConfigPath();
            string encryptedContent = EncryptContent(jsonContent);
            File.WriteAllText(filePath, encryptedContent);
        }

        public static bool TryDeleteServerById(Guid id)
        {
            bool result = false;

            // Load all server configurations
            var allConfigs = LoadAllFromEncryptedFile();

            // Find and remove the specific server configuration
            var configToRemove = allConfigs.FirstOrDefault(c => c.Id == id);

            if (configToRemove.HasAny())
            {
                allConfigs.Remove(configToRemove!);

                // Save the updated list back to the file
                SaveAllToEncryptedFile(allConfigs);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Get server configuration JSON file path
        /// </summary>
        public static string GetServerConfigPath()
        {
            string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigFolderName);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            string filePath = Path.Combine(configDir, Constants.ConfigFileName);

            return filePath;
        }

        /// <summary>
        /// Encrypt content using AES
        /// </summary>
        private static string EncryptContent(string plainText)
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
        private static string DecryptContent(string cipherText)
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

        private static void VerifyData(List<ServerConfiguration> serverConfigurations)
        {
            if (serverConfigurations.HasAny())
            {
                var savedData = LoadAllFromEncryptedFile(false);

                foreach (var item in serverConfigurations)
                {
                    if (savedData.Any(p => p.Server.ServerName == item.Server.ServerName &&
                                           p.Server.DatabaseServerType == item.Server.DatabaseServerType &&
                                           p.Id != item.Id))
                    {
                        throw new Exception($"Server {item.Server.ServerName} already exists");
                    }
                    if (item.Server.UseWindowsAuth)
                    {
                        item.Server.Username = null;
                        item.Server.Password = null;
                    }
                    if (item.Id.HasValue) // Modify
                    {
                        var previousData = savedData.FirstOrDefault(p => p.Id == item.Id);
                        if (previousData.HasAny())
                        {
                            item.CreateDate = previousData!.CreateDate;
                            item.LastModifiedDate = DateTime.UtcNow;
                        }
                        else
                        {
                            throw new Exception("Invalid server configure modified");
                        }
                    }
                    else // Insert
                    {
                        item.Id = Guid.NewGuid();
                        item.CreateDate = DateTime.UtcNow;
                        item.LastModifiedDate = null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Connection settings
    /// </summary>
    public class ServerSettings
    {
        public string ServerName { get; set; } = "localhost";
        public string? Alias { get; set; }
        public string DefaultDatabase { get; set; } = "master";
        public DatabaseServerType DatabaseServerType { get; set; }
        public bool UseWindowsAuth { get; set; } = false;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; }
        public bool TrustServerCertificate { get; set; } = true;
        public int ConnectionTimeoutSeconds { get; set; } = BprDevKit.Utility.Constants.DatabaseConnectionTimeoutSeconds;
        public int CommandTimeout { get; set; } = BprDevKit.Utility.Constants.DatabaseCommandTimeoutSeconds;
        public string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrWhiteSpace(ServerName) ? "." : ServerName,
                ConnectTimeout = ConnectionTimeoutSeconds,
                MultipleActiveResultSets = true,
                TrustServerCertificate = TrustServerCertificate,
            };

            if (UseWindowsAuth)
                builder.IntegratedSecurity = true;
            else
            {
                if (string.IsNullOrEmpty(Username))
                    throw new InvalidOperationException("Username is required for SQL Server authentication.");

                builder.UserID = Username;
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Database settings
    /// </summary>
    public class DatabaseSettings
    {
        public string DefaultSchema { get; set; } = BprDevKit.Utility.Constants.DefaultSchema;
        public string DefaultFileGroup { get; set; } = BprDevKit.Utility.Constants.DefaultFileGroup;
        public int MaxDatabaseNameLength { get; set; } = BprDevKit.Utility.Constants.MaxDatabaseNameLength;
        public int MaxSchemaNameLength { get; set; } = BprDevKit.Utility.Constants.MaxSchemaNameLength;
        public int MaxTableNameLength { get; set; } = BprDevKit.Utility.Constants.MaxTableNameLength;
        public int MaxColumnNameLength { get; set; } = BprDevKit.Utility.Constants.MaxColumnNameLength;
        public int MaxIndexNameLength { get; set; } = BprDevKit.Utility.Constants.MaxIndexNameLength;
        public int MaxForeignKeyNameLength { get; set; } = BprDevKit.Utility.Constants.MaxForeignKeyNameLength;
        public int DefaultDataFileSizeMB { get; set; } = BprDevKit.Utility.Constants.DefaultInitialDataSizeMB;
        public int DefaultLogFileSizeMB { get; set; } = BprDevKit.Utility.Constants.DefaultInitialLogSizeMB;
        public int DefaultFileGrowthMB { get; set; } = BprDevKit.Utility.Constants.DefaultFileGrowthMB;
        public int DefaultLogFileGrowthMB { get; set; } = BprDevKit.Utility.Constants.DefaultLogFileGrowthMB;
    }

    /// <summary>
    /// Security settings
    /// </summary>
    public class SecuritySettings
    {
        public bool EnableEncryption { get; set; } = true;
        public string ConfigFolder => BprDevKit.Utility.Constants.ConfigFolderName;
        public string DatabaseConfigFileName => BprDevKit.Utility.Constants.ConfigFileName;
        public string EncryptionKey => BprDevKit.Utility.Constants.EncryptionKey;
    }

    /// <summary>
    /// Logging settings
    /// </summary>
    public class LoggingSettings
    {
        public BprDevKit.Utility.LogLevel LogLevel { get; set; } = BprDevKit.Utility.LogLevel.Information;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = false;
        public string LogFolder => BprDevKit.Utility.Constants.LogFolderName;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int RetentionDays { get; set; } = 7;
    }
}