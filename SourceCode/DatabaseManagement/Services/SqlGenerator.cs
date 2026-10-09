using System;
using System.Text;
using System.Collections.Generic;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Main SQL script generator that dynamically generates SQL scripts based on configuration objects
    /// </summary>
    public class SqlGenerator : ISqlGenerator
    {
        #region Database Operations
        public string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
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

        public string GenerateAlterDatabaseScript(DatabaseOperation.Alter config)
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

        public string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
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

        public string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config)
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

        public string GenerateGetDatabasesScript()
        {
            return "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name;";
        }

        public string GenerateGetCollationsScript()
        {
            return "SELECT name FROM sys.fn_helpcollations() ORDER BY name;";
        }

        public string GenerateGetFileGroupsScript()
        {
            return "SELECT name FROM sys.filegroups ORDER BY name;";
        }
        #endregion

        #region Schema Operations
        public string GenerateCreateSchemaScript(SchemaOperation.Create config)
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
        public string GenerateAlterSchemaScript(SchemaOperation.Alter config)
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
        public string GenerateDropSchemaScript(SchemaOperation.Drop config)
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
        public string GenerateGetSchemasScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);

            return $@"SELECT s.name
FROM {QUOTENAME(databaseName)}.sys.schemas AS s
WHERE {UserSchemaFilter}
ORDER BY s.name;";
        }

        /// <summary>
        /// Schema list with owner and object count (tables, views, routines, synonyms, sequences,
        /// user-defined types and XML schema collections).
        /// </summary>
        public string GenerateGetSchemaSummariesScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);
            var database = QUOTENAME(databaseName);

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
WHERE {UserSchemaFilter}
ORDER BY s.name;";
        }

        /// <summary>
        /// Everything that lives inside one schema (the same set that must be empty before the schema can be dropped).
        /// </summary>
        public string GenerateGetSchemaObjectsScript(SchemaOperation.Information config)
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
        public string GenerateGetSchemaOwnersScript(string databaseName)
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
        public string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateDatabaseName(config.DatabaseName);
            ValidateObjectName(config.SchemaName, "Schema name");
            ValidateObjectName(config.TableName, "Table name");
            ValidateOptionalObjectName(config.FileGroup, "File group");
            ValidateDescription(config.Description);

            var columns = ValidateColumnDefinitions(config.Columns);
            var keyColumns = columns.Where(column => column.IsPrimaryKey).ToList();

            const string primaryKeyPrefix = "PK_";
            if (keyColumns.Count > 0 && primaryKeyPrefix.Length + config.TableName.Length > 128)
                throw new InvalidOperationException("[BprDevKit] The table name is too long to derive a primary key constraint name from it.");

            var builder = new StringBuilder();
            builder.AppendLine($"USE {QUOTENAME(config.DatabaseName)};")
                .AppendLine("GO");

            var definitions = columns.Select(BuildColumnDefinition).ToList();
            if (keyColumns.Count > 0)
            {
                var keyList = string.Join(", ", keyColumns.Select(column => QUOTENAME(column.ColumnName)));
                definitions.Add($"CONSTRAINT {QUOTENAME(primaryKeyPrefix + config.TableName)} PRIMARY KEY CLUSTERED ({keyList})");
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
        public string GenerateAlterTableScript(TableOperation.Alter config)
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
            {
                var schema = SQLLiteral(config.SchemaName);
                var table = SQLLiteral(currentTableName);
                var levels = $"@level0type = N'SCHEMA', @level0name = {schema}, @level1type = N'TABLE', @level1name = {table}";
                var exists = $"EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND minor_id = 0 AND name = N'MS_Description' AND major_id = OBJECT_ID(QUOTENAME({schema}) + N'.' + QUOTENAME({table})))";

                if (string.IsNullOrWhiteSpace(config.Description))
                {
                    builder.AppendLine($@"IF {exists}
    EXEC sys.sp_dropextendedproperty @name = N'MS_Description', {levels};");
                }
                else
                {
                    var value = SQLLiteral(config.Description);
                    builder.AppendLine($@"IF {exists}
    EXEC sys.sp_updateextendedproperty @name = N'MS_Description', @value = {value}, {levels};
ELSE
    EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = {value}, {levels};");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Drops a table. ForceDropDependent first drops the foreign keys of other tables that reference it.
        /// </summary>
        public string GenerateDropTableScript(TableOperation.Drop config)
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
        public string GenerateGetTableInfoScript(TableOperation.Information config)
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

        public string GenerateGetTablesScript(string databaseName, string schemaName)
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
        public string GenerateGetTableSummariesScript(string databaseName)
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

        public string GenerateGetTableColumnsScript(TableOperation.Information config)
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

        public string GenerateGetTableIndexesScript(TableOperation.Information config)
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
        public string GenerateGetTableForeignKeysScript(TableOperation.Information config)
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
        public string GenerateGetFileGroupsScript(string databaseName)
        {
            ValidateDatabaseName(databaseName);

            return $@"SELECT fg.name
FROM {QUOTENAME(databaseName)}.sys.filegroups AS fg
WHERE fg.type = 'FG'
ORDER BY fg.is_default DESC, fg.name;";
        }
        #endregion

        #region Column Operations
        public string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
                .AppendLine("ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " ADD ");

            // Build column definition
            var columnDef = QUOTENAME(config.ColumnName) + " " + config.DataType;

            if (config.Length.HasValue || config.Precision.HasValue || config.Scale.HasValue)
            {
                if (config.Length.HasValue && config.Precision.HasValue && config.Scale.HasValue)
                    columnDef += "(" + config.Precision.Value + "," + config.Scale.Value + ")";
                else if (config.Length.HasValue)
                    columnDef += "(" + config.Length.Value + ")";
                else if (config.Precision.HasValue)
                    columnDef += "(" + config.Precision.Value + ")";
            }

            if (!config.IsNullable)
                columnDef += " NOT NULL";

            if (config.IsIdentity)
                columnDef += " IDENTITY(" + config.IdentitySeed + "," + config.IdentityIncrement + ")";

            if (!string.IsNullOrWhiteSpace(config.Collation) && (config.DataType?.Contains("CHAR") == true || config.DataType?.Contains("NCHAR") == true || config.DataType?.Contains("VARCHAR") == true || config.DataType?.Contains("NVARCHAR") == true || config.DataType?.Contains("TEXT") == true || config.DataType?.Contains("NTEXT") == true))
                columnDef += " COLLATE " + config.Collation;

            sb.AppendLine("    " + columnDef + ";");

            if (!string.IsNullOrWhiteSpace(config.DefaultValue))
            {
                sb.AppendLine("ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " ADD CONSTRAINT DF_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + " DEFAULT (" + config.DefaultValue + ") FOR " + config.ColumnName + ";");
            }

            sb.AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';");

            return sb.ToString();
        }

        public string GenerateAlterColumnScript(ColumnOperation.Alter config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
                .AppendLine("ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " ALTER COLUMN ");

            // Build column definition
            var columnDef = QUOTENAME(config.ColumnName) + " " + config.DataType;

            if (config.Length.HasValue || config.Precision.HasValue || config.Scale.HasValue)
            {
                if (config.Length.HasValue && config.Precision.HasValue && config.Scale.HasValue)
                    columnDef += "(" + config.Precision.Value + "," + config.Scale.Value + ")";
                else if (config.Length.HasValue)
                    columnDef += "(" + config.Length.Value + ")";
                else if (config.Precision.HasValue)
                    columnDef += "(" + config.Precision.Value + ")";
            }

            if (config.IsNullable.HasValue)
                columnDef += config.IsNullable.Value ? " NULL" : " NOT NULL";

            if (!string.IsNullOrWhiteSpace(config.Collation) && (config.DataType?.Contains("CHAR") == true || config.DataType?.Contains("NCHAR") == true || config.DataType?.Contains("VARCHAR") == true || config.DataType?.Contains("NVARCHAR") == true || config.DataType?.Contains("TEXT") == true || config.DataType?.Contains("NTEXT") == true))
                columnDef += " COLLATE " + config.Collation;

            sb.AppendLine("    " + columnDef + ";");

            if (!string.IsNullOrWhiteSpace(config.DefaultValue))
            {
                sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";");
                sb.AppendLine("IF OBJECT_ID(N'DF_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "') IS NOT NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine("    EXEC('ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " DROP CONSTRAINT [DF_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "]');");
                sb.AppendLine("END");

                if (!string.IsNullOrWhiteSpace(config.DefaultName))
                    sb.AppendLine("ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " ADD CONSTRAINT " + QUOTENAME(config.DefaultName) + " DEFAULT (" + config.DefaultValue + ") FOR " + config.ColumnName + ";");
                else
                    sb.AppendLine("ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " ADD CONSTRAINT DF_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + " DEFAULT (" + config.DefaultValue + ") FOR " + config.ColumnName + ";");
            }

            if (!string.IsNullOrWhiteSpace(config.Description))
            {
                sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";");
                sb.AppendLine("IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'" + config.SchemaName + "." + config.TableName + "') AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'" + config.SchemaName + "." + config.TableName + "') AND name = N'" + config.ColumnName + "') AND name = N'MS_Description'))");
                sb.AppendLine("    EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=N'" + config.Description + "', @level0type=N'SCHEMA', @level0name=N'" + config.SchemaName + "', @level1type=N'TABLE', @level1name=N'" + config.TableName + "', @level2type=N'COLUMN', @level2name=N'" + config.ColumnName + "';");
                sb.AppendLine("ELSE");
                sb.AppendLine("    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'" + config.Description + "', @level0type=N'SCHEMA', @level0name=N'" + config.SchemaName + "', @level1type=N'TABLE', @level1name=N'" + config.TableName + "', @level2type=N'COLUMN', @level2name=N'" + config.ColumnName + "';");
            }

            sb.AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';");

            return sb.ToString();
        }

        public string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP COLUMN {QUOTENAME(config.ColumnName)}; PRINT N'[BprDevKit] Column [{config.ColumnName}] dropped successfully from table [{config.SchemaName}.{config.TableName}].';";
        }

        public string GenerateGetColumnInfoScript(ColumnOperation.Information config)
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

        public string GenerateGetDataTypesScript()
        {
            return "SELECT name FROM sys.types WHERE is_user_defined = 0 GROUP BY name ORDER BY name;";
        }

        public string GenerateGetColumnsScript(string databaseName, string schemaName, string tableName)
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
        public string GenerateCreateIndexScript(IndexOperation.Create config)
        {
            if (string.IsNullOrWhiteSpace(config.Columns))
                throw new ArgumentException("At least one index column is required.", nameof(config.Columns));

            var indexName = string.IsNullOrWhiteSpace(config.IndexName)
                ? $"IX_{config.TableName}_{config.Columns.Replace(" ", "").Replace(",", "_").Replace("ASC", "").Replace("DESC", "_desc")}"
                : config.IndexName;

            var indexType = (config.IndexType ?? "NONCLUSTERED").Trim();
            if (config.IsUnique && !indexType.StartsWith("UNIQUE ", StringComparison.OrdinalIgnoreCase))
                indexType = "UNIQUE " + indexType;

            var sb = new StringBuilder();
            sb.AppendLine($"USE {QUOTENAME(config.DatabaseName)};");
            sb.Append($"CREATE {indexType} INDEX {QUOTENAME(indexName)} ON {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} ({config.Columns})");

            if (!string.IsNullOrWhiteSpace(config.IncludeColumns))
                sb.Append($" INCLUDE ({config.IncludeColumns})");

            if (config.FillFactor > 0)
                sb.Append($" WITH (FILLFACTOR = {config.FillFactor})");

            if (!string.IsNullOrWhiteSpace(config.FileGroup))
                sb.Append($" ON {QUOTENAME(config.FileGroup)}");

            sb.AppendLine(";");
            return sb.ToString();
        }

        public string GenerateAlterIndexScript(IndexOperation.Alter config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; PRINT N'[BprDevKit] Index [{config.IndexName}] alter script generated (requires implementation).';";
        }

        public string GenerateDropIndexScript(IndexOperation.Drop config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; DROP INDEX {QUOTENAME(config.IndexName)} ON {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}; PRINT N'[BprDevKit] Index [{config.IndexName}] dropped successfully.';";
        }

        public string GenerateGetIndexInfoScript(IndexOperation.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    i.name AS IndexName,
    t.name AS TableName,
    s.name AS SchemaName,
    i.is_unique AS IsUnique,
    i.type_desc AS IndexType,
    i.fill_factor AS FillFactor,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
LEFT JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)} AND i.name = {SQLLiteral(config.IndexName)}
ORDER BY i.name;";
        }

        public string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT i.name AS IndexName
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type = 'U' AND s.name = {SQLLiteral(schemaName)} AND o.name = {SQLLiteral(tableName)} AND i.name IS NOT NULL AND i.is_primary_key = 0
ORDER BY i.name;";
        }

        public string GenerateGetIndexTypesScript()
        {
            return "SELECT 'NONCLUSTERED' AS IndexType UNION SELECT 'CLUSTERED' AS IndexType UNION SELECT 'UNIQUE CLUSTERED' AS IndexType UNION SELECT 'UNIQUE NONCLUSTERED' AS IndexType;";
        }
        #endregion

        #region Foreign Key Operations
        public string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config)
        {
            string fkName = "FK_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "_" + config.RefSchemaName + "_" + config.RefTableName + "_" + config.RefColumnName;

            return "USE " + QUOTENAME(config.DatabaseName) + ";\n" +
                   "ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + "\n" +
                   "ADD CONSTRAINT " + QUOTENAME(fkName) + "\n" +
                   "FOREIGN KEY (" + QUOTENAME(config.ColumnName) + ")\n" +
                   "REFERENCES " + QUOTENAME(config.RefSchemaName) + "." + QUOTENAME(config.RefTableName) + " (" + QUOTENAME(config.RefColumnName) + ")\n" +
                   "ON DELETE " + config.OnDelete + ";";
        }

        public string GenerateAlterForeignKeyScript(ForeignKeyOperation.Alter config)
        {
            string fkName = "FK_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "_" + config.RefSchemaName + "_" + config.RefTableName + "_" + config.RefColumnName;

            return "USE " + QUOTENAME(config.DatabaseName) + ";\n" +
                   "ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + "\n" +
                   "DROP CONSTRAINT " + QUOTENAME(fkName) + ";\n" +
                   "GO\n" +
                   "ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + "\n" +
                   "ADD CONSTRAINT " + QUOTENAME(fkName) + "\n" +
                   "FOREIGN KEY (" + QUOTENAME(config.ColumnName) + ")\n" +
                   "REFERENCES " + QUOTENAME(config.RefSchemaName) + "." + QUOTENAME(config.RefTableName) + " (" + QUOTENAME(config.RefColumnName) + ")\n" +
                   "ON DELETE " + config.OnDelete + ";";
        }

        public string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config)
        {
            string fkName = string.IsNullOrWhiteSpace(config.FKName) ? "FK_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "_" + config.RefSchemaName + "_" + config.RefTableName + "_" + config.RefColumnName : config.FKName;

            return $"USE {QUOTENAME(config.DatabaseName)}; ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP CONSTRAINT {QUOTENAME(fkName)}; PRINT N'[BprDevKit] Foreign key [{fkName}] dropped successfully.';";
        }

        public string GenerateGetForeignKeyInfoScript(ForeignKeyOperation.Information config)
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

        public string GenerateGetReferentialActionsScript()
        {
            return "SELECT 'NO ACTION' AS Action UNION SELECT 'CASCADE' AS Action UNION SELECT 'SET NULL' AS Action UNION SELECT 'SET DEFAULT' AS Action ORDER BY Action;";
        }
        #endregion

        #region View Operations
        public string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
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

        public string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
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

        public string GenerateDropViewScript(string databaseName, string schemaName, string viewName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP VIEW {QUOTENAME(schemaName)}.{QUOTENAME(viewName)}; PRINT N'[BprDevKit] View [{viewName}] dropped successfully.';";
        }

        public string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName)
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

        public string GenerateGetViewsScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT v.name AS ViewName, s.name AS SchemaName FROM sys.views v INNER JOIN sys.schemas s ON v.schema_id = s.schema_id ORDER BY s.name, v.name;";
        }

        public string GenerateTestViewScript(string databaseName, string selectStatement)
        {
            return $"USE {QUOTENAME(databaseName)}; SET NOCOUNT ON; {selectStatement};";
        }
        #endregion

        #region Stored Procedure Operations
        public string GenerateCreateProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
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

        public string GenerateAlterProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
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

        public string GenerateDropProcedureScript(string databaseName, string schemaName, string procName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP PROCEDURE {QUOTENAME(schemaName)}.{QUOTENAME(procName)}; PRINT N'[BprDevKit] Stored Procedure [{procName}] dropped successfully.';";
        }

        public string GenerateGetProcedureInfoScript(string databaseName, string schemaName, string procName)
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

        public string GenerateGetProceduresScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT p.name AS ProcedureName, s.name AS SchemaName FROM sys.procedures p INNER JOIN sys.schemas s ON p.schema_id = s.schema_id ORDER BY s.name, p.name;";
        }

        public string GenerateTestProcedureScript(string databaseName, string procName, string body)
        {
            return $"USE {QUOTENAME(databaseName)}; PRINT N'[BprDevKit] Testing Stored Procedure [{procName}]...'; BEGIN {body} END; PRINT N'[BprDevKit] Stored Procedure [{procName}] test completed successfully.';";
        }
        #endregion

        #region Function Operations
        public string GenerateCreateFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
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

        public string GenerateAlterFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
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

        public string GenerateDropFunctionScript(string databaseName, string schemaName, string funcName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP FUNCTION {QUOTENAME(schemaName)}.{QUOTENAME(funcName)}; PRINT N'[BprDevKit] Function [{funcName}] dropped successfully.';";
        }

        public string GenerateGetFunctionInfoScript(string databaseName, string schemaName, string funcName)
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

        public string GenerateGetFunctionsScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT f.name AS FunctionName, s.name AS SchemaName FROM sys.objects f INNER JOIN sys.schemas s ON f.schema_id = s.schema_id WHERE f.type = 'FN' ORDER BY s.name, f.name;";
        }

        public string GenerateTestFunctionScript(string databaseName, string funcName, string body)
        {
            return $"USE {QUOTENAME(databaseName)}; PRINT N'[BprDevKit] Testing Function [{funcName}]...'; DECLARE @Result INT; {body} SELECT @Result; PRINT N'[BprDevKit] Function [{funcName}] test completed successfully.';";
        }
        #endregion

        #region Backup/Restore
        public string GenerateBackupScript(BackupOperation config)
        {
            return $"BACKUP DATABASE {QUOTENAME(config.DatabaseName)} TO DISK = {SQLLiteral(config.BackupPath)} WITH FORMAT, INIT, STATS = 10; PRINT N'[BprDevKit] Database [{config.DatabaseName}] backed up successfully to [{config.BackupPath}].';";
        }

        public string GenerateRestoreScript(RestoreOperation config)
        {
            return $"RESTORE DATABASE {QUOTENAME(config.DatabaseName)} FROM DISK = {SQLLiteral(config.RestorePath)} WITH REPLACE; PRINT N'[BprDevKit] Database [{config.DatabaseName}] restored successfully from [{config.RestorePath}].';";
        }
        #endregion

        #region Utility
        public string GenerateUseDatabaseScript(string databaseName)
        {
            return "USE " + QUOTENAME(databaseName) + ";";
        }

        public string GenerateTransactionBeginScript()
        {
            return "BEGIN TRANSACTION;";
        }

        public string GenerateTransactionCommitScript()
        {
            return "COMMIT TRANSACTION;";
        }

        public string GenerateTransactionRollbackScript()
        {
            return "ROLLBACK TRANSACTION;";
        }

        public string GenerateTryCatchBlock(string tryBlock, string catchBlock)
        {
            return $"TRY\n{tryBlock}\nCATCH\nBEGIN\n    {catchBlock}\n    RETURN;\nEND";
        }
        #endregion

        #region Helper Methods
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
                    ValidateDefaultExpression(column.DefaultValue, column.ColumnName);

                result.Add(column);
            }

            if (result.Count(column => column.IsIdentity) > 1)
                throw new InvalidOperationException("[BprDevKit] A table can have only one identity column.");

            return result;
        }

        private static string BuildColumnDefinition(ColumnDefinition column)
        {
            var type = ColumnDataType.Find(column.DataType)!;
            var nullable = column.IsNullable && !column.IsPrimaryKey && !column.IsIdentity;

            var definition = new StringBuilder()
                .Append(QUOTENAME(column.ColumnName)).Append(' ')
                .Append(FormatColumnDataType(column, type)).Append(' ')
                .Append(nullable ? "NULL" : "NOT NULL");

            if (column.IsIdentity)
                definition.Append($" IDENTITY({column.IdentitySeed},{column.IdentityIncrement})");

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                definition.Append($" DEFAULT ({column.DefaultValue!.Trim()})");

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

                default:
                    return type.Name;
            }
        }

        /// <summary>
        /// A default is an arbitrary T-SQL expression, so it is only accepted when it cannot end the
        /// surrounding DEFAULT (...) clause or start another statement: no ';', no comments and balanced
        /// parentheses (string literals are skipped).
        /// </summary>
        private static void ValidateDefaultExpression(string expression, string columnName)
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
                    throw new InvalidOperationException($"[BprDevKit] The default of column [{columnName}] cannot contain ';'.");

                if (index + 1 < expression.Length)
                {
                    var pair = expression.Substring(index, 2);
                    if (pair == "--" || pair == "/*" || pair == "*/")
                        throw new InvalidOperationException($"[BprDevKit] The default of column [{columnName}] cannot contain comments.");
                }

                if (current == '(') depth++;
                if (current == ')' && --depth < 0)
                    throw new InvalidOperationException($"[BprDevKit] The default of column [{columnName}] has unbalanced parentheses.");
            }

            if (inString || depth != 0)
                throw new InvalidOperationException($"[BprDevKit] The default of column [{columnName}] has an unterminated string or unbalanced parentheses.");
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
