namespace BprDevKit.DatabaseManagement.Models.Operations
{
    public class CheckConstraintOperation
    {
        public class Create
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string? ConstraintName { get; set; }
            public string CheckExpression { get; set; } = null!;
            public bool IsNotForReplication { get; set; }
        }

        public class Drop
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ConstraintName { get; set; } = null!;
        }

        public class ToggleState
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ConstraintName { get; set; } = null!;
            public bool Enable { get; set; }
        }

        public class Information
        {
            public string DatabaseName { get; set; } = null!;
            public string SchemaName { get; set; } = null!;
            public string TableName { get; set; } = null!;
            public string ConstraintName { get; set; } = null!;
            public string CheckExpression { get; set; } = string.Empty;
            public bool IsDisabled { get; set; }
            public bool IsNotForReplication { get; set; }
        }
    }
}
