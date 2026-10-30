using System;
using System.Collections.Generic;

namespace BprDevKit.DataModelGenerator.Models
{
    public enum DataModelLanguage
    {
        CSharp,
        Python,
        Dart,
        Java
    }

    public enum NamingConvention
    {
        Original,
        PascalCase,
        CamelCase,
        SnakeCase
    }

    public enum ZipPackagingStructure
    {
        Flat = 0,
        BySchema = 1,
        ByNamespace = 2
    }

    public class DataModelOptions
    {
        public DataModelLanguage Language { get; set; } = DataModelLanguage.CSharp;
        public NamingConvention PropertyNaming { get; set; } = NamingConvention.PascalCase;
        public ZipPackagingStructure ZipPackaging { get; set; } = ZipPackagingStructure.BySchema;
        public bool RemoveTablePrefixes { get; set; } = true;
        public string TablePrefixes { get; set; } = "tbl_,tbl,vw_,view_,sp_,usp_,fn_,ufn_";
        public bool SingularizeClassNames { get; set; } = true;
        public bool IncludeNavigationProperties { get; set; } = true;
        public bool IncludeDocumentationComments { get; set; } = true;

        // Language-specific option bags
        public CSharpOptions CSharp { get; set; } = new();
        public PythonOptions Python { get; set; } = new();
        public DartOptions Dart { get; set; } = new();
        public JavaOptions Java { get; set; } = new();

        // Schema Access Control / Hierarchy
        public bool EnableSchemaAccessControl { get; set; } = false;
        public List<SchemaAccessRule> SchemaAccessRules { get; set; } = new();

        /// <summary>
        /// Checks whether fromSchema is permitted to reference or depend on toSchema.
        /// Internal same-schema references are always permitted.
        /// If access control is disabled, all references are permitted.
        /// If a rule exists for fromSchema, toSchema must be explicitly listed in AllowedTargetSchemas.
        /// </summary>
        public bool IsSchemaAccessAllowed(string fromSchema, string toSchema)
        {
            if (string.IsNullOrWhiteSpace(fromSchema) || string.IsNullOrWhiteSpace(toSchema))
                return true;

            if (string.Equals(fromSchema, toSchema, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!EnableSchemaAccessControl)
                return true;

            var rule = SchemaAccessRules.Find(r => string.Equals(r.SourceSchema, fromSchema, StringComparison.OrdinalIgnoreCase));
            if (rule == null)
            {
                return false;
            }

            return rule.AllowedTargetSchemas.Exists(s => string.Equals(s, toSchema, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Configures allowed cross-schema reference/dependency permissions.
    /// If SourceSchema is specified, it can only reference schemas in AllowedTargetSchemas.
    /// </summary>
    public class SchemaAccessRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SourceSchema { get; set; } = string.Empty;
        public List<string> AllowedTargetSchemas { get; set; } = new();
    }

    /// <summary>
    /// Configurable rule for transforming properties or creating [NotMapped] helpers.
    /// Can be used for JSON, XML, Enums, Encryption, and any custom mapping.
    /// </summary>
    public class PropertyTransformRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsEnabled { get; set; } = true;
        public string RuleName { get; set; } = "JSON to Dictionary";

        /// <summary>Column name pattern (e.g. "*Json", "*Payload", "Status", or "*")</summary>
        public string ColumnNamePattern { get; set; } = "*Json";

        /// <summary>SQL Data type pattern (e.g. "json", "nvarchar(max)", "int", or "*")</summary>
        public string DataTypePattern { get; set; } = "json";

        /// <summary>Action type: "NotMappedHelper", "CustomGetterSetter", "TypeOverride"</summary>
        public string ActionType { get; set; } = "NotMappedHelper";

        /// <summary>Helper property name template (e.g. "{PropertyName}Data", "{PropertyName}Dict")</summary>
        public string HelperPropertyNameTemplate { get; set; } = "{PropertyName}Data";

        /// <summary>Target C# Type (e.g. "Dictionary<string, object?>?", "JsonDocument?", "MyStatusEnum")</summary>
        public string TargetType { get; set; } = "Dictionary<string, object?>?";

        /// <summary>Getter template expression</summary>
        public string GetterTemplate { get; set; } = "string.IsNullOrWhiteSpace({PropertyName}) ? null : JsonSerializer.Deserialize<{CleanTargetType}>({PropertyName})";

        /// <summary>Setter template expression or statement</summary>
        public string SetterTemplate { get; set; } = "{PropertyName} = value == null ? null : JsonSerializer.Serialize(value);";

        /// <summary>Additional using required (e.g. "System.Text.Json", "System.Xml.Linq")</summary>
        public string RequiredUsing { get; set; } = "System.Text.Json";
    }

    /// <summary>
    /// User-defined constant definition for model class.
    /// </summary>
    public class ClassConstantDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AccessModifier { get; set; } = "public"; // public, private, internal, protected
        public string Type { get; set; } = "string";
        public string Name { get; set; } = "TABLE_NAME";
        public string ValueTemplate { get; set; } = "\"{TableName}\"";
    }

    /// <summary>
    /// User-defined override member definition for model class (Property or Method).
    /// </summary>
    public class ClassOverrideDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AccessModifier { get; set; } = "public"; // public, protected, internal
        public string MemberKind { get; set; } = "Property"; // "Property" or "Method"
        public string ReturnType { get; set; } = "string";
        public string Name { get; set; } = "TableName";
        public string Parameters { get; set; } = string.Empty;
        public bool IsExpressionBodied { get; set; } = true;
        public string Body { get; set; } = "\"{TableName}\"";
    }

    /// <summary>
    /// Per-schema namespace mapping.
    /// </summary>
    public class SchemaNamespaceMapping
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SchemaName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
    }

    public class CSharpOptions
    {
        public string TypeKind { get; set; } = "class"; // class, record, struct
        public string PropertyAccessor { get; set; } = "get_set"; // get_set, get_init

        // Namespace configuration (Single vs PerSchema)
        public string NamespaceMode { get; set; } = "Single"; // Single, PerSchema
        public string Namespace { get; set; } = "MyProject.Models";
        public string SchemaNamespacePattern { get; set; } = "{Database}.{Schema}.DataModel";
        public List<SchemaNamespaceMapping> SchemaNamespaceMappings { get; set; } = new();

        public bool NullableReferenceTypes { get; set; } = true;
        public bool IncludeDataAnnotations { get; set; } = true;

        // Fine-grained Data Annotations Control
        public bool IncludeKeyAttribute { get; set; } = true;
        public bool IncludeRequiredAttribute { get; set; } = true;
        public string RequiredAttributeScope { get; set; } = "AllNonNullable"; // AllNonNullable, StringsAndByteArrays, NonIdentity
        public bool RequiredIncludePrimaryKeys { get; set; } = false;
        public string RequiredErrorMessageTemplate { get; set; } = string.Empty;
        public bool IncludeStringLengthAttribute { get; set; } = true;
        public string StringLengthAttributeKind { get; set; } = "StringLength"; // StringLength, MaxLength
        public string ColumnAttributeMode { get; set; } = "WhenNameDiffers"; // WhenNameDiffers, Always, Never
        public bool IncludeColumnTypeName { get; set; } = false;
        public bool IncludeDatabaseGeneratedAttribute { get; set; } = true;
        public bool IncludeDisplayAttribute { get; set; } = false;
        public bool IncludeDescriptionAttribute { get; set; } = false;
        public bool IncludeKeylessAttributeForViews { get; set; } = true;

        public bool IncludeJsonAttributes { get; set; } = false;
        public bool IncludeEfCoreTableAttributes { get; set; } = true;
        public bool UseNameofInAttributes { get; set; } = false;
        public bool IncludeIndexesAsAttributes { get; set; } = false;
        public bool IsPartial { get; set; } = true;
        public string BaseClass { get; set; } = string.Empty;
        public string Interfaces { get; set; } = string.Empty;
        public string CustomClassAttributes { get; set; } = string.Empty;

        // Regions and Structure
        public bool UseRegions { get; set; } = false;
        public string OuterRegionName { get; set; } = string.Empty;
        public bool IncludeConstructor { get; set; } = false;
        public bool IncludeConstants { get; set; } = false;
        public List<ClassConstantDefinition> Constants { get; set; } = new();
        public bool IncludeOverrides { get; set; } = false;
        public List<ClassOverrideDefinition> Overrides { get; set; } = new();
        public bool IncludeTableSchemaOverrides { get; set; } = false;
        public string OverrideProperties { get; set; } = string.Empty;

        // Navigation Properties
        public string NavigationStyle { get; set; } = "Standard"; // Standard, Enterprise
        public bool UseFullNamespaceForNavTypes { get; set; } = false;
        public bool MakeCollectionsNullable { get; set; } = false;
        public bool IncludeSchemaInRelationName { get; set; } = false;
        public string CustomNavReferenceAttribute { get; set; } = string.Empty;
        public string CustomNavInversePropertyAttribute { get; set; } = string.Empty;
        public string NavigationPropertyAttributes { get; set; } = string.Empty;

        // Usings
        public string CustomUsings { get; set; } = string.Empty;

        // Custom Attributes with Templates
        public bool EnableCustomFkAttribute { get; set; } = false;
        public string CustomFkAttributeTemplate { get; set; } = "[FkProperty(Name = \"{FullFieldName}\")]";
        public bool EnableCustomPkAttribute { get; set; } = false;
        public string CustomPkAttributeTemplate { get; set; } = "[Key]";
        public string CustomColumnAttributeRules { get; set; } = string.Empty;
        public string DefaultPropertyAttributes { get; set; } = string.Empty;
        public bool IncludeTimestampAttribute { get; set; } = true;

        // Property Transformations Engine
        public List<PropertyTransformRule> TransformRules { get; set; } = new();

        // String Default Initialization
        public string StringInitialization { get; set; } = "EmptyString"; // EmptyString (= string.Empty;), NullForgiving (= null!;), None

        // Custom Snippet
        public string CustomClassBodySnippet { get; set; } = string.Empty;

        // DbContext
        public bool GenerateDbContext { get; set; } = false;
        public string DbContextName { get; set; } = "AppDbContext";
        public string DbContextNamespace { get; set; } = string.Empty;
        public bool DbContextUseFullyQualifiedModelNames { get; set; } = false;
    }

    public class PythonOptions
    {
        public string Style { get; set; } = "pydantic"; // pydantic, dataclass, sqlalchemy, typeddict
        public bool UseSnakeCaseProperties { get; set; } = true;
        public bool IncludeDocstrings { get; set; } = true;
        public bool UseOptionalTypeHinting { get; set; } = true;
        public string BaseClass { get; set; } = string.Empty;
        public string CustomImports { get; set; } = string.Empty;
        public string CustomClassBodySnippet { get; set; } = string.Empty;
    }

    public class DartOptions
    {
        public string Style { get; set; } = "plain"; // plain, json_serializable, freezed
        public bool NullSafety { get; set; } = true;
        public bool FinalFields { get; set; } = true;
        public bool GenerateCopyWith { get; set; } = true;
        public bool GenerateSerialization { get; set; } = true; // fromJson, toJson, toMap, fromMap
        public string BaseClass { get; set; } = string.Empty;
        public string CustomImports { get; set; } = string.Empty;
        public string CustomClassBodySnippet { get; set; } = string.Empty;
    }

    public class JavaOptions
    {
        public string Style { get; set; } = "pojo"; // pojo, record, lombok, jpa
        public string PackageName { get; set; } = "com.myproject.models";
        public bool IncludeJacksonAnnotations { get; set; } = false;
        public bool IncludeValidationAnnotations { get; set; } = true;
        public bool ImplementSerializable { get; set; } = true;
        public string BaseClass { get; set; } = string.Empty;
        public string Interfaces { get; set; } = string.Empty;
        public string CustomImports { get; set; } = string.Empty;
        public string CustomClassBodySnippet { get; set; } = string.Empty;
    }

    public class GeneratedModelFile
    {
        public string TableName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public DataModelLanguage Language { get; set; }
        public string ObjectType { get; set; } = Constants.ObjectTypes.Table;
    }
}
