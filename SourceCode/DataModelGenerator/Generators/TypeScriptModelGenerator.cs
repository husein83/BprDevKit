using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.DataModelGenerator.Models;
using BprDevKit.DataModelGenerator.Services;
using BprDevKit.Utility;

namespace BprDevKit.DataModelGenerator.Generators
{
    public static class TypeScriptModelGenerator
    {
        public static GeneratedModelFile Generate(
            TableOperation.Information table,
            DataModelOptions globalOptions,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            var options = globalOptions.TypeScript;
            var className = DataModelNamingHelper.FormatClassName(
                table.TableName,
                globalOptions.RemoveTablePrefixes,
                globalOptions.TablePrefixes,
                globalOptions.SingularizeClassNames,
                globalOptions.PropertyNaming,
                table.ObjectType);

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("/**");
            sb.AppendLine(" * Auto-generated TypeScript definitions by BprDevKit.");
            sb.AppendLine($" * Table: {table.TableName}");
            if (!string.IsNullOrWhiteSpace(table.SchemaName))
            {
                sb.AppendLine($" * Schema: {table.SchemaName}");
            }
            sb.AppendLine(" */");
            sb.AppendLine();

            // Custom imports
            if (!string.IsNullOrWhiteSpace(options.CustomImports))
            {
                sb.AppendLine(options.CustomImports.Trim());
                sb.AppendLine();
            }

            // Description JSDoc
            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/**");
                sb.AppendLine($" * {table.Description.Replace("\r", "").Replace("\n", " ")}");
                sb.AppendLine(" */");
            }

            var exportModifier = options.IncludeExport ? "export " : "";

            if (options.TypeKind == "type")
            {
                sb.AppendLine($"{exportModifier}type {className} = {{");
            }
            else if (options.TypeKind == "class")
            {
                sb.AppendLine($"{exportModifier}class {className} {{");
            }
            else // interface
            {
                sb.AppendLine($"{exportModifier}interface {className} {{");
            }

            // Fields / Properties
            foreach (var col in table.Columns)
            {
                if (!DataModelFilterHelper.ShouldIncludeColumn(col, globalOptions))
                {
                    continue;
                }

                var fieldName = DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                var tsType = SqlTypeMapper.MapToTypeScript(col.DataType, col.IsNullable, options.DateTypeAsDate);

                if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine($"  /** {col.Description.Replace("\r", "").Replace("\n", " ")} */");
                }

                var isComputed = DataModelFilterHelper.IsComputed(col);
                var isReadOnly = (options.IncludeReadonlyId && col.IsPrimaryKey)
                    || (isComputed && globalOptions.ComputedColumnMode == ComputedColumnHandling.IncludeReadOnly);
                var readonlyModifier = isReadOnly ? "readonly " : "";
                var optionalMarker = col.IsNullable ? "?" : "";

                if (options.TypeKind == "class")
                {
                    sb.AppendLine($"  {readonlyModifier}{fieldName}{optionalMarker}: {tsType};");
                }
                else
                {
                    sb.AppendLine($"  {readonlyModifier}{fieldName}{optionalMarker}: {tsType};");
                }
            }

            // Navigation Properties
            if (options.IncludeNavigationProperties && globalOptions.IncludeNavigationProperties && (string.IsNullOrWhiteSpace(table.ObjectType) || string.Equals(table.ObjectType, Constants.ObjectTypes.Table, StringComparison.OrdinalIgnoreCase)))
            {
                // Reference navigation properties
                var allowedFks = table.ForeignKeys
                    .Where(fk => string.Equals(fk.TableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                              && (string.IsNullOrWhiteSpace(fk.SchemaName) || string.Equals(fk.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase))
                              && !string.IsNullOrWhiteSpace(fk.RefTableName))
                    .Where(fk => globalOptions.IsSchemaAccessAllowed(table.SchemaName, string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName))
                    .ToList();

                if (allowedFks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  // References (Many-to-One / Foreign Keys)");
                    foreach (var fk in allowedFks)
                    {
                        var refClassName = DataModelNamingHelper.FormatClassName(
                            fk.RefTableName,
                            globalOptions.RemoveTablePrefixes,
                            globalOptions.TablePrefixes,
                            globalOptions.SingularizeClassNames,
                            globalOptions.PropertyNaming,
                            Constants.ObjectTypes.Table);

                        var refSchema = string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName;
                        var multipleToTarget = allowedFks.Count(other =>
                            string.Equals(other.RefTableName, fk.RefTableName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(string.IsNullOrWhiteSpace(other.RefSchemaName) ? table.SchemaName : other.RefSchemaName, refSchema, StringComparison.OrdinalIgnoreCase)) > 1;

                        var localProp = DataModelNamingHelper.FormatPropertyName(fk.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                        var navPropName = DataModelNamingHelper.FormatPropertyName(refClassName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes)
                            + (multipleToTarget ? localProp : "");
                        sb.AppendLine($"  {navPropName}?: {refClassName} | null;");
                    }
                }

                // Reverse relations (One-to-Many / Children)
                var allIncoming = new List<(string ChildTableName, string ChildSchemaName, ForeignKeyOperation.Information Fk)>();
                if (table.ForeignKeys != null)
                {
                    foreach (var fk in table.ForeignKeys)
                    {
                        if (string.Equals(fk.RefTableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                            && (string.IsNullOrWhiteSpace(fk.RefSchemaName) || string.Equals(fk.RefSchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                        {
                            var childTable = !string.IsNullOrWhiteSpace(fk.TableName) ? fk.TableName : table.TableName;
                            var childSchema = !string.IsNullOrWhiteSpace(fk.SchemaName) ? fk.SchemaName : table.SchemaName;
                            allIncoming.Add((childTable, childSchema, fk));
                        }
                    }
                }
                if (allTables != null)
                {
                    foreach (var otherTable in allTables)
                    {
                        if (otherTable.ForeignKeys == null) continue;
                        foreach (var fk in otherTable.ForeignKeys)
                        {
                            if (string.Equals(fk.RefTableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                                && (string.IsNullOrWhiteSpace(fk.RefSchemaName) || string.Equals(fk.RefSchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                            {
                                var childTable = !string.IsNullOrWhiteSpace(fk.TableName) ? fk.TableName : otherTable.TableName;
                                var childSchema = !string.IsNullOrWhiteSpace(fk.SchemaName) ? fk.SchemaName : otherTable.SchemaName;
                                allIncoming.Add((childTable, childSchema, fk));
                            }
                        }
                    }
                }

                var childFks = allIncoming
                    .GroupBy(x => $"{x.ChildSchemaName}.{x.ChildTableName}.{(string.IsNullOrWhiteSpace(x.Fk.FKName) ? x.Fk.ColumnName : x.Fk.FKName)}", StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Where(x => globalOptions.IsSchemaAccessAllowed(table.SchemaName, x.ChildSchemaName))
                    .OrderBy(x => x.ChildTableName)
                    .ThenBy(x => x.Fk.ColumnName)
                    .ToList();

                if (childFks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  // Relations (One-to-Many / Child Collections)");
                    var addedCollections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var child in childFks)
                    {
                        var childClassName = DataModelNamingHelper.FormatClassName(
                            child.ChildTableName,
                            globalOptions.RemoveTablePrefixes,
                            globalOptions.TablePrefixes,
                            globalOptions.SingularizeClassNames,
                            globalOptions.PropertyNaming,
                            Constants.ObjectTypes.Table);

                        var multipleFromChild = childFks.Count(other =>
                            string.Equals(other.ChildTableName, child.ChildTableName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(other.ChildSchemaName, child.ChildSchemaName, StringComparison.OrdinalIgnoreCase)) > 1;

                        var childFkProp = DataModelNamingHelper.FormatPropertyName(child.Fk.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                        var collPropName = DataModelNamingHelper.FormatPropertyName(child.ChildTableName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes)
                            + (multipleFromChild ? childFkProp : "");

                        if (addedCollections.Add(collPropName))
                        {
                            sb.AppendLine($"  {collPropName}?: {childClassName}[];");
                        }
                    }
                }
            }

            // Custom snippet
            if (!string.IsNullOrWhiteSpace(options.CustomClassBodySnippet))
            {
                sb.AppendLine();
                sb.AppendLine($"  {options.CustomClassBodySnippet.Trim()}");
            }

            if (options.TypeKind == "type")
            {
                sb.AppendLine("};");
            }
            else
            {
                sb.AppendLine("}");
            }

            var content = sb.ToString();
            var lineCount = content.Split('\n').Length;

            return new GeneratedModelFile
            {
                TableName = table.TableName,
                SchemaName = table.SchemaName,
                ClassName = className,
                FileName = $"{className}.ts",
                Language = DataModelLanguage.TypeScript,
                Content = content,
                ObjectType = table.ObjectType ?? Constants.ObjectTypes.Table
            };
        }
    }
}
