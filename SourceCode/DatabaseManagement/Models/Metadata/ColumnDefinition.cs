namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// A column as designed in a form: the input shape shared by "create table" and "add/alter column".
    /// (What is read back from the database is <c>ColumnOperation.Information</c>.)
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

        /// <summary>Only used by character types; null keeps the database default.</summary>
        public string? Collation { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Switches the data type and resets the size arguments to sensible defaults for the new type.
        /// </summary>
        public void ApplyDataType(string? dataType)
        {
            var type = ColumnDataType.Find(dataType);
            if (type is null) return;

            DataType = type.Name;
            Length = type.Kind == ColumnDataTypeKind.Length ? Math.Min(50, type.MaxLength)
                   : type.Kind == ColumnDataTypeKind.VectorDimensions ? 1536 : null;
            Precision = type.Kind == ColumnDataTypeKind.PrecisionScale ? 18 : null;
            Scale = type.Kind == ColumnDataTypeKind.PrecisionScale ? 0 : null;
            Collation = null;

            if (!type.SupportsIdentity)
                IsIdentity = false;
        }

        public void SetPrimaryKey(bool isPrimaryKey)
        {
            IsPrimaryKey = isPrimaryKey;
            if (isPrimaryKey)
                IsNullable = false;
        }

        public void SetIdentity(bool isIdentity)
        {
            IsIdentity = isIdentity;
            if (isIdentity)
            {
                IsNullable = false;
                DefaultValue = null;
            }
        }
    }
}
