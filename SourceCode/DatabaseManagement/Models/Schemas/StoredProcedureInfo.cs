using System;

namespace BprDevKit.DatabaseManagement.Models
{
    /// <summary>
    /// Information about a stored procedure
    /// </summary>
    public class StoredProcedureInfo
    {
        public string Name { get; set; } = null!;
        public string Schema { get; set; } = null!;
        public string Database { get; set; } = null!;
        public string Definition { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public string? Description { get; set; }
        public bool WithEncryption { get; set; } = true;
    }
}