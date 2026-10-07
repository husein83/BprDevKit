using System;
using System.Collections.Generic;
using System.Text;
using BprDevKit.DatabaseManagement.Models;
using BprDevKit.DatabaseManagement.Services;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Main SQL script generator that dynamically generates SQL scripts based on configuration objects
    /// </summary>
    public class SqlGenerator : ISqlGenerator
    {
        #region Database Operations
        public string GenerateCreateDatabaseScript(DatabaseProcedure.Create config)
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

        public string GenerateAlterDatabaseScript(DatabaseProcedure.Alter config)
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

        public string GenerateDropDatabaseScript(DatabaseProcedure.Drop config)
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

        public string GenerateGetDatabaseInfoScript(DatabaseProcedure.Information config)
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
        public string GenerateCreateSchemaScript(string databaseName, string schemaName)
        {
            return $"USE {QUOTENAME(databaseName)}; CREATE SCHEMA {QUOTENAME(schemaName)}; PRINT N'[BprDevKit] Schema [{schemaName}] created successfully.';";
        }

        public string GenerateDropSchemaScript(string databaseName, string schemaName)
        {
            return $"USE {QUOTENAME(databaseName)}; DROP SCHEMA {QUOTENAME(schemaName)}; PRINT N'[BprDevKit] Schema [{schemaName}] dropped successfully.';";
        }

        public string GenerateGetSchemasScript(string databaseName)
        {
            return $"USE {QUOTENAME(databaseName)}; SELECT name FROM sys.schemas WHERE principal_id = 1 ORDER BY name;";
        }

        /// <summary>
        /// Lists every object that lives under a schema, so the schema can be inspected or safely renamed.
        /// </summary>
        public string GenerateGetSchemaObjectsScript(string databaseName, string schemaName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT
    o.type_desc AS ObjectType,
    o.name AS ObjectName,
    CONVERT(varchar(24), o.create_date, 120) AS CreateDate
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(schemaName)}
ORDER BY o.type_desc, o.name;";
        }

        /// <summary>
        /// Renames a schema by transferring every object to the new name and dropping the old one.
        /// </summary>
        public string GenerateRenameSchemaScript(string databaseName, string oldSchemaName, string newSchemaName)
        {
            if (string.Equals(oldSchemaName, newSchemaName, StringComparison.Ordinal))
                throw new InvalidOperationException("[BprDevKit] New schema name must be different from the current name.");

            return $@"USE {QUOTENAME(databaseName)};
DECLARE @objectName NVARCHAR(520), @sql NVARCHAR(1000);
DECLARE @transfer CURSOR;
SET @transfer = CURSOR FOR
SELECT QUOTENAME(s.name) + '.' + QUOTENAME(o.name)
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(oldSchemaName)};
OPEN @transfer;
FETCH NEXT FROM @transfer INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'ALTER SCHEMA {QUOTENAME(newSchemaName)} TRANSFER ' + @objectName + N';';
    EXEC sp_executesql @sql;
    FETCH NEXT FROM @transfer INTO @objectName;
END
CLOSE @transfer;
DEALLOCATE @transfer;
DROP SCHEMA {QUOTENAME(oldSchemaName)};
PRINT N'[BprDevKit] Schema [{oldSchemaName}] renamed to [{newSchemaName}].';";
        }
        #endregion

        #region Table Operations
        public string GenerateCreateTableScript(TableProcedure.Create config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
                .AppendLine("CREATE TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " (");

            // Add primary key column
            sb.AppendLine("    Id INT PRIMARY KEY IDENTITY(1,1)");

            sb.AppendLine(") ON " + QUOTENAME(config.FileGroup) + ";");

            if (!string.IsNullOrWhiteSpace(config.Description))
            {
                sb.AppendLine("GO;")
                    .AppendLine("EXEC sys.sp_addextendedproperty")
                    .AppendLine("    N'MS_Description',")
                    .AppendLine("    N'" + config.Description + "',")
                    .AppendLine("    N'SCHEMA',")
                    .AppendLine("    N'" + config.SchemaName + "',")
                    .AppendLine("    N'TABLE',")
                    .AppendLine("    N'" + config.TableName + "';");
            }

            sb.AppendLine("PRINT N'[BprDevKit] Operation completed successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateAlterTableScript(TableProcedure.Alter config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; PRINT N'[BprDevKit] Table [{config.SchemaName}.{config.TableName}] alter script generated (requires implementation).';";
        }

        public string GenerateDropTableScript(TableProcedure.Drop config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; DROP TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}; PRINT N'[BprDevKit] Table [{config.SchemaName}.{config.TableName}] dropped successfully.';";
        }

        public string GenerateGetTableInfoScript(TableProcedure.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    t.create_date AS CreateDate,
    t.modify_date AS ModifyDate
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(config.SchemaName)} AND t.name = {SQLLiteral(config.TableName)};";
        }

        public string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = {SQLLiteral(schemaName)}
ORDER BY t.name;";
        }
        #endregion

        #region Column Operations
        public string GenerateCreateColumnScript(ColumnProcedure.Create config)
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

        public string GenerateAlterColumnScript(ColumnProcedure.Alter config)
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

        public string GenerateDropColumnScript(ColumnProcedure.Drop config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP COLUMN {QUOTENAME(config.ColumnName)}; PRINT N'[BprDevKit] Column [{config.ColumnName}] dropped successfully from table [{config.SchemaName}.{config.TableName}].';";
        }

        public string GenerateGetColumnInfoScript(ColumnProcedure.Information config)
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
        public string GenerateCreateIndexScript(IndexProcedure.Create config)
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

        public string GenerateAlterIndexScript(IndexProcedure.Alter config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; PRINT N'[BprDevKit] Index [{config.IndexName}] alter script generated (requires implementation).';";
        }

        public string GenerateDropIndexScript(IndexProcedure.Drop config)
        {
            return $"USE {QUOTENAME(config.DatabaseName)}; DROP INDEX {QUOTENAME(config.IndexName)} ON {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)}; PRINT N'[BprDevKit] Index [{config.IndexName}] dropped successfully.';";
        }

        public string GenerateGetIndexInfoScript(IndexProcedure.Information config)
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
        public string GenerateCreateForeignKeyScript(ForeignKeyProcedure.Create config)
        {
            string fkName = "FK_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "_" + config.RefSchemaName + "_" + config.RefTableName + "_" + config.RefColumnName;

            return "USE " + QUOTENAME(config.DatabaseName) + ";\n" +
                   "ALTER TABLE " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + "\n" +
                   "ADD CONSTRAINT " + QUOTENAME(fkName) + "\n" +
                   "FOREIGN KEY (" + QUOTENAME(config.ColumnName) + ")\n" +
                   "REFERENCES " + QUOTENAME(config.RefSchemaName) + "." + QUOTENAME(config.RefTableName) + " (" + QUOTENAME(config.RefColumnName) + ")\n" +
                   "ON DELETE " + config.OnDelete + ";";
        }

        public string GenerateAlterForeignKeyScript(ForeignKeyProcedure.Alter config)
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

        public string GenerateDropForeignKeyScript(ForeignKeyProcedure.Drop config)
        {
            string fkName = string.IsNullOrWhiteSpace(config.FKName) ? "FK_" + config.SchemaName + "_" + config.TableName + "_" + config.ColumnName + "_" + config.RefSchemaName + "_" + config.RefTableName + "_" + config.RefColumnName : config.FKName;

            return $"USE {QUOTENAME(config.DatabaseName)}; ALTER TABLE {QUOTENAME(config.SchemaName)}.{QUOTENAME(config.TableName)} DROP CONSTRAINT {QUOTENAME(fkName)}; PRINT N'[BprDevKit] Foreign key [{fkName}] dropped successfully.';";
        }

        public string GenerateGetForeignKeyInfoScript(ForeignKeyProcedure.Information config)
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
        public string GenerateBackupScript(BackupProcedure config)
        {
            return $"BACKUP DATABASE {QUOTENAME(config.DatabaseName)} TO DISK = {SQLLiteral(config.BackupPath)} WITH FORMAT, INIT, STATS = 10; PRINT N'[BprDevKit] Database [{config.DatabaseName}] backed up successfully to [{config.BackupPath}].';";
        }

        public string GenerateRestoreScript(RestoreProcedure config)
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

        private static void ValidateCreateConfiguration(DatabaseProcedure.Create config)
        {
            ValidateDatabaseName(config.DatabaseName);

            if (string.IsNullOrWhiteSpace(config.DataFilePath))
                throw new InvalidOperationException("[BprDevKit] DataFilePath cannot be empty.");

            ValidateSizes(config.InitialSizeMB, config.MaxSizeMB, config.FileGrowthMB, config.LogInitialSizeMB, config.LogMaxSizeMB, config.LogFileGrowthMB);
            ValidateDatabaseOptions(config.RecoveryModel, config.PageVerify, config.CompatibilityLevel, config.Collation);

            if (!string.IsNullOrWhiteSpace(config.FileGroup) && config.FileGroup.Length > 128)
                throw new InvalidOperationException("[BprDevKit] FileGroup cannot exceed 128 characters.");
        }

        private static void ValidateAlterConfiguration(DatabaseProcedure.Alter config)
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
