using System;

namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// Information about a table index
    /// </summary>
    public class IndexInfo
    {
        public string IndexName { get; set; } = null!;
        public string Columns { get; set; } = null!;
        public string? IncludeColumns { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string IndexType { get; set; } = null!;
        public short FillFactor { get; set; }
        public bool PadIndex { get; set; }
        public bool AllowRowLocks { get; set; }
        public bool AllowPageLocks { get; set; }
        public string? FilterPredicate { get; set; }
        public string FileGroup { get; set; } = null!;
        public string? Description { get; set; }
    }
}