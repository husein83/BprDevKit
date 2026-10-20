using System;
using Microsoft.Data.SqlClient;
using BprDevKit.Utility;

namespace BprDevKit.Config
{
    #region ServerConfiguration
    /// <summary>
    /// Configuration management for database connections and settings
    /// </summary>
    public sealed class ServerConfiguration
    {
        #region Property
        public Guid? Id { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public EntityState EntityState { get; set; } = EntityState.Unchanged;

        // Connection settings
        public ServerSettings Server { get; set; } = new();

        // Database settings
        public DatabaseSettings Database { get; set; } = new();
        #endregion

        #region Public
        /// <summary>
        /// Load all server configurations from JSON file
        /// </summary>
        public static List<ServerConfiguration> LoadAllFromEncryptedFile(bool sendErrorIfConfigFileNotExists = true)
        {
            List<ServerConfiguration>? result = null;

            string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ServerConfiguration, out bool isExistsFile);

            if (!isExistsFile && sendErrorIfConfigFileNotExists)
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }
            else if (isExistsFile)
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = encryptedContent.DecryptContent();

                // Parse the JSON array of configurations
                decryptedContent.TryDevDeserialize(out result);
            }

            return result ?? new();
        }

        /// <summary>
        /// Save all server configurations to JSON file
        /// </summary>
        public static void SaveChanges(ServerConfiguration serverConfiguration)
        {
            var serverDataList = GetVerifiedList(serverConfiguration);
            if (serverDataList.TryDevSerialize(out var jsonContent))
            {
                string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ServerConfiguration, out bool isExistsFile);
                string encryptedContent = jsonContent.EncryptContent();
                File.WriteAllText(filePath, encryptedContent);
            }
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
                configToRemove!.EntityState = EntityState.Deleted;
                SaveChanges(configToRemove);
                result = true;
            }

            return result;
        }
        #endregion

        #region Private
        private static List<ServerConfiguration> GetVerifiedList(ServerConfiguration serverConfiguration)
        {
            var result = LoadAllFromEncryptedFile(false);

            if (serverConfiguration.HasAny() && serverConfiguration.EntityState != EntityState.Unchanged)
            {
                if (result.Any(p => p.Server.ServerName == serverConfiguration.Server.ServerName &&
                                    p.Server.DatabaseServerType == serverConfiguration.Server.DatabaseServerType &&
                                    p.Id != serverConfiguration.Id))
                {
                    throw new Exception($"Server {serverConfiguration.Server.ServerName} already exists");
                }
                if (serverConfiguration.Server.UseWindowsAuth)
                {
                    serverConfiguration.Server.Username = null;
                    serverConfiguration.Server.Password = null;
                }
                if (serverConfiguration.Id.HasValue && serverConfiguration.EntityState == EntityState.Modified) // Modify
                {
                    var previousData = result.FirstOrDefault(p => p.Id == serverConfiguration.Id);
                    if (previousData.HasAny())
                    {
                        serverConfiguration.CreateDate = previousData!.CreateDate;
                        serverConfiguration.LastModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        throw new Exception("Invalid server configure modified");
                    }
                }
                else if (serverConfiguration.EntityState == EntityState.Added) // Insert
                {
                    serverConfiguration.Id = Guid.NewGuid();
                    serverConfiguration.CreateDate = DateTime.UtcNow;
                    serverConfiguration.LastModifiedDate = null;
                }
                switch (serverConfiguration.EntityState)
                {
                    case EntityState.Added:
                        result.Add(serverConfiguration);
                        break;

                    case EntityState.Modified:
                        result.RemoveAll(p => p.Id == serverConfiguration.Id);
                        result.Add(serverConfiguration);
                        break;

                    case EntityState.Deleted:
                        result.RemoveAll(p => p.Id == serverConfiguration.Id);
                        break;
                }
            }
            return result;
        }
        #endregion
    }
    #endregion

    #region ServerConfigurationItem
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
        public bool TrustServerCertificate { get; set; } = false;
        public int ConnectionTimeoutSeconds { get; set; } = BprDevKit.Utility.Constants.DatabaseConnectionTimeoutSeconds;
        public int CommandTimeout { get; set; } = BprDevKit.Utility.Constants.DatabaseCommandTimeoutSeconds;
        public bool Pooling { get; set; } = BprDevKit.Utility.Constants.DefaultPooling;
        public int MinPoolSize { get; set; } = BprDevKit.Utility.Constants.DefaultMinPoolSize;
        public int MaxPoolSize { get; set; } = BprDevKit.Utility.Constants.DefaultMaxPoolSize;
        public bool Encrypt { get; set; } = false;
        private string BuildDataSource()
        {
            var server = string.IsNullOrWhiteSpace(ServerName) ? "." : ServerName.Trim();
            if (!Port.HasValue || Port.Value <= 0)
                return server;

            // SQL Server named instances use a backslash; only append a TCP port to a host.
            if (server.Contains('\\', StringComparison.Ordinal))
                return server;

            return $"{server},{Port.Value}";
        }

        public string BuildConnectionString(string? databaseName = null)
        {
            var targetDatabase = !string.IsNullOrWhiteSpace(databaseName)
                ? databaseName
                : (string.IsNullOrWhiteSpace(DefaultDatabase) ? GetDefaultDatabaseName() : DefaultDatabase);

            return DatabaseServerType switch
            {
                DatabaseServerType.SqlServer => BuildSqlServerConnectionString(targetDatabase),
                DatabaseServerType.PostgreSql => BuildPostgreSqlConnectionString(targetDatabase),
                DatabaseServerType.MySql or DatabaseServerType.MariaDb => BuildMySqlConnectionString(targetDatabase),
                DatabaseServerType.Oracle => BuildOracleConnectionString(targetDatabase),
                DatabaseServerType.Sqlite => BuildSqliteConnectionString(targetDatabase),
                _ => BuildSqlServerConnectionString(targetDatabase)
            };
        }

        private string GetDefaultDatabaseName()
        {
            return DatabaseServerType switch
            {
                DatabaseServerType.SqlServer => "master",
                DatabaseServerType.PostgreSql => "postgres",
                DatabaseServerType.MySql or DatabaseServerType.MariaDb => "mysql",
                DatabaseServerType.Oracle => string.Empty,
                DatabaseServerType.Sqlite => "database.db",
                _ => "master"
            };
        }

        private string BuildSqlServerConnectionString(string database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = BuildDataSource(),
                InitialCatalog = database,
                ConnectTimeout = ConnectionTimeoutSeconds,
                MultipleActiveResultSets = true,
                TrustServerCertificate = TrustServerCertificate,
                Pooling = Pooling,
                Encrypt = Encrypt,
                ApplicationName = BprDevKit.Utility.Constants.ApplicationName,
            };

            if (Pooling)
            {
                if (MinPoolSize >= 0) builder.MinPoolSize = MinPoolSize;
                if (MaxPoolSize > 0) builder.MaxPoolSize = MaxPoolSize;
            }

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

        private string BuildPostgreSqlConnectionString(string database)
        {
            var host = string.IsNullOrWhiteSpace(ServerName) ? "localhost" : ServerName.Trim();
            var port = Port ?? 5432;
            var user = Username ?? "postgres";
            var pass = Password ?? string.Empty;
            var sslMode = Encrypt ? "Require" : "Prefer";
            var poolingStr = Pooling ? $"Pooling=true;Minimum Pool Size={MinPoolSize};Maximum Pool Size={MaxPoolSize};" : "Pooling=false;";
            return $"Host={host};Port={port};Database={database};Username={user};Password={pass};Timeout={ConnectionTimeoutSeconds};Command Timeout={CommandTimeout};SSL Mode={sslMode};{poolingStr}Application Name={BprDevKit.Utility.Constants.ApplicationName};";
        }

        private string BuildMySqlConnectionString(string database)
        {
            var host = string.IsNullOrWhiteSpace(ServerName) ? "localhost" : ServerName.Trim();
            var port = Port ?? 3306;
            var user = Username ?? "root";
            var pass = Password ?? string.Empty;
            var sslMode = Encrypt ? "Required" : "Preferred";
            var poolingStr = Pooling ? $"Pooling=true;Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};" : "Pooling=false;";
            return $"Server={host};Port={port};Database={database};User ID={user};Password={pass};ConnectionTimeout={ConnectionTimeoutSeconds};DefaultCommandTimeout={CommandTimeout};SslMode={sslMode};{poolingStr}";
        }

        private string BuildOracleConnectionString(string database)
        {
            var host = string.IsNullOrWhiteSpace(ServerName) ? "localhost" : ServerName.Trim();
            var port = Port ?? 1521;
            var user = Username ?? string.Empty;
            var pass = Password ?? string.Empty;
            return $"Data Source={host}:{port}/{database};User Id={user};Password={pass};Connection Timeout={ConnectionTimeoutSeconds};";
        }

        private string BuildSqliteConnectionString(string database)
        {
            var path = string.IsNullOrWhiteSpace(database) ? (string.IsNullOrWhiteSpace(ServerName) ? "database.db" : ServerName) : database;
            return $"Data Source={path};";
        }
    }

    /// <summary>
    /// Database settings
    /// </summary>
    public class DatabaseSettings
    {
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
    #endregion
}