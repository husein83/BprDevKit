using System;

namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// Information about a foreign key constraint
    /// </summary>
    public class ForeignKeyInfo
    {
        public string FKName { get; set; } = null!;
        public string SchemaName { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string RefSchemaName { get; set; } = null!;
        public string RefTableName { get; set; } = null!;
        public string RefColumnName { get; set; } = null!;
        public string OnDelete { get; set; } = "NO ACTION";
        public string OnUpdate { get; set; } = "NO ACTION";
        public bool IsNotForReplication { get; set; }
        public bool Enabled { get; set; }
        public string? Description { get; set; }
    }
}