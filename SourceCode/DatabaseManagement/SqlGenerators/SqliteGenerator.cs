using System;
using System.Text;
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

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            return "SELECT 1;";
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            return "SELECT 1;";
        }
        #endregion

        #region Schema Operations
        public override string GenerateGetSchemasScript(string databaseName)
        {
            return "SELECT 'main' AS SchemaName;";
        }

        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            return "SELECT 1;";
        }

        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            return "SELECT 1;";
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

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP TABLE IF EXISTS \"{Escape(config.TableName)}\";";
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
