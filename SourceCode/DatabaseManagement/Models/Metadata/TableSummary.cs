namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// One row of the table list.
    /// </summary>
    public class TableSummary
    {
        public string SchemaName { get; set; } = null!;
        public string TableName { get; set; } = null!;

        /// <summary>Approximate row count taken from the partition metadata.</summary>
        public long RowCount { get; set; }

        public int ColumnCount { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public string? Description { get; set; }

        public string FullName => $"{SchemaName}.{TableName}";
    }
}
