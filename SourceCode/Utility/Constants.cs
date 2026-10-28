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

        /// <summary>Encrypted application configuration file name</summary>
        public const string EncryptedApplicationConfigFileName = "application-configuration.enc.json";

        /// <summary>Encrypted server configuration file name</summary>
        public const string EncryptedServerConfigFileName = "server-configuration.enc.json";

        /// <summary>Encrypted model generator configuration file name</summary>
        public const string EncryptedModelGeneratorConfigFileName = "model-generator-configuration.enc.json";

        /// <summary>Encryption key for encrypt configuration file</summary>
        public const string EncryptionKey = "BprDevKit-Tools-Encryption-Key-2026";

        #endregion

        #region Database Management
        /// <summary>Database connection timeout in seconds</summary>
        public const int DatabaseConnectionTimeoutSeconds = 30;

        /// <summary>Database command timeout in seconds</summary>
        public const int DatabaseCommandTimeoutSeconds = 30;

        /// <summary>Default connection pooling setting</summary>
        public const bool DefaultPooling = true;

        /// <summary>Default minimum connection pool size</summary>
        public const int DefaultMinPoolSize = 0;

        /// <summary>Default maximum connection pool size</summary>
        public const int DefaultMaxPoolSize = 100;

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

        /// <summary>Default initial size for database data files (MB)</summary>
        public const int DefaultInitialDataSizeMB = 8;

        /// <summary>Default initial size for database log files (MB)</summary>
        public const int DefaultInitialLogSizeMB = 8;

        /// <summary>Default file growth for database data files (MB)</summary>
        public const int DefaultFileGrowthMB = 64;

        /// <summary>Default file growth for database log files (MB)</summary>
        public const int DefaultLogFileGrowthMB = 64;

        #endregion

        #region Database & DataModel Object Types
        /// <summary>
        /// Standard object type identifiers for database objects and generated data models
        /// </summary>
        public static class ObjectTypes
        {
            public const string Table = "Table";
            public const string View = "View";
            public const string StoredProcedure = "StoredProcedure";
            public const string Procedure = "Procedure";
            public const string ProcedureParameters = "ProcedureParameters";
            public const string Function = "Function";
            public const string FunctionParameters = "FunctionParameters";
            public const string Trigger = "Trigger";
            public const string UserDefinedType = "UserDefinedType";
            public const string Index = "Index";
            public const string CheckConstraint = "CheckConstraint";
            public const string ForeignKey = "ForeignKey";
        }

        /// <summary>
        /// Standard naming conventions and suffixes for generated data models
        /// </summary>
        public static class ModelGeneratorDefaults
        {
            public const string ResultSuffix = "Result";
            public const string ParametersSuffix = "Parameters";
            public const string DefaultSchema = "dbo";
        }

        /// <summary>
        /// Tab identifier constants for DataModelGenerator UI
        /// </summary>
        public static class DataModelTabs
        {
            public const string Tables = "tables";
            public const string Views = "views";
            public const string Procedures = "procedures";
            public const string Functions = "functions";
        }
        #endregion
    }
}