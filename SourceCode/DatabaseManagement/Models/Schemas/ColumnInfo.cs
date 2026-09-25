using System;

namespace BprDevKit.DatabaseManagement.Models
{
    /// <summary>
    /// Information about a table column
    /// </summary>
    public class ColumnInfo
    {
        public string ColumnName { get; set; } = null!;
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
        public int OrdinalPosition { get; set; }
    }
}