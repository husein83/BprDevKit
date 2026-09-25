using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models
{
    public class BackupProcedure
    {
        public string DatabaseName { get; set; } = null!;
        public string BackupPath { get; set; } = null!;
    }
}
