using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// SQL generator for PostgreSQL databases.
    /// </summary>
    public class PostgreSqlGenerator : SqlGeneratorBase
    {
        public override string ProviderName => "PostgreSQL";

        #region Database Operations
        public override string GenerateGetDatabasesScript()
        {
            return "SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true ORDER BY datname;";
        }

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"CREATE DATABASE \"{config.DatabaseName}\";";
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP DATABASE IF EXISTS \"{config.DatabaseName}\";";
        }
        #endregion

        #region Schema Operations
        public override string GenerateGetSchemasScript(string databaseName)
        {
            return "SELECT schema_name AS SchemaName FROM information_schema.schemata WHERE schema_name NOT IN ('pg_catalog', 'information_schema') AND schema_name NOT LIKE 'pg_toast%' AND schema_name NOT LIKE 'pg_temp%' ORDER BY schema_name;";
        }

        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"CREATE SCHEMA IF NOT EXISTS \"{config.SchemaName}\";";
        }

        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP SCHEMA IF EXISTS \"{config.SchemaName}\" CASCADE;";
        }
        #endregion

        #region Table Operations
        public override string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            var schema = string.IsNullOrWhiteSpace(schemaName) ? "public" : schemaName;
            return $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Escape(schema)}' AND table_type = 'BASE TABLE' ORDER BY table_name;";
        }

        public override string GenerateGetTableListScript(string databaseName)
        {
            return @"SELECT 
    t.table_schema AS SchemaName,
    t.table_name AS TableName,
    COALESCE(c.reltuples::bigint, 0) AS RowCount,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = t.table_schema AND table_name = t.table_name) AS ColumnCount,
    NOW() AS CreateDate,
    NOW() AS ModifyDate,
    COALESCE(obj_description(c.oid, 'pg_class'), '') AS Description
FROM information_schema.tables t
LEFT JOIN pg_class c ON c.relname = t.table_name AND c.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = t.table_schema)
WHERE t.table_type = 'BASE TABLE'
  AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY t.table_schema, t.table_name;";
        }

        public override string GenerateGetTableInfoScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            return $@"SELECT 
    t.table_schema AS SchemaName,
    t.table_name AS TableName,
    COALESCE(c.reltuples::bigint, 0) AS RowCount,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = t.table_schema AND table_name = t.table_name) AS ColumnCount,
    NOW() AS CreateDate,
    NOW() AS ModifyDate,
    COALESCE(obj_description(c.oid, 'pg_class'), '') AS Description
FROM information_schema.tables t
LEFT JOIN pg_class c ON c.relname = t.table_name AND c.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = t.table_schema)
WHERE t.table_schema = '{Escape(schema)}' AND t.table_name = '{Escape(config.TableName)}';";
        }

        public override string GenerateGetTableColumnsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;

            return $@"SELECT
    cols.ordinal_position AS OrdinalPosition,
    cols.column_name AS ColumnName,
    cols.udt_name AS DataType,
    COALESCE(cols.character_maximum_length, cols.character_octet_length, 0) AS MaxLength,
    COALESCE(cols.numeric_precision, cols.datetime_precision, 0) AS Precision,
    COALESCE(cols.numeric_scale, 0) AS Scale,
    CASE WHEN cols.is_nullable = 'YES' THEN 1 ELSE 0 END AS IsNullable,
    CASE WHEN cols.is_identity = 'YES' OR COALESCE(cols.column_default, '') LIKE 'nextval(%' THEN 1 ELSE 0 END AS IsIdentity,
    1 AS IdentitySeed,
    1 AS IdentityIncrement,
    CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
    COALESCE(cols.collation_name, '') AS Collation,
    cols.column_default AS DefaultValue,
    NULL AS DefaultConstraintName,
    cols.generation_expression AS ComputedFormula,
    CASE WHEN cols.is_generated = 'ALWAYS' THEN 1 ELSE 0 END AS IsPersisted,
    COALESCE(col_desc.description, '') AS Description
FROM information_schema.columns cols
LEFT JOIN (
    SELECT kcu.column_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu
        ON tc.constraint_name = kcu.constraint_name
        AND tc.table_schema = kcu.table_schema
        AND tc.table_name = kcu.table_name
    WHERE tc.constraint_type = 'PRIMARY KEY'
      AND tc.table_schema = '{Escape(schema)}'
      AND tc.table_name = '{Escape(config.TableName)}'
) pk ON cols.column_name = pk.column_name
LEFT JOIN pg_catalog.pg_statio_all_tables st
    ON st.schemaname = cols.table_schema AND st.relname = cols.table_name
LEFT JOIN pg_catalog.pg_description col_desc
    ON col_desc.objoid = st.relid AND col_desc.objsubid = cols.ordinal_position
WHERE cols.table_schema = '{Escape(schema)}' AND cols.table_name = '{Escape(config.TableName)}'
ORDER BY cols.ordinal_position;";
        }

        public override string GenerateGetTableIndexesScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;

            return $@"SELECT
    i.relname AS IndexName,
    am.amname AS IndexType,
    ARRAY_TO_STRING(ARRAY_AGG(a.attname ORDER BY array_position(ix.indkey, a.attnum)), ', ') AS KeyColumns,
    NULL AS IncludeColumns,
    CASE WHEN ix.indisunique THEN 1 ELSE 0 END AS IsUnique,
    CASE WHEN ix.indisprimary THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN ix.indisunique AND NOT ix.indisprimary THEN 1 ELSE 0 END AS IsUniqueConstraint,
    0 AS FillFactor,
    0 AS PadIndex,
    1 AS AllowRowLocks,
    1 AS AllowPageLocks,
    COALESCE(pg_get_expr(ix.indpred, ix.indrelid), '') AS FilterPredicate,
    'pg_default' AS FileGroup
FROM pg_index ix
JOIN pg_class t ON t.oid = ix.indrelid
JOIN pg_class i ON i.oid = ix.indexrelid
JOIN pg_namespace n ON n.oid = t.relnamespace
JOIN pg_am am ON am.oid = i.relam
JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
WHERE n.nspname = '{Escape(schema)}' AND t.relname = '{Escape(config.TableName)}'
GROUP BY i.relname, am.amname, ix.indisunique, ix.indisprimary, ix.indpred, ix.indrelid;";
        }

        public override string GenerateGetTableForeignKeysScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;

            return $@"SELECT
    tc.constraint_name AS FKName,
    tc.table_schema AS SchemaName,
    tc.table_name AS TableName,
    kcu.column_name AS ColumnName,
    ccu.table_schema AS RefSchemaName,
    ccu.table_name AS RefTableName,
    ccu.column_name AS RefColumnName,
    rc.delete_rule AS OnDelete,
    rc.update_rule AS OnUpdate,
    0 AS IsNotForReplication,
    1 AS Enabled
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.referential_constraints rc
    ON tc.constraint_name = rc.constraint_name
    AND tc.table_schema = rc.constraint_schema
JOIN information_schema.constraint_column_usage ccu
    ON rc.unique_constraint_name = ccu.constraint_name
    AND rc.unique_constraint_schema = ccu.constraint_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = '{Escape(schema)}'
  AND tc.table_name = '{Escape(config.TableName)}';";
        }

        public override string GenerateGetTableCheckConstraintsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;

            return $@"SELECT
    tc.table_schema AS SchemaName,
    tc.table_name AS TableName,
    tc.constraint_name AS ConstraintName,
    cc.check_clause AS CheckExpression,
    0 AS IsDisabled,
    0 AS IsNotForReplication
FROM information_schema.table_constraints tc
JOIN information_schema.check_constraints cc
    ON tc.constraint_name = cc.constraint_name
    AND tc.constraint_schema = cc.constraint_schema
WHERE tc.constraint_type = 'CHECK'
  AND tc.table_schema = '{Escape(schema)}'
  AND tc.table_name = '{Escape(config.TableName)}';";
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            return $"DROP TABLE IF EXISTS \"{Escape(schema)}\".\"{Escape(config.TableName)}\" CASCADE;";
        }
        #endregion

        #region View Operations
        public override string GenerateGetViewsScript(string databaseName)
        {
            return @"SELECT
    table_schema AS SchemaName,
    table_name AS ViewName,
    COALESCE(view_definition, '') AS Definition,
    0 AS IsEncrypted,
    NOW() AS CreateDate,
    NOW() AS ModifyDate
FROM information_schema.views
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name;";
        }
        #endregion

        #region Routine Operations (Procedures and Functions)
        public override string GenerateGetProceduresScript(string databaseName)
        {
            return @"SELECT
    r.routine_schema AS SchemaName,
    r.routine_name AS ProcedureName,
    COALESCE(r.routine_definition, '') AS Definition,
    0 AS IsEncrypted,
    NOW() AS CreateDate,
    NOW() AS ModifyDate
FROM information_schema.routines r
WHERE r.routine_type = 'PROCEDURE'
  AND r.routine_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY r.routine_schema, r.routine_name;";
        }

        public override string GenerateGetFunctionsScript(string databaseName)
        {
            return @"SELECT
    r.routine_schema AS SchemaName,
    r.routine_name AS FunctionName,
    CASE WHEN r.data_type = 'record' THEN 'TABLE' ELSE 'SCALAR' END AS FunctionType,
    COALESCE(r.routine_definition, '') AS Definition,
    0 AS IsEncrypted,
    NOW() AS CreateDate,
    NOW() AS ModifyDate
FROM information_schema.routines r
WHERE r.routine_type = 'FUNCTION'
  AND r.routine_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY r.routine_schema, r.routine_name;";
        }
        #endregion

        #region Transaction & Utilities
        public override string GenerateTransactionBeginScript() => "BEGIN;";
        public override string GenerateTransactionCommitScript() => "COMMIT;";
        public override string GenerateTransactionRollbackScript() => "ROLLBACK;";

        private static string Escape(string val) => (val ?? string.Empty).Replace("'", "''");
        #endregion
    }
}
