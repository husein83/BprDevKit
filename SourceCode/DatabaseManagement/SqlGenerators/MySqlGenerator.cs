using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// SQL generator for MySQL and MariaDB databases.
    /// </summary>
    public class MySqlGenerator : SqlGeneratorBase
    {
        public override string ProviderName => "MySQL";

        #region Database Operations
        public override string GenerateGetDatabasesScript()
        {
            return "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys') ORDER BY schema_name;";
        }

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"CREATE DATABASE `{config.DatabaseName}`;";
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP DATABASE IF EXISTS `{config.DatabaseName}`;";
        }
        #endregion

        #region Schema Operations
        public override string GenerateGetSchemasScript(string databaseName)
        {
            // In MySQL/MariaDB, schemas and databases are synonymous
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "SELECT schema_name AS SchemaName FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys') ORDER BY schema_name;";
            }
            return $"SELECT schema_name AS SchemaName FROM information_schema.schemata WHERE schema_name = '{Escape(databaseName)}';";
        }

        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"CREATE SCHEMA IF NOT EXISTS `{config.SchemaName}`;";
        }

        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP SCHEMA IF EXISTS `{config.SchemaName}`;";
        }
        #endregion

        #region Table Operations
        public override string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Escape(db)}' AND table_type = 'BASE TABLE' ORDER BY table_name;";
        }

        public override string GenerateGetTableListScript(string databaseName)
        {
            return $@"SELECT 
    table_schema AS SchemaName,
    table_name AS TableName,
    COALESCE(table_rows, 0) AS RowCount,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = t.table_schema AND table_name = t.table_name) AS ColumnCount,
    COALESCE(create_time, NOW()) AS CreateDate,
    COALESCE(update_time, NOW()) AS ModifyDate,
    COALESCE(table_comment, '') AS Description
FROM information_schema.tables t
WHERE table_type = 'BASE TABLE'
  AND table_schema = '{Escape(databaseName)}'
ORDER BY table_name;";
        }

        public override string GenerateGetTableInfoScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $@"SELECT 
    table_schema AS SchemaName,
    table_name AS TableName,
    COALESCE(table_rows, 0) AS RowCount,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = t.table_schema AND table_name = t.table_name) AS ColumnCount,
    COALESCE(create_time, NOW()) AS CreateDate,
    COALESCE(update_time, NOW()) AS ModifyDate,
    COALESCE(table_comment, '') AS Description
FROM information_schema.tables t
WHERE table_schema = '{Escape(db)}' AND table_name = '{Escape(config.TableName)}';";
        }

        public override string GenerateGetTableColumnsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;

            return $@"SELECT
    ordinal_position AS OrdinalPosition,
    column_name AS ColumnName,
    data_type AS DataType,
    COALESCE(character_maximum_length, 0) AS MaxLength,
    COALESCE(numeric_precision, datetime_precision, 0) AS Precision,
    COALESCE(numeric_scale, 0) AS Scale,
    CASE WHEN is_nullable = 'YES' THEN 1 ELSE 0 END AS IsNullable,
    CASE WHEN extra LIKE '%auto_increment%' THEN 1 ELSE 0 END AS IsIdentity,
    1 AS IdentitySeed,
    1 AS IdentityIncrement,
    CASE WHEN column_key = 'PRI' THEN 1 ELSE 0 END AS IsPrimaryKey,
    COALESCE(collation_name, '') AS Collation,
    column_default AS DefaultValue,
    NULL AS DefaultConstraintName,
    generation_expression AS ComputedFormula,
    CASE WHEN extra LIKE '%VIRTUAL%' OR extra LIKE '%STORED%' THEN 1 ELSE 0 END AS IsPersisted,
    COALESCE(column_comment, '') AS Description
FROM information_schema.columns
WHERE table_schema = '{Escape(db)}' AND table_name = '{Escape(config.TableName)}'
ORDER BY ordinal_position;";
        }

        public override string GenerateGetTableIndexesScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;

            return $@"SELECT
    index_name AS IndexName,
    index_type AS IndexType,
    GROUP_CONCAT(column_name ORDER BY seq_in_index SEPARATOR ', ') AS KeyColumns,
    NULL AS IncludeColumns,
    CASE WHEN non_unique = 0 THEN 1 ELSE 0 END AS IsUnique,
    CASE WHEN index_name = 'PRIMARY' THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN non_unique = 0 AND index_name != 'PRIMARY' THEN 1 ELSE 0 END AS IsUniqueConstraint,
    0 AS FillFactor,
    0 AS PadIndex,
    1 AS AllowRowLocks,
    1 AS AllowPageLocks,
    NULL AS FilterPredicate,
    'PRIMARY' AS FileGroup
FROM information_schema.statistics
WHERE table_schema = '{Escape(db)}' AND table_name = '{Escape(config.TableName)}'
GROUP BY index_name, index_type, non_unique;";
        }

        public override string GenerateGetTableForeignKeysScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;

            return $@"SELECT
    tc.constraint_name AS FKName,
    tc.table_schema AS SchemaName,
    tc.table_name AS TableName,
    kcu.column_name AS ColumnName,
    kcu.referenced_table_schema AS RefSchemaName,
    kcu.referenced_table_name AS RefTableName,
    kcu.referenced_column_name AS RefColumnName,
    rc.delete_rule AS OnDelete,
    rc.update_rule AS OnUpdate,
    0 AS IsNotForReplication,
    1 AS Enabled
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
    AND tc.table_name = kcu.table_name
JOIN information_schema.referential_constraints rc
    ON tc.constraint_name = rc.constraint_name
    AND tc.table_schema = rc.constraint_schema
    AND tc.table_name = rc.table_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = '{Escape(db)}'
  AND tc.table_name = '{Escape(config.TableName)}';";
        }

        public override string GenerateGetTableCheckConstraintsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;

            return $@"SELECT
    CONSTRAINT_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    CONSTRAINT_NAME AS ConstraintName,
    CHECK_CLAUSE AS CheckExpression,
    0 AS IsDisabled,
    0 AS IsNotForReplication
FROM information_schema.check_constraints
WHERE CONSTRAINT_SCHEMA = '{Escape(db)}' AND TABLE_NAME = '{Escape(config.TableName)}';";
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"DROP TABLE IF EXISTS `{Escape(db)}`.`{Escape(config.TableName)}`;";
        }
        #endregion

        #region View Operations
        public override string GenerateGetViewsScript(string databaseName)
        {
            return $@"SELECT
    table_schema AS SchemaName,
    table_name AS ViewName,
    COALESCE(view_definition, '') AS Definition,
    0 AS IsEncrypted,
    NOW() AS CreateDate,
    NOW() AS ModifyDate
FROM information_schema.views
WHERE table_schema = '{Escape(databaseName)}'
ORDER BY table_name;";
        }
        #endregion

        #region Routine Operations (Procedures and Functions)
        public override string GenerateGetProceduresScript(string databaseName)
        {
            return $@"SELECT
    routine_schema AS SchemaName,
    routine_name AS ProcedureName,
    COALESCE(routine_definition, '') AS Definition,
    0 AS IsEncrypted,
    created AS CreateDate,
    last_altered AS ModifyDate
FROM information_schema.routines
WHERE routine_type = 'PROCEDURE' AND routine_schema = '{Escape(databaseName)}'
ORDER BY routine_name;";
        }

        public override string GenerateGetFunctionsScript(string databaseName)
        {
            return $@"SELECT
    routine_schema AS SchemaName,
    routine_name AS FunctionName,
    'SCALAR' AS FunctionType,
    COALESCE(routine_definition, '') AS Definition,
    0 AS IsEncrypted,
    created AS CreateDate,
    last_altered AS ModifyDate
FROM information_schema.routines
WHERE routine_type = 'FUNCTION' AND routine_schema = '{Escape(databaseName)}'
ORDER BY routine_name;";
        }
        #endregion

        #region Transaction & Utilities
        public override string GenerateTransactionBeginScript() => "START TRANSACTION;";
        public override string GenerateTransactionCommitScript() => "COMMIT;";
        public override string GenerateTransactionRollbackScript() => "ROLLBACK;";
        public override string GenerateUseDatabaseScript(string databaseName) => $"USE `{Escape(databaseName)}`;";

        protected static string Escape(string val) => (val ?? string.Empty).Replace("'", "''");
        #endregion
    }
}
