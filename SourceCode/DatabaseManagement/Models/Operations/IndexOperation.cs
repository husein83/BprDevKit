namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class IndexOperation
    {
        /// <summary>One key column of an index with its sort direction.</summary>
        public class KeyColumn
        {
            public string ColumnName { get; set; } = string.Empty;
            public bool IsDescending { get; set; }
        }

        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;

            /// <summary>Null/empty generates a name (PK_/UX_/IX_ prefix plus table and column names).</summary>
            public string? IndexName { get; set; }

            public List<KeyColumn> Columns { get; set; } = new();
            public List<string> IncludeColumns { get; set; } = new();
            public bool IsUnique { get; set; }
            public bool IsClustered { get; set; }

            /// <summary>Creates a PRIMARY KEY constraint (and its index) instead of a plain index.</summary>
            public bool IsPrimaryKey { get; set; }

            /// <summary>0 keeps the server default.</summary>
            public int FillFactor { get; set; }

            public string? FilterPredicate { get; set; }

            /// <summary>Null/empty means the table's file group.</summary>
            public string? FileGroup { get; set; }
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;
            public string NewIndexName { get; set; } = null!;
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;

            // ---- Loaded data ----
            public string IndexType { get; set; } = string.Empty;

            /// <summary>Key columns as text, e.g. "[A] ASC, [B] DESC".</summary>
            public string Columns { get; set; } = string.Empty;

            public string? IncludeColumns { get; set; }
            public bool IsUnique { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsUniqueConstraint { get; set; }
            public short FillFactor { get; set; }
            public bool PadIndex { get; set; }
            public bool AllowRowLocks { get; set; }
            public bool AllowPageLocks { get; set; }
            public string? FilterPredicate { get; set; }
            public string FileGroup { get; set; } = string.Empty;
        }
    }
}
