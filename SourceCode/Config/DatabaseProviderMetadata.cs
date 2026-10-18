using System;
using System.Collections.Generic;
using System.Linq;
using BprDevKit.Utility;

namespace BprDevKit.Config
{
    /// <summary>
    /// Metadata and capabilities for each supported database provider.
    /// </summary>
    public sealed record ProviderCapabilities(
        DatabaseServerType ServerType,
        string DisplayName,
        int DefaultPort,
        string DefaultDatabase,
        string DefaultSchema,
        string DefaultUsername,
        string DefaultCollation,
        bool SupportsWindowsAuth,
        bool SupportsPort,
        bool SupportsCredentials,
        bool SupportsSchemas,
        bool SupportsFileGroups,
        string BatchSeparator,
        IReadOnlyList<string> SystemDatabases
    );

    /// <summary>
    /// Helper giving provider-specific defaults, capabilities and smart configuration.
    /// </summary>
    public static class DatabaseProviderMetadata
    {
        private static readonly Dictionary<DatabaseServerType, ProviderCapabilities> Capabilities = new()
        {
            [DatabaseServerType.SqlServer] = new ProviderCapabilities(
                ServerType: DatabaseServerType.SqlServer,
                DisplayName: "Microsoft SQL Server",
                DefaultPort: 1433,
                DefaultDatabase: "master",
                DefaultSchema: "dbo",
                DefaultUsername: "sa",
                DefaultCollation: "SQL_Latin1_General_CP1_CI_AS",
                SupportsWindowsAuth: true,
                SupportsPort: true,
                SupportsCredentials: true,
                SupportsSchemas: true,
                SupportsFileGroups: true,
                BatchSeparator: "GO",
                SystemDatabases: new[] { "master", "tempdb", "model", "msdb" }
            ),
            [DatabaseServerType.PostgreSql] = new ProviderCapabilities(
                ServerType: DatabaseServerType.PostgreSql,
                DisplayName: "PostgreSQL",
                DefaultPort: 5432,
                DefaultDatabase: "postgres",
                DefaultSchema: "public",
                DefaultUsername: "postgres",
                DefaultCollation: "en_US.utf8",
                SupportsWindowsAuth: false,
                SupportsPort: true,
                SupportsCredentials: true,
                SupportsSchemas: true,
                SupportsFileGroups: false,
                BatchSeparator: ";",
                SystemDatabases: new[] { "postgres", "template0", "template1" }
            ),
            [DatabaseServerType.MySql] = new ProviderCapabilities(
                ServerType: DatabaseServerType.MySql,
                DisplayName: "MySQL",
                DefaultPort: 3306,
                DefaultDatabase: "mysql",
                DefaultSchema: string.Empty,
                DefaultUsername: "root",
                DefaultCollation: "utf8mb4_unicode_ci",
                SupportsWindowsAuth: false,
                SupportsPort: true,
                SupportsCredentials: true,
                SupportsSchemas: false,
                SupportsFileGroups: false,
                BatchSeparator: ";",
                SystemDatabases: new[] { "mysql", "information_schema", "performance_schema", "sys" }
            ),
            [DatabaseServerType.MariaDb] = new ProviderCapabilities(
                ServerType: DatabaseServerType.MariaDb,
                DisplayName: "MariaDB",
                DefaultPort: 3306,
                DefaultDatabase: "mysql",
                DefaultSchema: string.Empty,
                DefaultUsername: "root",
                DefaultCollation: "utf8mb4_unicode_ci",
                SupportsWindowsAuth: false,
                SupportsPort: true,
                SupportsCredentials: true,
                SupportsSchemas: false,
                SupportsFileGroups: false,
                BatchSeparator: ";",
                SystemDatabases: new[] { "mysql", "information_schema", "performance_schema", "sys" }
            ),
            [DatabaseServerType.Oracle] = new ProviderCapabilities(
                ServerType: DatabaseServerType.Oracle,
                DisplayName: "Oracle Database",
                DefaultPort: 1521,
                DefaultDatabase: "ORCL",
                DefaultSchema: string.Empty,
                DefaultUsername: "SYSTEM",
                DefaultCollation: "AL32UTF8",
                SupportsWindowsAuth: false,
                SupportsPort: true,
                SupportsCredentials: true,
                SupportsSchemas: true,
                SupportsFileGroups: false,
                BatchSeparator: "/",
                SystemDatabases: new[] { "SYS", "SYSTEM" }
            ),
            [DatabaseServerType.Sqlite] = new ProviderCapabilities(
                ServerType: DatabaseServerType.Sqlite,
                DisplayName: "SQLite",
                DefaultPort: 0,
                DefaultDatabase: "database.db",
                DefaultSchema: "main",
                DefaultUsername: string.Empty,
                DefaultCollation: "BINARY",
                SupportsWindowsAuth: false,
                SupportsPort: false,
                SupportsCredentials: false,
                SupportsSchemas: false,
                SupportsFileGroups: false,
                BatchSeparator: ";",
                SystemDatabases: Array.Empty<string>()
            )
        };

        public static ProviderCapabilities GetCapabilities(DatabaseServerType serverType)
        {
            return Capabilities.TryGetValue(serverType, out var caps)
                ? caps
                : Capabilities[DatabaseServerType.SqlServer];
        }

        public static int GetDefaultPort(DatabaseServerType serverType) => GetCapabilities(serverType).DefaultPort;

        public static string GetDefaultDatabase(DatabaseServerType serverType) => GetCapabilities(serverType).DefaultDatabase;

        public static string GetDefaultSchema(DatabaseServerType serverType) => GetCapabilities(serverType).DefaultSchema;

        public static string GetDefaultCollation(DatabaseServerType serverType) => GetCapabilities(serverType).DefaultCollation;

        public static bool SupportsWindowsAuth(DatabaseServerType serverType) => GetCapabilities(serverType).SupportsWindowsAuth;

        public static bool SupportsPort(DatabaseServerType serverType) => GetCapabilities(serverType).SupportsPort;

        public static bool SupportsCredentials(DatabaseServerType serverType) => GetCapabilities(serverType).SupportsCredentials;

        public static bool IsSystemDatabase(DatabaseServerType serverType, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) return false;
            var caps = GetCapabilities(serverType);
            return caps.SystemDatabases.Contains(databaseName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adjusts ServerSettings when the provider type changes, preserving custom values while applying sensible defaults.
        /// </summary>
        public static void ApplyDefaultsOnTypeChange(ServerSettings settings, DatabaseServerType newType)
        {
            var oldCaps = GetCapabilities(settings.DatabaseServerType);
            var newCaps = GetCapabilities(newType);

            settings.DatabaseServerType = newType;

            // If Port is default or unset for old provider, switch to new provider's default port
            if (!settings.Port.HasValue || settings.Port == oldCaps.DefaultPort)
            {
                settings.Port = newCaps.SupportsPort ? newCaps.DefaultPort : null;
            }

            // If DefaultDatabase is default or empty for old provider, switch to new provider's default database
            if (string.IsNullOrWhiteSpace(settings.DefaultDatabase) ||
                string.Equals(settings.DefaultDatabase, oldCaps.DefaultDatabase, StringComparison.OrdinalIgnoreCase))
            {
                settings.DefaultDatabase = newCaps.DefaultDatabase;
            }

            // If new provider does not support Windows Auth, disable it
            if (!newCaps.SupportsWindowsAuth)
            {
                settings.UseWindowsAuth = false;
            }

            // If username is empty or default of old, apply new default
            if (string.IsNullOrWhiteSpace(settings.Username) ||
                string.Equals(settings.Username, oldCaps.DefaultUsername, StringComparison.OrdinalIgnoreCase))
            {
                settings.Username = newCaps.DefaultUsername;
            }
        }
    }
}
