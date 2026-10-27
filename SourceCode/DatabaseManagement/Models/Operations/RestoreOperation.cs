using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class RestoreOperation
    {
        public string DatabaseName { get; set; } = null!;
        public string RestorePath { get; set; } = null!;
        public bool WithReplace { get; set; } = true;
        public bool CloseExistingConnections { get; set; } = true;
    }
}
