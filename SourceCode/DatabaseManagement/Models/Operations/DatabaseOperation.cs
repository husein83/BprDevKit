using System;
using System.Collections.Generic;
using System.Text;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class DatabaseOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string DataFilePath { get; set; } = null!;
            public string? LogFilePath { get; set; }
            public int InitialSizeMB { get; set; } = Constants.DefaultInitialDataSizeMB;
            public int? MaxSizeMB { get; set; }
            public int FileGrowthMB { get; set; } = Constants.DefaultFileGrowthMB;
            public int LogInitialSizeMB { get; set; } = Constants.DefaultInitialLogSizeMB;
            public int? LogMaxSizeMB { get; set; }
            public int LogFileGrowthMB { get; set; } = Constants.DefaultLogFileGrowthMB;
            public string? Collation { get; set; }
            public string RecoveryModel { get; set; } = "SIMPLE";
            public short? CompatibilityLevel { get; set; }
            public bool IsReadOnly { get; set; } = false;
            public bool AutoShrink { get; set; } = false;
            public bool AutoClose { get; set; } = false;
            public string PageVerify { get; set; } = "CHECKSUM";
            public string? FileGroup { get; set; }
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string? Collation { get; set; }
            public string? RecoveryModel { get; set; }
            public short? CompatibilityLevel { get; set; }
            public bool? IsReadOnly { get; set; }
            public bool? AutoShrink { get; set; }
            public bool? AutoClose { get; set; }
            public string? PageVerify { get; set; }
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public bool IgnoreIfNotExists { get; set; } = true;
            public bool ForceDisconnect { get; set; } = true;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
        }
    }
}