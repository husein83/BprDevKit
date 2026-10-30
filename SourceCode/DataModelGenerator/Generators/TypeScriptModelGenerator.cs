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
                var fieldName = DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                var tsType = SqlTypeMapper.MapToTypeScript(col.DataType, col.IsNullable, options.DateTypeAsDate);

                if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine($"  /** {col.Description.Replace("\r", "").Replace("\n", " ")} */");
                }

                var readonlyModifier = (options.IncludeReadonlyId && col.IsPrimaryKey) ? "readonly " : "";
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
            if (options.IncludeNavigationProperties && globalOptions.IncludeNavigationProperties)
            {
                // Reference navigation properties
                var allowedFks = table.ForeignKeys
                    .Where(fk => string.Equals(fk.TableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                              && (string.IsNullOrWhiteSpace(fk.SchemaName) || string.Equals(fk.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                    .Where(fk => globalOptions.IsSchemaAccessAllowed(table.SchemaName, string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName))
                    .ToList();

                if (allowedFks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  // References (Many-to-One / Foreign Keys)");
                    foreach (var fk in allowedFks)
                    {
                        if (string.IsNullOrWhiteSpace(fk.RefTableName)) continue;
                        var refClassName = DataModelNamingHelper.FormatClassName(
                            fk.RefTableName,
                            globalOptions.RemoveTablePrefixes,
                            globalOptions.TablePrefixes,
                            globalOptions.SingularizeClassNames,
                            globalOptions.PropertyNaming,
                            Constants.ObjectTypes.Table);

                        var navPropName = DataModelNamingHelper.FormatPropertyName(refClassName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                        sb.AppendLine($"  {navPropName}?: {refClassName} | null;");
                    }
                }

                // Reverse relations (One-to-Many / Children)
                var childFks = allTables
                    .SelectMany(t => t.ForeignKeys.Select(f => new { ChildTable = t, Fk = f }))
                    .Where(x => string.Equals(x.Fk.TableName, x.ChildTable.TableName, StringComparison.OrdinalIgnoreCase)
                             && (string.IsNullOrWhiteSpace(x.Fk.SchemaName) || string.Equals(x.Fk.SchemaName, x.ChildTable.SchemaName, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => string.Equals(x.Fk.RefTableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                             && (string.IsNullOrWhiteSpace(x.Fk.RefSchemaName) || string.Equals(x.Fk.RefSchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => globalOptions.IsSchemaAccessAllowed(table.SchemaName, x.ChildTable.SchemaName))
                    .OrderBy(x => x.ChildTable.TableName)
                    .ToList();

                if (childFks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  // Relations (One-to-Many / Child Collections)");
                    var addedCollections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var child in childFks)
                    {
                        var childClassName = DataModelNamingHelper.FormatClassName(
                            child.ChildTable.TableName,
                            globalOptions.RemoveTablePrefixes,
                            globalOptions.TablePrefixes,
                            globalOptions.SingularizeClassNames,
                            globalOptions.PropertyNaming,
                            Constants.ObjectTypes.Table);

                        var collPropName = DataModelNamingHelper.FormatPropertyName(child.ChildTable.TableName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
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
