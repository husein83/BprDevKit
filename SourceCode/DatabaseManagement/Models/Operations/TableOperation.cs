using BprDevKit.DatabaseManagement.Models.Metadata;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    /// <summary>
    /// Everything about tables: Create / Alter / Drop describe a change, Information describes a table
    /// (it identifies the table in a request and carries the loaded data in a result).
    /// </summary>
    public class TableOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;

            /// <summary>Null/empty means the database default file group.</summary>
            public string? FileGroup { get; set; }

            public string? Description { get; set; }
            public List<ColumnDefinition> Columns { get; set; } = new();
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;

            /// <summary>Null/empty or equal to <see cref="TableName"/> means "do not rename".</summary>
            public string? NewTableName { get; set; }

            /// <summary>Null means "unchanged", an empty string removes the description.</summary>
            public string? Description { get; set; }

            public bool IsRename =>
                !string.IsNullOrWhiteSpace(NewTableName)
                && !string.Equals(NewTableName, TableName, StringComparison.Ordinal);

            public bool ChangesDescription => Description is not null;
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public bool IgnoreIfNotExists { get; set; } = true;
            public bool ForceDropDependent { get; set; } = false;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ObjectType { get; set; } = Constants.ObjectTypes.Table;

            // ---- Loaded data (a table list fills only the first block, a single table fills everything) ----
            public long RowCount { get; set; }
            public int ColumnCount { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime ModifyDate { get; set; }
            public string? Description { get; set; }
            public string? FileGroup { get; set; }

            public List<ColumnOperation.Information> Columns { get; set; } = new();
            public List<IndexOperation.Information> Indexes { get; set; } = new();
            public List<ForeignKeyOperation.Information> ForeignKeys { get; set; } = new();
            public List<CheckConstraintOperation.Information> CheckConstraints { get; set; } = new();

            public string FullName => $"{SchemaName}.{TableName}";
        }
    }
}
