namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class ForeignKeyOperation
    {
        public static readonly IReadOnlyList<string> ReferentialActions = new[] { "NO ACTION", "CASCADE", "SET NULL", "SET DEFAULT" };

        /// <summary>One column pair of a foreign key (local column to referenced column).</summary>
        public class ColumnMapping
        {
            public string ColumnName { get; set; } = string.Empty;
            public string RefColumnName { get; set; } = string.Empty;
        }

        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;

            /// <summary>Null/empty generates FK_{table}_{referenced table}_{columns}.</summary>
            public string? FKName { get; set; }

            public string RefSchemaName { get; set; } = null!;
            public string RefTableName { get; set; } = null!;
            public List<ColumnMapping> Columns { get; set; } = new();
            public string OnDelete { get; set; } = "NO ACTION";
            public string OnUpdate { get; set; } = "NO ACTION";
            public bool IsNotForReplication { get; set; }

            /// <summary>False creates the constraint disabled and without checking the existing rows.</summary>
            public bool Enabled { get; set; } = true;
        }

        /// <summary>Enables (re-validating the existing rows) or disables a foreign key.</summary>
        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string FKName { get; set; } = null!;
            public bool Enabled { get; set; }
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string FKName { get; set; } = null!;
        }

        public class Information
        {
            /// <summary>Schema and table that own the foreign key (the "child" side).</summary>
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;

            /// <summary>Local column(s); several columns are separated by ", ".</summary>
            public string ColumnName { get; set; } = null!;

            // ---- Loaded data ----
            public string FKName { get; set; } = string.Empty;
            public string RefSchemaName { get; set; } = string.Empty;
            public string RefTableName { get; set; } = string.Empty;
            public string RefColumnName { get; set; } = string.Empty;
            public string OnDelete { get; set; } = "NO ACTION";
            public string OnUpdate { get; set; } = "NO ACTION";
            public bool IsNotForReplication { get; set; }
            public bool Enabled { get; set; }
            public string? Description { get; set; }
        }
    }
}
