using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class FunctionOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string FunctionName { get; set; } = null!;
            public string Body { get; set; } = null!;
            public bool WithEncryption { get; set; } = true;
        }

        public class Alter
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string FunctionName { get; set; } = null!;
            public string Body { get; set; } = null!;
            public bool WithEncryption { get; set; } = true;
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string FunctionName { get; set; } = null!;
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string FunctionName { get; set; } = null!;
        }
    }
}