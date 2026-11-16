using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// SQL generator for SQLite databases.
    /// </summary>
    public class SqliteGenerator : SqlGeneratorBase
    {
        public override string ProviderName => "SQLite";

        #region Database Operations
        public override string GenerateGetDatabasesScript()
        {
            return "SELECT 'main' AS datname;";
        }

        public override string GenerateGetCollationsScript()
        {
            return "SELECT 'BINARY' AS Collation UNION ALL SELECT 'NOCASE' UNION ALL SELECT 'RTRIM';";
        }

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            throw new NotSupportedException("SQLite databases are files and cannot be created with SQL. Create the database by opening its file.");
        }

        public override string GenerateAlterDatabaseScript(DatabaseOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return "-- SQLite databases are stored in files; database-level ALTER options are not supported.";
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            throw new NotSupportedException("SQLite databases are files and cannot be dropped with SQL. Remove the file explicitly after closing connections.");
        }

        public override string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return @"SELECT 
    'main' AS DatabaseName,
    'BINARY' AS Collation,
    'UTF-8' AS CharacterSet,
    'ONLINE' AS State,
    ROUND((page_count * page_size) / 1024.0 / 1024.0, 2) AS DataFileSizeMB,
    0 AS IsReadOnly
FROM pragma_page_count(), pragma_page_size();";
        }
        #endregion

        #region Backup/Restore
        public override string GenerateBackupScript(BackupOperation config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var escaped = (config.BackupPath ?? string.Empty).Replace("'", "''");
            return $"VACUUM INTO '{escaped}';";
        }
        #endregion

        #region Schema Operations
        public override string GenerateGetSchemasScript(string databaseName)
        {
            return "SELECT 'main' AS SchemaName;";
        }

        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            throw new NotSupportedException("SQLite does not support creating SQL schemas.");
        }

        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            throw new NotSupportedException("SQLite does not support dropping SQL schemas.");
        }
        #endregion

        #region Table Operations
        public override string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            return "SELECT name AS table_name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        }

        public override string GenerateGetTableListScript(string databaseName)
        {
            return @"SELECT 
    'main' AS SchemaName,
    name AS TableName,
    0 AS RowCount,
    0 AS ColumnCount,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate,
    '' AS Description
FROM sqlite_master
WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
ORDER BY name;";
        }

        public override string GenerateGetTableInfoScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $@"SELECT 
    'main' AS SchemaName,
    '{Escape(config.TableName)}' AS TableName,
    0 AS RowCount,
    0 AS ColumnCount,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate,
    '' AS Description
FROM sqlite_master
WHERE type = 'table' AND name = '{Escape(config.TableName)}';";
        }

        public override string GenerateGetTableColumnsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $@"SELECT
    cid AS OrdinalPosition,
    name AS ColumnName,
    CASE 
        WHEN instr(type, '(') > 0 THEN substr(type, 1, instr(type, '(') - 1)
        ELSE type
    END AS DataType,
    CASE 
        WHEN instr(type, '(') > 0 AND instr(type, ')') > 0 THEN 
            CAST(substr(type, instr(type, '(') + 1, instr(type, ')') - instr(type, '(') - 1) AS INTEGER)
        ELSE 0
    END AS MaxLength,
    0 AS Precision,
    0 AS Scale,
    CASE WHEN ""notnull"" = 0 THEN 1 ELSE 0 END AS IsNullable,
    CASE WHEN pk = 1 AND upper(type) = 'INTEGER' THEN 1 ELSE 0 END AS IsIdentity,
    1 AS IdentitySeed,
    1 AS IdentityIncrement,
    pk AS IsPrimaryKey,
    '' AS Collation,
    dflt_value AS DefaultValue,
    NULL AS DefaultConstraintName,
    NULL AS ComputedFormula,
    0 AS IsPersisted,
    '' AS Description
FROM pragma_table_info('{Escape(config.TableName)}')
ORDER BY cid;";
        }

        public override string GenerateGetTableIndexesScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $@"SELECT
    name AS IndexName,
    'BTREE' AS IndexType,
    name AS KeyColumns,
    NULL AS IncludeColumns,
    ""unique"" AS IsUnique,
    0 AS IsPrimaryKey,
    ""unique"" AS IsUniqueConstraint,
    0 AS FillFactor,
    0 AS PadIndex,
    1 AS AllowRowLocks,
    1 AS AllowPageLocks,
    NULL AS FilterPredicate,
    'main' AS FileGroup
FROM pragma_index_list('{Escape(config.TableName)}');";
        }

        public override string GenerateGetTableForeignKeysScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $@"SELECT
    'FK_' || '{Escape(config.TableName)}' || '_' || ""to"" || '_' || id AS FKName,
    'main' AS SchemaName,
    '{Escape(config.TableName)}' AS TableName,
    ""from"" AS ColumnName,
    'main' AS RefSchemaName,
    ""table"" AS RefTableName,
    ""to"" AS RefColumnName,
    on_delete AS OnDelete,
    on_update AS OnUpdate,
    0 AS IsNotForReplication,
    1 AS Enabled
FROM pragma_foreign_key_list('{Escape(config.TableName)}');";
        }

        public override string GenerateGetTableCheckConstraintsScript(TableOperation.Information config)
        {
            return "SELECT '' AS SchemaName, '' AS TableName, '' AS ConstraintName, '' AS CheckExpression, 0 AS IsDisabled, 0 AS IsNotForReplication WHERE 1 = 0;";
        }

        public override string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{Escape(config.TableName)}\" (");
            for (int i = 0; i < config.Columns.Count; i++)
            {
                var col = config.Columns[i];
                var comma = (i < config.Columns.Count - 1) ? "," : "";
                sb.AppendLine($"    {BuildSqliteColumnDefinition(col)}{comma}");
            }
            sb.AppendLine(");");
            return sb.ToString();
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP TABLE IF EXISTS \"{Escape(config.TableName)}\";";
        }
        #endregion

        #region Column Operations
        public override string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"ALTER TABLE \"{Escape(config.TableName)}\" ADD COLUMN {BuildSqliteColumnDefinition(config.Column)};";
        }

        public override string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"ALTER TABLE \"{Escape(config.TableName)}\" DROP COLUMN \"{Escape(config.ColumnName)}\";";
        }

        private static string BuildSqliteColumnDefinition(ColumnDefinition col)
        {
            var colName = $"\"{Escape(col.ColumnName)}\"";
            if (col.IsComputed || !string.IsNullOrWhiteSpace(col.ComputedFormula))
            {
                var formula = (col.ComputedFormula ?? "").Trim();
                if (!formula.StartsWith('(') || !formula.EndsWith(')'))
                    formula = $"({formula})";
                var storage = col.IsPersisted ? "STORED" : "VIRTUAL";
                return $"{colName} AS {formula} {storage}";
            }

            var type = (col.DataType ?? "TEXT").ToUpperInvariant();
            var identity = col.IsIdentity && col.IsPrimaryKey ? " PRIMARY KEY AUTOINCREMENT" : (col.IsPrimaryKey ? " PRIMARY KEY" : "");
            var nullability = (col.IsNullable || col.IsPrimaryKey) ? "" : " NOT NULL";
            var defVal = !string.IsNullOrWhiteSpace(col.DefaultValue) && !col.IsIdentity ? $" DEFAULT ({col.DefaultValue.Trim()})" : "";
            return $"{colName} {type}{identity}{nullability}{defVal}";
        }
        #endregion

        #region View Operations
        public override string GenerateGetViewsScript(string databaseName)
        {
            return @"SELECT
    'main' AS SchemaName,
    name AS ViewName,
    COALESCE(sql, '') AS Definition,
    0 AS IsEncrypted,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate
FROM sqlite_master
WHERE type = 'view'
ORDER BY name;";
        }

        public override string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            return $"CREATE VIEW IF NOT EXISTS [{Escape(viewName)}] AS\n{selectStatement.Trim()};";
        }

        public override string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            return $"DROP VIEW IF EXISTS [{Escape(viewName)}];\nCREATE VIEW [{Escape(viewName)}] AS\n{selectStatement.Trim()};";
        }

        public override string GenerateDropViewScript(string databaseName, string schemaName, string viewName)
        {
            return $"DROP VIEW IF EXISTS [{Escape(viewName)}];";
        }

        public override string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName)
        {
            return $@"SELECT
    'main' AS SchemaName,
    name AS ViewName,
    COALESCE(sql, '') AS ViewDefinition,
    0 AS IsEncrypted,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate
FROM sqlite_master
WHERE type = 'view' AND name = '{Escape(viewName)}';";
        }
        #endregion

        #region Index Operations
        public override string GenerateCreateIndexScript(IndexOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var unique = config.IsUnique ? "UNIQUE " : "";
            var cols = string.Join(", ", config.Columns.ConvertAll(c => $"[{Escape(c.ColumnName)}]"));
            return $"CREATE {unique}INDEX IF NOT EXISTS [{Escape(config.IndexName ?? "")}] ON [{Escape(config.TableName)}] ({cols});";
        }

        public override string GenerateDropIndexScript(IndexOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP INDEX IF EXISTS [{Escape(config.IndexName ?? "")}];";
        }
        #endregion

        #region Foreign Key Operations
        public override string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config)
        {
            return "-- SQLite does not support ALTER TABLE ADD CONSTRAINT FOREIGN KEY. Foreign keys must be defined within CREATE TABLE.";
        }

        public override string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config)
        {
            return "-- SQLite does not support ALTER TABLE DROP CONSTRAINT. Table must be recreated.";
        }
        #endregion

        #region Trigger Operations
        public override string GenerateGetTriggersScript(string databaseName)
        {
            return @"SELECT
    'main' AS SchemaName,
    name AS TriggerName,
    'main' AS TableSchema,
    tbl_name AS TableName,
    0 AS IsDisabled,
    0 AS IsInsteadOfTrigger,
    '' AS TriggerEvents,
    0 AS IsEncrypted,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate
FROM sqlite_master
WHERE type = 'trigger'
ORDER BY name;";
        }

        public override string GenerateGetTriggerInfoScript(string databaseName, string schemaName, string triggerName)
        {
            return $@"SELECT
    'main' AS SchemaName,
    name AS TriggerName,
    'main' AS TableSchema,
    tbl_name AS TableName,
    0 AS IsDisabled,
    0 AS IsInsteadOfTrigger,
    '' AS TriggerEvents,
    COALESCE(sql, '') AS TriggerDefinition,
    0 AS IsEncrypted,
    datetime('now') AS CreateDate,
    datetime('now') AS ModifyDate
FROM sqlite_master
WHERE type = 'trigger' AND name = '{Escape(triggerName)}';";
        }

        public override string GenerateCreateTriggerScript(TriggerOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return config.Body.Trim();
        }

        public override string GenerateDropTriggerScript(string databaseName, string schemaName, string triggerName)
        {
            return $"DROP TRIGGER IF EXISTS [{Escape(triggerName)}];";
        }
        #endregion

        #region Routine Operations (Procedures and Functions)
        public override string GenerateGetProceduresScript(string databaseName)
        {
            return "SELECT '' AS SchemaName, '' AS ProcedureName, '' AS Definition, 0 AS IsEncrypted, datetime('now') AS CreateDate, datetime('now') AS ModifyDate WHERE 1 = 0;";
        }

        public override string GenerateGetFunctionsScript(string databaseName)
        {
            return "SELECT '' AS SchemaName, '' AS FunctionName, '' AS FunctionType, '' AS Definition, 0 AS IsEncrypted, datetime('now') AS CreateDate, datetime('now') AS ModifyDate WHERE 1 = 0;";
        }
        #endregion

        #region Transaction & Utilities
        public override string GenerateTransactionBeginScript() => "BEGIN TRANSACTION;";
        public override string GenerateTransactionCommitScript() => "COMMIT;";
        public override string GenerateTransactionRollbackScript() => "ROLLBACK;";
        public override string GenerateUseDatabaseScript(string databaseName) => string.Empty;

        private static string Escape(string val) => (val ?? string.Empty).Replace("'", "''");
        #endregion
    }
}
