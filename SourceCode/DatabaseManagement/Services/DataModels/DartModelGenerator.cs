using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.Services.DataModels
{
    public static class DartModelGenerator
    {
        public static GeneratedModelFile Generate(
            TableOperation.Information table,
            DataModelOptions globalOptions,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            var options = globalOptions.Dart;
            var className = DataModelNamingHelper.FormatClassName(
                table.TableName,
                globalOptions.RemoveTablePrefixes,
                globalOptions.TablePrefixes,
                globalOptions.SingularizeClassNames);

            var sb = new StringBuilder();

            if (options.Style == "freezed")
            {
                GenerateFreezed(sb, table, className, globalOptions, options);
            }
            else if (options.Style == "json_serializable")
            {
                GenerateJsonSerializable(sb, table, className, globalOptions, options);
            }
            else
            {
                GeneratePlain(sb, table, className, globalOptions, options);
            }

            var fileName = DataModelNamingHelper.ToSnakeCase(className) + ".dart";

            return new GeneratedModelFile
            {
                TableName = table.TableName,
                SchemaName = table.SchemaName,
                ClassName = className,
                FileName = fileName,
                Content = sb.ToString().TrimEnd() + Environment.NewLine,
                Language = DataModelLanguage.Dart
            };
        }

        private static void GeneratePlain(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            DartOptions options)
        {
            sb.AppendLine("import 'dart:convert';");
            sb.AppendLine("import 'dart:typed_data';");
            sb.AppendLine();

            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/// " + table.Description.Replace("\r", "").Replace("\n", "\n/// "));
            }

            sb.AppendLine($"class {className} {{");

            var fields = new List<(string Name, string Type, string OriginalColName, bool IsNullable)>();

            foreach (var col in table.Columns)
            {
                var fieldName = DataModelNamingHelper.ToCamelCase(col.ColumnName);
                var dartType = SqlTypeMapper.MapToDart(col.DataType, col.IsNullable, options.NullSafety);
                fields.Add((fieldName, dartType, col.ColumnName, col.IsNullable));

                if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine($"  /// {col.Description.Replace("\r", "").Replace("\n", " ")}");
                }

                var fieldModifier = options.FinalFields ? "final " : "";
                sb.AppendLine($"  {fieldModifier}{dartType} {fieldName};");
            }

            sb.AppendLine();

            // Constructor
            sb.AppendLine($"  const {className}({{");
            foreach (var field in fields)
            {
                var req = (!field.IsNullable && options.NullSafety) ? "required " : "";
                sb.AppendLine($"    {req}this.{field.Name},");
            }
            sb.AppendLine("  });");

            // CopyWith
            if (options.GenerateCopyWith)
            {
                sb.AppendLine();
                sb.AppendLine($"  {className} copyWith({{");
                foreach (var field in fields)
                {
                    var type = field.Type.EndsWith("?") ? field.Type : field.Type + "?";
                    sb.AppendLine($"    {type} {field.Name},");
                }
                sb.AppendLine("  }) {");
                sb.AppendLine($"    return {className}(");
                foreach (var field in fields)
                {
                    sb.AppendLine($"      {field.Name}: {field.Name} ?? this.{field.Name},");
                }
                sb.AppendLine("    );");
                sb.AppendLine("  }");
            }

            // Serialization (toMap / fromMap / toJson / fromJson)
            if (options.GenerateSerialization)
            {
                sb.AppendLine();
                sb.AppendLine("  Map<String, dynamic> toMap() {");
                sb.AppendLine("    return <String, dynamic>{");
                foreach (var field in fields)
                {
                    sb.AppendLine($"      '{field.OriginalColName}': {field.Name},");
                }
                sb.AppendLine("    };");
                sb.AppendLine("  }");
                sb.AppendLine();

                sb.AppendLine($"  factory {className}.fromMap(Map<String, dynamic> map) {{");
                sb.AppendLine($"    return {className}(");
                foreach (var field in fields)
                {
                    var rawType = field.Type.TrimEnd('?');
                    string castExpr;
                    if (rawType == "int")
                        castExpr = $"map['{field.OriginalColName}'] != null ? (map['{field.OriginalColName}'] as num).toInt() : {(field.IsNullable ? "null" : "0")}";
                    else if (rawType == "double")
                        castExpr = $"map['{field.OriginalColName}'] != null ? (map['{field.OriginalColName}'] as num).toDouble() : {(field.IsNullable ? "null" : "0.0")}";
                    else if (rawType == "bool")
                        castExpr = $"map['{field.OriginalColName}'] != null ? map['{field.OriginalColName}'] as bool : {(field.IsNullable ? "null" : "false")}";
                    else if (rawType == "DateTime")
                        castExpr = $"map['{field.OriginalColName}'] != null ? DateTime.parse(map['{field.OriginalColName}'].toString()) : null";
                    else
                        castExpr = $"map['{field.OriginalColName}'] != null ? map['{field.OriginalColName}'] as {rawType} : null";

                    if (!field.IsNullable && options.NullSafety && rawType != "int" && rawType != "double" && rawType != "bool")
                    {
                        castExpr += $" ?? ''";
                    }

                    sb.AppendLine($"      {field.Name}: {castExpr},");
                }
                sb.AppendLine("    );");
                sb.AppendLine("  }");
                sb.AppendLine();

                sb.AppendLine("  String toJson() => json.encode(toMap());");
                sb.AppendLine();
                sb.AppendLine($"  factory {className}.fromJson(String source) => {className}.fromMap(json.decode(source) as Map<String, dynamic>);");
            }

            sb.AppendLine("}");
        }

        private static void GenerateJsonSerializable(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            DartOptions options)
        {
            var fileNameWithoutExt = DataModelNamingHelper.ToSnakeCase(className);

            sb.AppendLine("import 'dart:typed_data';");
            sb.AppendLine("import 'package:json_annotation/json_annotation.dart';");
            sb.AppendLine();
            sb.AppendLine($"part '{fileNameWithoutExt}.g.dart';");
            sb.AppendLine();

            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/// " + table.Description.Replace("\r", "").Replace("\n", "\n/// "));
            }

            sb.AppendLine("@JsonSerializable()");
            sb.AppendLine($"class {className} {{");

            var fields = new List<(string Name, string Type, string OriginalColName, bool IsNullable)>();

            foreach (var col in table.Columns)
            {
                var fieldName = DataModelNamingHelper.ToCamelCase(col.ColumnName);
                var dartType = SqlTypeMapper.MapToDart(col.DataType, col.IsNullable, options.NullSafety);
                fields.Add((fieldName, dartType, col.ColumnName, col.IsNullable));

                if (fieldName != col.ColumnName)
                {
                    sb.AppendLine($"  @JsonKey(name: '{col.ColumnName}')");
                }

                var fieldModifier = options.FinalFields ? "final " : "";
                sb.AppendLine($"  {fieldModifier}{dartType} {fieldName};");
                sb.AppendLine();
            }

            // Constructor
            sb.AppendLine($"  {className}({{");
            foreach (var field in fields)
            {
                var req = (!field.IsNullable && options.NullSafety) ? "required " : "";
                sb.AppendLine($"    {req}this.{field.Name},");
            }
            sb.AppendLine("  });");
            sb.AppendLine();

            sb.AppendLine($"  factory {className}.fromJson(Map<String, dynamic> json) => _${className}FromJson(json);");
            sb.AppendLine($"  Map<String, dynamic> toJson() => _${className}ToJson(this);");
            sb.AppendLine("}");
        }

        private static void GenerateFreezed(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            DartOptions options)
        {
            var fileNameWithoutExt = DataModelNamingHelper.ToSnakeCase(className);

            sb.AppendLine("import 'dart:typed_data';");
            sb.AppendLine("import 'package:freezed_annotation/freezed_annotation.dart';");
            sb.AppendLine();
            sb.AppendLine($"part '{fileNameWithoutExt}.freezed.dart';");
            sb.AppendLine($"part '{fileNameWithoutExt}.g.dart';");
            sb.AppendLine();

            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/// " + table.Description.Replace("\r", "").Replace("\n", "\n/// "));
            }

            sb.AppendLine("@freezed");
            sb.AppendLine($"class {className} with _${className} {{");
            sb.AppendLine($"  const factory {className}({{");

            foreach (var col in table.Columns)
            {
                var fieldName = DataModelNamingHelper.ToCamelCase(col.ColumnName);
                var dartType = SqlTypeMapper.MapToDart(col.DataType, col.IsNullable, options.NullSafety);

                if (fieldName != col.ColumnName)
                {
                    sb.AppendLine($"    @JsonKey(name: '{col.ColumnName}')");
                }

                var req = (!col.IsNullable && options.NullSafety) ? "required " : "";
                sb.AppendLine($"    {req}{dartType} {fieldName},");
            }

            sb.AppendLine($"  }}) = _{className};");
            sb.AppendLine();
            sb.AppendLine($"  factory {className}.fromJson(Map<String, dynamic> json) => _${className}FromJson(json);");
            sb.AppendLine("}");
        }
    }
}
