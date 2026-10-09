namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// A column as designed in the "create table" form.
    /// </summary>
    public class ColumnDefinition
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = "int";

        /// <summary>Character/binary length; -1 means MAX.</summary>
        public int? Length { get; set; }

        public int? Precision { get; set; }

        /// <summary>Scale for decimal/numeric, fractional-seconds precision for datetime2/time/datetimeoffset.</summary>
        public int? Scale { get; set; }

        public bool IsNullable { get; set; } = true;
        public bool IsIdentity { get; set; }
        public int IdentitySeed { get; set; } = 1;
        public int IdentityIncrement { get; set; } = 1;
        public bool IsPrimaryKey { get; set; }

        /// <summary>T-SQL expression, e.g. <c>0</c>, <c>'N/A'</c> or <c>SYSUTCDATETIME()</c>.</summary>
        public string? DefaultValue { get; set; }

        public string? Description { get; set; }
    }
}
