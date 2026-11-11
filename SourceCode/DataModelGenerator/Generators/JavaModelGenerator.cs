using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.DataModelGenerator.Models;
using BprDevKit.DataModelGenerator.Services;

namespace BprDevKit.DataModelGenerator.Generators
{
    public static class JavaModelGenerator
    {
        public static GeneratedModelFile Generate(
            TableOperation.Information table,
            DataModelOptions globalOptions,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            var options = globalOptions.Java;
            var className = DataModelNamingHelper.FormatClassName(
                table.TableName,
                globalOptions.RemoveTablePrefixes,
                globalOptions.TablePrefixes,
                globalOptions.SingularizeClassNames,
                globalOptions.PropertyNaming,
                table.ObjectType);

            var sb = new StringBuilder();

            // Package
            if (!string.IsNullOrWhiteSpace(options.PackageName))
            {
                sb.AppendLine($"package {options.PackageName};");
                sb.AppendLine();
            }

            // Imports
            sb.AppendLine("import java.io.Serializable;");
            sb.AppendLine("import java.math.BigDecimal;");
            sb.AppendLine("import java.time.LocalDateTime;");
            sb.AppendLine("import java.time.LocalDate;");
            sb.AppendLine("import java.time.LocalTime;");
            sb.AppendLine("import java.time.OffsetDateTime;");
            sb.AppendLine("import java.util.UUID;");
            sb.AppendLine("import java.util.Objects;");
            sb.AppendLine("import java.util.List;");
            sb.AppendLine("import java.util.ArrayList;");

            if (options.Style == "jpa")
            {
                sb.AppendLine("import jakarta.persistence.*;");
            }

            if (options.Style == "lombok")
            {
                sb.AppendLine("import lombok.Data;");
                sb.AppendLine("import lombok.Builder;");
                sb.AppendLine("import lombok.NoArgsConstructor;");
                sb.AppendLine("import lombok.AllArgsConstructor;");
            }

            if (options.IncludeJacksonAnnotations)
            {
                sb.AppendLine("import com.fasterxml.jackson.annotation.JsonProperty;");
            }

            if (options.IncludeValidationAnnotations)
            {
                sb.AppendLine("import jakarta.validation.constraints.NotNull;");
                sb.AppendLine("import jakarta.validation.constraints.Size;");
            }

            sb.AppendLine();

            if (options.Style == "record")
            {
                GenerateRecord(sb, table, className, globalOptions, options);
            }
            else
            {
                GenerateClass(sb, table, className, globalOptions, options, allTables);
            }

            return new GeneratedModelFile
            {
                TableName = table.TableName,
                SchemaName = table.SchemaName,
                ClassName = className,
                FileName = $"{className}.java",
                Content = sb.ToString().TrimEnd() + Environment.NewLine,
                Language = DataModelLanguage.Java,
                ObjectType = table.ObjectType
            };
        }

        private static void GenerateRecord(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            JavaOptions options)
        {
            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/**");
                sb.AppendLine(" * " + table.Description.Replace("\r", "").Replace("\n", "\n * "));
                sb.AppendLine(" */");
            }

            var implementsClause = options.ImplementSerializable ? " implements Serializable" : "";
            sb.AppendLine($"public record {className}(");

            var cols = table.Columns.Where(c => DataModelFilterHelper.ShouldIncludeColumn(c, globalOptions)).ToList();
            for (int i = 0; i < cols.Count; i++)
            {
                var col = cols[i];
                var fieldName = DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                var javaType = SqlTypeMapper.MapToJava(col.DataType, col.IsNullable);
                var comma = (i < cols.Count - 1) ? "," : "";

                if (options.IncludeJacksonAnnotations && fieldName != col.ColumnName)
                {
                    sb.AppendLine($"    @JsonProperty(\"{col.ColumnName}\")");
                }

                sb.AppendLine($"    {javaType} {fieldName}{comma}");
            }

            sb.AppendLine($"){implementsClause} {{");
            sb.AppendLine("}");
        }

        private static void GenerateClass(
            StringBuilder sb,
            TableOperation.Information table,
            string className,
            DataModelOptions globalOptions,
            JavaOptions options,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine("/**");
                sb.AppendLine(" * " + table.Description.Replace("\r", "").Replace("\n", "\n * "));
                sb.AppendLine(" */");
            }

            if (options.Style == "jpa")
            {
                sb.AppendLine("@Entity");
                if (!string.IsNullOrWhiteSpace(table.SchemaName) && !string.Equals(table.SchemaName, "dbo", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"@Table(name = \"{table.TableName}\", schema = \"{table.SchemaName}\")");
                }
                else
                {
                    sb.AppendLine($"@Table(name = \"{table.TableName}\")");
                }
            }

            if (options.Style == "lombok")
            {
                sb.AppendLine("@Data");
                sb.AppendLine("@Builder");
                sb.AppendLine("@NoArgsConstructor");
                sb.AppendLine("@AllArgsConstructor");
            }

            var implementsClause = options.ImplementSerializable ? " implements Serializable" : "";
            sb.AppendLine($"public class {className}{implementsClause} {{");

            if (options.ImplementSerializable)
            {
                sb.AppendLine("    private static final long serialVersionUID = 1L;");
                sb.AppendLine();
            }

            var fields = new List<(string Name, string Type, string OriginalName, ColumnOperation.Information Col)>();

            foreach (var col in table.Columns)
            {
                if (!DataModelFilterHelper.ShouldIncludeColumn(col, globalOptions))
                {
                    continue;
                }

                var fieldName = DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                var javaType = SqlTypeMapper.MapToJava(col.DataType, col.IsNullable);
                fields.Add((fieldName, javaType, col.ColumnName, col));

                if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine("    /**");
                    sb.AppendLine($"     * {col.Description.Replace("\r", "").Replace("\n", " ")}");
                    sb.AppendLine("     */");
                }

                if (options.Style == "jpa")
                {
                    if (col.IsPrimaryKey)
                    {
                        sb.AppendLine("    @Id");
                        if (col.IsIdentity)
                        {
                            sb.AppendLine("    @GeneratedValue(strategy = GenerationType.IDENTITY)");
                        }
                    }

                    sb.AppendLine($"    @Column(name = \"{col.ColumnName}\", nullable = {(col.IsNullable ? "true" : "false")})");
                }

                if (options.IncludeValidationAnnotations && !col.IsNullable)
                {
                    sb.AppendLine("    @NotNull");
                }

                if (options.IncludeValidationAnnotations && col.Length.HasValue && col.Length.Value > 0 && col.Length.Value < 8000 && col.DataType.Contains("char"))
                {
                    sb.AppendLine($"    @Size(max = {col.Length.Value})");
                }

                if (options.IncludeJacksonAnnotations)
                {
                    sb.AppendLine($"    @JsonProperty(\"{col.ColumnName}\")");
                }

                sb.AppendLine($"    private {javaType} {fieldName};");
                sb.AppendLine();
            }

            // JPA Relationships
            var allowedFks = table.ForeignKeys
                .Where(fk => string.Equals(fk.TableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                          && (string.IsNullOrWhiteSpace(fk.SchemaName) || string.Equals(fk.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase))
                          && !string.IsNullOrWhiteSpace(fk.RefTableName))
                .Where(fk => globalOptions.IsSchemaAccessAllowed(table.SchemaName, string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName))
                .ToList();

            if (options.Style == "jpa" && globalOptions.IncludeNavigationProperties && (string.IsNullOrWhiteSpace(table.ObjectType) || string.Equals(table.ObjectType, Constants.ObjectTypes.Table, StringComparison.OrdinalIgnoreCase)) && allowedFks.Count > 0)
            {
                sb.AppendLine("    // JPA Relationships");
                foreach (var fk in allowedFks)
                {
                    var parentClassName = DataModelNamingHelper.FormatClassName(
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

                    var localColProp = DataModelNamingHelper.FormatPropertyName(fk.ColumnName, globalOptions.PropertyNaming, globalOptions.RemoveColumnPrefixes, globalOptions.ColumnPrefixes);
                    var fieldName = DataModelNamingHelper.ToCamelCase(parentClassName) + (multipleToTarget ? localColProp : "");
                    sb.AppendLine("    @ManyToOne(fetch = FetchType.LAZY)");
                    sb.AppendLine($"    @JoinColumn(name = \"{fk.ColumnName}\")");
                    sb.AppendLine($"    private {parentClassName} {fieldName};");
                    sb.AppendLine();
                }
            }

            // If not Lombok, generate constructors, getters, setters, equals, hashCode, toString
            if (options.Style != "lombok")
            {
                // Default constructor
                sb.AppendLine($"    public {className}() {{");
                sb.AppendLine("    }");
                sb.AppendLine();

                // Getters & Setters
                foreach (var field in fields)
                {
                    var isComputed = DataModelFilterHelper.IsComputed(field.Col);
                    var isReadOnly = isComputed && globalOptions.ComputedColumnMode == ComputedColumnHandling.IncludeReadOnly;

                    var propPascal = DataModelNamingHelper.ToPascalCase(field.Name);
                    // Getter
                    var getterPrefix = field.Type == "boolean" || field.Type == "Boolean" ? "is" : "get";
                    sb.AppendLine($"    public {field.Type} {getterPrefix}{propPascal}() {{");
                    sb.AppendLine($"        return this.{field.Name};");
                    sb.AppendLine("    }");
                    sb.AppendLine();

                    // Setter
                    if (!isReadOnly)
                    {
                        sb.AppendLine($"    public void set{propPascal}({field.Type} {field.Name}) {{");
                        sb.AppendLine($"        this.{field.Name} = {field.Name};");
                        sb.AppendLine("    }");
                        sb.AppendLine();
                    }
                }

                // toString
                sb.AppendLine("    @Override");
                sb.AppendLine("    public String toString() {");
                var propsStr = string.Join(" + \", \" + ", fields.Take(5).Select(f => $"\"{f.Name}=\" + {f.Name}"));
                sb.AppendLine($"        return \"{className}{{\" + {propsStr} + \"}}\";");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
        }
    }
}
