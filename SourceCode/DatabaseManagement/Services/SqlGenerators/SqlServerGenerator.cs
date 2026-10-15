using System;
using System.Collections.Generic;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Main SQL script generator that dynamically generates SQL scripts based on configuration objects
    /// </summary>
    public class SqlServerGenerator : SqlGeneratorBase
    {
        #region Database Operations
        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            ValidateCreateConfiguration(config);

            var dataFilePath = BuildDatabaseFilePath(config.DataFilePath, config.DatabaseName, ".mdf");
            var logDirectory = string.IsNullOrWhiteSpace(config.LogFilePath) ? config.DataFilePath : config.LogFilePath;
            var logFilePath = BuildDatabaseFilePath(logDirectory!, config.DatabaseName, "_log.ldf");
            var maxSize = config.MaxSizeMB.HasValue ? $"{config.MaxSizeMB.Value}MB" : "UNLIMITED";
            var logMaxSize = config.LogMaxSizeMB.HasValue ? $"{config.LogMaxSizeMB.Value}MB" : "UNLIMITED";

            var builder = new StringBuilder();
            builder.AppendLine("CREATE DATABASE " + QUOTENAME(config.DatabaseName))
                .AppendLine("ON PRIMARY")
                .AppendLine("(")
                .AppendLine("    NAME = " + QUOTENAME(config.DatabaseName, "'") + ",")
                .AppendLine("    FILENAME = " + QUOTENAME(dataFilePath, "'") + ",")
                .AppendLine("    SIZE = " + config.InitialSizeMB + "MB,")
                .AppendLine("    MAXSIZE = " + maxSize + ",")
                .AppendLine("    FILEGROWTH = " + config.FileGrowthMB + "MB")
                .AppendLine(")")
                .AppendLine("LOG ON")
                .AppendLine("(")
                .AppendLine("    NAME = " + QUOTENAME(config.DatabaseName + "_log", "'") + ",")
                .AppendLine("    FILENAME = " + QUOTENAME(logFilePath, "'") + ",")
                .AppendLine("    SIZE = " + config.LogInitialSizeMB + "MB,")
                .AppendLine("    MAXSIZE = " + logMaxSize + ",")
                .AppendLine("    FILEGROWTH = " + config.LogFileGrowthMB + "MB")
                .AppendLine(")");

            if (!string.IsNullOrWhiteSpace(config.Collation))
                builder.AppendLine("COLLATE " + config.Collation + ";");
            else
                builder.AppendLine(";");

            // CREATE DATABASE must be isolated from subsequent ALTER DATABASE statements.
            builder.AppendLine("GO");

            if (!string.IsNullOrWhiteSpace(config.FileGroup) && !string.Equals(config.FileGroup, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(config.DatabaseName)} ADD FILEGROUP {QUOTENAME(config.FileGroup)};");

            AppendDatabaseSettings(
                builder,
                config.DatabaseName,
                config.RecoveryModel,
                config.CompatibilityLevel,
                config.IsReadOnly,
                config.AutoShrink,
                config.AutoClose,
                config.PageVerify);

            return builder.ToString();
        }

        public override string GenerateAlterDatabaseScript(DatabaseOperation.Alter config)
        {
            ValidateAlterConfiguration(config);

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(config.Collation))
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(config.DatabaseName)} COLLATE {config.Collation};");

            AppendDatabaseSettings(
                builder,
                config.DatabaseName,
                config.RecoveryModel,
                config.CompatibilityLevel,
                config.IsReadOnly,
                config.AutoShrink,
                config.AutoClose,
                config.PageVerify);

            return builder.ToString();
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            ValidateDatabaseName(config.DatabaseName);

            if (IsSystemDatabase(config.DatabaseName))
                throw new InvalidOperationException("[BprDevKit] Dropping system databases is not allowed.");

            var builder = new StringBuilder();
            if (config.ForceDisconnect)
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(config.DatabaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");

            builder.AppendLine($"DROP DATABASE {QUOTENAME(config.DatabaseName)};");
            return builder.ToString();
        }

        public override string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config)
        {
            ValidateDatabaseName(config.DatabaseName);

            return $@"SELECT
    d.name AS DatabaseName,
    d.database_id AS DatabaseId,
    d.collation_name AS Collation,
    d.compatibility_level AS CompatibilityLevel,
    d.recovery_model_desc AS RecoveryModel,
    d.page_verify_option_desc AS PageVerify,
    d.is_read_only AS IsReadOnly,
    d.is_auto_shrink_on AS AutoShrink,
    d.is_auto_close_on AS AutoClose,
    d.state_desc AS [State],
    d.create_date AS CreateDate,
    dataFile.physical_name AS DataFilePath,
    CAST(dataFile.size AS bigint) * 8 / 1024 AS DataFileSizeMB,
    CASE WHEN dataFile.max_size = -1 THEN 'UNLIMITED' ELSE CONVERT(varchar(20), CAST(dataFile.max_size AS bigint) * 8 / 1024) + ' MB' END AS DataFileMaxSize,
    CAST(dataFile.growth AS bigint) * 8 / 1024 AS DataFileGrowthMB,
    logFile.physical_name AS LogFilePath,
    CAST(logFile.size AS bigint) * 8 / 1024 AS LogFileSizeMB,
    CASE WHEN logFile.max_size = -1 THEN 'UNLIMITED' ELSE CONVERT(varchar(20), CAST(logFile.max_size AS bigint) * 8 / 1024) + ' MB' END AS LogFileMaxSize,
    CAST(logFile.growth AS bigint) * 8 / 1024 AS LogFileGrowthMB
FROM sys.databases AS d
LEFT JOIN sys.master_files AS dataFile ON d.database_id = dataFile.database_id AND dataFile.type = 0 AND dataFile.file_id = 1
LEFT JOIN sys.master_files AS logFile ON d.database_id = logFile.database_id AND logFile.type = 1 AND logFile.file_id = 2
WHERE d.name = {QUOTENAME(config.DatabaseName, "'")};";
        }

        public override string GenerateGetDatabasesScript()
        {
            return "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name;";
        }

        public override string GenerateGetCollationsScript()
        {
            return "SELECT name FROM sys.fn_helpcollations() ORDER BY name;";
        }

        public override string GenerateGetFileGroupsScript()
        {
            return "SELECT name FROM sys.filegroups ORDER BY name;";
        }
        #endregion

        #region Schema Operations
        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ThrowIfProtectedSchema(config.SchemaName);
            ValidateOptionalObjectName(config.Owner, "Owner");

            var builder = new StringBuilder();

            // CREATE SCHEMA must be the first statement of its batch, so USE is isolated with GO.
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            builder.Append("CREATE SCHEMA ").Append(QUOTENAME(config.SchemaName));
            if (!string.IsNullOrWhiteSpace(config.Owner))
                builder.Append(" AUTHORIZATION ").Append(QUOTENAME(config.Owner));

            builder.AppendLine(";");
            return builder.ToString();
        }

        /// <summary>
        /// Renames a schema and/or changes its owner. SQL Server has no RENAME for schemas, so a rename
        /// creates the new schema (same owner), transfers every object into it and drops the old schema.
        /// </summary>
        public override string GenerateAlterSchemaScript(SchemaOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ThrowIfProtectedSchema(config.SchemaName);
            ValidateOptionalObjectName(config.Owner, "Owner");

            if (!config.IsRename && !config.ChangesOwner)
                throw new InvalidOperationException("[BprDevKit] Specify a new schema name or a new owner.");

            if (config.IsRename)
            {
                ValidateObjectName(config.NewSchemaName, "New schema name");
                ThrowIfProtectedSchema(config.NewSchemaName!);
            }

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            var currentSchemaName = config.SchemaName;
            if (config.IsRename)
            {
                AppendRenameSchemaStatements(builder, config.SchemaName, config.NewSchemaName!);
                builder.AppendLine("GO");
                currentSchemaName = config.NewSchemaName!;
            }

            if (config.ChangesOwner)
                builder.AppendLine($"ALTER AUTHORIZATION ON SCHEMA::{QUOTENAME(currentSchemaName)} TO {QUOTENAME(config.Owner)};");

            return builder.ToString();
        }

        /// <summary>
        /// Drops an empty schema. SQL Server rejects the statement while the schema still contains objects.
        /// </summary>
        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ThrowIfProtectedSchema(config.SchemaName);

            return $"USE {QUOTENAME(config.DatabaseName)};{Environment.NewLine}GO{Environment.NewLine}DROP SCHEMA {QUOTENAME(config.SchemaName)};{Environment.NewLine}";
        }

        /// <summary>
        /// Names of the schemas a user can work with: dbo plus every user-created schema.
        /// System schemas (sys, INFORMATION_SCHEMA, guest) and the fixed-role schemas are left out.
        /// </summary>
        public override string GenerateGetSchemasScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);

            return $@"SELECT s.name
FROM {QUOTENAME(databaseName)}.sys.schemas AS s
WHERE {UserSchemaFilter}
ORDER BY s.name;";
        }

        /// <summary>
        /// Schema list with owner and object count (tables, views, routines, synonyms, sequences,
        /// user-defined types and XML schema collections). Pass a schema name to get just that schema.
        /// </summary>
        public override string GenerateGetSchemaListScript(string databaseName, string? schemaName = null)
        {
            ValidateDatabaseName(databaseName);
            var database = QUOTENAME(databaseName);

            var schemaFilter = string.Empty;
            if (schemaName is not null)
            {
                ValidateObjectName(schemaName, "Schema name");
                schemaFilter = $" AND s.name = {SQLLiteral(schemaName)}";
            }

            return $@"SELECT
    s.name AS SchemaName,
    dp.name AS OwnerName,
    (SELECT COUNT(*) FROM {database}.sys.objects AS o
        WHERE o.schema_id = s.schema_id AND o.parent_object_id = 0 AND o.is_ms_shipped = 0 AND o.type <> 'TT')
    + (SELECT COUNT(*) FROM {database}.sys.types AS t
        WHERE t.schema_id = s.schema_id AND t.is_user_defined = 1)
    + (SELECT COUNT(*) FROM {database}.sys.xml_schema_collections AS x
        WHERE x.schema_id = s.schema_id) AS ObjectCount
FROM {database}.sys.schemas AS s
INNER JOIN {database}.sys.database_principals AS dp ON dp.principal_id = s.principal_id
WHERE {UserSchemaFilter}{schemaFilter}
ORDER BY s.name;";
        }

        /// <summary>
        /// Everything that lives inside one schema (the same set that must be empty before the schema can be dropped).
        /// </summary>
        public override string GenerateGetSchemaInformationScript(SchemaOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");

            var database = QUOTENAME(config.DatabaseName);
            var schemaName = SQLLiteral(config.SchemaName);

            return $@"SELECT o.type_desc AS ObjectType, o.name AS ObjectName, o.create_date AS CreateDate
FROM {database}.sys.objects AS o
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = o.schema_id
WHERE s.name = {schemaName} AND o.parent_object_id = 0 AND o.is_ms_shipped = 0 AND o.type <> 'TT'
UNION ALL
SELECT
    CASE WHEN t.is_table_type = 1 THEN N'TABLE_TYPE' WHEN t.is_assembly_type = 1 THEN N'CLR_TYPE' ELSE N'USER_DEFINED_TYPE' END,
    t.name,
    CAST(NULL AS datetime)
FROM {database}.sys.types AS t
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = {schemaName} AND t.is_user_defined = 1
UNION ALL
SELECT N'XML_SCHEMA_COLLECTION', x.name, x.create_date
FROM {database}.sys.xml_schema_collections AS x
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = x.schema_id
WHERE s.name = {schemaName}
ORDER BY ObjectType, ObjectName;";
        }

        /// <summary>
        /// Database principals that can own a schema.
        /// </summary>
        public override string GenerateGetSchemaOwnersScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);

            return $@"SELECT dp.name
FROM {QUOTENAME(databaseName)}.sys.database_principals AS dp
WHERE dp.type IN ('S', 'U', 'G', 'R', 'A')
    AND dp.principal_id > 0
    AND dp.name NOT IN (N'guest', N'INFORMATION_SCHEMA', N'sys')
    AND (dp.is_fixed_role = 0 OR dp.name = N'db_owner')
ORDER BY dp.name;";
        }
        #endregion

        #region Table Operations
        public override string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            ValidateOptionalObjectName(config.FileGroup, "File group");
            ValidateDescription(config.Description);

            var columns = ValidateColumnDefinitions(config.Columns);
            var keyColumns = columns.Where(column => column.IsPrimaryKey).ToList();

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            var definitions = columns.Select(column => BuildColumnDefinition(column, config.SchemaName, config.TableName)).ToList();
            if (keyColumns.Count > 0)
            {
                var keyList = string.Join(", ", keyColumns.Select(column => QUOTENAME(column.ColumnName)));
                var pkColNames = string.Join("_", keyColumns.Select(column => column.ColumnName));
                var pkName = $"PK_{config.SchemaName}_{config.TableName}_{pkColNames}";
                if (pkName.Length > 128)
                    throw new InvalidOperationException("[BprDevKit] The generated primary key constraint name is longer than 128 characters; use a shorter schema/table/column name.");
                definitions.Add($"CONSTRAINT {QUOTENAME(pkName)} PRIMARY KEY CLUSTERED ({keyList})");
            }

            builder.AppendLine($"CREATE TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} (");
            builder.AppendLine(string.Join("," + Environment.NewLine, definitions.Select(definition => "    " + definition)));
            builder.Append(')');
            if (!string.IsNullOrWhiteSpace(config.FileGroup))
                builder.Append(" ON ").Append(QUOTENAME(config.FileGroup));
            builder.AppendLine(";");

            if (!string.IsNullOrWhiteSpace(config.Description))
                builder.AppendLine(BuildAddDescriptionStatement(config.Description, config.SchemaName, config.TableName, null));

            foreach (var column in columns.Where(column => !string.IsNullOrWhiteSpace(column.Description)))
                builder.AppendLine(BuildAddDescriptionStatement(column.Description!, config.SchemaName, config.TableName, column.ColumnName));

            return builder.ToString();
        }

        /// <summary>
        /// Renames a table (sp_rename) and/or sets, replaces or removes its description.
        /// </summary>
        public override string GenerateAlterTableScript(TableOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            ValidateDescription(config.Description);

            if (!config.IsRename && !config.ChangesDescription)
                throw new InvalidOperationException("[BprDevKit] Specify a new table name or a new description.");

            if (config.IsRename)
                ValidateObjectName(config.NewTableName, "New table name");

            var currentTableName = config.IsRename ? config.NewTableName! : config.TableName;
            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            if (config.IsRename)
            {
                builder.AppendLine($@"DECLARE @renameTarget nvarchar(517) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)});
EXEC sys.sp_rename @objname = @renameTarget, @newname = {SQLLiteral(config.NewTableName!)}, @objtype = N'OBJECT';");
            }

            if (config.ChangesDescription)
                AppendDescriptionUpsert(builder, config.SchemaName, currentTableName, null, config.Description);

            return builder.ToString();
        }

        /// <summary>
        /// Drops a table. ForceDropDependent first drops the foreign keys of other tables that reference it.
        /// </summary>
        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            builder.AppendLine($"DECLARE @tableName nvarchar(517) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)});");

            if (config.ForceDropDependent)
            {
                builder.AppendLine(@"DECLARE @dropForeignKeys nvarchar(max) = N'';
SELECT @dropForeignKeys += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys AS fk
WHERE fk.referenced_object_id = OBJECT_ID(@tableName) AND fk.parent_object_id <> fk.referenced_object_id;
IF LEN(@dropForeignKeys) > 0
    EXEC (@dropForeignKeys);");
            }

            var dropStatement = $"DROP TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)};";
            builder.AppendLine(config.IgnoreIfNotExists
                ? $"IF OBJECT_ID(@tableName, N'U') IS NOT NULL{Environment.NewLine}    {dropStatement}"
                : dropStatement);

            return builder.ToString();
        }

        /// <summary>
        /// Header information of one table: dates, file group and description.
        /// </summary>
        public override string GenerateGetTableInfoScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            var database = QUOTENAME(config.DatabaseName);

            return $@"SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    t.create_date AS CreateDate,
    t.modify_date AS ModifyDate,
    (SELECT TOP (1) ds.name
        FROM {database}.sys.indexes AS i
        INNER JOIN {database}.sys.data_spaces AS ds ON ds.data_space_id = i.data_space_id
        WHERE i.object_id = t.object_id AND i.index_id IN (0, 1)) AS FileGroup,
    (SELECT CAST(ep.value AS nvarchar(4000))
        FROM {database}.sys.extended_properties AS ep
        WHERE ep.class = 1 AND ep.major_id = t.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description') AS Description
FROM {database}.sys.tables AS t
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)};";
        }

        public override string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            ValidateDatabaseName(databaseName);
            ValidateObjectName(schemaName, "Schema name");
            var database = QUOTENAME(databaseName);

            return $@"SELECT t.name AS TableName
FROM {database}.sys.tables AS t
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = {SQLLiteral(schemaName)} AND t.is_ms_shipped = 0
ORDER BY t.name;";
        }

        /// <summary>
        /// Every user table of the database with an approximate row count, column count and description.
        /// </summary>
        public override string GenerateGetTableListScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);
            var database = QUOTENAME(databaseName);

            return $@"SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    ISNULL((SELECT SUM(p.rows) FROM {database}.sys.partitions AS p WHERE p.object_id = t.object_id AND p.index_id IN (0, 1)), 0) AS [RowCount],
    (SELECT COUNT(*) FROM {database}.sys.columns AS c WHERE c.object_id = t.object_id) AS ColumnCount,
    t.create_date AS CreateDate,
    t.modify_date AS ModifyDate,
    (SELECT CAST(ep.value AS nvarchar(4000))
        FROM {database}.sys.extended_properties AS ep
        WHERE ep.class = 1 AND ep.major_id = t.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description') AS Description
FROM {database}.sys.tables AS t
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";
        }

        public override string GenerateGetTableColumnsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            var database = QUOTENAME(config.DatabaseName);

            return $@"SELECT
    c.column_id AS OrdinalPosition,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS [Precision],
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    TRY_CAST(ic.seed_value AS int) AS IdentitySeed,
    TRY_CAST(ic.increment_value AS int) AS IdentityIncrement,
    CASE WHEN EXISTS (
        SELECT 1
        FROM {database}.sys.index_columns AS ixc
        INNER JOIN {database}.sys.indexes AS i ON i.object_id = ixc.object_id AND i.index_id = ixc.index_id
        WHERE i.is_primary_key = 1 AND ixc.object_id = c.object_id AND ixc.column_id = c.column_id) THEN 1 ELSE 0 END AS IsPrimaryKey,
    c.collation_name AS Collation,
    dc.definition AS DefaultValue,
    cc.definition AS ComputedFormula,
    ISNULL(cc.is_persisted, 0) AS IsPersisted,
    CAST(ep.value AS nvarchar(4000)) AS Description
FROM {database}.sys.columns AS c
INNER JOIN {database}.sys.tables AS t ON t.object_id = c.object_id
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN {database}.sys.types AS ty ON ty.user_type_id = c.user_type_id
LEFT JOIN {database}.sys.identity_columns AS ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
LEFT JOIN {database}.sys.default_constraints AS dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
LEFT JOIN {database}.sys.computed_columns AS cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN {database}.sys.extended_properties AS ep ON ep.class = 1 AND ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = N'MS_Description'
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)}
ORDER BY c.column_id;";
        }

        public override string GenerateGetTableIndexesScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            var database = QUOTENAME(config.DatabaseName);

            return $@"SELECT
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique_constraint AS IsUniqueConstraint,
    i.fill_factor AS [FillFactor],
    i.is_padded AS PadIndex,
    i.allow_row_locks AS AllowRowLocks,
    i.allow_page_locks AS AllowPageLocks,
    i.filter_definition AS FilterPredicate,
    ds.name AS FileGroup,
    STUFF((SELECT N', ' + QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N'' END
        FROM {database}.sys.index_columns AS ic
        INNER JOIN {database}.sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH(N''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS KeyColumns,
    STUFF((SELECT N', ' + QUOTENAME(c.name)
        FROM {database}.sys.index_columns AS ic
        INNER JOIN {database}.sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
        ORDER BY ic.index_column_id
        FOR XML PATH(N''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS IncludeColumns
FROM {database}.sys.indexes AS i
INNER JOIN {database}.sys.tables AS t ON t.object_id = i.object_id
INNER JOIN {database}.sys.schemas AS s ON s.schema_id = t.schema_id
LEFT JOIN {database}.sys.data_spaces AS ds ON ds.data_space_id = i.data_space_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)}
    AND i.index_id > 0 AND i.is_hypothetical = 0
ORDER BY i.is_primary_key DESC, i.name;";
        }

        /// <summary>
        /// Foreign keys the table owns (outgoing) and foreign keys of other tables that point to it (incoming).
        /// </summary>
        public override string GenerateGetTableForeignKeysScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            var database = QUOTENAME(config.DatabaseName);
            var schemaName = SQLLiteral(config.SchemaName);
            var tableName = SQLLiteral(config.TableName);

            return $@"SELECT
    fk.name AS FKName,
    ps.name AS SchemaName,
    pt.name AS TableName,
    STUFF((SELECT N', ' + QUOTENAME(pc.name)
        FROM {database}.sys.foreign_key_columns AS fkc
        INNER JOIN {database}.sys.columns AS pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
        WHERE fkc.constraint_object_id = fk.object_id
        ORDER BY fkc.constraint_column_id
        FOR XML PATH(N''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS ColumnName,
    rs.name AS RefSchemaName,
    rt.name AS RefTableName,
    STUFF((SELECT N', ' + QUOTENAME(rc.name)
        FROM {database}.sys.foreign_key_columns AS fkc
        INNER JOIN {database}.sys.columns AS rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
        WHERE fkc.constraint_object_id = fk.object_id
        ORDER BY fkc.constraint_column_id
        FOR XML PATH(N''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS RefColumnName,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate,
    fk.is_not_for_replication AS IsNotForReplication,
    CASE WHEN fk.is_disabled = 0 THEN 1 ELSE 0 END AS Enabled
FROM {database}.sys.foreign_keys AS fk
INNER JOIN {database}.sys.tables AS pt ON pt.object_id = fk.parent_object_id
INNER JOIN {database}.sys.schemas AS ps ON ps.schema_id = pt.schema_id
INNER JOIN {database}.sys.tables AS rt ON rt.object_id = fk.referenced_object_id
INNER JOIN {database}.sys.schemas AS rs ON rs.schema_id = rt.schema_id
WHERE (ps.name = {schemaName} AND pt.name = {tableName})
    OR (rs.name = {schemaName} AND rt.name = {tableName})
ORDER BY fk.name;";
        }

        /// <summary>
        /// Row-data file groups of the database, the default one first.
        /// </summary>
        public override string GenerateGetFileGroupsScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);

            return $@"SELECT fg.name
FROM {QUOTENAME(databaseName)}.sys.filegroups AS fg
WHERE fg.type = 'FG'
ORDER BY fg.is_default DESC, fg.name;";
        }
        #endregion

        #region Column Operations
        public override string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ArgumentNullException.ThrowIfNull(config.Column);

            if (config.Column.IsPrimaryKey)
                throw new InvalidOperationException("[BprDevKit] A primary key is added from the index section, not together with a new column.");

            var column = ValidateColumnDefinitions(new[] { config.Column })[0];

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");
            builder.AppendLine($"ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} ADD {BuildColumnDefinition(column, config.SchemaName, config.TableName)};");

            if (!string.IsNullOrWhiteSpace(column.Description))
                builder.AppendLine(BuildAddDescriptionStatement(column.Description, config.SchemaName, config.TableName, column.ColumnName));

            return builder.ToString();
        }

        /// <summary>
        /// Renames a column, changes its data type / nullability, replaces or removes its default and/or its description.
        /// A default constraint blocks ALTER COLUMN, so it is dropped first and put back unchanged unless a new default was requested.
        /// </summary>
        public override string GenerateAlterColumnScript(ColumnOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.ColumnName, "Column name");
            ValidateDescription(config.Description);

            if (!config.IsRename && !config.ChangesDefinition && !config.ChangesDefault && !config.ChangesDescription)
                throw new InvalidOperationException("[BprDevKit] Nothing to change: specify a new name, type, default or description.");

            if (config.IsRename)
                ValidateObjectName(config.NewColumnName, "New column name");

            var currentColumnName = config.IsRename ? config.NewColumnName! : config.ColumnName;
            var newDefault = config.ChangesDefault && !string.IsNullOrWhiteSpace(config.DefaultValue) ? config.DefaultValue!.Trim() : null;
            if (newDefault is not null)
                ValidateExpression(newDefault, $"default of column [{currentColumnName}]");

            ColumnDefinition? definition = null;
            if (config.ChangesDefinition)
            {
                var requested = config.Definition!;
                definition = ValidateColumnDefinitions(new[]
                {
                    new ColumnDefinition
                    {
                        ColumnName = currentColumnName,
                        DataType = requested.DataType,
                        Length = requested.Length,
                        Precision = requested.Precision,
                        Scale = requested.Scale,
                        IsNullable = requested.IsNullable,
                        Collation = requested.Collation
                    }
                })[0];
            }

            var table = $"{QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}";
            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            if (config.IsRename)
            {
                builder.AppendLine($@"DECLARE @renameTarget nvarchar(776) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)}) + N'.' + QUOTENAME({SQLLiteral(config.ColumnName)});
EXEC sys.sp_rename @objname = @renameTarget, @newname = {SQLLiteral(config.NewColumnName!)}, @objtype = N'COLUMN';")
                    .AppendLine("GO");
            }

            if (config.ChangesDefinition || config.ChangesDefault)
            {
                builder.AppendLine($@"DECLARE @table nvarchar(517) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)});
DECLARE @column sysname = {SQLLiteral(currentColumnName)}, @defaultName sysname, @defaultDefinition nvarchar(max), @sql nvarchar(max);

SELECT @defaultName = dc.name, @defaultDefinition = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(@table) AND c.name = @column;

IF @defaultName IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE ' + @table + N' DROP CONSTRAINT ' + QUOTENAME(@defaultName) + N';';
    EXEC (@sql);
END");

                if (definition is not null)
                {
                    var collation = BuildCollationClause(definition);
                    builder.AppendLine($"ALTER TABLE {table} ALTER COLUMN {QUOTENAME(definition.ColumnName)} {FormatColumnDataType(definition, ColumnDataType.Find(definition.DataType)!)}{collation} {(definition.IsNullable ? "NULL" : "NOT NULL")};");
                }

                if (newDefault is not null)
                {
                    builder.AppendLine($"ALTER TABLE {table} ADD DEFAULT ({newDefault}) FOR {QUOTENAME(currentColumnName)};");
                }
                else if (!config.ChangesDefault)
                {
                    builder.AppendLine(@"IF @defaultName IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE ' + @table + N' ADD CONSTRAINT ' + QUOTENAME(@defaultName) + N' DEFAULT ' + @defaultDefinition + N' FOR ' + QUOTENAME(@column) + N';';
    EXEC (@sql);
END");
                }

                builder.AppendLine("GO");
            }

            if (config.ChangesDescription)
                AppendDescriptionUpsert(builder, config.SchemaName, config.TableName, currentColumnName, config.Description);

            return builder.ToString();
        }

        /// <summary>
        /// Drops a column. Its default constraint is removed first because SQL Server refuses to drop a column that still has one;
        /// indexes, keys and check constraints that use the column must be removed beforehand (SQL Server reports which).
        /// </summary>
        public override string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.ColumnName, "Column name");

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");
            builder.AppendLine($@"DECLARE @table nvarchar(517) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)});
DECLARE @sql nvarchar(max) = N'';

SELECT @sql += N'ALTER TABLE ' + @table + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(@table) AND c.name = {SQLLiteral(config.ColumnName)};

IF LEN(@sql) > 0
    EXEC (@sql);

ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP COLUMN {QUOTENAME(config.ColumnName)};");

            return builder.ToString();
        }

        public override string GenerateGetColumnInfoScript(ColumnOperation.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    c.name AS ColumnName,
    t.name AS TableName,
    s.name AS SchemaName,
    c.user_type_id AS UserTypeId,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    c.is_computed AS IsComputed,
    c.collation_name AS CollationName
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)} AND c.name = {SQLLiteral(config.ColumnName)};";
        }

        public override string GenerateGetDataTypesScript()
        {
            return "SELECT name FROM sys.types WHERE is_user_defined = 0 GROUP BY name ORDER BY name;";
        }

        public override string GenerateGetColumnsScript(string databaseName, string schemaName, string tableName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT c.name AS ColumnName
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(schemaName)} AND t.name = {SQLLiteral(tableName)}
ORDER BY c.column_id;";
        }
        #endregion

        #region Index Operations
        /// <summary>
        /// Creates a nonclustered/clustered (unique) index, or a PRIMARY KEY constraint when IsPrimaryKey is set.
        /// </summary>
        public override string GenerateCreateIndexScript(IndexOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateOptionalObjectName(config.IndexName, "Index name");
            ValidateOptionalObjectName(config.FileGroup, "File group");

            var keys = config.Columns ?? new List<IndexOperation.KeyColumn>();
            var includes = config.IncludeColumns ?? new List<string>();
            ValidateIndexColumns(keys, includes);

            if (config.FillFactor < 0 || config.FillFactor > 100)
                throw new InvalidOperationException("[BprDevKit] The fill factor must be between 0 (server default) and 100.");

            var hasFilter = !string.IsNullOrWhiteSpace(config.FilterPredicate);
            if (config.IsPrimaryKey && (includes.Count > 0 || hasFilter))
                throw new InvalidOperationException("[BprDevKit] A primary key cannot have included columns or a filter.");
            if (config.IsClustered && (includes.Count > 0 || hasFilter))
                throw new InvalidOperationException("[BprDevKit] A clustered index cannot have included columns or a filter.");
            if (hasFilter)
                ValidateExpression(config.FilterPredicate!, "index filter");

            var indexName = ResolveIndexName(config, keys);
            var table = $"{QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}";
            var keyList = string.Join(", ", keys.Select(key => QUOTENAME(key.ColumnName) + (key.IsDescending ? " DESC" : " ASC")));
            var clustering = config.IsClustered ? "CLUSTERED" : "NONCLUSTERED";

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            if (config.IsPrimaryKey)
                builder.Append($"ALTER TABLE {table} ADD CONSTRAINT {QUOTENAME(indexName)} PRIMARY KEY {clustering} ({keyList})");
            else
            {
                builder.Append($"CREATE {(config.IsUnique ? "UNIQUE " : string.Empty)}{clustering} INDEX {QUOTENAME(indexName)} ON {table} ({keyList})");
                if (includes.Count > 0)
                    builder.Append($" INCLUDE ({string.Join(", ", includes.Select(include => QUOTENAME(include)))})");
                if (hasFilter)
                    builder.Append($" WHERE {config.FilterPredicate!.Trim()}");
            }

            if (config.FillFactor > 0)
                builder.Append($" WITH (FILLFACTOR = {config.FillFactor})");
            if (!string.IsNullOrWhiteSpace(config.FileGroup))
                builder.Append($" ON {QUOTENAME(config.FileGroup)}");

            builder.AppendLine(";");
            return builder.ToString();
        }

        /// <summary>
        /// Renames an index or key constraint. Other changes require dropping and recreating the index.
        /// </summary>
        public override string GenerateAlterIndexScript(IndexOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.IndexName, "Index name");
            ValidateObjectName(config.NewIndexName, "New index name");

            if (string.Equals(config.IndexName, config.NewIndexName, StringComparison.Ordinal))
                throw new InvalidOperationException("[BprDevKit] The new index name is the same as the current one.");

            return $@"USE {QUOTENAME(config.DatabaseName)};
GO
DECLARE @renameTarget nvarchar(776) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)}) + N'.' + QUOTENAME({SQLLiteral(config.IndexName)});
EXEC sys.sp_rename @objname = @renameTarget, @newname = {SQLLiteral(config.NewIndexName)}, @objtype = N'INDEX';
";
        }

        /// <summary>
        /// Drops an index; primary key and unique constraints are dropped as constraints because DROP INDEX rejects them.
        /// </summary>
        public override string GenerateDropIndexScript(IndexOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.IndexName, "Index name");

            return $@"USE {QUOTENAME(config.DatabaseName)};
GO
DECLARE @tableName nvarchar(517) = QUOTENAME({SQLLiteral(config.SchemaName)}) + N'.' + QUOTENAME({SQLLiteral(config.TableName)});
DECLARE @indexName sysname = {SQLLiteral(config.IndexName)}, @sql nvarchar(max);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(@tableName) AND name = @indexName AND (is_primary_key = 1 OR is_unique_constraint = 1))
    SET @sql = N'ALTER TABLE ' + @tableName + N' DROP CONSTRAINT ' + QUOTENAME(@indexName) + N';';
ELSE
    SET @sql = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON ' + @tableName + N';';

EXEC (@sql);
";
        }

        public override string GenerateGetIndexInfoScript(IndexOperation.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    i.name AS IndexName,
    t.name AS TableName,
    s.name AS SchemaName,
    i.is_unique AS IsUnique,
    i.type_desc AS IndexType,
    i.fill_factor AS [FillFactor],
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
LEFT JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)} AND i.name = {SQLLiteral(config.IndexName)}
ORDER BY i.name;";
        }

        public override string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT i.name AS IndexName
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type = 'U' AND s.name = {SQLLiteral(schemaName)} AND o.name = {SQLLiteral(tableName)} AND i.name IS NOT NULL AND i.is_primary_key = 0
ORDER BY i.name;";
        }

        public override string GenerateGetIndexTypesScript()
        {
            return "SELECT 'NONCLUSTERED' AS IndexType UNION SELECT 'CLUSTERED' AS IndexType UNION SELECT 'UNIQUE CLUSTERED' AS IndexType UNION SELECT 'UNIQUE NONCLUSTERED' AS IndexType;";
        }
        #endregion

        #region Foreign Key Operations
        public override string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateOptionalObjectName(config.FKName, "Foreign key name");
            ValidateObjectName(config.RefSchemaName, "Referenced schema name");
            ValidateObjectName(config.RefTableName, "Referenced table name");

            var mappings = config.Columns ?? new List<ForeignKeyOperation.ColumnMapping>();
            if (mappings.Count == 0)
                throw new InvalidOperationException("[BprDevKit] A foreign key needs at least one column pair.");
            if (mappings.Count > 32)
                throw new InvalidOperationException("[BprDevKit] A foreign key can use at most 32 columns.");

            var localColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in mappings)
            {
                ValidateObjectName(mapping.ColumnName, "Column name");
                ValidateObjectName(mapping.RefColumnName, "Referenced column name");
                if (!localColumns.Add(mapping.ColumnName))
                    throw new InvalidOperationException($"[BprDevKit] Column [{mapping.ColumnName}] is used more than once in the foreign key.");
            }

            var onDelete = NormalizeReferentialAction(config.OnDelete, "ON DELETE");
            var onUpdate = NormalizeReferentialAction(config.OnUpdate, "ON UPDATE");

            var foreignKeyName = string.IsNullOrWhiteSpace(config.FKName)
                ? $"FK_{config.RefSchemaName}_{config.RefTableName}_{string.Join("_", mappings.Select(mapping => mapping.RefColumnName))}_2_{config.SchemaName}_{config.TableName}_{string.Join("_", mappings.Select(mapping => mapping.ColumnName))}"
                : config.FKName;
            if (foreignKeyName.Length > 128)
                throw new InvalidOperationException("[BprDevKit] The generated foreign key name is longer than 128 characters; specify a shorter name.");

            var table = $"{QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}";
            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            builder.Append($"ALTER TABLE {table} {(config.Enabled ? "WITH CHECK" : "WITH NOCHECK")} ADD CONSTRAINT {QUOTENAME(foreignKeyName)} ")
                .Append($"FOREIGN KEY ({string.Join(", ", mappings.Select(mapping => QUOTENAME(mapping.ColumnName)))}) ")
                .Append($"REFERENCES {QUOTENAME(config.RefSchemaName)}.{QUOTENAME(config.RefTableName)} ({string.Join(", ", mappings.Select(mapping => QUOTENAME(mapping.RefColumnName)))}) ")
                .Append($"ON DELETE {onDelete} ON UPDATE {onUpdate}");
            if (config.IsNotForReplication)
                builder.Append(" NOT FOR REPLICATION");
            builder.AppendLine(";");

            if (!config.Enabled)
                builder.AppendLine($"ALTER TABLE {table} NOCHECK CONSTRAINT {QUOTENAME(foreignKeyName)};");

            return builder.ToString();
        }

        /// <summary>
        /// Enables a foreign key (re-validating the existing rows so it is trusted again) or disables it.
        /// </summary>
        public override string GenerateAlterForeignKeyScript(ForeignKeyOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.FKName, "Foreign key name");

            var table = $"{QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}";
            var statement = config.Enabled
                ? $"ALTER TABLE {table} WITH CHECK CHECK CONSTRAINT {QUOTENAME(config.FKName)};"
                : $"ALTER TABLE {table} NOCHECK CONSTRAINT {QUOTENAME(config.FKName)};";

            return $"USE {QUOTENAME(config.DatabaseName)};{Environment.NewLine}GO{Environment.NewLine}{statement}{Environment.NewLine}";
        }

        public override string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateTableTarget(config.DatabaseName, config.SchemaName, config.TableName);
            ValidateObjectName(config.FKName, "Foreign key name");

            return $"USE {QUOTENAME(config.DatabaseName)};{Environment.NewLine}GO{Environment.NewLine}"
                + $"ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP CONSTRAINT {QUOTENAME(config.FKName)};{Environment.NewLine}";
        }

        public override string GenerateGetForeignKeyInfoScript(ForeignKeyOperation.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    fk.name AS ForeignKeyName,
    t.name AS TableName,
    s.name AS SchemaName,
    c.name AS ColumnName,
    rt.name AS ReferencedTableName,
    rs.name AS ReferencedSchemaName,
    rc.name AS ReferencedColumnName,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
INNER JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
INNER JOIN sys.columns rc ON fk.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)} AND c.name = {SQLLiteral(config.ColumnName)};";
        }

        public override string GenerateGetReferentialActionsScript()
        {
            return "SELECT 'NO ACTION' AS Action UNION SELECT 'CASCADE' AS Action UNION SELECT 'SET NULL' AS Action UNION SELECT 'SET DEFAULT' AS Action ORDER BY Action;";
        }
        #endregion

        #region View Operations
        public override string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("CREATE VIEW " + QUOTENAME(schemaName) + "." + QUOTENAME(viewName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to SELECT statement
            string indentSelect = "    ";
            string[] lines = selectStatement.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentSelect + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("ALTER VIEW " + QUOTENAME(schemaName) + "." + QUOTENAME(viewName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to SELECT statement
            string indentSelect = "    ";
            string[] lines = selectStatement.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentSelect + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateDropViewScript(string databaseName, string schemaName, string viewName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP VIEW {QUOTENAME(schemaName)}.{QUOTENAME(viewName)}; PRINT N'[BprDevKit] View [{viewName}] dropped successfully.';";
        }

        public override string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT
    v.name AS ViewName,
    s.name AS SchemaName,
    v.create_date AS CreateDate,
    v.modify_date AS ModifyDate,
    m.definition AS ViewDefinition
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
LEFT JOIN sys.sql_modules m ON v.object_id = m.object_id
WHERE s.name = {SQLLiteral(schemaName)} AND v.name = {SQLLiteral(viewName)};";
        }

        public override string GenerateGetViewsScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT v.name AS ViewName, s.name AS SchemaName FROM sys.views v INNER JOIN sys.schemas s ON v.schema_id = s.schema_id ORDER BY s.name, v.name;";
        }

        public override string GenerateTestViewScript(string databaseName, string selectStatement)
        {
            return $"USE {QUOTENAME(databaseName)}; SET NOCOUNT ON; {selectStatement};";
        }
        #endregion

        #region Stored Procedure Operations
        public override string GenerateCreateProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("CREATE PROCEDURE " + QUOTENAME(schemaName) + "." + QUOTENAME(procName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateAlterProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("ALTER PROCEDURE " + QUOTENAME(schemaName) + "." + QUOTENAME(procName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateDropProcedureScript(string databaseName, string schemaName, string procName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP PROCEDURE {QUOTENAME(schemaName)}.{QUOTENAME(procName)}; PRINT N'[BprDevKit] Stored Procedure [{procName}] dropped successfully.';";
        }

        public override string GenerateGetProcedureInfoScript(string databaseName, string schemaName, string procName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT
    p.name AS ProcedureName,
    s.name AS SchemaName,
    p.create_date AS CreateDate,
    p.modify_date AS ModifyDate,
    m.definition AS ProcedureDefinition
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
LEFT JOIN sys.sql_modules m ON p.object_id = m.object_id
WHERE s.name = {SQLLiteral(schemaName)} AND p.name = {SQLLiteral(procName)};";
        }

        public override string GenerateGetProceduresScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT p.name AS ProcedureName, s.name AS SchemaName FROM sys.procedures p INNER JOIN sys.schemas s ON p.schema_id = s.schema_id ORDER BY s.name, p.name;";
        }

        public override string GenerateTestProcedureScript(string databaseName, string procName, string body)
        {
            return $"USE {QUOTENAME(databaseName)}; PRINT N'[BprDevKit] Testing Stored Procedure [{procName}]...'; BEGIN {body} END; PRINT N'[BprDevKit] Stored Procedure [{procName}] test completed successfully.';";
        }
        #endregion

        #region Function Operations
        public override string GenerateCreateFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("CREATE FUNCTION " + QUOTENAME(schemaName) + "." + QUOTENAME(funcName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateAlterFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .Append("ALTER FUNCTION " + QUOTENAME(schemaName) + "." + QUOTENAME(funcName));
            if (withEncryption)
                sb.Append(" WITH ENCRYPTION");
            sb.AppendLine(" AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public override string GenerateDropFunctionScript(string databaseName, string schemaName, string funcName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP FUNCTION {QUOTENAME(schemaName)}.{QUOTENAME(funcName)}; PRINT N'[BprDevKit] Function [{funcName}] dropped successfully.';";
        }

        public override string GenerateGetFunctionInfoScript(string databaseName, string schemaName, string funcName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT
    f.name AS FunctionName,
    s.name AS SchemaName,
    f.create_date AS CreateDate,
    f.modify_date AS ModifyDate,
    m.definition AS FunctionDefinition
FROM sys.objects f
INNER JOIN sys.schemas s ON f.schema_id = s.schema_id
LEFT JOIN sys.sql_modules m ON f.object_id = m.object_id
WHERE s.name = {SQLLiteral(schemaName)} AND f.name = {SQLLiteral(funcName)} AND f.type = 'FN';";
        }

        public override string GenerateGetFunctionsScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT f.name AS FunctionName, s.name AS SchemaName FROM sys.objects f INNER JOIN sys.schemas s ON f.schema_id = s.schema_id WHERE f.type = 'FN' ORDER BY s.name, f.name;";
        }

        public override string GenerateTestFunctionScript(string databaseName, string funcName, string body)
        {
            return $"USE {QUOTENAME(databaseName)}; PRINT N'[BprDevKit] Testing Function [{funcName}]...'; DECLARE @Result INT; {body} SELECT @Result; PRINT N'[BprDevKit] Function [{funcName}] test completed successfully.';";
        }
        #endregion

        #region Backup/Restore
        public override string GenerateBackupScript(BackupOperation config)
        {
            return $"BACKUP DATABASE {QUOTENAME(config.DatabaseName)} TO DISK = {SQLLiteral(config.BackupPath)} WITH FORMAT, INIT, STATS = 10; PRINT N'[BprDevKit] Database [{config.DatabaseName}] backed up successfully to [{config.BackupPath}].';";
        }

        public override string GenerateRestoreScript(RestoreOperation config)
        {
            return $"RESTORE DATABASE {QUOTENAME(config.DatabaseName)} FROM DISK = {SQLLiteral(config.RestorePath)} WITH REPLACE; PRINT N'[BprDevKit] Database [{config.DatabaseName}] restored successfully from [{config.RestorePath}].';";
        }
        #endregion

        #region Utility
        public override string GenerateUseDatabaseScript(string databaseName)
        {
            return "USE " + QUOTENAME(databaseName) + ";";
        }

        public override string GenerateTransactionBeginScript()
        {
            return "BEGIN TRANSACTION;";
        }

        public override string GenerateTransactionCommitScript()
        {
            return "COMMIT TRANSACTION;";
        }

        public override string GenerateTransactionRollbackScript()
        {
            return "ROLLBACK TRANSACTION;";
        }

        public override string GenerateTryCatchBlock(string tryBlock, string catchBlock)
        {
            return $"TRY\n{tryBlock}\nCATCH\nBEGIN\n    {catchBlock}\n    RETURN;\nEND";
        }
        #endregion

        #region Helper Methods
        private static void ValidateTableTarget(string databaseName, string schemaName, string tableName)
        {
            ValidateDatabaseName(databaseName);
            ValidateObjectName(schemaName, "Schema name");
            ValidateObjectName(tableName, "Table name");
        }

        private static string NormalizeReferentialAction(string? action, string label)
        {
            var normalized = (action ?? string.Empty).Trim().ToUpperInvariant();
            if (!ForeignKeyOperation.ReferentialActions.Contains(normalized))
                throw new InvalidOperationException($"[BprDevKit] {label} must be one of: {string.Join(", ", ForeignKeyOperation.ReferentialActions)}.");

            return normalized;
        }

        private static void ValidateIndexColumns(IReadOnlyCollection<IndexOperation.KeyColumn> keys, IReadOnlyCollection<string> includes)
        {
            if (keys.Count == 0)
                throw new InvalidOperationException("[BprDevKit] An index needs at least one key column.");
            if (keys.Count > 32)
                throw new InvalidOperationException("[BprDevKit] An index can have at most 32 key columns.");
            if (includes.Count > 1023)
                throw new InvalidOperationException("[BprDevKit] An index can include at most 1023 columns.");

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                ValidateObjectName(key.ColumnName, "Column name");
                if (!used.Add(key.ColumnName))
                    throw new InvalidOperationException($"[BprDevKit] Column [{key.ColumnName}] is used more than once in the index.");
            }

            foreach (var include in includes)
            {
                ValidateObjectName(include, "Included column name");
                if (!used.Add(include))
                    throw new InvalidOperationException($"[BprDevKit] Column [{include}] is used more than once in the index.");
            }
        }

        private static string ResolveIndexName(IndexOperation.Create config, IReadOnlyCollection<IndexOperation.KeyColumn> keys)
        {
            if (!string.IsNullOrWhiteSpace(config.IndexName))
                return config.IndexName;

            string name;
            if (config.IsPrimaryKey)
            {
                var pkCols = string.Join("_", keys.Select(key => key.ColumnName));
                name = $"PK_{config.SchemaName}_{config.TableName}_{pkCols}";
            }
            else
            {
                var prefix = config.IsUnique ? "UIX" : "IX";
                name = $"{prefix}_{config.SchemaName}_{config.TableName}_{string.Join("_", keys.Select(key => key.ColumnName))}";
            }

            if (name.Length > 128)
                throw new InvalidOperationException("[BprDevKit] The generated index name is longer than 128 characters; specify a shorter name.");

            return name;
        }

        private static string BuildCollationClause(ColumnDefinition column)
        {
            if (string.IsNullOrWhiteSpace(column.Collation))
                return string.Empty;

            var type = ColumnDataType.Find(column.DataType);
            var isCharacterType = type is { Kind: ColumnDataTypeKind.Length } && !type.Name.Contains("binary", StringComparison.OrdinalIgnoreCase);
            if (!isCharacterType)
                return string.Empty;

            if (!column.Collation.All(character => char.IsAsciiLetterOrDigit(character) || character == '_'))
                throw new InvalidOperationException($"[BprDevKit] The collation of column [{column.ColumnName}] contains invalid characters.");

            return " COLLATE " + column.Collation;
        }

        /// <summary>
        /// Sets, replaces or (for an empty description) removes the MS_Description property of a table or one of its columns.
        /// </summary>
        private static void AppendDescriptionUpsert(StringBuilder builder, string schemaName, string tableName, string? columnName, string? description)
        {
            var schema = SQLLiteral(schemaName);
            var table = SQLLiteral(tableName);
            var tableId = $"OBJECT_ID(QUOTENAME({schema}) + N'.' + QUOTENAME({table}))";

            var levels = $"@level0type = N'SCHEMA', @level0name = {schema}, @level1type = N'TABLE', @level1name = {table}";
            var target = $"major_id = {tableId} AND minor_id = 0";
            if (columnName is not null)
            {
                var column = SQLLiteral(columnName);
                levels += $", @level2type = N'COLUMN', @level2name = {column}";
                target = $"major_id = {tableId} AND minor_id = COLUMNPROPERTY({tableId}, {column}, 'ColumnId')";
            }

            var exists = $"EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND name = N'MS_Description' AND {target})";

            if (string.IsNullOrWhiteSpace(description))
            {
                builder.AppendLine($@"IF {exists}
    EXEC sys.sp_dropextendedproperty @name = N'MS_Description', {levels};");
                return;
            }

            var value = SQLLiteral(description);
            builder.AppendLine($@"IF {exists}
    EXEC sys.sp_updateextendedproperty @name = N'MS_Description', @value = {value}, {levels};
ELSE
    EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = {value}, {levels};");
        }

        private const int MaxDescriptionLength = 3000;

        private static void ValidateDescription(string? description)
        {
            if (description is not null && description.Length > MaxDescriptionLength)
                throw new InvalidOperationException($"[BprDevKit] A description cannot exceed {MaxDescriptionLength} characters.");
        }

        /// <summary>
        /// Validates the whole column set of a new table (names, types, sizes, identity and defaults).
        /// </summary>
        private static List<ColumnDefinition> ValidateColumnDefinitions(IReadOnlyCollection<ColumnDefinition>? columns)
        {
            if (columns is null || columns.Count == 0)
                throw new InvalidOperationException("[BprDevKit] A table needs at least one column.");

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<ColumnDefinition>();

            foreach (var column in columns)
            {
                ValidateObjectName(column.ColumnName, "Column name");
                if (!names.Add(column.ColumnName))
                    throw new InvalidOperationException($"[BprDevKit] Column name [{column.ColumnName}] is used more than once.");

                var type = ColumnDataType.Find(column.DataType)
                    ?? throw new InvalidOperationException($"[BprDevKit] Column [{column.ColumnName}] uses an unsupported data type '{column.DataType}'.");

                // Throws when the size arguments are missing or out of range.
                FormatColumnDataType(column, type);
                ValidateDescription(column.Description);

                if (column.IsIdentity)
                {
                    if (!type.SupportsIdentity)
                        throw new InvalidOperationException($"[BprDevKit] Column [{column.ColumnName}] cannot be an identity column because {type.Name} does not support identity.");
                    if (type.Kind == ColumnDataTypeKind.PrecisionScale && (column.Scale ?? 0) != 0)
                        throw new InvalidOperationException($"[BprDevKit] Identity column [{column.ColumnName}] must have a scale of 0.");
                    if (column.IdentityIncrement == 0)
                        throw new InvalidOperationException($"[BprDevKit] Identity column [{column.ColumnName}] needs an increment other than 0.");
                    if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                        throw new InvalidOperationException($"[BprDevKit] Identity column [{column.ColumnName}] cannot have a default value.");
                }

                if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                    ValidateExpression(column.DefaultValue, $"default of column [{column.ColumnName}]");

                result.Add(column);
            }

            if (result.Count(column => column.IsIdentity) > 1)
                throw new InvalidOperationException("[BprDevKit] A table can have only one identity column.");

            return result;
        }

        private static string BuildColumnDefinition(ColumnDefinition column, string schemaName, string tableName)
        {
            var type = ColumnDataType.Find(column.DataType)!;
            var nullable = column.IsNullable && !column.IsPrimaryKey && !column.IsIdentity;

            var definition = new StringBuilder()
                .Append(QUOTENAME(column.ColumnName)).Append(' ')
                .Append(FormatColumnDataType(column, type))
                .Append(BuildCollationClause(column)).Append(' ')
                .Append(nullable ? "NULL" : "NOT NULL");

            if (column.IsIdentity)
                definition.Append($" IDENTITY({column.IdentitySeed},{column.IdentityIncrement})");

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                var dfName = $"DF_{schemaName}_{tableName}_{column.ColumnName}";
                definition.Append($" CONSTRAINT {QUOTENAME(dfName)} DEFAULT ({column.DefaultValue!.Trim()})");
            }

            return definition.ToString();
        }

        private static string FormatColumnDataType(ColumnDefinition column, ColumnDataType type)
        {
            switch (type.Kind)
            {
                case ColumnDataTypeKind.Length:
                    if (column.Length is null)
                        throw new InvalidOperationException($"[BprDevKit] Column [{column.ColumnName}] needs a length.");
                    if (column.Length == -1)
                    {
                        if (!type.SupportsMax)
                            throw new InvalidOperationException($"[BprDevKit] {type.Name} does not support MAX (column [{column.ColumnName}]).");
                        return $"{type.Name}(MAX)";
                    }
                    if (column.Length < 1 || column.Length > type.MaxLength)
                        throw new InvalidOperationException($"[BprDevKit] The length of column [{column.ColumnName}] must be between 1 and {type.MaxLength}.");
                    return $"{type.Name}({column.Length})";

                case ColumnDataTypeKind.PrecisionScale:
                    if (column.Precision is null || column.Precision < 1 || column.Precision > 38)
                        throw new InvalidOperationException($"[BprDevKit] The precision of column [{column.ColumnName}] must be between 1 and 38.");
                    var scale = column.Scale ?? 0;
                    if (scale < 0 || scale > column.Precision)
                        throw new InvalidOperationException($"[BprDevKit] The scale of column [{column.ColumnName}] must be between 0 and its precision.");
                    return $"{type.Name}({column.Precision},{scale})";

                case ColumnDataTypeKind.FractionalSeconds:
                    if (column.Scale is null)
                        return type.Name;
                    if (column.Scale < 0 || column.Scale > 7)
                        throw new InvalidOperationException($"[BprDevKit] The fractional seconds precision of column [{column.ColumnName}] must be between 0 and 7.");
                    return $"{type.Name}({column.Scale})";

                case ColumnDataTypeKind.VectorDimensions:
                    if (column.Length is null || column.Length < 1)
                        throw new InvalidOperationException($"[BprDevKit] Column [{column.ColumnName}] of type vector requires a positive dimension count.");
                    return $"{type.Name}({column.Length})";

                default:
                    return type.Name;
            }
        }

        /// <summary>
        /// A default is an arbitrary T-SQL expression, so it is only accepted when it cannot end the
        /// surrounding DEFAULT (...) clause or start another statement: no ';', no comments and balanced
        /// parentheses (string literals are skipped).
        /// </summary>
        private static void ValidateExpression(string expression, string subject)
        {
            var depth = 0;
            var inString = false;

            for (var index = 0; index < expression.Length; index++)
            {
                var current = expression[index];

                if (current == '\'')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (current == ';')
                    throw new InvalidOperationException($"[BprDevKit] The {subject} cannot contain ';'.");

                if (index + 1 < expression.Length)
                {
                    var pair = expression.Substring(index, 2);
                    if (pair == "--" || pair == "/*" || pair == "*/")
                        throw new InvalidOperationException($"[BprDevKit] The {subject} cannot contain comments.");
                }

                if (current == '(') depth++;
                if (current == ')' && --depth < 0)
                    throw new InvalidOperationException($"[BprDevKit] The {subject} has unbalanced parentheses.");
            }

            if (inString || depth != 0)
                throw new InvalidOperationException($"[BprDevKit] The {subject} has an unterminated string or unbalanced parentheses.");
        }

        private static string BuildAddDescriptionStatement(string description, string schemaName, string tableName, string? columnName)
        {
            var statement = $"EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = {SQLLiteral(description)}, "
                + $"@level0type = N'SCHEMA', @level0name = {SQLLiteral(schemaName)}, "
                + $"@level1type = N'TABLE', @level1name = {SQLLiteral(tableName)}";

            if (columnName is not null)
                statement += $", @level2type = N'COLUMN', @level2name = {SQLLiteral(columnName)}";

            return statement + ";";
        }

        private const string UserSchemaFilter =
            "s.schema_id < 16384 AND s.name NOT IN (N'guest', N'INFORMATION_SCHEMA', N'sys')";

        /// <summary>
        /// Appends the batch that renames a schema. Names are passed as variables (not concatenated into the
        /// dynamic statements) so quotes inside a name can never break the generated SQL.
        /// </summary>
        private static void AppendRenameSchemaStatements(StringBuilder builder, string oldSchemaName, string newSchemaName)
        {
            builder.AppendLine($@"DECLARE @oldSchema sysname = {SQLLiteral(oldSchemaName)}, @newSchema sysname = {SQLLiteral(newSchemaName)};
DECLARE @owner sysname, @objectName sysname, @sql nvarchar(max);

SELECT @owner = dp.name
FROM sys.schemas AS s
INNER JOIN sys.database_principals AS dp ON dp.principal_id = s.principal_id
WHERE s.name = @oldSchema;

IF @owner IS NULL
    THROW 50000, N'[BprDevKit] The schema to rename does not exist.', 1;

IF SCHEMA_ID(@newSchema) IS NOT NULL
    THROW 50000, N'[BprDevKit] A schema with the new name already exists.', 1;

IF EXISTS (SELECT 1 FROM sys.types WHERE schema_id = SCHEMA_ID(@oldSchema) AND is_user_defined = 1)
    OR EXISTS (SELECT 1 FROM sys.xml_schema_collections WHERE schema_id = SCHEMA_ID(@oldSchema))
    THROW 50000, N'[BprDevKit] A schema that contains user-defined types or XML schema collections cannot be renamed.', 1;

SET @sql = N'CREATE SCHEMA ' + QUOTENAME(@newSchema) + N' AUTHORIZATION ' + QUOTENAME(@owner) + N';';
EXEC (@sql);

DECLARE transfer_cursor CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
    SELECT o.name
    FROM sys.objects AS o
    WHERE o.schema_id = SCHEMA_ID(@oldSchema)
        AND o.parent_object_id = 0
        AND o.is_ms_shipped = 0
        AND o.type IN ('U', 'V', 'P', 'PC', 'X', 'FN', 'IF', 'TF', 'FS', 'FT', 'AF', 'SN', 'SO');

OPEN transfer_cursor;
FETCH NEXT FROM transfer_cursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'ALTER SCHEMA ' + QUOTENAME(@newSchema) + N' TRANSFER OBJECT::' + QUOTENAME(@oldSchema) + N'.' + QUOTENAME(@objectName) + N';';
    EXEC (@sql);
    FETCH NEXT FROM transfer_cursor INTO @objectName;
END
CLOSE transfer_cursor;
DEALLOCATE transfer_cursor;

SET @sql = N'DROP SCHEMA ' + QUOTENAME(@oldSchema) + N';';
EXEC (@sql);");
        }

        private static void ThrowIfProtectedSchema(string schemaName)
        {
            if (SchemaOperation.IsProtected(schemaName))
                throw new InvalidOperationException($"[BprDevKit] Schema [{schemaName}] is a built-in SQL Server schema and cannot be created, modified, or dropped.");
        }

        private static void ValidateOptionalObjectName(string? name, string label)
        {
            if (!string.IsNullOrWhiteSpace(name))
                ValidateObjectName(name, label);
        }

        private static void ValidateObjectName(string? name, string label)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"[BprDevKit] {label} cannot be empty.");

            if (name.Length > 128)
                throw new InvalidOperationException($"[BprDevKit] {label} cannot exceed 128 characters.");

            if (!string.Equals(name, name.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"[BprDevKit] {label} cannot start or end with whitespace.");

            if (name.Any(char.IsControl))
                throw new InvalidOperationException($"[BprDevKit] {label} cannot contain control characters.");
        }

        private static void AppendDatabaseSettings(
           StringBuilder builder,
           string databaseName,
           string? recoveryModel,
           short? compatibilityLevel,
           bool? isReadOnly,
           bool? autoShrink,
           bool? autoClose,
           string? pageVerify)
        {
            if (!string.IsNullOrWhiteSpace(recoveryModel))
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET RECOVERY {recoveryModel.ToUpperInvariant()} WITH NO_WAIT;");

            if (compatibilityLevel.HasValue)
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET COMPATIBILITY_LEVEL = {compatibilityLevel.Value};");

            if (autoShrink.HasValue)
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET AUTO_SHRINK {(autoShrink.Value ? "ON" : "OFF")};");

            if (autoClose.HasValue)
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET AUTO_CLOSE {(autoClose.Value ? "ON" : "OFF")};");

            if (!string.IsNullOrWhiteSpace(pageVerify))
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET PAGE_VERIFY {pageVerify.ToUpperInvariant()} WITH NO_WAIT;");

            if (isReadOnly.HasValue)
                builder.AppendLine($"ALTER DATABASE {QUOTENAME(databaseName)} SET {(isReadOnly.Value ? "READ_ONLY" : "READ_WRITE")} WITH NO_WAIT;");
        }

        private static string BuildDatabaseFilePath(string directory, string databaseName, string extension)
        {
            return directory.TrimEnd('\\') + "\\" + databaseName + extension;
        }

        private static void ValidateCreateConfiguration(DatabaseOperation.Create config)
        {
            ValidateDatabaseName(config.DatabaseName);

            if (string.IsNullOrWhiteSpace(config.DataFilePath))
                throw new InvalidOperationException("[BprDevKit] DataFilePath cannot be empty.");

            ValidateSizes(config.InitialSizeMB, config.MaxSizeMB, config.FileGrowthMB, config.LogInitialSizeMB, config.LogMaxSizeMB, config.LogFileGrowthMB);
            ValidateDatabaseOptions(config.RecoveryModel, config.PageVerify, config.CompatibilityLevel, config.Collation);

            if (!string.IsNullOrWhiteSpace(config.FileGroup) && config.FileGroup.Length > 128)
                throw new InvalidOperationException("[BprDevKit] FileGroup cannot exceed 128 characters.");
        }

        private static void ValidateAlterConfiguration(DatabaseOperation.Alter config)
        {
            ValidateDatabaseName(config.DatabaseName);
            ValidateDatabaseOptions(config.RecoveryModel, config.PageVerify, config.CompatibilityLevel, config.Collation);
        }

        private static void ValidateDatabaseName(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("[BprDevKit] Database name cannot be empty.");

            if (databaseName.Length > 128)
                throw new InvalidOperationException("[BprDevKit] Database name cannot exceed 128 characters.");
        }

        private static void ValidateSizes(int initialSize, int? maxSize, int fileGrowth, int logInitialSize, int? logMaxSize, int logFileGrowth)
        {
            if (initialSize <= 0 || fileGrowth <= 0 || logInitialSize <= 0 || logFileGrowth <= 0)
                throw new InvalidOperationException("[BprDevKit] Database file sizes and growth values must be greater than zero.");

            if (maxSize.HasValue && maxSize.Value < initialSize)
                throw new InvalidOperationException("[BprDevKit] MaxSizeMB cannot be less than InitialSizeMB.");

            if (logMaxSize.HasValue && logMaxSize.Value < logInitialSize)
                throw new InvalidOperationException("[BprDevKit] LogMaxSizeMB cannot be less than LogInitialSizeMB.");
        }

        private static void ValidateDatabaseOptions(string? recoveryModel, string? pageVerify, short? compatibilityLevel, string? collation)
        {
            if (!string.IsNullOrWhiteSpace(recoveryModel) &&
                !new[] { "SIMPLE", "FULL", "BULK_LOGGED" }.Contains(recoveryModel, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("[BprDevKit] RecoveryModel must be SIMPLE, FULL, or BULK_LOGGED.");

            if (!string.IsNullOrWhiteSpace(pageVerify) &&
                !new[] { "CHECKSUM", "TORN_PAGE_DETECTION", "NONE" }.Contains(pageVerify, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("[BprDevKit] PageVerify must be CHECKSUM, TORN_PAGE_DETECTION, or NONE.");

            if (compatibilityLevel.HasValue && compatibilityLevel.Value <= 0)
                throw new InvalidOperationException("[BprDevKit] CompatibilityLevel must be greater than zero.");

            if (!string.IsNullOrWhiteSpace(collation) && !System.Text.RegularExpressions.Regex.IsMatch(collation, "^[A-Za-z0-9_]+$"))
                throw new InvalidOperationException("[BprDevKit] Collation contains unsupported characters.");
        }

        private static bool IsSystemDatabase(string databaseName)
        {
            return new[] { "master", "model", "msdb", "tempdb" }.Contains(databaseName, StringComparer.OrdinalIgnoreCase);
        }

        private static string QUOTENAME(string? name, string quoteChar = "[")
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return quoteChar switch
            {
                "[" => "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]",
                "'" => "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'",
                "\"" => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"",
                _ => quoteChar + name.Replace(quoteChar, quoteChar + quoteChar, StringComparison.Ordinal) + quoteChar
            };
        }

        private static string SQLLiteral(string value) => "N'" + (value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal) + "'";
        #endregion
    }
}
