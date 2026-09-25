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
            var sb = new StringBuilder();
            sb.AppendLine("USE master;")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("CREATE DATABASE " + QUOTENAME(config.DatabaseName))
                .AppendLine("ON PRIMARY")
                .AppendLine("(")
                .AppendLine("    NAME = " + QUOTENAME(config.DatabaseName, "'"))
                .AppendLine("    FILENAME = '" + config.DataFilePath + "'")
                .AppendLine("    SIZE = " + config.InitialSizeMB + "MB")
                .AppendLine("    MAXSIZE = " + (config.MaxSizeMB.HasValue ? config.MaxSizeMB.Value + "MB" : "UNLIMITED"))
                .AppendLine("    FILEGROWTH = " + config.FileGrowthMB + "MB")
                .AppendLine(")")
                .AppendLine("LOG ON")
                .AppendLine("(")
                .AppendLine("    NAME = " + QUOTENAME(config.DatabaseName + "_log", "'"))
                .AppendLine("    FILENAME = '" + (config.LogFilePath ?? config.DataFilePath) + "'")
                .AppendLine("    SIZE = " + config.LogInitialSizeMB + "MB")
                .AppendLine("    MAXSIZE = " + (config.LogMaxSizeMB.HasValue ? config.LogMaxSizeMB.Value + "MB" : "UNLIMITED"))
                .AppendLine("    FILEGROWTH = " + config.LogFileGrowthMB + "MB")
                .AppendLine(")");

            if (!string.IsNullOrWhiteSpace(config.Collation))
                sb.AppendLine("COLLATE " + config.Collation);

            sb.AppendLine("GO")
                .AppendLine();

            // Apply settings
            sb.AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET RECOVERY " + config.RecoveryModel)
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET " + (config.IsReadOnly ? "READ_ONLY" : "READ_WRITE"))
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET AUTO_SHRINK " + (config.AutoShrink ? "ON" : "OFF"))
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET AUTO_CLOSE " + (config.AutoClose ? "ON" : "OFF"))
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET PAGE_VERIFY " + config.PageVerify)
                .AppendLine("GO");

            if (config.CompatibilityLevel.HasValue)
                sb.AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName) + " SET COMPATIBILITY_LEVEL = " + config.CompatibilityLevel.Value)
                    .AppendLine("GO")
                    .AppendLine();

            sb.AppendLine("PRINT N'[BprDevKit] Database [' + " + QUOTENAME(config.DatabaseName) + " + N'] created successfully.'")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateAlterDatabaseScript(DatabaseProcedure.Alter config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE master;")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER DATABASE " + QUOTENAME(config.DatabaseName));

            if (!string.IsNullOrWhiteSpace(config.Collation))
                sb.AppendLine("SET COLLATE " + config.Collation)
                    .AppendLine("GO")
                    .AppendLine();

            if (!string.IsNullOrWhiteSpace(config.RecoveryModel))
                sb.AppendLine("SET RECOVERY " + config.RecoveryModel)
                    .AppendLine("GO")
                    .AppendLine();

            if (config.CompatibilityLevel.HasValue)
                sb.AppendLine("SET COMPATIBILITY_LEVEL = " + config.CompatibilityLevel.Value)
                    .AppendLine("GO")
                    .AppendLine();

            if (config.IsReadOnly.HasValue)
                sb.AppendLine("SET " + (config.IsReadOnly.Value ? "READ_ONLY" : "READ_WRITE"))
                    .AppendLine("GO")
                    .AppendLine();

            if (config.AutoShrink.HasValue)
                sb.AppendLine("SET AUTO_SHRINK " + (config.AutoShrink.Value ? "ON" : "OFF"))
                    .AppendLine("GO")
                    .AppendLine();

            if (config.AutoClose.HasValue)
                sb.AppendLine("SET AUTO_CLOSE " + (config.AutoClose.Value ? "ON" : "OFF"))
                    .AppendLine("GO")
                    .AppendLine();

            if (!string.IsNullOrWhiteSpace(config.PageVerify))
                sb.AppendLine("SET PAGE_VERIFY " + config.PageVerify)
                    .AppendLine("GO")
                    .AppendLine();

            sb.AppendLine("PRINT N'[BprDevKit] Database [' + " + QUOTENAME(config.DatabaseName) + " + N'] altered successfully.'")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateDropDatabaseScript(DatabaseProcedure.Drop config)
        {
            return $"USE master; GO\n\nDROP DATABASE {QUOTENAME(config.DatabaseName)}; GO\n\nPRINT N'[BprDevKit] Database [{config.DatabaseName}] dropped successfully.'; GO";
        }

        public string GenerateGetDatabaseInfoScript(DatabaseProcedure.Information config)
        {
            return $@"USE {QUOTENAME(config.DatabaseName)};
SELECT
    name AS DatabaseName,
    create_date AS CreateDate,
    modify_date AS ModifyDate,
    compatibility_level AS CompatibilityLevel,
    recovery_model_desc AS RecoveryModel,
    is_read_only AS IsReadOnly,
    is_auto_shrink_on AS IsAutoShrink,
    is_auto_close_on AS IsAutoClose,
    page_verify_option_desc AS PageVerify,
    CASE
        WHEN source_database_id = 0 THEN 'System'
        ELSE 'User'
    END AS DatabaseType
FROM sys.databases
WHERE name = '{config.DatabaseName}';";
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

            sb.AppendLine("PRINT N'[BprDevKit] Table [' + " + QUOTENAME(config.SchemaName) + " + N'.' + " + QUOTENAME(config.TableName) + " + N'] created successfully.';")
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
WHERE s.name = '{config.SchemaName}' AND t.name = '{config.TableName}';";
        }

        public string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = '{schemaName}'
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

            sb.AppendLine("PRINT N'[BprDevKit] Column [' + " + QUOTENAME(config.ColumnName) + " + N'] created successfully in table [' + " + QUOTENAME(config.SchemaName) + " + N'.' + " + QUOTENAME(config.TableName) + " + N'].';");

            return sb.ToString();
        }

        public string GenerateAlterColumnScript(ColumnProcedure.Alter config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
                .AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
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

            sb.AppendLine("PRINT N'[BprDevKit] Column [' + " + QUOTENAME(config.ColumnName) + " + N'] altered successfully in table [' + " + QUOTENAME(config.SchemaName) + " + N'.' + " + QUOTENAME(config.TableName) + " + N'].';");

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
WHERE s.name = '{config.SchemaName}' AND t.name = '{config.TableName}' AND c.name = '{config.ColumnName}';";
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
WHERE s.name = '{schemaName}' AND t.name = '{tableName}'
ORDER BY c.column_id;";
        }
        #endregion

        #region Index Operations
        public string GenerateCreateIndexScript(IndexProcedure.Create config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(config.DatabaseName) + ";")
                .AppendLine("CREATE ");

            if (config.IsUnique)
                sb.AppendLine("UNIQUE ");

            sb.AppendLine(config.IndexType + " INDEX ");

            if (string.IsNullOrWhiteSpace(config.IndexName))
                sb.AppendLine("IX_" + config.TableName + "_" + config.Columns.Replace(" ", "").Replace(",", "_").Replace("ASC", "").Replace("DESC", "_desc"));
            else
                sb.AppendLine(config.IndexName);

            sb.AppendLine("ON " + QUOTENAME(config.SchemaName) + "." + QUOTENAME(config.TableName) + " (" + config.Columns + ");");

            if (!string.IsNullOrWhiteSpace(config.IncludeColumns))
                sb.AppendLine("INCLUDE (" + config.IncludeColumns + ");");

            sb.AppendLine("WITH (FILLFACTOR = " + config.FillFactor + ");");

            sb.AppendLine("ON " + QUOTENAME(config.FileGroup) + ";");

            sb.AppendLine("PRINT N'[BprDevKit] Index [' + " + QUOTENAME(config.IndexName ?? "(auto-generated)") + " + N'] created successfully on table [' + " + QUOTENAME(config.SchemaName) + " + N'.' + " + QUOTENAME(config.TableName) + " + N'].';");

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
WHERE s.name = '{config.SchemaName}' AND t.name = '{config.TableName}' AND i.name = '{config.IndexName}'
ORDER BY i.name;";
        }

        public string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName)
        {
            return $@"USE {QUOTENAME(databaseName)};
SELECT i.name AS IndexName
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type = 'U' AND s.name = '{schemaName}' AND o.name = '{tableName}' AND i.name IS NOT NULL AND i.is_primary_key = 0
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
                   "ON DELETE " + config.OnDelete + "\n" +
                   "ON UPDATE " + config.OnUpdate + ";";
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
                   "ON DELETE " + config.OnDelete + "\n" +
                   "ON UPDATE " + config.OnUpdate + ";\n" +
                   "PRINT N'[BprDevKit] Foreign key [' + " + QUOTENAME(fkName) + " + N'] altered successfully.';";
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
WHERE s.name = '{config.SchemaName}' AND t.name = '{config.TableName}' AND c.name = '{config.ColumnName}';";
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
                .AppendLine("CREATE VIEW " + QUOTENAME(schemaName) + "." + QUOTENAME(viewName) + " AS");

            // Add indentation to SELECT statement
            string indentSelect = "    ";
            string[] lines = selectStatement.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentSelect + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] View [' + " + QUOTENAME(viewName) + " + N'] created successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER VIEW " + QUOTENAME(schemaName) + "." + QUOTENAME(viewName) + " AS");

            // Add indentation to SELECT statement
            string indentSelect = "    ";
            string[] lines = selectStatement.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentSelect + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] View [' + " + QUOTENAME(viewName) + " + N'] altered successfully.';")
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
WHERE s.name = '{schemaName}' AND v.name = '{viewName}';";
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
                .AppendLine("CREATE PROCEDURE " + QUOTENAME(schemaName) + "." + QUOTENAME(procName) + " AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Stored Procedure [' + " + QUOTENAME(procName) + " + N'] created successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateAlterProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER PROCEDURE " + QUOTENAME(schemaName) + "." + QUOTENAME(procName) + " AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Stored Procedure [' + " + QUOTENAME(procName) + " + N'] altered successfully.';")
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
WHERE s.name = '{schemaName}' AND p.name = '{procName}';";
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
                .AppendLine("CREATE FUNCTION " + QUOTENAME(schemaName) + "." + QUOTENAME(funcName) + " AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Function [' + " + QUOTENAME(funcName) + " + N'] created successfully.';")
                .AppendLine("GO");

            return sb.ToString();
        }

        public string GenerateAlterFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("USE " + QUOTENAME(databaseName) + ";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("ALTER FUNCTION " + QUOTENAME(schemaName) + "." + QUOTENAME(funcName) + " AS");

            // Add indentation to body
            string indentBody = "    ";
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(indentBody + lines[i]);
            }

            if (withEncryption)
                sb.AppendLine("WITH ENCRYPTION");

            sb.AppendLine(";")
                .AppendLine("GO")
                .AppendLine()
                .AppendLine("PRINT N'[BprDevKit] Function [' + " + QUOTENAME(funcName) + " + N'] altered successfully.';")
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
WHERE s.name = '{schemaName}' AND f.name = '{funcName}' AND f.type = 'FN';";
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
            return $"BACKUP DATABASE {QUOTENAME(config.DatabaseName)} TO DISK = N'{config.BackupPath}' WITH FORMAT, INIT, STATS = 10; PRINT N'[BprDevKit] Database [{config.DatabaseName}] backed up successfully to [{config.BackupPath}].';";
        }

        public string GenerateRestoreScript(RestoreProcedure config)
        {
            return $"RESTORE DATABASE {QUOTENAME(config.DatabaseName)} FROM DISK = N'{config.RestorePath}' WITH REPLACE; PRINT N'[BprDevKit] Database [{config.DatabaseName}] restored successfully from [{config.RestorePath}].';";
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
        private static string QUOTENAME(string name, string quoteChar = "'")
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            return quoteChar + name.Replace(quoteChar, quoteChar + quoteChar) + quoteChar;
        }
        #endregion
    }
}