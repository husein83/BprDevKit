namespace BprDevKit.Utility
{
    /// <summary>
    /// Application constants used throughout the database management kit
    /// </summary>
    public static class Constants
    {
        #region Application
        /// <summary>Default version for the database kit</summary>
        public const string Version = "1.0.0";

        /// <summary>Copyright notice</summary>
        public const string Copyright = "© 2026 BprDevKit";

        /// <summary>Application name</summary>
        public const string ApplicationName = "BprDevKit";

        /// <summary>Log folder name</summary>
        public const string LogFolderName = "BprDevLog";

        #endregion

        #region Configuration Setting

        /// <summary>Configuration folder name</summary>
        public const string ConfigFolderName = "BprDevConfig";

        /// <summary>Encrypted configuration file name</summary>
        public const string ConfigFileName = "server-configuration.enc.json";

        /// <summary>Encryption key for encrypt configuration file</summary>
        public const string EncryptionKey = "BprDevKit-Tools-Encryption-Key-2026";

        #endregion

        #region Database Management
        /// <summary>Database connection timeout in seconds</summary>
        public const int DatabaseConnectionTimeoutSeconds = 30;

        /// <summary>Database command timeout in seconds</summary>
        public const int DatabaseCommandTimeoutSeconds = 30;

        /// <summary>Maximum database name length</summary>
        public const int MaxDatabaseNameLength = 128;

        /// <summary>Maximum schema name length</summary>
        public const int MaxSchemaNameLength = 128;

        /// <summary>Maximum table name length</summary>
        public const int MaxTableNameLength = 128;

        /// <summary>Maximum column name length</summary>
        public const int MaxColumnNameLength = 128;

        /// <summary>Maximum index name length</summary>
        public const int MaxIndexNameLength = 128;

        /// <summary>Maximum foreign key name length</summary>
        public const int MaxForeignKeyNameLength = 128;

        /// <summary>Default file group for new tables</summary>
        public const string DefaultFileGroup = "PRIMARY";

        /// <summary>Default schema for new objects</summary>
        public const string DefaultSchema = "dbo";

        /// <summary>Default recovery model for new databases</summary>
        public const string DefaultRecoveryModel = "SIMPLE";

        /// <summary>Default page verify option for new databases</summary>
        public const string DefaultPageVerify = "CHECKSUM";

        /// <summary>Default collation for new databases</summary>
        public const string DefaultCollation = "SQL_Latin1_General_CP1_CI_AS";

        /// <summary>Default initial size for database data files (MB)</summary>
        public const int DefaultInitialDataSizeMB = 8;

        /// <summary>Default initial size for database log files (MB)</summary>
        public const int DefaultInitialLogSizeMB = 8;

        /// <summary>Default file growth for database data files (MB)</summary>
        public const int DefaultFileGrowthMB = 64;

        /// <summary>Default file growth for database log files (MB)</summary>
        public const int DefaultLogFileGrowthMB = 64;

        /// <summary>Default identity seed</summary>
        public const int DefaultIdentitySeed = 1;

        /// <summary>Default identity increment</summary>
        public const int DefaultIdentityIncrement = 1;

        /// <summary>Default fill factor for indexes</summary>
        public const short DefaultFillFactor = 80;

        #endregion
    }
}