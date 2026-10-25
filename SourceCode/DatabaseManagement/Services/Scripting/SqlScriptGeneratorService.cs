using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services.Scripting
{
    public sealed class SqlScriptGeneratorService
    {
        private readonly DatabaseManager _manager;

        public SqlScriptGeneratorService(DatabaseManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public async Task<string> GenerateScriptAsync(string databaseName, ScriptingOptions options, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name is required.", nameof(databaseName));
            ArgumentNullException.ThrowIfNull(options);

            var sb = new StringBuilder();

            if (options.IncludeComments)
            {
                sb.AppendLine("/*");
                sb.AppendLine("================================================================================");
                sb.AppendLine("BprDevKit SQL Script Generator");
                sb.AppendLine($"Database: [{databaseName}]");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Script Mode: {options.ScriptType}");
                sb.AppendLine("================================================================================");
                sb.AppendLine("*/");
                sb.AppendLine();
            }

            if (options.IncludeUseDatabase)
            {
                sb.AppendLine($"USE [{databaseName}];");
                sb.AppendLine("GO");
                sb.AppendLine();
            }

            // 1. Schemas
            if (options.ScriptType != ScriptType.DataOnly && options.SelectedSchemas.Count > 0)
            {
                progress?.Report("Scripting Schemas...");
                await ScriptSchemasAsync(databaseName, options, sb);
            }

            // Cache Table Details for DDL and DML
            var tableDetailsMap = new Dictionary<string, TableOperation.Information>(StringComparer.OrdinalIgnoreCase);
            if (options.SelectedTables.Count > 0)
            {
                progress?.Report("Loading Table Metadata...");
                foreach (var tableKey in options.SelectedTables)
                {
                    var parts = tableKey.Split('.', 2);
                    if (parts.Length == 2)
                    {
                        var schema = parts[0];
                        var tbl = parts[1];
                        var detailsResult = await _manager.GetTableInformation(new TableOperation.Information
                        {
                            DatabaseName = databaseName,
                            SchemaName = schema,
                            TableName = tbl
                        });
                        if (detailsResult.operationResult.Success && detailsResult.table is not null)
                        {
                            tableDetailsMap[tableKey] = detailsResult.table;
                        }
                    }
                }
            }

            // Order tables topologically (dependencies first, dependents later) and group by schema
            List<TableOperation.Information> orderedTables = new();
            if (tableDetailsMap.Count > 0)
            {
                orderedTables = OrderTablesByDependency(tableDetailsMap);
            }

            // 2. Tables DDL
            if (options.ScriptType != ScriptType.DataOnly && orderedTables.Count > 0)
            {
                progress?.Report("Scripting Tables DDL...");
                ScriptTablesDdl(options, orderedTables, tableDetailsMap, sb);
            }

            // 3. Table Data (DML)
            if (options.ScriptType != ScriptType.SchemaOnly && orderedTables.Count > 0)
            {
                progress?.Report("Scripting Table Data...");
                await ScriptTableDataAsync(databaseName, options, orderedTables, sb);
            }

            // 4. Functions
            if (options.ScriptType != ScriptType.DataOnly && options.SelectedFunctions.Count > 0)
            {
                progress?.Report("Scripting Functions...");
                await ScriptFunctionsAsync(databaseName, options, sb);
            }

            // 5. Views
            if (options.ScriptType != ScriptType.DataOnly && options.SelectedViews.Count > 0)
            {
                progress?.Report("Scripting Views...");
                await ScriptViewsAsync(databaseName, options, sb);
            }

            // 6. Stored Procedures
            if (options.ScriptType != ScriptType.DataOnly && options.SelectedProcedures.Count > 0)
            {
                progress?.Report("Scripting Stored Procedures...");
                await ScriptProceduresAsync(databaseName, options, sb);
            }

            // 7. Triggers
            if (options.ScriptType != ScriptType.DataOnly && options.IncludeTriggers && options.SelectedTriggers.Count > 0)
            {
                progress?.Report("Scripting Triggers...");
                await ScriptTriggersAsync(databaseName, options, sb);
            }

            progress?.Report("Done");
            return sb.ToString();
        }

        private async Task ScriptSchemasAsync(string databaseName, ScriptingOptions options, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Schemas");
                sb.AppendLine("-- =============================================");
            }

            foreach (var schema in options.SelectedSchemas)
            {
                if (string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (options.IncludeIfNotExists)
                {
                    sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema.Replace("'", "''")}')");
                    sb.AppendLine($"    EXEC('CREATE SCHEMA [{schema}]');");
                }
                else
                {
                    sb.AppendLine($"CREATE SCHEMA [{schema}];");
                }
                sb.AppendLine("GO");
            }
            sb.AppendLine();
        }

        private static void ScriptTablesDdl(ScriptingOptions options, List<TableOperation.Information> orderedTables, Dictionary<string, TableOperation.Information> tableDetailsMap, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Tables (DDL)");
                sb.AppendLine("-- =============================================");
            }

            // If Drop and Create is requested, drop tables in reverse dependency order first
            if (options.IncludeDropAndCreate)
            {
                if (options.IncludeComments)
                {
                    sb.AppendLine("-- Drop Tables (reverse dependency order)");
                }
                for (var i = orderedTables.Count - 1; i >= 0; i--)
                {
                    var t = orderedTables[i];
                    sb.AppendLine($"IF OBJECT_ID(N'[{t.SchemaName}].[{t.TableName}]', N'U') IS NOT NULL");
                    sb.AppendLine($"    DROP TABLE [{t.SchemaName}].[{t.TableName}];");
                    sb.AppendLine("GO");
                }
                sb.AppendLine();
            }

            var createdTablesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deferredFks = new List<(TableOperation.Information parent, ForeignKeyOperation.Information fk)>();

            // Group tables by schema to keep each schema's tables clearly partitioned and organized
            var schemaGroups = orderedTables.GroupBy(t => t.SchemaName, StringComparer.OrdinalIgnoreCase);

            foreach (var schemaGroup in schemaGroups)
            {
                if (options.IncludeComments)
                {
                    sb.AppendLine("-- =============================================");
                    sb.AppendLine($"-- Schema: [{schemaGroup.Key}]");
                    sb.AppendLine("-- =============================================");
                }

                foreach (var details in schemaGroup)
                {
                    var schema = details.SchemaName;
                    var tbl = details.TableName;

                    if (options.IncludeComments)
                    {
                        sb.AppendLine("------------------------------------------------");
                        sb.AppendLine($"-- Table: [{schema}].[{tbl}]");
                        sb.AppendLine("------------------------------------------------");
                    }

                    if (options.IncludeIfNotExists)
                    {
                        sb.AppendLine($"IF OBJECT_ID(N'[{schema}].[{tbl}]', N'U') IS NULL");
                        sb.AppendLine("BEGIN");
                    }

                    var indent = options.IncludeIfNotExists ? "    " : "";
                    sb.AppendLine($"{indent}CREATE TABLE [{schema}].[{tbl}] (");

                    var tableItemDefs = new List<string>();

                    // 1. Column definitions
                    foreach (var col in details.Columns.OrderBy(c => c.OrdinalPosition))
                    {
                        tableItemDefs.Add($"{indent}    {BuildColumnDefinition(col)}");
                    }

                    // 2. Primary Key constraint (from database sys.indexes)
                    var pkIndex = details.Indexes.FirstOrDefault(i => i.IsPrimaryKey);
                    if (pkIndex is not null)
                    {
                        var pkName = pkIndex.IndexName;
                        var pkType = string.Equals(pkIndex.IndexType, "NONCLUSTERED", StringComparison.OrdinalIgnoreCase) ? "NONCLUSTERED" : "CLUSTERED";
                        var pkCols = !string.IsNullOrWhiteSpace(pkIndex.Columns)
                            ? pkIndex.Columns
                            : string.Join(", ", details.Columns.Where(c => c.IsPrimaryKey).Select(c => $"[{c.ColumnName}]"));
                        tableItemDefs.Add($"{indent}    CONSTRAINT [{pkName}] PRIMARY KEY {pkType} ({pkCols})");
                    }
                    else
                    {
                        var pkCols = details.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.OrdinalPosition).ToList();
                        if (pkCols.Count > 0)
                        {
                            var pkName = $"PK_{schema}_{tbl}";
                            var pkList = string.Join(", ", pkCols.Select(c => $"[{c.ColumnName}]"));
                            tableItemDefs.Add($"{indent}    CONSTRAINT [{pkName}] PRIMARY KEY CLUSTERED ({pkList})");
                        }
                    }

                    // 3. Foreign Key constraints (defined inline under the table)
                    if (options.IncludeForeignKeys && details.ForeignKeys.Count > 0)
                    {
                        var outgoingFks = details.ForeignKeys.Where(fk =>
                            string.Equals(fk.SchemaName, schema, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(fk.TableName, tbl, StringComparison.OrdinalIgnoreCase)).ToList();

                        foreach (var fk in outgoingFks)
                        {
                            var targetTableKey = $"{fk.RefSchemaName}.{fk.RefTableName}";
                            var isSelfRef = string.Equals(targetTableKey, $"{schema}.{tbl}", StringComparison.OrdinalIgnoreCase);
                            var isTargetCreated = isSelfRef || createdTablesSet.Contains(targetTableKey) || !tableDetailsMap.ContainsKey(targetTableKey);

                            var colList = FormatColumnList(fk.ColumnName);
                            var refColList = FormatColumnList(fk.RefColumnName);
                            var onDelete = string.IsNullOrWhiteSpace(fk.OnDelete) ? "NO ACTION" : fk.OnDelete;
                            var onUpdate = string.IsNullOrWhiteSpace(fk.OnUpdate) ? "NO ACTION" : fk.OnUpdate;
                            var notForRep = fk.IsNotForReplication ? " NOT FOR REPLICATION" : "";

                            if (isTargetCreated)
                            {
                                tableItemDefs.Add($"{indent}    CONSTRAINT [{fk.FKName}] FOREIGN KEY ({colList}) REFERENCES [{fk.RefSchemaName}].[{fk.RefTableName}] ({refColList}) ON DELETE {onDelete} ON UPDATE {onUpdate}{notForRep}");
                            }
                            else
                            {
                                deferredFks.Add((details, fk));
                            }
                        }
                    }

                    // 4. Check Constraints
                    if (options.IncludeCheckConstraints && details.CheckConstraints.Count > 0)
                    {
                        foreach (var ck in details.CheckConstraints)
                        {
                            var checkExpr = ck.CheckExpression.Trim();
                            if (!checkExpr.StartsWith("(") || !checkExpr.EndsWith(")"))
                            {
                                checkExpr = $"({checkExpr})";
                            }
                            tableItemDefs.Add($"{indent}    CONSTRAINT [{ck.ConstraintName}] CHECK {checkExpr}");
                        }
                    }

                    sb.AppendLine(string.Join("," + Environment.NewLine, tableItemDefs));
                    sb.AppendLine($"{indent});");

                    if (options.IncludeIfNotExists)
                    {
                        sb.AppendLine("END");
                    }
                    sb.AppendLine("GO");
                    sb.AppendLine();

                    createdTablesSet.Add($"{schema}.{tbl}");

                    // 5. Non-PK Indexes right under each table
                    if (options.IncludeIndexes && details.Indexes.Count > 0)
                    {
                        var nonPkIndexes = details.Indexes.Where(i => !i.IsPrimaryKey).ToList();
                        if (nonPkIndexes.Count > 0)
                        {
                            foreach (var idx in nonPkIndexes)
                            {
                                var unique = idx.IsUnique ? "UNIQUE " : "";
                                var type = string.Equals(idx.IndexType, "CLUSTERED", StringComparison.OrdinalIgnoreCase) ? "CLUSTERED" : "NONCLUSTERED";
                                var cols = string.IsNullOrWhiteSpace(idx.Columns) ? "" : idx.Columns;
                                var includes = string.IsNullOrWhiteSpace(idx.IncludeColumns) ? "" : $" INCLUDE ({idx.IncludeColumns})";
                                var filter = string.IsNullOrWhiteSpace(idx.FilterPredicate) ? "" : $" WHERE {idx.FilterPredicate}";

                                if (options.IncludeIfNotExists)
                                {
                                    sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'{idx.IndexName.Replace("'", "''")}' AND object_id = OBJECT_ID(N'[{schema}].[{tbl}]'))");
                                    sb.AppendLine($"    CREATE {unique}{type} INDEX [{idx.IndexName}] ON [{schema}].[{tbl}] ({cols}){includes}{filter};");
                                }
                                else
                                {
                                    sb.AppendLine($"CREATE {unique}{type} INDEX [{idx.IndexName}] ON [{schema}].[{tbl}] ({cols}){includes}{filter};");
                                }
                                sb.AppendLine("GO");
                            }
                            sb.AppendLine();
                        }
                    }
                }
            }

            // Deferred foreign keys for rare circular cross-table references
            if (deferredFks.Count > 0)
            {
                if (options.IncludeComments)
                {
                    sb.AppendLine("-- =============================================");
                    sb.AppendLine("-- Deferred Foreign Keys (Circular Dependencies)");
                    sb.AppendLine("-- =============================================");
                }

                foreach (var (parentTable, fk) in deferredFks)
                {
                    var colList = FormatColumnList(fk.ColumnName);
                    var refColList = FormatColumnList(fk.RefColumnName);
                    var onDelete = string.IsNullOrWhiteSpace(fk.OnDelete) ? "NO ACTION" : fk.OnDelete;
                    var onUpdate = string.IsNullOrWhiteSpace(fk.OnUpdate) ? "NO ACTION" : fk.OnUpdate;
                    var notForRep = fk.IsNotForReplication ? " NOT FOR REPLICATION" : "";

                    if (options.IncludeIfNotExists)
                    {
                        sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'{fk.FKName.Replace("'", "''")}' AND parent_object_id = OBJECT_ID(N'[{parentTable.SchemaName}].[{parentTable.TableName}]'))");
                        sb.AppendLine($"    ALTER TABLE [{parentTable.SchemaName}].[{parentTable.TableName}] WITH CHECK ADD CONSTRAINT [{fk.FKName}] FOREIGN KEY ({colList}) REFERENCES [{fk.RefSchemaName}].[{fk.RefTableName}] ({refColList}) ON DELETE {onDelete} ON UPDATE {onUpdate}{notForRep};");
                    }
                    else
                    {
                        sb.AppendLine($"ALTER TABLE [{parentTable.SchemaName}].[{parentTable.TableName}] WITH CHECK ADD CONSTRAINT [{fk.FKName}] FOREIGN KEY ({colList}) REFERENCES [{fk.RefSchemaName}].[{fk.RefTableName}] ({refColList}) ON DELETE {onDelete} ON UPDATE {onUpdate}{notForRep};");
                    }
                    sb.AppendLine("GO");
                }
                sb.AppendLine();
            }
        }

        private async Task ScriptTableDataAsync(string databaseName, ScriptingOptions options, List<TableOperation.Information> orderedTables, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Table Data (DML)");
                sb.AppendLine("-- =============================================");
            }

            using var connection = await _manager.OpenConnectionAsync(databaseName);
            var totalRowsScripted = 0;

            foreach (var details in orderedTables)
            {
                var schema = details.SchemaName;
                var tbl = details.TableName;

                // Identify insertable columns: exclude computed columns and timestamp/rowversion
                var insertableColumns = details.Columns
                    .Where(c => string.IsNullOrWhiteSpace(c.ComputedFormula)
                             && !string.Equals(c.DataType, "timestamp", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(c.DataType, "rowversion", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.OrdinalPosition)
                    .ToList();

                var colSelect = insertableColumns.Count > 0
                    ? string.Join(", ", insertableColumns.Select(c => $"[{c.ColumnName}]"))
                    : "*";

                var topClause = options.MaxDataRowsPerTable > 0 ? $"TOP ({options.MaxDataRowsPerTable}) " : "";
                var dataQuery = $"SELECT {topClause}{colSelect} FROM [{schema}].[{tbl}] WITH (NOLOCK);";

                var rows = new List<List<string>>();
                List<string> columnNames = new();

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = dataQuery;
                    command.CommandTimeout = 180;

                    using var reader = await command.ExecuteReaderAsync();
                    var fieldCount = reader.FieldCount;
                    for (var f = 0; f < fieldCount; f++)
                    {
                        columnNames.Add($"[{reader.GetName(f)}]");
                    }

                    while (await reader.ReadAsync())
                    {
                        var rowVals = new List<string>(fieldCount);
                        for (var f = 0; f < fieldCount; f++)
                        {
                            if (await reader.IsDBNullAsync(f))
                            {
                                rowVals.Add("NULL");
                            }
                            else
                            {
                                var val = reader.GetValue(f);
                                rowVals.Add(FormatSqlLiteral(val));
                            }
                        }
                        rows.Add(rowVals);
                    }
                }
                catch (Exception ex)
                {
                    if (options.IncludeComments)
                    {
                        sb.AppendLine($"-- Error reading data for [{schema}].[{tbl}]: {ex.Message}");
                        sb.AppendLine();
                    }
                    continue;
                }

                if (rows.Count == 0)
                    continue;

                totalRowsScripted += rows.Count;
                var hasIdentity = details.Columns.Any(c => c.IsIdentity);

                if (options.IncludeComments)
                {
                    sb.AppendLine($"-- Data for [{schema}].[{tbl}] ({rows.Count} rows)");
                }

                if (hasIdentity)
                {
                    sb.AppendLine($"SET IDENTITY_INSERT [{schema}].[{tbl}] ON;");
                }

                var colListStr = string.Join(", ", columnNames);
                const int batchSize = 100;

                for (var i = 0; i < rows.Count; i += batchSize)
                {
                    var chunk = rows.Skip(i).Take(batchSize).ToList();
                    sb.AppendLine($"INSERT INTO [{schema}].[{tbl}] ({colListStr})");
                    sb.AppendLine("VALUES");

                    for (var r = 0; r < chunk.Count; r++)
                    {
                        var rowStr = $"    ({string.Join(", ", chunk[r])})";
                        sb.AppendLine(r == chunk.Count - 1 ? rowStr + ";" : rowStr + ",");
                    }
                }

                if (hasIdentity)
                {
                    sb.AppendLine($"SET IDENTITY_INSERT [{schema}].[{tbl}] OFF;");
                }

                sb.AppendLine("GO");
                sb.AppendLine();
            }

            if (totalRowsScripted == 0 && options.IncludeComments)
            {
                sb.AppendLine("-- (No data rows found in selected table(s))");
                sb.AppendLine();
            }
        }

        private async Task ScriptViewsAsync(string databaseName, ScriptingOptions options, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Views");
                sb.AppendLine("-- =============================================");
            }

            foreach (var viewKey in options.SelectedViews)
            {
                var parts = viewKey.Split('.', 2);
                if (parts.Length != 2) continue;
                var schema = parts[0];
                var viewName = parts[1];

                var info = await _manager.GetViewInformation(databaseName, schema, viewName);
                if (!info.operationResult.Success || info.view is null) continue;

                var definition = info.view.Definition;
                if (info.view.IsEncrypted && string.IsNullOrWhiteSpace(definition))
                {
                    var decrypted = await _manager.DecryptView(databaseName, schema, viewName);
                    if (decrypted.Success && !string.IsNullOrWhiteSpace(decrypted.DecryptedDefinition))
                    {
                        definition = decrypted.DecryptedDefinition;
                    }
                }

                if (string.IsNullOrWhiteSpace(definition)) continue;

                if (options.IncludeDropAndCreate)
                {
                    sb.AppendLine($"IF OBJECT_ID(N'[{schema}].[{viewName}]', N'V') IS NOT NULL");
                    sb.AppendLine($"    DROP VIEW [{schema}].[{viewName}];");
                    sb.AppendLine("GO");
                }

                sb.AppendLine(definition.Trim());
                sb.AppendLine("GO");
                sb.AppendLine();
            }
        }

        private async Task ScriptProceduresAsync(string databaseName, ScriptingOptions options, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Stored Procedures");
                sb.AppendLine("-- =============================================");
            }

            foreach (var procKey in options.SelectedProcedures)
            {
                var parts = procKey.Split('.', 2);
                if (parts.Length != 2) continue;
                var schema = parts[0];
                var procName = parts[1];

                var info = await _manager.GetProcedureInformation(databaseName, schema, procName);
                if (!info.operationResult.Success || info.procedure is null) continue;

                var definition = info.procedure.Definition;
                if (info.procedure.IsEncrypted && string.IsNullOrWhiteSpace(definition))
                {
                    var decrypted = await _manager.DecryptProcedure(databaseName, schema, procName);
                    if (decrypted.Success && !string.IsNullOrWhiteSpace(decrypted.DecryptedDefinition))
                    {
                        definition = decrypted.DecryptedDefinition;
                    }
                }

                if (string.IsNullOrWhiteSpace(definition)) continue;

                if (options.IncludeDropAndCreate)
                {
                    sb.AppendLine($"IF OBJECT_ID(N'[{schema}].[{procName}]', N'P') IS NOT NULL");
                    sb.AppendLine($"    DROP PROCEDURE [{schema}].[{procName}];");
                    sb.AppendLine("GO");
                }

                sb.AppendLine(definition.Trim());
                sb.AppendLine("GO");
                sb.AppendLine();
            }
        }

        private async Task ScriptFunctionsAsync(string databaseName, ScriptingOptions options, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Functions");
                sb.AppendLine("-- =============================================");
            }

            foreach (var funcKey in options.SelectedFunctions)
            {
                var parts = funcKey.Split('.', 2);
                if (parts.Length != 2) continue;
                var schema = parts[0];
                var funcName = parts[1];

                var info = await _manager.GetFunctionInformation(databaseName, schema, funcName);
                if (!info.operationResult.Success || info.function is null) continue;

                var definition = info.function.Definition;
                if (info.function.IsEncrypted && string.IsNullOrWhiteSpace(definition))
                {
                    var decrypted = await _manager.DecryptFunction(databaseName, schema, funcName, info.function.FunctionType);
                    if (decrypted.Success && !string.IsNullOrWhiteSpace(decrypted.DecryptedDefinition))
                    {
                        definition = decrypted.DecryptedDefinition;
                    }
                }

                if (string.IsNullOrWhiteSpace(definition)) continue;

                if (options.IncludeDropAndCreate)
                {
                    sb.AppendLine($"IF OBJECT_ID(N'[{schema}].[{funcName}]') IS NOT NULL");
                    sb.AppendLine($"    DROP FUNCTION [{schema}].[{funcName}];");
                    sb.AppendLine("GO");
                }

                sb.AppendLine(definition.Trim());
                sb.AppendLine("GO");
                sb.AppendLine();
            }
        }

        private async Task ScriptTriggersAsync(string databaseName, ScriptingOptions options, StringBuilder sb)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- =============================================");
                sb.AppendLine("-- Triggers");
                sb.AppendLine("-- =============================================");
            }

            foreach (var trigKey in options.SelectedTriggers)
            {
                var parts = trigKey.Split('.', 2);
                if (parts.Length != 2) continue;
                var schema = parts[0];
                var trigName = parts[1];

                var info = await _manager.GetTriggerInformation(databaseName, schema, trigName);
                if (!info.operationResult.Success || info.trigger is null) continue;

                var definition = info.trigger.Definition;
                if (info.trigger.IsEncrypted && string.IsNullOrWhiteSpace(definition))
                {
                    var decrypted = await _manager.DecryptTrigger(databaseName, schema, trigName);
                    if (decrypted.Success && !string.IsNullOrWhiteSpace(decrypted.DecryptedDefinition))
                    {
                        definition = decrypted.DecryptedDefinition;
                    }
                }

                if (string.IsNullOrWhiteSpace(definition)) continue;

                if (options.IncludeDropAndCreate)
                {
                    sb.AppendLine($"IF OBJECT_ID(N'[{schema}].[{trigName}]', N'TR') IS NOT NULL");
                    sb.AppendLine($"    DROP TRIGGER [{schema}].[{trigName}];");
                    sb.AppendLine("GO");
                }

                sb.AppendLine(definition.Trim());
                sb.AppendLine("GO");
                sb.AppendLine();
            }
        }

        private static string BuildColumnDefinition(ColumnOperation.Information col)
        {
            if (!string.IsNullOrWhiteSpace(col.ComputedFormula))
            {
                var formula = col.ComputedFormula.Trim();
                var persisted = col.IsPersisted ? " PERSISTED" : "";
                return $"[{col.ColumnName}] AS {formula}{persisted}";
            }

            var typeDef = FormatColumnDataType(col);
            var sb = new StringBuilder($"[{col.ColumnName}] {typeDef}");

            if (col.IsIdentity)
            {
                var seed = col.IdentitySeed != 0 ? col.IdentitySeed : 1;
                var inc = col.IdentityIncrement != 0 ? col.IdentityIncrement : 1;
                sb.Append($" IDENTITY({seed},{inc})");
            }

            sb.Append(col.IsNullable ? " NULL" : " NOT NULL");

            if (!string.IsNullOrWhiteSpace(col.DefaultValue))
            {
                var defaultVal = col.DefaultValue.Trim();
                if (!defaultVal.StartsWith("(") || !defaultVal.EndsWith(")"))
                {
                    defaultVal = $"({defaultVal})";
                }

                string constraintName;
                if (!string.IsNullOrWhiteSpace(col.DefaultConstraintName) &&
                    !col.DefaultConstraintName.StartsWith("DF__", StringComparison.OrdinalIgnoreCase))
                {
                    constraintName = col.DefaultConstraintName.Trim();
                }
                else
                {
                    var tbl = !string.IsNullOrWhiteSpace(col.TableName) ? col.TableName : "Table";
                    constraintName = $"DF_{tbl}_{col.ColumnName}";
                }

                sb.Append($" CONSTRAINT [{constraintName}] DEFAULT {defaultVal}");
            }

            return sb.ToString();
        }

        public static string FormatColumnList(string? rawCols)
        {
            if (string.IsNullOrWhiteSpace(rawCols)) return string.Empty;
            var cols = rawCols.Split(',')
                .Select(c => c.Trim().Trim('[', ']'))
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => $"[{c}]");
            return string.Join(", ", cols);
        }

        private static List<TableOperation.Information> OrderTablesByDependency(Dictionary<string, TableOperation.Information> tableMap)
        {
            var tables = tableMap.Values.ToList();
            if (tables.Count <= 1)
                return tables;

            var tableKeys = new HashSet<string>(tableMap.Keys, StringComparer.OrdinalIgnoreCase);

            // Dependencies of each table (tables that MUST precede this table)
            var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                var key = $"{table.SchemaName}.{table.TableName}";
                var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var outgoingFks = table.ForeignKeys.Where(fk =>
                    string.Equals(fk.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(fk.TableName, table.TableName, StringComparison.OrdinalIgnoreCase));

                foreach (var fk in outgoingFks)
                {
                    var refKey = $"{fk.RefSchemaName}.{fk.RefTableName}";
                    if (!string.Equals(refKey, key, StringComparison.OrdinalIgnoreCase) && tableKeys.Contains(refKey))
                    {
                        deps.Add(refKey);
                    }
                }
                dependencies[key] = deps;
            }

            // Build schema dependencies: Schema A depends on Schema B if any table in Schema A references a table in Schema B
            var schemas = tables.Select(t => t.SchemaName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var schemaDependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in schemas)
            {
                schemaDependencies[s] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var table in tables)
            {
                var tableKey = $"{table.SchemaName}.{table.TableName}";
                foreach (var depTableKey in dependencies[tableKey])
                {
                    var depSchema = depTableKey.Split('.')[0];
                    if (!string.Equals(depSchema, table.SchemaName, StringComparison.OrdinalIgnoreCase))
                    {
                        schemaDependencies[table.SchemaName].Add(depSchema);
                    }
                }
            }

            // Topologically sort schemas
            var sortedSchemas = TopologicalSort(schemas, schemaDependencies);

            // Topologically sort tables within each schema
            var result = new List<TableOperation.Information>();
            foreach (var schema in sortedSchemas)
            {
                var schemaTables = tables
                    .Where(t => string.Equals(t.SchemaName, schema, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var localDeps = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in schemaTables)
                {
                    var key = $"{t.SchemaName}.{t.TableName}";
                    var deps = new HashSet<string>(
                        dependencies[key].Where(d => string.Equals(d.Split('.')[0], schema, StringComparison.OrdinalIgnoreCase)),
                        StringComparer.OrdinalIgnoreCase);
                    localDeps[key] = deps;
                }

                var schemaTableKeys = schemaTables.Select(t => $"{t.SchemaName}.{t.TableName}").ToList();
                var sortedKeys = TopologicalSort(schemaTableKeys, localDeps);

                foreach (var k in sortedKeys)
                {
                    result.Add(tableMap[k]);
                }
            }

            return result;
        }

        private static List<string> TopologicalSort(List<string> items, Dictionary<string, HashSet<string>> dependencies)
        {
            var sorted = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sortedItems = items.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var item in sortedItems)
            {
                Visit(item);
            }

            void Visit(string node)
            {
                if (visited.Contains(node))
                    return;
                if (visiting.Contains(node))
                    return;

                visiting.Add(node);

                if (dependencies.TryGetValue(node, out var deps))
                {
                    foreach (var dep in deps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        if (items.Contains(dep, StringComparer.OrdinalIgnoreCase))
                        {
                            Visit(dep);
                        }
                    }
                }

                visiting.Remove(node);
                visited.Add(node);
                sorted.Add(node);
            }

            return sorted;
        }

        private static string FormatColumnDataType(ColumnOperation.Information col)
        {
            var dt = (col.DataType ?? string.Empty).ToLowerInvariant();
            return dt switch
            {
                "nvarchar" or "nchar" => col.Length == -1 ? $"{dt}(MAX)" : $"{dt}({col.Length ?? 50})",
                "varchar" or "char" or "varbinary" or "binary" => col.Length == -1 ? $"{dt}(MAX)" : $"{dt}({col.Length ?? 50})",
                "decimal" or "numeric" => $"{dt}({col.Precision ?? 18},{col.Scale ?? 0})",
                "time" or "datetime2" or "datetimeoffset" => col.Scale.HasValue ? $"{dt}({col.Scale.Value})" : dt,
                _ => dt
            };
        }

        public static string FormatSqlLiteral(object? value)
        {
            if (value is null || value is DBNull) return "NULL";

            return value switch
            {
                bool b => b ? "1" : "0",
                byte bt => bt.ToString(CultureInfo.InvariantCulture),
                sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
                short s => s.ToString(CultureInfo.InvariantCulture),
                ushort us => us.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                uint ui => ui.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),
                ulong ul => ul.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString("G9", CultureInfo.InvariantCulture),
                double d => d.ToString("G17", CultureInfo.InvariantCulture),
                decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
                DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'",
                DateOnly dOnly => $"'{dOnly:yyyy-MM-dd}'",
                TimeOnly tOnly => $"'{tOnly:HH:mm:ss.fff}'",
                TimeSpan ts => $"'{ts}'",
                Guid g => $"'{g}'",
                byte[] bytes => "0x" + Convert.ToHexString(bytes),
                _ => $"N'{value.ToString()?.Replace("'", "''") ?? ""}'"
            };
        }
    }
}
