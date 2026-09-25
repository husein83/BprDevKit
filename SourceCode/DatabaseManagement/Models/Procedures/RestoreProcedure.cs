using System;
using System.Collections.Generic;
using System.Text;

namespace BprDevKit.DatabaseManagement.Models
{
    public class RestoreProcedure
    {
        public string DatabaseName { get; set; } = null!;
        public string RestorePath { get; set; } = null!;
    }
}
