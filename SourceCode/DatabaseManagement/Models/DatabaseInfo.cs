using System;
using System.Collections.Generic;
using System.Data;

namespace BprDevKit.DatabaseManagement.Models
{
    /// <summary>
    /// Collection of database objects for script generation
    /// </summary>
    public class DatabaseInfo
    {
        public List<SchemaInfo> Schemas { get; set; } = new();
        public List<TableInfo> Tables { get; set; } = new();
        public List<ColumnInfo> Columns { get; set; } = new();
        public List<IndexInfo> Indexes { get; set; } = new();
        public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
        public List<StoredProcedureInfo> StoredProcedures { get; set; } = new();
        public List<ViewInfo> Views { get; set; } = new();
        public List<FunctionInfo> Functions { get; set; } = new();
    }
}