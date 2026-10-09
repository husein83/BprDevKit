namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    public enum ColumnDataTypeKind
    {
        /// <summary>No size arguments (int, bit, date, ...).</summary>
        Plain,
        /// <summary>Character/binary types: (length) or (MAX).</summary>
        Length,
        /// <summary>decimal/numeric: (precision, scale).</summary>
        PrecisionScale,
        /// <summary>datetime2/time/datetimeoffset: optional (fractional seconds precision 0-7).</summary>
        FractionalSeconds
    }

    /// <summary>
    /// The data types the table designer supports, and how each one takes its size arguments.
    /// This list is also the whitelist that keeps arbitrary text out of generated DDL.
    /// </summary>
    public sealed class ColumnDataType
    {
        public string Name { get; }
        public ColumnDataTypeKind Kind { get; }

        /// <summary>Largest explicit length (Length kind only).</summary>
        public int MaxLength { get; }

        /// <summary>Whether (MAX) is allowed (Length kind only).</summary>
        public bool SupportsMax { get; }

        public bool SupportsIdentity { get; }

        private ColumnDataType(string name, ColumnDataTypeKind kind = ColumnDataTypeKind.Plain, int maxLength = 0, bool supportsMax = false, bool supportsIdentity = false)
        {
            Name = name;
            Kind = kind;
            MaxLength = maxLength;
            SupportsMax = supportsMax;
            SupportsIdentity = supportsIdentity;
        }

        public static IReadOnlyList<ColumnDataType> All { get; } = new List<ColumnDataType>
        {
            new("int", supportsIdentity: true),
            new("bigint", supportsIdentity: true),
            new("smallint", supportsIdentity: true),
            new("tinyint", supportsIdentity: true),
            new("bit"),
            new("decimal", ColumnDataTypeKind.PrecisionScale, supportsIdentity: true),
            new("numeric", ColumnDataTypeKind.PrecisionScale, supportsIdentity: true),
            new("money"),
            new("smallmoney"),
            new("float"),
            new("real"),
            new("date"),
            new("datetime"),
            new("datetime2", ColumnDataTypeKind.FractionalSeconds),
            new("datetimeoffset", ColumnDataTypeKind.FractionalSeconds),
            new("smalldatetime"),
            new("time", ColumnDataTypeKind.FractionalSeconds),
            new("char", ColumnDataTypeKind.Length, 8000),
            new("varchar", ColumnDataTypeKind.Length, 8000, supportsMax: true),
            new("nchar", ColumnDataTypeKind.Length, 4000),
            new("nvarchar", ColumnDataTypeKind.Length, 4000, supportsMax: true),
            new("binary", ColumnDataTypeKind.Length, 8000),
            new("varbinary", ColumnDataTypeKind.Length, 8000, supportsMax: true),
            new("uniqueidentifier"),
            new("xml")
        };

        public static ColumnDataType? Find(string? name)
        {
            return All.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
