using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DataModelGenerator.Generators
{
    public static class PythonModelGenerator
    {
        public static GeneratedModelFile Generate(
            TableOperation.Information table,
            DataModelOptions globalOptions,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            var options = globalOptions.Python;
            var className = DataModelNamingHelper.FormatClassName(
                table.TableName,
                globalOptions.RemoveTablePrefixes,
                globalOptions.TablePrefixes,
                globalOptions.SingularizeClassNames,
                globalOptions.PropertyNaming,
                table.ObjectType);

            var sb = new StringBuilder();

            switch (options.Style?.ToLowerInvariant())
            {
                case "sqlalchemy":
                    GenerateSqlAlchemy(sb, table, className, globalOptions, options, allTables);
                    break;
                case "dataclass":
                    GenerateDataclass(sb, table, className, globalOptions, options);
                    break;
                case "typeddict":
                    GenerateTypedDict(sb, table, className, globalOptions, options);
                    break;
                case "pydantic":
                default:
                    GeneratePydantic(sb, table, className, globalOptions, options);
                    break;
            }

            var fileName = DataModelNamingHelper.ToSnakeCase(className) + ".py";

            return new GeneratedModelFile
            {
                TableName = table.TableName,
                SchemaName = table.SchemaName,
                ClassName = className,
                FileName = fileName,
                Content = sb.ToString().TrimEnd() + Environment.NewLine,
                Language = DataModelLanguage.Python,
                ObjectType = table.ObjectType
            };
        }

        private static void GeneratePydantic(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            PythonOptions options)
        {
            sb.AppendLine("from typing import Optional, List, Any");
            sb.AppendLine("from datetime import datetime, date, time");
            sb.AppendLine("from decimal import Decimal");
            sb.AppendLine("from uuid import UUID");
            sb.AppendLine("from pydantic import BaseModel, Field, ConfigDict");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"class {className}(BaseModel):");
            if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("    \"\"\"");
                sb.AppendLine($"    {table.Description.Replace("\r", "").Replace("\n", "\n    ")}");
                sb.AppendLine("    \"\"\"");
            }

            sb.AppendLine("    model_config = ConfigDict(from_attributes=True, populate_by_name=True)");
            sb.AppendLine();

            foreach (var col in table.Columns)
            {
                var fieldName = options.UseSnakeCaseProperties
                    ? DataModelNamingHelper.ToSnakeCase(col.ColumnName)
                    : DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming);

                var pyType = SqlTypeMapper.MapToPython(col.DataType, col.IsNullable, options.UseOptionalTypeHinting);

                var fieldArgs = new List<string>();
                if (fieldName != col.ColumnName)
                {
                    fieldArgs.Add($"alias=\"{col.ColumnName}\"");
                }

                if (col.IsNullable)
                {
                    fieldArgs.Add("default=None");
                }
                else
                {
                    fieldArgs.Add("...");
                }

                if (!string.IsNullOrWhiteSpace(col.Description) && options.IncludeDocstrings)
                {
                    fieldArgs.Add($"description=\"{EscapeString(col.Description)}\"");
                }

                if (col.Length.HasValue && col.Length.Value > 0 && col.Length.Value < 8000 && col.DataType.Contains("char"))
                {
                    fieldArgs.Add($"max_length={col.Length.Value}");
                }

                var fieldCall = fieldArgs.Count > 0 ? $" = Field({string.Join(", ", fieldArgs)})" : "";
                sb.AppendLine($"    {fieldName}: {pyType}{fieldCall}");
            }
        }

        private static void GenerateDataclass(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            PythonOptions options)
        {
            sb.AppendLine("from dataclasses import dataclass, field");
            sb.AppendLine("from typing import Optional, List, Any");
            sb.AppendLine("from datetime import datetime, date, time");
            sb.AppendLine("from decimal import Decimal");
            sb.AppendLine("from uuid import UUID");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("@dataclass");
            sb.AppendLine($"class {className}:");
            if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("    \"\"\"");
                sb.AppendLine($"    {table.Description.Replace("\r", "").Replace("\n", "\n    ")}");
                sb.AppendLine("    \"\"\"");
            }

            foreach (var col in table.Columns)
            {
                var fieldName = options.UseSnakeCaseProperties
                    ? DataModelNamingHelper.ToSnakeCase(col.ColumnName)
                    : DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming);

                var pyType = SqlTypeMapper.MapToPython(col.DataType, col.IsNullable, options.UseOptionalTypeHinting);
                var defaultVal = col.IsNullable ? " = None" : "";

                sb.AppendLine($"    {fieldName}: {pyType}{defaultVal}");
                if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine($"    \"\"\"{col.Description}\"\"\"");
                }
            }
        }

        private static void GenerateSqlAlchemy(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            PythonOptions options,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            sb.AppendLine("from sqlalchemy import Column, Integer, BigInteger, SmallInteger, String, Boolean, DateTime, Date, Time, Numeric, ForeignKey");
            sb.AppendLine("from sqlalchemy.dialects.mssql import UNIQUEIDENTIFIER, VARBINARY");
            sb.AppendLine("from sqlalchemy.orm import declarative_base, relationship");
            sb.AppendLine();
            sb.AppendLine("Base = declarative_base()");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"class {className}(Base):");
            if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("    \"\"\"");
                sb.AppendLine($"    {table.Description.Replace("\r", "").Replace("\n", "\n    ")}");
                sb.AppendLine("    \"\"\"");
            }

            sb.AppendLine($"    __tablename__ = \"{table.TableName}\"");
            if (!string.IsNullOrWhiteSpace(table.SchemaName) && !string.Equals(table.SchemaName, "dbo", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    __table_args__ = {{\"schema\": \"{table.SchemaName}\"}}");
            }
            sb.AppendLine();

            foreach (var col in table.Columns)
            {
                var fieldName = options.UseSnakeCaseProperties
                    ? DataModelNamingHelper.ToSnakeCase(col.ColumnName)
                    : DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming);

                var sqlaType = MapToSqlAlchemyType(col.DataType, col.Length);
                var args = new List<string> { sqlaType };

                if (col.IsPrimaryKey) args.Add("primary_key=True");
                if (col.IsIdentity) args.Add("autoincrement=True");
                if (!col.IsNullable && !col.IsPrimaryKey) args.Add("nullable=False");

                var fk = table.ForeignKeys.FirstOrDefault(f => f.ColumnName == col.ColumnName);
                if (fk != null && !string.IsNullOrWhiteSpace(fk.RefTableName))
                {
                    args.Add($"ForeignKey(\"{fk.RefTableName}.{fk.RefColumnName}\")");
                }

                if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(col.Description))
                {
                    args.Add($"doc=\"{EscapeString(col.Description)}\"");
                }

                sb.AppendLine($"    {fieldName} = Column({string.Join(", ", args)})");
            }

            // Relationships
            var allowedFks = table.ForeignKeys
                .Where(fk => globalOptions.IsSchemaAccessAllowed(table.SchemaName, string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName))
                .ToList();

            if (globalOptions.IncludeNavigationProperties && allowedFks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    # Relationships");
                foreach (var fk in allowedFks)
                {
                    var parentClassName = DataModelNamingHelper.FormatClassName(
                        fk.RefTableName,
                        globalOptions.RemoveTablePrefixes,
                        globalOptions.TablePrefixes,
                        globalOptions.SingularizeClassNames);

                    var relName = DataModelNamingHelper.ToSnakeCase(parentClassName);
                    sb.AppendLine($"    {relName} = relationship(\"{parentClassName}\")");
                }
            }
        }

        private static void GenerateTypedDict(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            PythonOptions options)
        {
            sb.AppendLine("from typing import TypedDict, Optional");
            sb.AppendLine("from datetime import datetime, date, time");
            sb.AppendLine("from decimal import Decimal");
            sb.AppendLine("from uuid import UUID");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"class {className}(TypedDict, total=False):");
            if (options.IncludeDocstrings && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("    \"\"\"");
                sb.AppendLine($"    {table.Description.Replace("\r", "").Replace("\n", "\n    ")}");
                sb.AppendLine("    \"\"\"");
            }

            foreach (var col in table.Columns)
            {
                var fieldName = options.UseSnakeCaseProperties
                    ? DataModelNamingHelper.ToSnakeCase(col.ColumnName)
                    : DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming);

                var pyType = SqlTypeMapper.MapToPython(col.DataType, col.IsNullable, options.UseOptionalTypeHinting);
                sb.AppendLine($"    {fieldName}: {pyType}");
            }
        }

        private static string MapToSqlAlchemyType(string sqlDataType, int? length)
        {
            var baseType = SqlTypeMapper.NormalizeType(sqlDataType);
            switch (baseType)
            {
                case "bigint": return "BigInteger";
                case "int": return "Integer";
                case "smallint":
                case "tinyint": return "SmallInteger";
                case "bit": return "Boolean";
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney": return "Numeric";
                case "float":
                case "real": return "Numeric";
                case "datetime":
                case "datetime2":
                case "smalldatetime": return "DateTime";
                case "date": return "Date";
                case "time": return "Time";
                case "uniqueidentifier": return "UNIQUEIDENTIFIER";
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion": return "VARBINARY";
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                default:
                    if (length.HasValue && length.Value > 0 && length.Value < 8000)
                        return $"String({length.Value})";
                    return "String";
            }
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}
