using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models
{
    public class ForeignKeyProcedure
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;
            public string RefSchemaName { get; set; } = null!;
            public string RefTableName { get; set; } = null!;
            public string RefColumnName { get; set; } = null!;
            public string OnDelete { get; set; } = "NO ACTION";
            public string OnUpdate { get; set; } = "NO ACTION";
            public bool IsNotForReplication { get; set; } = false;
            public bool Enabled { get; set; } = true;
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;
            public string? RefSchemaName { get; set; }
            public string? RefTableName { get; set; }
            public string? RefColumnName { get; set; }
            public string? OnDelete { get; set; }
            public string? OnUpdate { get; set; }
            public bool? IsNotForReplication { get; set; }
            public bool? Enabled { get; set; }
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string? ColumnName { get; set; }
            public string? RefSchemaName { get; set; }
            public string? RefTableName { get; set; }
            public string? RefColumnName { get; set; }
            public string? FKName { get; set; }
            public bool IgnoreIfNotExists { get; set; } = true;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ColumnName { get; set; } = null!;
        }
    }
}
