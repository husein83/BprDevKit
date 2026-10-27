using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DataModelGenerator.Generators
{
    public static class CSharpModelGenerator
    {
        public static GeneratedModelFile Generate(
            TableOperation.Information table,
            DataModelOptions globalOptions,
            IReadOnlyList<TableOperation.Information> allTables)
        {
            var options = globalOptions.CSharp;
            var className = DataModelNamingHelper.FormatClassName(
                table.TableName,
                globalOptions.RemoveTablePrefixes,
                globalOptions.TablePrefixes,
                globalOptions.SingularizeClassNames);

            var resolvedNamespace = ResolveNamespace(
                options,
                table.SchemaName,
                table.DatabaseName,
                table.TableName);

            var sb = new StringBuilder();

            // Usings collection
            var usings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            usings.Add("System");
            usings.Add("System.Collections.Generic");

            if (options.IncludeDataAnnotations)
            {
                usings.Add("System.ComponentModel.DataAnnotations");
                usings.Add("System.ComponentModel.DataAnnotations.Schema");
            }

            if (options.IncludeJsonAttributes)
            {
                usings.Add("System.Text.Json.Serialization");
            }

            if (options.IncludeEfCoreTableAttributes || options.IncludeIndexesAsAttributes || options.NavigationStyle == "Enterprise")
            {
                usings.Add("Microsoft.EntityFrameworkCore");
            }

            // Check if any transform rule requires NotMapped or custom usings
            if (options.TransformRules != null)
            {
                foreach (var rule in options.TransformRules.Where(r => r.IsEnabled))
                {
                    if (rule.ActionType == "NotMappedHelper")
                    {
                        usings.Add("System.ComponentModel.DataAnnotations.Schema");
                    }
                    if (!string.IsNullOrWhiteSpace(rule.RequiredUsing))
                    {
                        var reqUsings = rule.RequiredUsing.Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var ru in reqUsings)
                        {
                            var cleanRu = ru.StartsWith("using ", StringComparison.OrdinalIgnoreCase) ? ru.Substring(6).Trim().TrimEnd(';') : ru.TrimEnd(';');
                            if (!string.IsNullOrWhiteSpace(cleanRu)) usings.Add(cleanRu);
                        }
                    }
                }
            }

            // Custom usings
            if (!string.IsNullOrWhiteSpace(options.CustomUsings))
            {
                var customLines = options.CustomUsings.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var line in customLines)
                {
                    var cleanLine = line.Trim();
                    if (cleanLine.StartsWith("using ", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanLine = cleanLine.Substring(6).Trim().TrimEnd(';');
                    }
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        usings.Add(cleanLine);
                    }
                }
            }

            // Write Usings
            foreach (var u in usings.OrderBy(x => x.StartsWith("System", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(x => x))
            {
                sb.AppendLine($"using {u};");
            }

            sb.AppendLine();

            // Namespace
            if (!string.IsNullOrWhiteSpace(resolvedNamespace))
            {
                sb.AppendLine($"namespace {resolvedNamespace}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrWhiteSpace(resolvedNamespace) ? "" : "    ";

            // Class-level comment
            if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {table.Description.Replace("\r", "").Replace("\n", $" \n{indent}/// ")}");
                sb.AppendLine($"{indent}/// </summary>");
            }

            // Table attribute
            if (options.IncludeEfCoreTableAttributes)
            {
                if (options.UseNameofInAttributes)
                {
                    if (!string.IsNullOrWhiteSpace(table.SchemaName) && !string.Equals(table.SchemaName, "dbo", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"{indent}[Table(nameof({className}), Schema = nameof({table.SchemaName}))]");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}[Table(nameof({className}))]");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(table.SchemaName) && !string.Equals(table.SchemaName, "dbo", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"{indent}[Table(\"{table.TableName}\", Schema = \"{table.SchemaName}\")]");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}[Table(\"{table.TableName}\")]");
                    }
                }
            }

            // Index attributes from real database indexes
            if (options.IncludeIndexesAsAttributes && table.Indexes.Count > 0)
            {
                var nonPkIndexes = table.Indexes
                    .Where(i => !i.IsPrimaryKey && !string.IsNullOrWhiteSpace(i.IndexName))
                    .OrderBy(i => i.IndexName)
                    .ToList();

                foreach (var idx in nonPkIndexes)
                {
                    var cols = ParseIndexColumns(idx.Columns);
                    if (cols.Count == 0) continue;

                    var colArgs = new List<string>();
                    foreach (var c in cols)
                    {
                        var propName = DataModelNamingHelper.FormatPropertyName(c, globalOptions.PropertyNaming);
                        if (options.UseNameofInAttributes)
                        {
                            colArgs.Add($"nameof({propName})");
                        }
                        else
                        {
                            colArgs.Add($"\"{propName}\"");
                        }
                    }

                    var uniqueStr = idx.IsUnique ? ", IsUnique = true" : "";
                    sb.AppendLine($"{indent}[Index({string.Join(", ", colArgs)}, Name = \"{idx.IndexName}\"{uniqueStr})]");
                }
            }

            // Class-level custom attributes
            if (!string.IsNullOrWhiteSpace(options.CustomClassAttributes))
            {
                var customAttrs = options.CustomClassAttributes
                    .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                    .Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{Database}", table.DatabaseName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

                foreach (var line in customAttrs.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var attr = line.Trim();
                    if (!attr.StartsWith("[")) attr = $"[{attr}]";
                    sb.AppendLine($"{indent}{attr}");
                }
            }

            // Class header: public [partial] class ClassName [: BaseClass, Interfaces]
            var partialKeyword = options.IsPartial ? "partial " : "";
            var typeKeyword = options.TypeKind switch
            {
                "record" => "record",
                "struct" => "struct",
                _ => "class"
            };

            var inheritances = new List<string>();
            if (!string.IsNullOrWhiteSpace(options.BaseClass))
            {
                inheritances.Add(options.BaseClass.Trim());
            }
            if (!string.IsNullOrWhiteSpace(options.Interfaces))
            {
                var ifaces = options.Interfaces.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                inheritances.AddRange(ifaces);
            }

            var inheritanceStr = inheritances.Count > 0 ? " : " + string.Join(", ", inheritances) : "";
            sb.AppendLine($"{indent}public {partialKeyword}{typeKeyword} {className}{inheritanceStr}");
            sb.AppendLine($"{indent}{{");

            // Outer Region (e.g. #region Generator)
            if (options.UseRegions && !string.IsNullOrWhiteSpace(options.OuterRegionName))
            {
                sb.AppendLine($"{indent}    #region {options.OuterRegionName.Trim()}");
                sb.AppendLine();
            }

            // #region Constructors
            if (options.UseRegions && options.IncludeConstructor)
            {
                sb.AppendLine($"{indent}    #region Constructors");
                sb.AppendLine($"{indent}    public {className}()");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    #endregion");
                sb.AppendLine();
            }

            // Parse override properties list
            var overridePropNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(options.OverrideProperties))
            {
                var opList = options.OverrideProperties.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var op in opList) overridePropNames.Add(op);
            }

            // #region Columns
            if (options.UseRegions)
            {
                sb.AppendLine($"{indent}    #region Columns");
            }

            var fkColumnNames = new HashSet<string>(table.ForeignKeys.Select(f => f.ColumnName), StringComparer.OrdinalIgnoreCase);

            foreach (var col in table.Columns)
            {
                var propName = DataModelNamingHelper.FormatPropertyName(col.ColumnName, globalOptions.PropertyNaming);
                if (propName == className)
                {
                    propName += "Property";
                }

                var csharpType = SqlTypeMapper.MapToCSharp(col.DataType, col.IsNullable, options.NullableReferenceTypes);
                var isFk = fkColumnNames.Contains(col.ColumnName);

                // Find any matching transform rule
                PropertyTransformRule? matchingRule = null;
                if (options.TransformRules != null && options.TransformRules.Count > 0)
                {
                    matchingRule = options.TransformRules.FirstOrDefault(r =>
                        r.IsEnabled &&
                        (MatchesPattern(col.ColumnName, r.ColumnNamePattern) || MatchesPattern(col.DataType, r.DataTypePattern)));
                }

                // If rule overrides the type
                if (matchingRule != null && matchingRule.ActionType == "TypeOverride" && !string.IsNullOrWhiteSpace(matchingRule.TargetType))
                {
                    csharpType = matchingRule.TargetType;
                }

                // Documentation comments
                if (globalOptions.IncludeDocumentationComments && !string.IsNullOrWhiteSpace(col.Description))
                {
                    sb.AppendLine($"{indent}    /// <summary>");
                    sb.AppendLine($"{indent}    /// {col.Description.Replace("\r", "").Replace("\n", $" \n{indent}    /// ")}");
                    sb.AppendLine($"{indent}    /// </summary>");
                }

                var backingFieldName = "_" + DataModelNamingHelper.ToCamelCase(propName);
                if (options.PropertyAccessor == "backing_field")
                {
                    sb.AppendLine($"{indent}    private {csharpType} {backingFieldName};");
                }

                // Data Annotations
                if (options.IncludeDataAnnotations)
                {
                    if (col.IsPrimaryKey)
                    {
                        sb.AppendLine($"{indent}    [Key]");
                    }

                    if (col.IsIdentity)
                    {
                        sb.AppendLine($"{indent}    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                    }

                    if (options.IncludeTimestampAttribute && (string.Equals(col.DataType, "rowversion", StringComparison.OrdinalIgnoreCase) || string.Equals(col.DataType, "timestamp", StringComparison.OrdinalIgnoreCase)))
                    {
                        sb.AppendLine($"{indent}    [Timestamp]");
                    }

                    if (!col.IsNullable && (csharpType == "string" || csharpType.StartsWith("byte[]")) && !string.Equals(col.DataType, "rowversion", StringComparison.OrdinalIgnoreCase) && !string.Equals(col.DataType, "timestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"{indent}    [Required]");
                    }

                    if (col.Length.HasValue && col.Length.Value > 0 && col.Length.Value < 8000 && (col.DataType.Contains("char") || col.DataType.Contains("varchar")))
                    {
                        sb.AppendLine($"{indent}    [StringLength({col.Length.Value})]");
                    }

                    if (propName != col.ColumnName)
                    {
                        sb.AppendLine($"{indent}    [Column(\"{col.ColumnName}\")]");
                    }
                }

                // Custom PK attribute
                if (options.EnableCustomPkAttribute && col.IsPrimaryKey && !string.IsNullOrWhiteSpace(options.CustomPkAttributeTemplate))
                {
                    var customPk = InterpolateAttributeTemplate(
                        options.CustomPkAttributeTemplate,
                        className,
                        propName,
                        col.ColumnName,
                        csharpType,
                        table.TableName,
                        table.SchemaName,
                        length: col.Length,
                        precision: col.Precision,
                        scale: col.Scale,
                        database: table.DatabaseName);
                    sb.AppendLine($"{indent}    {customPk}");
                }

                // Custom FK attribute
                if (options.EnableCustomFkAttribute && isFk && !string.IsNullOrWhiteSpace(options.CustomFkAttributeTemplate))
                {
                    var fkInfo = table.ForeignKeys.FirstOrDefault(f => string.Equals(f.ColumnName, col.ColumnName, StringComparison.OrdinalIgnoreCase));
                    var toSchema = fkInfo != null && !string.IsNullOrWhiteSpace(fkInfo.RefSchemaName) ? fkInfo.RefSchemaName : table.SchemaName;
                    if (globalOptions.IsSchemaAccessAllowed(table.SchemaName, toSchema))
                    {
                        var customFk = InterpolateAttributeTemplate(
                            options.CustomFkAttributeTemplate,
                            className,
                            propName,
                            col.ColumnName,
                            csharpType,
                            table.TableName,
                            table.SchemaName,
                            fkInfo?.RefTableName,
                            fkInfo?.RefColumnName,
                            length: col.Length,
                            precision: col.Precision,
                            scale: col.Scale,
                            database: table.DatabaseName);
                        sb.AppendLine($"{indent}    {customFk}");
                    }
                }

                // Custom Column Attribute Rules
                if (!string.IsNullOrWhiteSpace(options.CustomColumnAttributeRules))
                {
                    var rules = options.CustomColumnAttributeRules.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var rule in rules)
                    {
                        var ruleParts = rule.Split(new[] { ':' }, 2);
                        if (ruleParts.Length == 2)
                        {
                            var pattern = ruleParts[0].Trim();
                            var attrTemplate = ruleParts[1].Trim();
                            if (MatchesPattern(col.ColumnName, pattern) || MatchesPattern(col.DataType, pattern))
                            {
                                var customAttr = InterpolateAttributeTemplate(
                                    attrTemplate,
                                    className,
                                    propName,
                                    col.ColumnName,
                                    csharpType,
                                    table.TableName,
                                    table.SchemaName,
                                    length: col.Length,
                                    precision: col.Precision,
                                    scale: col.Scale,
                                    database: table.DatabaseName);
                                sb.AppendLine($"{indent}    {customAttr}");
                            }
                        }
                    }
                }

                // JSON attribute
                if (options.IncludeJsonAttributes)
                {
                    sb.AppendLine($"{indent}    [JsonPropertyName(\"{col.ColumnName}\")]");
                }

                // Default property attributes (e.g. [DataMember])
                if (!string.IsNullOrWhiteSpace(options.DefaultPropertyAttributes))
                {
                    foreach (var attrLine in options.DefaultPropertyAttributes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var attr = InterpolateAttributeTemplate(
                            attrLine.Trim(),
                            className,
                            propName,
                            col.ColumnName,
                            csharpType,
                            table.TableName,
                            table.SchemaName,
                            length: col.Length,
                            precision: col.Precision,
                            scale: col.Scale);

                        if (!string.IsNullOrWhiteSpace(attr))
                        {
                            if (!attr.StartsWith("[")) attr = $"[{attr}]";
                            sb.AppendLine($"{indent}    {attr}");
                        }
                    }
                }

                var isOverridden = overridePropNames.Contains(propName) || overridePropNames.Contains(col.ColumnName);
                var modifier = isOverridden ? "public override " : "public ";

                if (matchingRule != null && matchingRule.ActionType == "CustomGetterSetter")
                {
                    // Custom Getter / Setter body
                    var getterCode = InterpolateTemplate(matchingRule.GetterTemplate, propName, col.ColumnName, matchingRule.TargetType, className);
                    var setterCode = InterpolateTemplate(matchingRule.SetterTemplate, propName, col.ColumnName, matchingRule.TargetType, className);

                    sb.AppendLine($"{indent}    {modifier}{csharpType} {propName}");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        get => {getterCode};");
                    sb.AppendLine($"{indent}        set => {setterCode};");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                }
                else if (options.PropertyAccessor == "backing_field")
                {
                    sb.AppendLine($"{indent}    {modifier}{csharpType} {propName}");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        get");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            return {backingFieldName};");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}        set");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            {backingFieldName} = value;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                }
                else
                {
                    var accessor = options.PropertyAccessor == "get_init" ? "{ get; init; }" : "{ get; set; }";

                    var defaultValue = "";
                    if (!col.IsNullable && options.NullableReferenceTypes)
                    {
                        if (options.StringInitialization == "NullForgiving")
                        {
                            if (csharpType == "string" || csharpType.StartsWith("byte[]"))
                            {
                                defaultValue = " = null!;";
                            }
                        }
                        else if (options.StringInitialization == "EmptyString")
                        {
                            if (csharpType == "string") defaultValue = " = string.Empty;";
                            else if (csharpType == "byte[]") defaultValue = " = Array.Empty<byte>();";
                        }
                    }

                    sb.AppendLine($"{indent}    {modifier}{csharpType} {propName} {accessor}{defaultValue}");
                    sb.AppendLine();
                }

                // Smart Transformation Rule: NotMappedHelper
                if (matchingRule != null && matchingRule.ActionType == "NotMappedHelper")
                {
                    var helperName = InterpolateTemplate(matchingRule.HelperPropertyNameTemplate, propName, col.ColumnName, matchingRule.TargetType, className);
                    if (string.IsNullOrWhiteSpace(helperName))
                    {
                        helperName = propName + "Helper";
                    }

                    var getterCode = InterpolateTemplate(matchingRule.GetterTemplate, propName, col.ColumnName, matchingRule.TargetType, className);
                    var setterCode = InterpolateTemplate(matchingRule.SetterTemplate, propName, col.ColumnName, matchingRule.TargetType, className);

                    if (options.PropertyAccessor == "backing_field")
                    {
                        var helperBacking = "_" + DataModelNamingHelper.ToCamelCase(helperName);
                        sb.AppendLine($"{indent}    private {matchingRule.TargetType} {helperBacking};");
                        sb.AppendLine($"{indent}    [NotMapped]");
                        if (!string.IsNullOrWhiteSpace(options.DefaultPropertyAttributes))
                        {
                            foreach (var attrLine in options.DefaultPropertyAttributes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                var attr = attrLine.Trim();
                                if (!attr.StartsWith("[")) attr = $"[{attr}]";
                                sb.AppendLine($"{indent}    {attr}");
                            }
                        }
                        sb.AppendLine($"{indent}    public {matchingRule.TargetType} {helperName}");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        get");
                        sb.AppendLine($"{indent}        {{");
                        sb.AppendLine($"{indent}            return {helperBacking};");
                        sb.AppendLine($"{indent}        }}");
                        sb.AppendLine($"{indent}        set");
                        sb.AppendLine($"{indent}        {{");
                        sb.AppendLine($"{indent}            {helperBacking} = value;");
                        sb.AppendLine($"{indent}        }}");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"{indent}    [NotMapped]");
                        sb.AppendLine($"{indent}    public {matchingRule.TargetType} {helperName}");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        get => {getterCode};");
                        sb.AppendLine($"{indent}        set => {setterCode};");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine();
                    }
                }
            }

            if (options.UseRegions)
            {
                sb.AppendLine($"{indent}    #endregion");
                sb.AppendLine();
            }

            // Navigation Properties (Outgoing Foreign Keys / References)
            // A table only has an outgoing reference if it owns the FK column (fk.TableName == table.TableName)
            var allowedFks = table.ForeignKeys
                .Where(fk => string.Equals(fk.TableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                          && (string.IsNullOrWhiteSpace(fk.SchemaName) || string.Equals(fk.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                .Where(fk => globalOptions.IsSchemaAccessAllowed(table.SchemaName, string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName))
                .ToList();

            if (globalOptions.IncludeNavigationProperties && allowedFks.Count > 0)
            {
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #region References");
                }

                foreach (var fk in allowedFks)
                {
                    if (string.IsNullOrWhiteSpace(fk.RefTableName)) continue;

                    var refClassName = DataModelNamingHelper.FormatClassName(
                        fk.RefTableName,
                        globalOptions.RemoveTablePrefixes,
                        globalOptions.TablePrefixes,
                        globalOptions.SingularizeClassNames);

                    var localPropName = DataModelNamingHelper.FormatPropertyName(fk.ColumnName, globalOptions.PropertyNaming);
                    var refSchema = string.IsNullOrWhiteSpace(fk.RefSchemaName) ? table.SchemaName : fk.RefSchemaName;
                    var refNamespace = ResolveNamespace(options, refSchema, table.DatabaseName ?? string.Empty, fk.RefTableName);
                    var currentNamespace = ResolveNamespace(options, table.SchemaName, table.DatabaseName ?? string.Empty, table.TableName);

                    var isDifferentNamespace = !string.Equals(refNamespace, currentNamespace, StringComparison.OrdinalIgnoreCase);
                    var useFullNav = options.UseFullNamespaceForNavTypes || isDifferentNamespace || string.Equals(options.NamespaceMode, "PerSchema", StringComparison.OrdinalIgnoreCase);

                    var targetNamespacePrefix = useFullNav && !string.IsNullOrWhiteSpace(refNamespace)
                        ? $"{refNamespace}."
                        : "";

                    var navTypeName = $"{targetNamespacePrefix}{refClassName}";
                    var targetInversePropName = $"{className}Set{localPropName}";

                    // Only output InverseProperty if the target schema also has permission to access this schema
                    bool canTargetAccessCurrent = globalOptions.IsSchemaAccessAllowed(refSchema, table.SchemaName);

                    var refPropName = options.NavigationStyle == "Enterprise"
                        ? $"{refClassName}Ref{localPropName}"
                        : (refClassName == className ? "Parent" + refClassName : refClassName);

                    var refBackingName = "_" + DataModelNamingHelper.ToCamelCase(refPropName);
                    if (options.PropertyAccessor == "backing_field")
                    {
                        sb.AppendLine($"{indent}    private {navTypeName}? {refBackingName};");
                    }

                    if (!string.IsNullOrWhiteSpace(options.CustomNavReferenceAttribute))
                    {
                        var targetPk = string.IsNullOrWhiteSpace(fk.RefColumnName) ? "Id" : DataModelNamingHelper.FormatPropertyName(fk.RefColumnName, globalOptions.PropertyNaming);
                        var customRef = options.CustomNavReferenceAttribute
                            .Replace("{FkProperty}", localPropName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{TargetType}", navTypeName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{TargetClass}", refClassName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{OtherKey}", targetPk, StringComparison.OrdinalIgnoreCase);

                        if (!customRef.StartsWith("[")) customRef = $"[{customRef}]";
                        sb.AppendLine($"{indent}    {customRef}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}    [ForeignKey(nameof({localPropName}))]");
                    }

                    if (canTargetAccessCurrent)
                    {
                        if (options.NavigationStyle == "Enterprise")
                        {
                            sb.AppendLine($"{indent}    [InverseProperty(nameof({navTypeName}.{targetInversePropName}))]");
                        }
                        else if (options.IncludeEfCoreTableAttributes)
                        {
                            sb.AppendLine($"{indent}    [InverseProperty(nameof({navTypeName}.{className}))]");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(options.NavigationPropertyAttributes))
                    {
                        foreach (var navAttrLine in options.NavigationPropertyAttributes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            var attr = navAttrLine.Trim();
                            if (!attr.StartsWith("[")) attr = $"[{attr}]";
                            sb.AppendLine($"{indent}    {attr}");
                        }
                    }

                    if (options.PropertyAccessor == "backing_field")
                    {
                        sb.AppendLine($"{indent}    public virtual {navTypeName}? {refPropName}");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        get");
                        sb.AppendLine($"{indent}        {{");
                        sb.AppendLine($"{indent}            return {refBackingName};");
                        sb.AppendLine($"{indent}        }}");
                        sb.AppendLine($"{indent}        set");
                        sb.AppendLine($"{indent}        {{");
                        sb.AppendLine($"{indent}            {refBackingName} = value;");
                        sb.AppendLine($"{indent}        }}");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine();
                    }
                    else
                    {
                        var nullSuffix = options.NullableReferenceTypes ? "?" : "";
                        sb.AppendLine($"{indent}    public virtual {navTypeName}{nullSuffix} {refPropName} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }

                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #endregion");
                    sb.AppendLine();
                }
            }

            // Reverse Navigation Properties (Incoming Foreign Keys / Relations / Children)
            if (globalOptions.IncludeNavigationProperties)
            {
                var childFks = allTables
                    .SelectMany(t => t.ForeignKeys.Select(f => new { ChildTable = t, Fk = f }))
                    .Where(x => string.Equals(x.Fk.TableName, x.ChildTable.TableName, StringComparison.OrdinalIgnoreCase)
                             && (string.IsNullOrWhiteSpace(x.Fk.SchemaName) || string.Equals(x.Fk.SchemaName, x.ChildTable.SchemaName, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => string.Equals(x.Fk.RefTableName, table.TableName, StringComparison.OrdinalIgnoreCase)
                             && (string.IsNullOrWhiteSpace(x.Fk.RefSchemaName) || string.Equals(x.Fk.RefSchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => globalOptions.IsSchemaAccessAllowed(table.SchemaName, x.ChildTable.SchemaName))
                    .OrderBy(x => x.ChildTable.TableName)
                    .ThenBy(x => x.Fk.ColumnName)
                    .ToList();

                if (childFks.Count > 0)
                {
                    if (options.UseRegions)
                    {
                        sb.AppendLine($"{indent}    #region Relations");
                    }

                    foreach (var child in childFks)
                    {
                        var childClassName = DataModelNamingHelper.FormatClassName(
                            child.ChildTable.TableName,
                            globalOptions.RemoveTablePrefixes,
                            globalOptions.TablePrefixes,
                            globalOptions.SingularizeClassNames);

                        var childFkPropName = DataModelNamingHelper.FormatPropertyName(child.Fk.ColumnName, globalOptions.PropertyNaming);
                        var childNamespace = ResolveNamespace(options, child.ChildTable.SchemaName, table.DatabaseName ?? string.Empty, child.ChildTable.TableName);
                        var currentNamespace = ResolveNamespace(options, table.SchemaName, table.DatabaseName ?? string.Empty, table.TableName);

                        var isDifferentNamespace = !string.Equals(childNamespace, currentNamespace, StringComparison.OrdinalIgnoreCase);
                        var useFullNav = options.UseFullNamespaceForNavTypes || isDifferentNamespace || string.Equals(options.NamespaceMode, "PerSchema", StringComparison.OrdinalIgnoreCase);

                        var targetNamespacePrefix = useFullNav && !string.IsNullOrWhiteSpace(childNamespace)
                            ? $"{childNamespace}."
                            : "";

                        var childFullTypeName = $"{targetNamespacePrefix}{childClassName}";
                        var childInversePropName = $"{className}Ref{childFkPropName}";

                        // Only output InverseProperty if child schema has permission to access current schema
                        bool canChildAccessCurrent = globalOptions.IsSchemaAccessAllowed(child.ChildTable.SchemaName, table.SchemaName);

                        var collectionPropName = options.NavigationStyle == "Enterprise"
                            ? (options.IncludeSchemaInRelationName && !string.IsNullOrWhiteSpace(child.ChildTable.SchemaName)
                                ? $"{child.ChildTable.SchemaName}_{childClassName}Set{childFkPropName}"
                                : $"{childClassName}Set{childFkPropName}")
                            : (childClassName == className ? "Child" + childClassName + "s" : childClassName + "s");

                        var colBackingName = "_" + DataModelNamingHelper.ToCamelCase(collectionPropName);
                        if (options.PropertyAccessor == "backing_field")
                        {
                            sb.AppendLine($"{indent}    private ICollection<{childFullTypeName}> {colBackingName};");
                        }

                        if (!string.IsNullOrWhiteSpace(options.CustomNavInversePropertyAttribute))
                        {
                            var customInv = options.CustomNavInversePropertyAttribute
                                .Replace("{InverseType}", childFullTypeName, StringComparison.OrdinalIgnoreCase)
                                .Replace("{InverseProperty}", childInversePropName, StringComparison.OrdinalIgnoreCase)
                                .Replace("{ChildClass}", childClassName, StringComparison.OrdinalIgnoreCase)
                                .Replace("{ChildFk}", childFkPropName, StringComparison.OrdinalIgnoreCase)
                                .Replace("{OtherKey}", childFkPropName, StringComparison.OrdinalIgnoreCase);

                            if (!customInv.StartsWith("[")) customInv = $"[{customInv}]";
                            sb.AppendLine($"{indent}    {customInv}");
                        }
                        else if (canChildAccessCurrent)
                        {
                            if (options.NavigationStyle == "Enterprise")
                            {
                                sb.AppendLine($"{indent}    [InverseProperty(nameof({childFullTypeName}.{childInversePropName}))]");
                            }
                            else if (options.IncludeEfCoreTableAttributes && !string.IsNullOrWhiteSpace(child.Fk.ColumnName))
                            {
                                sb.AppendLine($"{indent}    [InverseProperty(nameof({childFullTypeName}.{className}))]");
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(options.NavigationPropertyAttributes))
                        {
                            foreach (var navAttrLine in options.NavigationPropertyAttributes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                var attr = navAttrLine.Trim();
                                if (!attr.StartsWith("[")) attr = $"[{attr}]";
                                sb.AppendLine($"{indent}    {attr}");
                            }
                        }

                        if (options.PropertyAccessor == "backing_field")
                        {
                            sb.AppendLine($"{indent}    public virtual ICollection<{childFullTypeName}> {collectionPropName}");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        get");
                            sb.AppendLine($"{indent}        {{");
                            sb.AppendLine($"{indent}            return {colBackingName};");
                            sb.AppendLine($"{indent}        }}");
                            sb.AppendLine($"{indent}        set");
                            sb.AppendLine($"{indent}        {{");
                            sb.AppendLine($"{indent}            {colBackingName} = value;");
                            sb.AppendLine($"{indent}        }}");
                            sb.AppendLine($"{indent}    }}");
                            sb.AppendLine();
                        }
                        else
                        {
                            if (options.MakeCollectionsNullable)
                            {
                                sb.AppendLine($"{indent}    public virtual ICollection<{childFullTypeName}>? {collectionPropName} {{ get; set; }}");
                            }
                            else
                            {
                                sb.AppendLine($"{indent}    public virtual ICollection<{childFullTypeName}> {collectionPropName} {{ get; set; }} = new List<{childFullTypeName}>();");
                            }
                            sb.AppendLine();
                        }
                    }

                    if (options.UseRegions)
                    {
                        sb.AppendLine($"{indent}    #endregion");
                        sb.AppendLine();
                    }
                }
            }

            // #region Override
            if (options.IncludeOverrides && options.Overrides.Count > 0)
            {
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #region Override");
                }

                foreach (var ov in options.Overrides)
                {
                    if (string.IsNullOrWhiteSpace(ov.Name)) continue;
                    var mod = string.IsNullOrWhiteSpace(ov.AccessModifier) ? "public" : ov.AccessModifier;
                    var retType = string.IsNullOrWhiteSpace(ov.ReturnType) ? "string" : ov.ReturnType;
                    var body = ov.Body ?? string.Empty;

                    // Token replacement
                    body = body.Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                               .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase)
                               .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                               .Replace("{Database}", table.DatabaseName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

                    var name = ov.Name.Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                                      .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase)
                                      .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase);

                    if (string.Equals(ov.MemberKind, "Method", StringComparison.OrdinalIgnoreCase))
                    {
                        var parameters = (ov.Parameters ?? string.Empty)
                            .Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                            .Replace("{Database}", table.DatabaseName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

                        if (ov.IsExpressionBodied)
                        {
                            var trimmed = body.TrimEnd(';').Trim();
                            sb.AppendLine($"{indent}    {mod} override {retType} {name}({parameters}) => {trimmed};");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    {mod} override {retType} {name}({parameters})");
                            sb.AppendLine($"{indent}    {{");
                            var lines = body.Replace("\r\n", "\n").Split('\n');
                            foreach (var line in lines)
                            {
                                sb.AppendLine($"{indent}        {line}");
                            }
                            sb.AppendLine($"{indent}    }}");
                        }
                    }
                    else
                    {
                        // Property
                        if (ov.IsExpressionBodied)
                        {
                            var trimmed = body.TrimEnd(';').Trim();
                            sb.AppendLine($"{indent}    {mod} override {retType} {name} => {trimmed};");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    {mod} override {retType} {name}");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        get");
                            sb.AppendLine($"{indent}        {{");
                            var lines = body.Replace("\r\n", "\n").Split('\n');
                            foreach (var line in lines)
                            {
                                sb.AppendLine($"{indent}            {line}");
                            }
                            sb.AppendLine($"{indent}        }}");
                            sb.AppendLine($"{indent}    }}");
                        }
                    }
                }

                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #endregion");
                    sb.AppendLine();
                }
            }
            else if (options.IncludeTableSchemaOverrides)
            {
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #region Override");
                }
                sb.AppendLine($"{indent}    public override string GetTableName => TABLE_NAME;");
                sb.AppendLine($"{indent}    public override string GetSchemaName => SCHEMA_NAME;");
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #endregion");
                    sb.AppendLine();
                }
            }

            // #region Constants
            if (options.IncludeConstants && options.Constants.Count > 0)
            {
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #region Constants");
                }
                foreach (var c in options.Constants)
                {
                    if (string.IsNullOrWhiteSpace(c.Name)) continue;
                    var mod = string.IsNullOrWhiteSpace(c.AccessModifier) ? "public" : c.AccessModifier;
                    var type = string.IsNullOrWhiteSpace(c.Type) ? "string" : c.Type;
                    var val = c.ValueTemplate ?? string.Empty;

                    val = val.Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                             .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase)
                             .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                             .Replace("{Database}", table.DatabaseName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

                    val = val.TrimEnd(';').Trim();

                    if (type.Equals("string", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("\"") && !val.EndsWith("\"") && !val.Contains("\n") && !val.Contains("+"))
                    {
                        val = $"\"{val}\"";
                    }

                    if (c.Name.Contains("(") && c.Name.EndsWith(")"))
                    {
                        sb.AppendLine($"{indent}    {mod} {type} {c.Name} => {val};");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}    {mod} const {type} {c.Name} = {val};");
                    }
                }
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #endregion");
                    sb.AppendLine();
                }
            }

            // Custom Class Body Snippet
            if (!string.IsNullOrWhiteSpace(options.CustomClassBodySnippet))
            {
                var customSnippet = options.CustomClassBodySnippet
                    .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                    .Replace("{TableName}", table.TableName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{SchemaName}", table.SchemaName, StringComparison.OrdinalIgnoreCase);

                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #region Custom Code");
                }
                foreach (var line in customSnippet.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                {
                    sb.AppendLine($"{indent}    {line}");
                }
                if (options.UseRegions)
                {
                    sb.AppendLine($"{indent}    #endregion");
                }
                sb.AppendLine();
            }

            if (options.UseRegions && !string.IsNullOrWhiteSpace(options.OuterRegionName))
            {
                sb.AppendLine($"{indent}    #endregion // {options.OuterRegionName.Trim()}");
                sb.AppendLine();
            }

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrWhiteSpace(resolvedNamespace))
            {
                sb.AppendLine("}");
            }

            return new GeneratedModelFile
            {
                TableName = table.TableName,
                SchemaName = table.SchemaName,
                ClassName = className,
                FileName = $"{className}.cs",
                Content = sb.ToString().TrimEnd() + Environment.NewLine,
                Namespace = resolvedNamespace,
                Language = DataModelLanguage.CSharp
            };
        }

        private static string ResolveNamespace(CSharpOptions options, string schema, string database, string table)
        {
            if (string.Equals(options.NamespaceMode, "PerSchema", StringComparison.OrdinalIgnoreCase))
            {
                var mapping = options.SchemaNamespaceMappings?.Find(m =>
                    string.Equals(m.SchemaName, schema, StringComparison.OrdinalIgnoreCase));
                if (mapping != null && !string.IsNullOrWhiteSpace(mapping.Namespace))
                {
                    return ApplyTokens(mapping.Namespace, schema, database, table);
                }

                var pattern = !string.IsNullOrWhiteSpace(options.SchemaNamespacePattern)
                    ? options.SchemaNamespacePattern
                    : (!string.IsNullOrWhiteSpace(options.Namespace) ? options.Namespace : "{Database}.{Schema}.DataModel");

                return ApplyTokens(pattern, schema, database, table);
            }
            else
            {
                return ApplyTokens(options.Namespace, schema, database, table);
            }
        }

        private static string ApplyTokens(string pattern, string schema, string database, string table)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return string.Empty;
            var resolved = pattern
                .Replace("{Schema}", string.IsNullOrWhiteSpace(schema) ? "dbo" : schema, StringComparison.OrdinalIgnoreCase)
                .Replace("{Database}", database ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{Table}", table ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            while (resolved.Contains(".."))
            {
                resolved = resolved.Replace("..", ".");
            }
            return resolved.Trim('.');
        }

        private static List<string> ParseIndexColumns(string columnsRaw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(columnsRaw)) return list;

            var parts = columnsRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var cleaned = part;
                if (cleaned.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned.Substring(0, cleaned.Length - 4).Trim();
                else if (cleaned.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned.Substring(0, cleaned.Length - 5).Trim();

                cleaned = cleaned.Trim('[', ']', ' ');
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    list.Add(cleaned);
                }
            }
            return list;
        }

        private static string InterpolateAttributeTemplate(
            string template,
            string className,
            string propName,
            string colName,
            string dataType,
            string tableName,
            string schemaName,
            string? refTable = null,
            string? refCol = null,
            int? length = null,
            int? precision = null,
            int? scale = null,
            string? database = null)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;

            var fullFieldName = $"{className}.{propName}";
            return template
                .Replace("{FullFieldName}", fullFieldName, StringComparison.OrdinalIgnoreCase)
                .Replace("{FieldName}", propName, StringComparison.OrdinalIgnoreCase)
                .Replace("{PropertyName}", propName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ColumnName}", colName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase)
                .Replace("{TableName}", tableName, StringComparison.OrdinalIgnoreCase)
                .Replace("{SchemaName}", schemaName, StringComparison.OrdinalIgnoreCase)
                .Replace("{DataType}", dataType, StringComparison.OrdinalIgnoreCase)
                .Replace("{RefTable}", refTable ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{RefColumn}", refCol ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{Length}", length?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{MaxLength}", length?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{Precision}", precision?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{Scale}", scale?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{Database}", database ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static string InterpolateTemplate(string template, string propName, string colName, string targetType, string className)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;
            var cleanTarget = (targetType ?? string.Empty).TrimEnd('?');
            return template
                .Replace("{PropertyName}", propName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ColumnName}", colName, StringComparison.OrdinalIgnoreCase)
                .Replace("{TargetType}", targetType ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{CleanTargetType}", cleanTarget, StringComparison.OrdinalIgnoreCase)
                .Replace("{ClassName}", className, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern)) return false;
            if (pattern == "*") return true;

            var parts = pattern.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                var cur = p.Trim();
                if (cur == "*") return true;

                if (cur.StartsWith("*") && cur.EndsWith("*") && cur.Length > 2)
                {
                    var middle = cur.Substring(1, cur.Length - 2);
                    if (text.Contains(middle, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (cur.StartsWith("*") && cur.Length > 1)
                {
                    var suffix = cur.Substring(1);
                    if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (cur.EndsWith("*") && cur.Length > 1)
                {
                    var prefix = cur.Substring(0, cur.Length - 1);
                    if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (text.Equals(cur, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static GeneratedModelFile GenerateDbContext(
            string dbContextName,
            string? ns,
            IReadOnlyList<TableOperation.Information> tables,
            DataModelOptions globalOptions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrWhiteSpace(ns) ? "" : "    ";

            sb.AppendLine($"{indent}public class {dbContextName} : DbContext");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    public {dbContextName}(DbContextOptions<{dbContextName}> options) : base(options)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();

            foreach (var table in tables)
            {
                var className = DataModelNamingHelper.FormatClassName(
                    table.TableName,
                    globalOptions.RemoveTablePrefixes,
                    globalOptions.TablePrefixes,
                    globalOptions.SingularizeClassNames);

                var dbSetName = className + "s";
                sb.AppendLine($"{indent}    public DbSet<{className}> {dbSetName} {{ get; set; }}");
            }

            sb.AppendLine();
            sb.AppendLine($"{indent}    protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        base.OnModelCreating(modelBuilder);");
            sb.AppendLine();

            foreach (var table in tables)
            {
                var className = DataModelNamingHelper.FormatClassName(
                    table.TableName,
                    globalOptions.RemoveTablePrefixes,
                    globalOptions.TablePrefixes,
                    globalOptions.SingularizeClassNames);

                var pks = table.Columns.Where(c => c.IsPrimaryKey).ToList();
                if (pks.Count > 1)
                {
                    var keys = string.Join(", ", pks.Select(k => $"e.{DataModelNamingHelper.FormatPropertyName(k.ColumnName, globalOptions.PropertyNaming)}"));
                    sb.AppendLine($"{indent}        modelBuilder.Entity<{className}>().HasKey(e => new {{ {keys} }});");
                }
            }

            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrWhiteSpace(ns))
            {
                sb.AppendLine("}");
            }

            return new GeneratedModelFile
            {
                TableName = dbContextName,
                SchemaName = "Context",
                ClassName = dbContextName,
                FileName = $"{dbContextName}.cs",
                Content = sb.ToString().TrimEnd() + Environment.NewLine,
                Namespace = ns ?? string.Empty,
                Language = DataModelLanguage.CSharp
            };
        }
    }
}
