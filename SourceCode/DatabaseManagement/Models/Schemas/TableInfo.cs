using System;

namespace BprDevKit.DatabaseManagement.Models
{
    /// <summary>
    /// Information about a database table
    /// </summary>
    public class TableInfo
    {
        public string DatabaseName { get; set; } = null!;
        public string SchemaName { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public string FileGroup { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new();
        public List<IndexInfo> Indexes { get; set; } = new();
        public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    }
}