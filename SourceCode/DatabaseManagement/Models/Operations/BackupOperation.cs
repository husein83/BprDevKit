using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class BackupOperation
    {
        public string DatabaseName { get; set; } = null!;
        public string BackupPath { get; set; } = null!;
        public bool WithCompression { get; set; } = false;
        public bool CopyOnly { get; set; } = false;
    }
}
