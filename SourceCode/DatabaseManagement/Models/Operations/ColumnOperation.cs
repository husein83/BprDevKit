using BprDevKit.DatabaseManagement.Models.Metadata;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class ColumnOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public ColumnDefinition Column { get; set; } = new();
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;

            /// <summary>Null/empty or equal to <see cref="ColumnName"/> means "do not rename".</summary>
            public string? NewColumnName { get; set; }

            /// <summary>
            /// The complete new data type and nullability (SQL Server's ALTER COLUMN always restates both).
            /// Null means "keep the current type". Identity and primary key flags are ignored here.
            /// </summary>
            public ColumnDefinition? Definition { get; set; }

            /// <summary>Null means "unchanged", an empty string removes the default, anything else replaces it.</summary>
            public string? DefaultValue { get; set; }

            /// <summary>Null means "unchanged", an empty string removes the description.</summary>
            public string? Description { get; set; }

            public bool IsRename =>
                !string.IsNullOrWhiteSpace(NewColumnName)
                && !string.Equals(NewColumnName, ColumnName, StringComparison.Ordinal);

            public bool ChangesDefinition => Definition is not null;
            public bool ChangesDefault => DefaultValue is not null;
            public bool ChangesDescription => Description is not null;
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;

            // ---- Loaded data ----
            public int OrdinalPosition { get; set; }
            public string DataType { get; set; } = null!;
            public int? Length { get; set; }
            public int? Precision { get; set; }
            public int? Scale { get; set; }
            public bool IsNullable { get; set; }
            public string? DefaultValue { get; set; }
            public bool IsIdentity { get; set; }
            public int IdentitySeed { get; set; }
            public int IdentityIncrement { get; set; }
            public bool IsPrimaryKey { get; set; }
            public string? Collation { get; set; }
            public string? ComputedFormula { get; set; }
            public bool IsPersisted { get; set; }
            public string? Description { get; set; }
        }
    }
}
