using System;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Metadata;
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

        public override string GenerateGetCollationsScript()
        {
            return "SELECT collation_name FROM information_schema.collations ORDER BY collation_name;";
        }

        public override string GenerateCreateDatabaseScript(DatabaseOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var collationClause = !string.IsNullOrWhiteSpace(config.Collation) ? $" COLLATE {config.Collation}" : "";
            return $"CREATE DATABASE `{config.DatabaseName}`{collationClause};";
        }

        public override string GenerateAlterDatabaseScript(DatabaseOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var sb = new StringBuilder();
            var db = Escape(config.DatabaseName);

            if (!string.IsNullOrWhiteSpace(config.Collation))
            {
                sb.AppendLine($"ALTER DATABASE `{db}` COLLATE {config.Collation};");
            }

            if (sb.Length == 0)
            {
                sb.AppendLine($"-- No alterations applied for database `{db}`");
            }

            return sb.ToString();
        }

        public override string GenerateDropDatabaseScript(DatabaseOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return $"DROP DATABASE IF EXISTS `{config.DatabaseName}`;";
        }

        public override string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = Escape(config.DatabaseName);
            return $@"SELECT 
    s.schema_name AS DatabaseName,
    s.default_character_set_name AS CharacterSet,
    s.default_collation_name AS Collation,
    'ONLINE' AS `State`,
    COALESCE(ROUND((SELECT SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = s.schema_name) / 1024 / 1024, 2), 0) AS DataFileSizeMB,
    (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = s.schema_name) AS TableCount,
    (SELECT MIN(create_time) FROM information_schema.tables WHERE table_schema = s.schema_name) AS CreateDate,
    @@datadir AS DataFilePath,
    CASE WHEN @@read_only = 1 THEN 1 ELSE 0 END AS IsReadOnly
FROM information_schema.schemata s
WHERE s.schema_name = '{db}';";
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
    COALESCE(numeric_precision, datetime_precision, 0) AS `Precision`,
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

        public override string GenerateCreateTableScript(TableOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE `{Escape(db)}`.`{Escape(config.TableName)}` (");
            for (int i = 0; i < config.Columns.Count; i++)
            {
                var col = config.Columns[i];
                var comma = (i < config.Columns.Count - 1 || config.Columns.Exists(c => c.IsPrimaryKey)) ? "," : "";
                sb.AppendLine($"    {BuildMySqlColumnDefinition(col)}{comma}");
            }

            var pkCols = config.Columns.FindAll(c => c.IsPrimaryKey);
            if (pkCols.Count > 0)
            {
                var pkNames = string.Join(", ", pkCols.ConvertAll(c => $"`{Escape(c.ColumnName)}`"));
                sb.AppendLine($"    PRIMARY KEY ({pkNames})");
            }
            sb.AppendLine(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            return sb.ToString();
        }

        public override string GenerateDropTableScript(TableOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"DROP TABLE IF EXISTS `{Escape(db)}`.`{Escape(config.TableName)}`;";
        }
        #endregion

        #region Column Operations
        public override string GenerateCreateColumnScript(ColumnOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` ADD COLUMN {BuildMySqlColumnDefinition(config.Column)};";
        }

        public override string GenerateDropColumnScript(ColumnOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` DROP COLUMN `{Escape(config.ColumnName)}`;";
        }

        public override string GenerateAlterColumnScript(ColumnOperation.Alter config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            var table = $"`{Escape(db)}`.`{Escape(config.TableName)}`";
            var sb = new StringBuilder();

            if (config.IsRename)
            {
                sb.AppendLine($"ALTER TABLE {table} RENAME COLUMN `{Escape(config.ColumnName)}` TO `{Escape(config.NewColumnName!)}`;");
            }

            var currentCol = config.IsRename ? config.NewColumnName! : config.ColumnName;

            if (config.ChangesDefinition && config.Definition != null)
            {
                config.Definition.ColumnName = currentCol;
                sb.AppendLine($"ALTER TABLE {table} MODIFY COLUMN {BuildMySqlColumnDefinition(config.Definition)};");
            }
            else if (config.ChangesDefault)
            {
                if (string.IsNullOrWhiteSpace(config.DefaultValue))
                    sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN `{Escape(currentCol)}` DROP DEFAULT;");
                else
                    sb.AppendLine($"ALTER TABLE {table} ALTER COLUMN `{Escape(currentCol)}` SET DEFAULT {config.DefaultValue.Trim()};");
            }

            return sb.ToString();
        }

        private static string MapToMySqlType(ColumnDefinition col)
        {
            var dt = (col.DataType ?? "varchar").ToLowerInvariant();
            return dt switch
            {
                "int" => "INT",
                "bigint" => "BIGINT",
                "smallint" => "SMALLINT",
                "tinyint" => "TINYINT",
                "bit" => "BIT",
                "boolean" => "TINYINT(1)",
                "decimal" => $"DECIMAL({col.Precision ?? 18},{col.Scale ?? 0})",
                "numeric" => $"DECIMAL({col.Precision ?? 18},{col.Scale ?? 0})",
                "float" => "DOUBLE",
                "real" => "FLOAT",
                "datetime" or "datetime2" => "DATETIME",
                "datetimeoffset" => "TIMESTAMP",
                "date" => "DATE",
                "time" => "TIME",
                "char" or "nchar" => $"CHAR({(col.Length > 0 ? col.Length : 1)})",
                "varchar" or "nvarchar" => col.Length == -1 ? "LONGTEXT" : $"VARCHAR({(col.Length > 0 ? col.Length : 255)})",
                "text" or "ntext" => "LONGTEXT",
                "binary" or "varbinary" or "image" => "LONGBLOB",
                "uniqueidentifier" or "uuid" => "CHAR(36)",
                "json" => "JSON",
                _ => dt.ToUpperInvariant()
            };
        }

        private static string BuildMySqlColumnDefinition(ColumnDefinition col)
        {
            var colName = $"`{Escape(col.ColumnName)}`";
            if (col.IsComputed || !string.IsNullOrWhiteSpace(col.ComputedFormula))
            {
                var formula = (col.ComputedFormula ?? "").Trim();
                if (!formula.StartsWith('(') || !formula.EndsWith(')'))
                    formula = $"({formula})";
                var type = MapToMySqlType(col);
                var storage = col.IsPersisted ? "STORED" : "VIRTUAL";
                return $"{colName} {type} AS {formula} {storage}";
            }

            var typeStr = MapToMySqlType(col);
            var identity = col.IsIdentity ? " AUTO_INCREMENT" : "";
            var nullability = (col.IsNullable && !col.IsPrimaryKey && !col.IsIdentity) ? " NULL" : " NOT NULL";
            var defVal = !string.IsNullOrWhiteSpace(col.DefaultValue) && !col.IsIdentity ? $" DEFAULT {col.DefaultValue.Trim()}" : "";
            return $"{colName} {typeStr}{identity}{nullability}{defVal}";
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

        public override string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"CREATE OR REPLACE VIEW `{Escape(db)}`.`{Escape(viewName)}` AS\n{selectStatement.Trim()};";
        }

        public override string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
            => GenerateCreateViewScript(databaseName, schemaName, viewName, selectStatement, withEncryption);

        public override string GenerateDropViewScript(string databaseName, string schemaName, string viewName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"DROP VIEW IF EXISTS `{Escape(db)}`.`{Escape(viewName)}`;";
        }

        public override string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $@"SELECT
    table_schema AS SchemaName,
    table_name AS ViewName,
    COALESCE(view_definition, '') AS ViewDefinition,
    0 AS IsEncrypted,
    NOW() AS CreateDate,
    NOW() AS ModifyDate
FROM information_schema.views
WHERE table_schema = '{Escape(db)}' AND table_name = '{Escape(viewName)}';";
        }
        #endregion

        #region Foreign Key Operations
        public override string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            var refDb = !string.IsNullOrWhiteSpace(config.RefSchemaName) ? config.RefSchemaName : db;
            var fkName = !string.IsNullOrWhiteSpace(config.FKName) ? config.FKName : $"FK_{config.TableName}_{config.RefTableName}";
            var cols = string.Join(", ", config.Columns.ConvertAll(c => $"`{Escape(c.ColumnName)}`"));
            var refCols = string.Join(", ", config.Columns.ConvertAll(c => $"`{Escape(c.RefColumnName)}`"));
            var onDelete = string.IsNullOrWhiteSpace(config.OnDelete) ? "NO ACTION" : config.OnDelete;
            var onUpdate = string.IsNullOrWhiteSpace(config.OnUpdate) ? "NO ACTION" : config.OnUpdate;

            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` ADD CONSTRAINT `{Escape(fkName)}` FOREIGN KEY ({cols}) REFERENCES `{Escape(refDb)}`.`{Escape(config.RefTableName)}` ({refCols}) ON DELETE {onDelete} ON UPDATE {onUpdate};";
        }

        public override string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` DROP FOREIGN KEY `{Escape(config.FKName)}`;";
        }
        #endregion

        #region Index Operations
        public override string GenerateCreateIndexScript(IndexOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            var unique = config.IsUnique ? "UNIQUE " : "";
            var cols = string.Join(", ", config.Columns.ConvertAll(c => $"`{Escape(c.ColumnName)}`"));
            return $"CREATE {unique}INDEX `{Escape(config.IndexName ?? "")}` ON `{Escape(db)}`.`{Escape(config.TableName)}` ({cols});";
        }

        public override string GenerateDropIndexScript(IndexOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"DROP INDEX `{Escape(config.IndexName ?? "")}` ON `{Escape(db)}`.`{Escape(config.TableName)}`;";
        }
        #endregion

        #region Check Constraint Operations
        public override string GenerateCreateCheckConstraintScript(CheckConstraintOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            var expr = config.CheckExpression.Trim();
            if (!expr.StartsWith('(') || !expr.EndsWith(')')) expr = $"({expr})";
            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` ADD CONSTRAINT `{Escape(config.ConstraintName ?? "")}` CHECK {expr};";
        }

        public override string GenerateDropCheckConstraintScript(CheckConstraintOperation.Drop config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var db = !string.IsNullOrWhiteSpace(config.SchemaName) ? config.SchemaName : config.DatabaseName;
            return $"ALTER TABLE `{Escape(db)}`.`{Escape(config.TableName)}` DROP CHECK `{Escape(config.ConstraintName ?? "")}`;";
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

        public override string GenerateCreateProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            return body.Trim();
        }

        public override string GenerateDropProcedureScript(string databaseName, string schemaName, string procName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"DROP PROCEDURE IF EXISTS `{Escape(db)}`.`{Escape(procName)}`;";
        }

        public override string GenerateGetProcedureInfoScript(string databaseName, string schemaName, string procName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $@"SELECT
    routine_schema AS SchemaName,
    routine_name AS ProcedureName,
    COALESCE(routine_definition, '') AS ProcedureDefinition,
    0 AS IsEncrypted,
    created AS CreateDate,
    last_altered AS ModifyDate
FROM information_schema.routines
WHERE routine_type = 'PROCEDURE' AND routine_schema = '{Escape(db)}' AND routine_name = '{Escape(procName)}';";
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

        public override string GenerateCreateFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            return body.Trim();
        }

        public override string GenerateDropFunctionScript(string databaseName, string schemaName, string funcName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"DROP FUNCTION IF EXISTS `{Escape(db)}`.`{Escape(funcName)}`;";
        }

        public override string GenerateGetFunctionInfoScript(string databaseName, string schemaName, string funcName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $@"SELECT
    routine_schema AS SchemaName,
    routine_name AS FunctionName,
    'SCALAR' AS FunctionType,
    COALESCE(routine_definition, '') AS FunctionDefinition,
    0 AS IsEncrypted,
    created AS CreateDate,
    last_altered AS ModifyDate
FROM information_schema.routines
WHERE routine_type = 'FUNCTION' AND routine_schema = '{Escape(db)}' AND routine_name = '{Escape(funcName)}';";
        }
        #endregion

        #region Trigger Operations
        public override string GenerateGetTriggersScript(string databaseName)
        {
            return $@"SELECT
    trigger_schema AS SchemaName,
    trigger_name AS TriggerName,
    event_object_schema AS TableSchema,
    event_object_table AS TableName,
    0 AS IsDisabled,
    0 AS IsInsteadOfTrigger,
    action_timing AS TriggerEvents,
    0 AS IsEncrypted,
    created AS CreateDate,
    created AS ModifyDate
FROM information_schema.triggers
WHERE trigger_schema = '{Escape(databaseName)}'
ORDER BY trigger_name;";
        }

        public override string GenerateGetTriggerInfoScript(string databaseName, string schemaName, string triggerName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $@"SELECT
    trigger_schema AS SchemaName,
    trigger_name AS TriggerName,
    event_object_schema AS TableSchema,
    event_object_table AS TableName,
    0 AS IsDisabled,
    0 AS IsInsteadOfTrigger,
    action_timing AS TriggerEvents,
    COALESCE(action_statement, '') AS TriggerDefinition,
    0 AS IsEncrypted,
    created AS CreateDate,
    created AS ModifyDate
FROM information_schema.triggers
WHERE trigger_schema = '{Escape(db)}' AND trigger_name = '{Escape(triggerName)}';";
        }

        public override string GenerateCreateTriggerScript(TriggerOperation.Create config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return config.Body.Trim();
        }

        public override string GenerateDropTriggerScript(string databaseName, string schemaName, string triggerName)
        {
            var db = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : databaseName;
            return $"DROP TRIGGER IF EXISTS `{Escape(db)}`.`{Escape(triggerName)}`;";
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
