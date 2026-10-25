using System;

namespace BprDevKit.DatabaseManagement.Services.DataModels
{
    public static class SqlTypeMapper
    {
        public static string MapToCSharp(string sqlDataType, bool isNullable, bool nullableReferenceTypes)
        {
            var baseType = NormalizeType(sqlDataType);
            string csharpType;
            bool isValueType;

            switch (baseType)
            {
                case "bigint":
                    csharpType = "long";
                    isValueType = true;
                    break;
                case "int":
                    csharpType = "int";
                    isValueType = true;
                    break;
                case "smallint":
                    csharpType = "short";
                    isValueType = true;
                    break;
                case "tinyint":
                    csharpType = "byte";
                    isValueType = true;
                    break;
                case "bit":
                    csharpType = "bool";
                    isValueType = true;
                    break;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    csharpType = "decimal";
                    isValueType = true;
                    break;
                case "float":
                    csharpType = "double";
                    isValueType = true;
                    break;
                case "real":
                    csharpType = "float";
                    isValueType = true;
                    break;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    csharpType = "DateTime";
                    isValueType = true;
                    break;
                case "date":
                    csharpType = "DateOnly";
                    isValueType = true;
                    break;
                case "time":
                    csharpType = "TimeSpan";
                    isValueType = true;
                    break;
                case "datetimeoffset":
                    csharpType = "DateTimeOffset";
                    isValueType = true;
                    break;
                case "uniqueidentifier":
                    csharpType = "Guid";
                    isValueType = true;
                    break;
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                case "timestamp":
                    csharpType = "byte[]";
                    isValueType = false;
                    break;
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "text":
                case "ntext":
                case "xml":
                case "json":
                default:
                    csharpType = "string";
                    isValueType = false;
                    break;
            }

            if (isNullable)
            {
                if (isValueType)
                    return csharpType + "?";
                if (nullableReferenceTypes)
                    return csharpType + "?";
            }

            return csharpType;
        }

        public static string MapToPython(string sqlDataType, bool isNullable, bool useOptional)
        {
            var baseType = NormalizeType(sqlDataType);
            string pyType;

            switch (baseType)
            {
                case "bigint":
                case "int":
                case "smallint":
                case "tinyint":
                    pyType = "int";
                    break;
                case "bit":
                    pyType = "bool";
                    break;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    pyType = "Decimal";
                    break;
                case "float":
                case "real":
                    pyType = "float";
                    break;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "datetimeoffset":
                    pyType = "datetime";
                    break;
                case "date":
                    pyType = "date";
                    break;
                case "time":
                    pyType = "time";
                    break;
                case "uniqueidentifier":
                    pyType = "UUID";
                    break;
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                case "timestamp":
                    pyType = "bytes";
                    break;
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "text":
                case "ntext":
                case "xml":
                case "json":
                default:
                    pyType = "str";
                    break;
            }

            if (isNullable && useOptional)
            {
                return $"Optional[{pyType}]";
            }

            return pyType;
        }

        public static string MapToDart(string sqlDataType, bool isNullable, bool nullSafety)
        {
            var baseType = NormalizeType(sqlDataType);
            string dartType;

            switch (baseType)
            {
                case "bigint":
                case "int":
                case "smallint":
                case "tinyint":
                    dartType = "int";
                    break;
                case "bit":
                    dartType = "bool";
                    break;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                case "float":
                case "real":
                    dartType = "double";
                    break;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "date":
                case "datetimeoffset":
                    dartType = "DateTime";
                    break;
                case "time":
                    dartType = "Duration";
                    break;
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                case "timestamp":
                    dartType = "Uint8List";
                    break;
                case "uniqueidentifier":
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "text":
                case "ntext":
                case "xml":
                case "json":
                default:
                    dartType = "String";
                    break;
            }

            if (isNullable && nullSafety)
            {
                return dartType + "?";
            }

            return dartType;
        }

        public static string MapToJava(string sqlDataType, bool isNullable, bool useWrapperObjects = true)
        {
            var baseType = NormalizeType(sqlDataType);

            switch (baseType)
            {
                case "bigint":
                    return isNullable || useWrapperObjects ? "Long" : "long";
                case "int":
                    return isNullable || useWrapperObjects ? "Integer" : "int";
                case "smallint":
                    return isNullable || useWrapperObjects ? "Short" : "short";
                case "tinyint":
                    return isNullable || useWrapperObjects ? "Short" : "short";
                case "bit":
                    return isNullable || useWrapperObjects ? "Boolean" : "boolean";
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    return "BigDecimal";
                case "float":
                    return isNullable || useWrapperObjects ? "Double" : "double";
                case "real":
                    return isNullable || useWrapperObjects ? "Float" : "float";
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    return "LocalDateTime";
                case "date":
                    return "LocalDate";
                case "time":
                    return "LocalTime";
                case "datetimeoffset":
                    return "OffsetDateTime";
                case "uniqueidentifier":
                    return "UUID";
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                case "timestamp":
                    return "byte[]";
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "text":
                case "ntext":
                case "xml":
                case "json":
                default:
                    return "String";
            }
        }

        public static string NormalizeType(string sqlDataType)
        {
            if (string.IsNullOrWhiteSpace(sqlDataType)) return "varchar";
            var clean = sqlDataType.Trim().ToLowerInvariant();
            var parenIdx = clean.IndexOf('(');
            if (parenIdx > 0)
            {
                clean = clean.Substring(0, parenIdx).Trim();
            }
            return clean;
        }
    }
}
