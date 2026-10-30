using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// Comprehensive SQL generator for Oracle Database.
    /// </summary>
    public class OracleGenerator : SqlGeneratorBase
    {
        public override string ProviderName => "Oracle";

        #region Database Operations
        public override string GenerateGetDatabasesScript()
        {
            return "SELECT SYS_CONTEXT('USERENV', 'DB_NAME') AS DatabaseName FROM dual;";
        }

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            // In Oracle multitenant, create pluggable database
            return $"CREATE PLUGGABLE DATABASE \"{config.DatabaseName}\" ADMIN USER pdb_admin IDENTIFIED BY \"Password123!\";";
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP PLUGGABLE DATABASE \"{config.DatabaseName}\" INCLUDING DATAFILES;";
        }
        #endregion

        #region Schema Operations
        public override string GenerateGetSchemasScript(string databaseName)
        {
            return @"SELECT username AS SchemaName 
FROM all_users 
WHERE username NOT IN (
    'SYS', 'SYSTEM', 'OUTLN', 'XS$NULL', 'ANONYMOUS', 'APEX_PUBLIC_USER', 
    'FLOWS_FILES', 'MDSYS', 'CTXSYS', 'XDB', 'WMSYS', 'LBACSYS', 'DVSYS', 
    'DBSNMP', 'APPQOSSYS', 'GSMADMIN_INTERNAL', 'AUDSYS', 'GSMUSER', 'DIP', 
    'REMOTE_SCHEDULER_AGENT', 'OJVMSYS', 'GSMCATUSER', 'MDDATA', 'SYSBACKUP', 
    'SYSDG', 'SYSKM', 'SYSRAC'
) 
ORDER BY username;";
        }

        public override string GenerateCreateSchemaScript(SchemaOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"CREATE USER \"{config.SchemaName.ToUpperInvariant()}\" IDENTIFIED BY \"SchemaPass123!\" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;";
        }

        public override string GenerateDropSchemaScript(SchemaOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP USER \"{config.SchemaName.ToUpperInvariant()}\" CASCADE;";
        }
        #endregion

        #region Table Operations
        public override string GenerateGetTablesScript(string databaseName, string schemaName)
        {
            var schema = string.IsNullOrWhiteSpace(schemaName) ? "SYSTEM" : schemaName.ToUpperInvariant();
            return $"SELECT table_name FROM all_tables WHERE owner = '{Escape(schema)}' AND secondary = 'N' ORDER BY table_name;";
        }

        public override string GenerateGetTableListScript(string databaseName)
        {
            return @"SELECT 
    t.owner AS SchemaName,
    t.table_name AS TableName,
    NVL(t.num_rows, 0) AS RowCount,
    (SELECT COUNT(*) FROM all_tab_columns c WHERE c.owner = t.owner AND c.table_name = t.table_name) AS ColumnCount,
    SYSDATE AS CreateDate,
    SYSDATE AS ModifyDate,
    NVL(cm.comments, '') AS Description
FROM all_tables t
LEFT JOIN all_tab_comments cm ON cm.owner = t.owner AND cm.table_name = t.table_name
WHERE t.owner NOT IN (
    'SYS', 'SYSTEM', 'OUTLN', 'XS$NULL', 'ANONYMOUS', 'APEX_PUBLIC_USER', 
    'FLOWS_FILES', 'MDSYS', 'CTXSYS', 'XDB', 'WMSYS', 'LBACSYS', 'DVSYS', 
    'DBSNMP', 'APPQOSSYS', 'GSMADMIN_INTERNAL', 'AUDSYS', 'GSMUSER', 'DIP'
)
  AND t.secondary = 'N'
ORDER BY t.owner, t.table_name;";
        }

        public override string GenerateGetTableColumnsScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = Escape(config.SchemaName ?? "").ToUpperInvariant();
            var table = Escape(config.TableName).ToUpperInvariant();

            return $@"SELECT 
    c.column_name AS ColumnName,
    c.data_type AS DataType,
    CASE WHEN c.nullable = 'Y' THEN 1 ELSE 0 END AS IsNullable,
    c.column_id AS ColumnOrder,
    c.data_default AS DefaultValue,
    c.data_length AS MaxLength,
    c.data_precision AS Precision,
    c.data_scale AS Scale,
    CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN c.identity_column = 'YES' THEN 1 ELSE 0 END AS IsIdentity,
    NVL(comm.comments, '') AS Description
FROM all_tab_columns c
LEFT JOIN (
    SELECT cc.owner, cc.table_name, cc.column_name
    FROM all_constraints con
    JOIN all_cons_columns cc ON con.owner = cc.owner AND con.constraint_name = cc.constraint_name
    WHERE con.constraint_type = 'P'
) pk ON pk.owner = c.owner AND pk.table_name = c.table_name AND pk.column_name = c.column_name
LEFT JOIN all_col_comments comm ON comm.owner = c.owner AND comm.table_name = c.table_name AND comm.column_name = c.column_name
WHERE c.owner = '{schema}'
  AND c.table_name = '{table}'
ORDER BY c.column_id;";
        }

        public override string GenerateGetTableIndexesScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = Escape(config.SchemaName ?? "").ToUpperInvariant();
            var table = Escape(config.TableName).ToUpperInvariant();

            return $@"SELECT 
    i.index_name AS IndexName,
    ic.column_name AS ColumnName,
    ic.column_position AS KeyOrdinal,
    CASE WHEN i.uniqueness = 'UNIQUE' THEN 1 ELSE 0 END AS IsUnique,
    CASE WHEN con.constraint_type = 'P' THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN ic.descend = 'DESC' THEN 1 ELSE 0 END AS IsDescending
FROM all_indexes i
JOIN all_ind_columns ic ON i.owner = ic.index_owner AND i.index_name = ic.index_name
LEFT JOIN all_constraints con ON con.owner = i.owner AND con.constraint_name = i.index_name
WHERE i.table_owner = '{schema}'
  AND i.table_name = '{table}'
ORDER BY i.index_name, ic.column_position;";
        }

        public override string GenerateGetTableForeignKeysScript(TableOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = Escape(config.SchemaName ?? "").ToUpperInvariant();
            var table = Escape(config.TableName).ToUpperInvariant();

            return $@"SELECT 
    con.constraint_name AS ForeignKeyName,
    c.column_name AS ColumnName,
    ref_con.owner AS RefSchemaName,
    ref_con.table_name AS RefTableName,
    ref_c.column_name AS RefColumnName,
    CASE WHEN con.delete_rule = 'CASCADE' THEN 'CASCADE' ELSE 'NO ACTION' END AS DeleteAction,
    'NO ACTION' AS UpdateAction
FROM all_constraints con
JOIN all_cons_columns c ON con.owner = c.owner AND con.constraint_name = c.constraint_name
JOIN all_constraints ref_con ON con.r_owner = ref_con.owner AND con.r_constraint_name = ref_con.constraint_name
JOIN all_cons_columns ref_c ON ref_con.owner = ref_c.owner AND ref_con.constraint_name = ref_c.constraint_name AND c.position = ref_c.position
WHERE con.constraint_type = 'R'
  AND con.owner = '{schema}'
  AND con.table_name = '{table}'
ORDER BY con.constraint_name, c.position;";
        }

        public override string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var sb = new StringBuilder();
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = config.TableName.ToUpperInvariant();

            sb.AppendLine($"CREATE TABLE \"{schema}\".\"{table}\" (");
            for (int i = 0; i < config.Columns.Count; i++)
            {
                var col = config.Columns[i];
                var nullability = col.IsNullable ? "" : " NOT NULL";
                var identity = col.IsIdentity ? " GENERATED BY DEFAULT AS IDENTITY" : "";
                var comma = (i < config.Columns.Count - 1) ? "," : "";
                sb.AppendLine($"    \"{col.ColumnName.ToUpperInvariant()}\" {MapToOracleType(col)}{identity}{nullability}{comma}");
            }
            sb.AppendLine(");");

            // Primary Key
            var pkCols = config.Columns.FindAll(c => c.IsPrimaryKey);
            if (pkCols.Count > 0)
            {
                var pkNames = string.Join(", ", pkCols.ConvertAll(c => $"\"{c.ColumnName.ToUpperInvariant()}\""));
                sb.AppendLine($"ALTER TABLE \"{schema}\".\"{table}\" ADD CONSTRAINT \"PK_{table}\" PRIMARY KEY ({pkNames});");
            }

            return sb.ToString();
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = config.TableName.ToUpperInvariant();
            return $"DROP TABLE \"{schema}\".\"{table}\" CASCADE CONSTRAINTS;";
        }
        #endregion

        #region Column Operations
        public override string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = config.TableName.ToUpperInvariant();
            var col = config.Column.ColumnName.ToUpperInvariant();
            var nullability = config.Column.IsNullable ? "" : " NOT NULL";
            return $"ALTER TABLE \"{schema}\".\"{table}\" ADD \"{col}\" {MapToOracleType(config.Column)}{nullability};";
        }

        public override string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = config.TableName.ToUpperInvariant();
            var col = config.ColumnName.ToUpperInvariant();
            return $"ALTER TABLE \"{schema}\".\"{table}\" DROP COLUMN \"{col}\";";
        }

        public override string GenerateAlterColumnScript(ColumnOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = config.TableName.ToUpperInvariant();
            var col = config.ColumnName.ToUpperInvariant();
            if (config.Definition == null) return string.Empty;
            var nullability = config.Definition.IsNullable ? "NULL" : "NOT NULL";
            return $"ALTER TABLE \"{schema}\".\"{table}\" MODIFY \"{col}\" {MapToOracleType(config.Definition)} {nullability};";
        }
        #endregion

        #region Index Operations
        public override string GenerateCreateIndexScript(IndexOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var table = (config.TableName ?? "").ToUpperInvariant();
            var indexName = (config.IndexName ?? "").ToUpperInvariant();
            var unique = config.IsUnique ? "UNIQUE " : "";
            var cols = string.Join(", ", config.Columns.ConvertAll(c => $"\"{c.ColumnName.ToUpperInvariant()}\""));
            return $"CREATE {unique}INDEX \"{schema}\".\"{indexName}\" ON \"{schema}\".\"{table}\" ({cols});";
        }

        public override string GenerateDropIndexScript(IndexOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var schema = (config.SchemaName ?? "").ToUpperInvariant();
            var indexName = (config.IndexName ?? "").ToUpperInvariant();
            return $"DROP INDEX \"{schema}\".\"{indexName}\";";
        }
        #endregion

        #region Views, Stored Procedures & Functions
        public override string GenerateGetViewsScript(string databaseName)
        {
            return @"SELECT 
    v.owner AS SchemaName,
    v.view_name AS ViewName,
    NVL(c.comments, '') AS Description
FROM all_views v
LEFT JOIN all_tab_comments c ON c.owner = v.owner AND c.table_name = v.view_name
WHERE v.owner NOT IN (
    'SYS', 'SYSTEM', 'OUTLN', 'XS$NULL', 'ANONYMOUS', 'APEX_PUBLIC_USER', 
    'FLOWS_FILES', 'MDSYS', 'CTXSYS', 'XDB', 'WMSYS', 'LBACSYS', 'DVSYS', 
    'DBSNMP', 'APPQOSSYS', 'GSMADMIN_INTERNAL', 'AUDSYS', 'GSMUSER', 'DIP'
)
ORDER BY v.owner, v.view_name;";
        }

        public override string GenerateGetProceduresScript(string databaseName)
        {
            return @"SELECT 
    owner AS SchemaName,
    object_name AS ProcedureName,
    created AS CreateDate,
    last_ddl_time AS ModifyDate,
    0 AS IsEncrypted
FROM all_objects
WHERE object_type = 'PROCEDURE'
  AND owner NOT IN (
    'SYS', 'SYSTEM', 'OUTLN', 'XS$NULL', 'ANONYMOUS', 'APEX_PUBLIC_USER', 
    'FLOWS_FILES', 'MDSYS', 'CTXSYS', 'XDB', 'WMSYS', 'LBACSYS', 'DVSYS', 
    'DBSNMP', 'APPQOSSYS', 'GSMADMIN_INTERNAL', 'AUDSYS', 'GSMUSER', 'DIP'
)
ORDER BY owner, object_name;";
        }

        public override string GenerateGetFunctionsScript(string databaseName)
        {
            return @"SELECT 
    owner AS SchemaName,
    object_name AS FunctionName,
    created AS CreateDate,
    last_ddl_time AS ModifyDate,
    0 AS IsEncrypted,
    'FN' AS FunctionType
FROM all_objects
WHERE object_type = 'FUNCTION'
  AND owner NOT IN (
    'SYS', 'SYSTEM', 'OUTLN', 'XS$NULL', 'ANONYMOUS', 'APEX_PUBLIC_USER', 
    'FLOWS_FILES', 'MDSYS', 'CTXSYS', 'XDB', 'WMSYS', 'LBACSYS', 'DVSYS', 
    'DBSNMP', 'APPQOSSYS', 'GSMADMIN_INTERNAL', 'AUDSYS', 'GSMUSER', 'DIP'
)
ORDER BY owner, object_name;";
        }

        public override string GenerateDropProcedureScript(string databaseName, string schemaName, string procedureName)
        {
            return $"DROP PROCEDURE \"{schemaName.ToUpperInvariant()}\".\"{procedureName.ToUpperInvariant()}\";";
        }

        public override string GenerateDropFunctionScript(string databaseName, string schemaName, string functionName)
        {
            return $"DROP FUNCTION \"{schemaName.ToUpperInvariant()}\".\"{functionName.ToUpperInvariant()}\";";
        }
        #endregion

        #region Helpers
        private static string Escape(string input) => (input ?? "").Replace("'", "''");

        private static string MapToOracleType(ColumnDefinition col)
        {
            var type = (col.DataType ?? "varchar2").ToLowerInvariant();
            return type switch
            {
                "int" or "integer" => "NUMBER(10)",
                "bigint" => "NUMBER(19)",
                "smallint" => "NUMBER(5)",
                "tinyint" => "NUMBER(3)",
                "bit" or "boolean" => "NUMBER(1)",
                "decimal" or "numeric" => $"NUMBER({(col.Precision > 0 ? col.Precision : 18)}, {(col.Scale > 0 ? col.Scale : 2)})",
                "float" or "real" => "BINARY_FLOAT",
                "double" => "BINARY_DOUBLE",
                "varchar" or "varchar2" or "nvarchar" or "nvarchar2" => $"VARCHAR2({(col.Length > 0 ? col.Length : 255)})",
                "char" or "nchar" => $"CHAR({(col.Length > 0 ? col.Length : 1)})",
                "text" or "ntext" => "CLOB",
                "blob" or "image" or "varbinary" => "BLOB",
                "date" => "DATE",
                "datetime" or "datetime2" or "timestamp" => "TIMESTAMP",
                _ => col.DataType?.ToUpperInvariant() ?? "VARCHAR2(255)"
            };
        }
        #endregion
    }
}
