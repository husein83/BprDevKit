namespace BprDevKit.DatabaseManagement.Models.Operations
{
    /// <summary>
    /// Request models for schema operations. Every operation targets one schema inside one database.
    /// </summary>
    public class SchemaOperation
    {
        /// <summary>
        /// Built-in schemas (and the schemas of the fixed database roles) that SQL Server owns.
        /// They can neither be created nor renamed, re-owned or dropped by this tool.
        /// </summary>
        private static readonly HashSet<string> ProtectedSchemas = new(StringComparer.OrdinalIgnoreCase)
        {
            "dbo", "guest", "INFORMATION_SCHEMA", "sys",
            "db_owner", "db_accessadmin", "db_securityadmin", "db_ddladmin", "db_backupoperator",
            "db_datareader", "db_datawriter", "db_denydatareader", "db_denydatawriter"
        };

        public static bool IsProtected(string? schemaName)
        {
            return !string.IsNullOrWhiteSpace(schemaName) && ProtectedSchemas.Contains(schemaName);
        }

        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;

            /// <summary>Database principal that owns the schema. Null/empty means the executing user.</summary>
            public string? Owner { get; set; }
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;

            /// <summary>Null/empty or equal to <see cref="SchemaName"/> means "do not rename".</summary>
            public string? NewSchemaName { get; set; }

            /// <summary>Null/empty means "keep the current owner".</summary>
            public string? Owner { get; set; }

            public bool IsRename =>
                !string.IsNullOrWhiteSpace(NewSchemaName)
                && !string.Equals(NewSchemaName, SchemaName, StringComparison.Ordinal);

            public bool ChangesOwner => !string.IsNullOrWhiteSpace(Owner);
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
        }
    }
}
