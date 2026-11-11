using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Metadata;
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
    con.conname AS FKName,
    source_ns.nspname AS SchemaName,
    source_table.relname AS TableName,
    string_agg(source_col.attname, ', ' ORDER BY pairs.ordinality) AS ColumnName,
    target_ns.nspname AS RefSchemaName,
    target_table.relname AS RefTableName,
    string_agg(target_col.attname, ', ' ORDER BY pairs.ordinality) AS RefColumnName,
    CASE con.confdeltype WHEN 'c' THEN 'CASCADE' WHEN 'n' THEN 'SET NULL' WHEN 'd' THEN 'SET DEFAULT' WHEN 'r' THEN 'RESTRICT' ELSE 'NO ACTION' END AS OnDelete,
    CASE con.confupdtype WHEN 'c' THEN 'CASCADE' WHEN 'n' THEN 'SET NULL' WHEN 'd' THEN 'SET DEFAULT' WHEN 'r' THEN 'RESTRICT' ELSE 'NO ACTION' END AS OnUpdate,
    0 AS IsNotForReplication,
    1 AS Enabled
FROM pg_constraint con
JOIN pg_class source_table ON source_table.oid = con.conrelid
JOIN pg_namespace source_ns ON source_ns.oid = source_table.relnamespace
JOIN pg_class target_table ON target_table.oid = con.confrelid
JOIN pg_namespace target_ns ON target_ns.oid = target_table.relnamespace
JOIN LATERAL unnest(con.conkey, con.confkey) WITH ORDINALITY AS pairs(source_attnum, target_attnum, ordinality) ON TRUE
JOIN pg_attribute source_col ON source_col.attrelid = con.conrelid AND source_col.attnum = pairs.source_attnum
JOIN pg_attribute target_col ON target_col.attrelid = con.confrelid AND target_col.attnum = pairs.target_attnum
WHERE con.contype = 'f'
  AND source_ns.nspname = '{Escape(schema)}'
  AND source_table.relname = '{Escape(config.TableName)}'
GROUP BY con.oid, con.conname, source_ns.nspname, source_table.relname,
    target_ns.nspname, target_table.relname, con.confdeltype, con.confupdtype;";
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

        public override string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{Escape(schema)}\".\"{Escape(config.TableName)}\" (");
            for (int i = 0; i < config.Columns.Count; i++)
            {
                var col = config.Columns[i];
                var comma = (i < config.Columns.Count - 1) ? "," : "";
                sb.AppendLine($"    {BuildPostgreSqlColumnDefinition(col)}{comma}");
            }
            sb.AppendLine(");");

            var pkCols = config.Columns.FindAll(c => c.IsPrimaryKey);
            if (pkCols.Count > 0)
            {
                var pkNames = string.Join(", ", pkCols.ConvertAll(c => $"\"{Escape(c.ColumnName)}\""));
                sb.AppendLine($"ALTER TABLE \"{Escape(schema)}\".\"{Escape(config.TableName)}\" ADD CONSTRAINT \"PK_{Escape(config.TableName)}\" PRIMARY KEY ({pkNames});");
            }

            return sb.ToString();
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            return $"DROP TABLE IF EXISTS \"{Escape(schema)}\".\"{Escape(config.TableName)}\" CASCADE;";
        }
        #endregion

        #region Column Operations
        public override string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            var table = $"\"{Escape(schema)}\".\"{Escape(config.TableName)}\"";
            return $"ALTER TABLE {table} ADD COLUMN {BuildPostgreSqlColumnDefinition(config.Column)};";
        }

        public override string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            return $"ALTER TABLE \"{Escape(schema)}\".\"{Escape(config.TableName)}\" DROP COLUMN IF EXISTS \"{Escape(config.ColumnName)}\" CASCADE;";
        }

        public override string GenerateAlterColumnScript(ColumnOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = string.IsNullOrWhiteSpace(config.SchemaName) ? "public" : config.SchemaName;
            var table = $"\"{Escape(schema)}\".\"{Escape(config.TableName)}\"";
            var sb = new StringBuilder();

            if (config.IsRename)
            {
                sb.AppendLine($"ALTER TABLE {table} RENAME COLUMN \"{Escape(config.ColumnName)}\" TO \"{Escape(config.NewColumnName!)}\";");
            }

            var currentCol = config.IsRename ? config.NewColumnName! : config.ColumnName;

            if (config.ChangesDefinition && config.Definition != null)
            {
                if (config.Definition.IsComputed || !string.IsNullOrWhiteSpace(config.Definition.ComputedFormula))
                {
                    sb.AppendLine($"ALTER TABLE {table} DROP COLUMN IF EXISTS \"{Escape(currentCol)}\";");
                    sb.AppendLine($"ALTER TABLE {table} ADD COLUMN {BuildPostgreSqlColumnDefinition(config.Definition)};");
                }
                else
                {
                    var newType = MapToPostgreSqlType(config.Definition);
                    sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN \"{Escape(currentCol)}\" TYPE {newType};");
                    if (config.Definition.IsNullable)
                        sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN \"{Escape(currentCol)}\" DROP NOT NULL;");
                    else
                        sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN \"{Escape(currentCol)}\" SET NOT NULL;");
                }
            }

            if (config.ChangesDefault)
            {
                if (string.IsNullOrWhiteSpace(config.DefaultValue))
                    sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN \"{Escape(currentCol)}\" DROP DEFAULT;");
                else
                    sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN \"{Escape(currentCol)}\" SET DEFAULT {config.DefaultValue.Trim()};");
            }

            return sb.ToString();
        }

        private static string MapToPostgreSqlType(ColumnDefinition col)
        {
            var dt = (col.DataType ?? "varchar").ToLowerInvariant();
            return dt switch
            {
                "int" => "integer",
                "bigint" => "bigint",
                "smallint" => "smallint",
                "tinyint" => "smallint",
                "bit" => "boolean",
                "boolean" => "boolean",
                "decimal" => $"numeric({col.Precision ?? 18},{col.Scale ?? 0})",
                "numeric" => $"numeric({col.Precision ?? 18},{col.Scale ?? 0})",
                "money" or "smallmoney" => "numeric(19,4)",
                "float" => "double precision",
                "real" => "real",
                "datetime" or "datetime2" => "timestamp",
                "datetimeoffset" => "timestamp with time zone",
                "date" => "date",
                "time" => "time",
                "char" or "nchar" => $"char({(col.Length > 0 ? col.Length : 1)})",
                "varchar" or "nvarchar" => col.Length == -1 ? "text" : $"varchar({(col.Length > 0 ? col.Length : 255)})",
                "text" or "ntext" => "text",
                "binary" or "varbinary" or "image" => "bytea",
                "uniqueidentifier" or "uuid" => "uuid",
                "json" => "jsonb",
                "xml" => "xml",
                _ => dt
            };
        }

        private static string BuildPostgreSqlColumnDefinition(ColumnDefinition col)
        {
            var colName = $"\"{Escape(col.ColumnName)}\"";
            if (col.IsComputed || !string.IsNullOrWhiteSpace(col.ComputedFormula))
            {
                var formula = (col.ComputedFormula ?? "").Trim();
                if (!formula.StartsWith('(') || !formula.EndsWith(')'))
                    formula = $"({formula})";
                var type = MapToPostgreSqlType(col);
                return $"{colName} {type} GENERATED ALWAYS AS {formula} STORED";
            }

            var typeStr = MapToPostgreSqlType(col);
            var identity = col.IsIdentity ? " GENERATED ALWAYS AS IDENTITY" : "";
            var nullability = (col.IsNullable && !col.IsPrimaryKey && !col.IsIdentity) ? "" : " NOT NULL";
            var defVal = !string.IsNullOrWhiteSpace(col.DefaultValue) && !col.IsIdentity ? $" DEFAULT {col.DefaultValue.Trim()}" : "";
            return $"{colName} {typeStr}{identity}{nullability}{defVal}";
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
