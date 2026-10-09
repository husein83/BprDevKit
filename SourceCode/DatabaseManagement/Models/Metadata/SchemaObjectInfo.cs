namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// An object that lives inside a schema (table, view, procedure, type, ...).
    /// </summary>
    public class SchemaObjectInfo
    {
        public string ObjectType { get; set; } = null!;
        public string ObjectName { get; set; } = null!;

        /// <summary>User-defined types do not expose a creation date in SQL Server.</summary>
        public DateTime? CreateDate { get; set; }
    }
}
