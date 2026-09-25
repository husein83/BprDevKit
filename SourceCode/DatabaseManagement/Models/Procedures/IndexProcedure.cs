using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models
{
    public class IndexProcedure
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string? IndexName { get; set; }
            public string Columns { get; set; } = null!;
            public string? IncludeColumns { get; set; }
            public bool IsUnique { get; set; } = false;
            public string IndexType { get; set; } = "NONCLUSTERED";
            public short FillFactor { get; set; } = 80;
            public bool PadIndex { get; set; } = true;
            public bool AllowRowLocks { get; set; } = true;
            public bool AllowPageLocks { get; set; } = true;
            public string? FilterPredicate { get; set; }
            public string FileGroup { get; set; } = "PRIMARY";
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;
            public string? Columns { get; set; }
            public string? IncludeColumns { get; set; }
            public bool? IsUnique { get; set; }
            public string? IndexType { get; set; }
            public short? FillFactor { get; set; }
            public bool? PadIndex { get; set; }
            public bool? AllowRowLocks { get; set; }
            public bool? AllowPageLocks { get; set; }
            public string? FilterPredicate { get; set; }
            public string? FileGroup { get; set; }
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;
            public bool IgnoreIfNotExists { get; set; } = true;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string IndexName { get; set; } = null!;
        }
    }
}
