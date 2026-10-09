using System;

namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// Model for function information
    /// </summary>
    public class FunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public string Definition { get; set; } = string.Empty;
        public bool WithEncryption { get; set; } = true;
    }
}