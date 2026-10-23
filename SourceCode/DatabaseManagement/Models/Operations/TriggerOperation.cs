using System;
using System.Collections.Generic;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class TriggerOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = "dbo";
            public string TriggerName { get; set; } = null!;
            public string TableSchema { get; set; } = "dbo";
            public string TableName { get; set; } = null!;
            public string Timing { get; set; } = "AFTER"; // AFTER or INSTEAD OF
            public bool OnInsert { get; set; } = true;
            public bool OnUpdate { get; set; }
            public bool OnDelete { get; set; }
            public string Body { get; set; } = null!;
            public bool WithEncryption { get; set; } = true;
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = "dbo";
            public string TriggerName { get; set; } = null!;
            public string TableSchema { get; set; } = "dbo";
            public string TableName { get; set; } = null!;
            public string Body { get; set; } = null!;
            public bool WithEncryption { get; set; } = true;
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = "dbo";
            public string TriggerName { get; set; } = null!;
        }

        public class ToggleState
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = "dbo";
            public string TriggerName { get; set; } = null!;
            public string TableSchema { get; set; } = "dbo";
            public string TableName { get; set; } = null!;
            public bool Enable { get; set; }
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = "dbo";
            public string TriggerName { get; set; } = null!;
            public string TableSchema { get; set; } = "dbo";
            public string TableName { get; set; } = null!;
            public bool IsDisabled { get; set; }
            public bool IsInsteadOfTrigger { get; set; }
            public string TriggerEvents { get; set; } = string.Empty;
            public string Definition { get; set; } = string.Empty;
            public DateTime CreateDate { get; set; }
            public DateTime ModifyDate { get; set; }
            public bool WithEncryption { get; set; } = true;
            public bool IsEncrypted { get; set; }
        }
    }
}
