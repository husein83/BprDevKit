using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.DatabaseManagement.Services;
using BprDevKit.Utility;
using BprDevKit.Config;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Main database operations service that handles transactions and uses ISqlGenerator for dynamic SQL generation
    /// </summary>
    public class DatabaseManager
    {
        private readonly ISqlGenerator _sqlGenerator;
        private readonly ServerConfiguration _config;

        public DatabaseManager(ISqlGenerator sqlGenerator, ServerConfiguration config)
        {
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #region Database Operations
        public async Task<OperationResult> CreateDatabase(DatabaseOperation.Create config)
        {
            try
            {
                if (await DatabaseExistsAsync(config.DatabaseName))
                    return OperationResult.ErrorResult($"[BprDevKit] Database [{config.DatabaseName}] already exists.");

                var script = _sqlGenerator.GenerateCreateDatabaseScript(config);
                return await ExecuteDatabaseCommand(script, $"Database '{config.DatabaseName}' created successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating database: {ex.Message} The database may have been created before a later setting failed; verify the server state.", ex);
            }
        }

        public async Task<OperationResult> AlterDatabase(DatabaseOperation.Alter config)
        {
            try
            {
                if (!await DatabaseExistsAsync(config.DatabaseName))
                    return OperationResult.ErrorResult($"[BprDevKit] Database [{config.DatabaseName}] does not exist.");

                if (!HasDatabaseChanges(config))
                    return OperationResult.ErrorResult("[BprDevKit] Select at least one database setting to update.");

                var script = _sqlGenerator.GenerateAlterDatabaseScript(config);
                return await ExecuteDatabaseCommand(script, $"Database '{config.DatabaseName}' updated successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error updating database: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropDatabase(DatabaseOperation.Drop config)
        {
            try
            {
                if (!await DatabaseExistsAsync(config.DatabaseName))
                {
                    return config.IgnoreIfNotExists
                        ? OperationResult.SuccessResult($"[BprDevKit] Database [{config.DatabaseName}] does not exist. Skipping.")
                        : OperationResult.ErrorResult($"[BprDevKit] Database [{config.DatabaseName}] does not exist.");
                }

                var script = _sqlGenerator.GenerateDropDatabaseScript(config);
                return await ExecuteDatabaseCommand(script, $"Database '{config.DatabaseName}' dropped successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping database: {ex.Message}", ex);
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetDatabaseInformation(DatabaseOperation.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetDatabaseInfoScript(config);
                var result = await ExecuteQuery(sql);

                if (result.Rows.Count == 0)
                    return (OperationResult.ErrorResult($"Database '{config.DatabaseName}' was not found."), null);

                return (OperationResult.SuccessResult($"Retrieved information for database '{config.DatabaseName}'."), result);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving database information: {ex.Message}", ex), null);
            }
        }

        /// <summary>
        /// Loads the current database options into an Alter configuration so the edit form can be pre-populated.
        /// </summary>
        public async Task<(OperationResult operationResult, DatabaseOperation.Alter? configuration)> GetDatabaseConfiguration(DatabaseOperation.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetDatabaseInfoScript(config);
                var result = await ExecuteQuery(sql);

                if (result.Rows.Count == 0)
                    return (OperationResult.ErrorResult($"Database '{config.DatabaseName}' was not found."), null);

                var row = result.Rows[0];
                var alter = new DatabaseOperation.Alter
                {
                    DatabaseName = config.DatabaseName,
                    Collation = GetNullableString(row, "Collation"),
                    RecoveryModel = GetNullableString(row, "RecoveryModel"),
                    CompatibilityLevel = TryConvertToInt16(row["CompatibilityLevel"]),
                    IsReadOnly = GetNullableBoolean(row, "IsReadOnly"),
                    AutoShrink = GetNullableBoolean(row, "AutoShrink"),
                    AutoClose = GetNullableBoolean(row, "AutoClose"),
                    PageVerify = GetNullableString(row, "PageVerify")
                };

                return (OperationResult.SuccessResult($"Retrieved configuration for database '{config.DatabaseName}'."), alter);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving database configuration: {ex.Message}", ex), null);
            }
        }

        private static string? GetNullableString(DataRow row, string columnName)
        {
            var value = row[columnName];
            return value == null || value == DBNull.Value ? null : value.ToString();
        }

        private static bool? GetNullableBoolean(DataRow row, string columnName)
        {
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return null;
            return Convert.ToInt32(value) == 1;
        }

        private static short? TryConvertToInt16(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            return value switch
            {
                short level => level,
                byte byteLevel => (short)byteLevel,
                int intLevel when intLevel >= short.MinValue && intLevel <= short.MaxValue => (short)intLevel,
                _ => Convert.ToInt16(value)
            };
        }
        #endregion

        #region Schema Operations
        public async Task<OperationResult> CreateSchema(SchemaOperation.Create config)
        {
            try
            {
                var script = _sqlGenerator.GenerateCreateSchemaScript(config);

                if (await SchemaExistsAsync(config.DatabaseName, config.SchemaName))
                    return OperationResult.ErrorResult($"[BprDevKit] Schema [{config.SchemaName}] already exists in database [{config.DatabaseName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Schema created successfully",
                    $"Schema '{config.SchemaName}' created successfully in database '{config.DatabaseName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating schema: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> AlterSchema(SchemaOperation.Alter config)
        {
            try
            {
                var script = _sqlGenerator.GenerateAlterSchemaScript(config);

                if (!await SchemaExistsAsync(config.DatabaseName, config.SchemaName))
                    return OperationResult.ErrorResult($"[BprDevKit] Schema [{config.SchemaName}] does not exist in database [{config.DatabaseName}].");

                if (config.IsRename && await SchemaExistsAsync(config.DatabaseName, config.NewSchemaName!))
                    return OperationResult.ErrorResult($"[BprDevKit] Schema [{config.NewSchemaName}] already exists in database [{config.DatabaseName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Schema updated successfully",
                    DescribeSchemaChange(config));
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error updating schema: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropSchema(SchemaOperation.Drop config)
        {
            try
            {
                var script = _sqlGenerator.GenerateDropSchemaScript(config);

                if (!await SchemaExistsAsync(config.DatabaseName, config.SchemaName))
                    return OperationResult.ErrorResult($"[BprDevKit] Schema [{config.SchemaName}] does not exist in database [{config.DatabaseName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Schema dropped successfully",
                    $"Schema '{config.SchemaName}' dropped successfully from database '{config.DatabaseName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping schema: {ex.Message}", ex);
            }
        }

        public async Task<(OperationResult operationResult, List<string> schemas)> GetSchemas(string databaseName)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetSchemasScript(databaseName);
                var results = await ExecuteListQuery(sql);

                return (OperationResult.SuccessResult($"Retrieved {results.Count} schema(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving schemas: {ex.Message}", ex), new List<string>());
            }
        }

        /// <summary>
        /// Lists the schemas of a database with their owner and object count.
        /// </summary>
        public async Task<(OperationResult operationResult, List<SchemaOperation.Information> schemas)> GetSchemaList(string databaseName)
        {
            try
            {
                var table = await ExecuteQuery(_sqlGenerator.GenerateGetSchemaListScript(databaseName));
                var schemas = table.Rows.Cast<DataRow>().Select(row => MapSchema(databaseName, row)).ToList();

                return (OperationResult.SuccessResult($"Retrieved {schemas.Count} schema(s)"), schemas);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving schemas: {ex.Message}", ex), new List<SchemaOperation.Information>());
            }
        }

        /// <summary>
        /// Loads one schema completely: owner, object count and the objects that live in it.
        /// </summary>
        public async Task<(OperationResult operationResult, SchemaOperation.Information? schema)> GetSchemaInformation(SchemaOperation.Information config)
        {
            try
            {
                var header = await ExecuteQuery(_sqlGenerator.GenerateGetSchemaListScript(config.DatabaseName, config.SchemaName));
                if (header.Rows.Count == 0)
                    return (OperationResult.ErrorResult($"[BprDevKit] Schema [{config.SchemaName}] does not exist in database [{config.DatabaseName}]."), null);

                var schema = MapSchema(config.DatabaseName, header.Rows[0]);

                var objects = await ExecuteQuery(_sqlGenerator.GenerateGetSchemaInformationScript(config));
                schema.Objects = objects.Rows.Cast<DataRow>()
                    .Select(row => (
                        ObjectType: Convert.ToString(row["ObjectType"]) ?? string.Empty,
                        ObjectName: Convert.ToString(row["ObjectName"]) ?? string.Empty,
                        CreateDate: row["CreateDate"] is DateTime createDate ? createDate : (DateTime?)null))
                    .ToList();

                return (OperationResult.SuccessResult($"Retrieved schema '{config.SchemaName}' with {schema.Objects.Count} object(s)."), schema);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving schema information: {ex.Message}", ex), null);
            }
        }

        public async Task<(OperationResult operationResult, List<string> owners)> GetSchemaOwners(string databaseName)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetSchemaOwnersScript(databaseName);
                var results = await ExecuteListQuery(sql);

                return (OperationResult.SuccessResult($"Retrieved {results.Count} owner(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving schema owners: {ex.Message}", ex), new List<string>());
            }
        }
        #endregion

        #region Table Operations
        public async Task<OperationResult> CreateTable(TableOperation.Create config)
        {
            try
            {
                var script = _sqlGenerator.GenerateCreateTableScript(config);

                if (await TableExistsAsync(config.DatabaseName, config.SchemaName, config.TableName))
                    return OperationResult.ErrorResult($"[BprDevKit] Table [{config.SchemaName}].[{config.TableName}] already exists in database [{config.DatabaseName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Table created successfully",
                    $"Table '{config.SchemaName}.{config.TableName}' created successfully in database '{config.DatabaseName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating table: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> AlterTable(TableOperation.Alter config)
        {
            try
            {
                var script = _sqlGenerator.GenerateAlterTableScript(config);

                if (!await TableExistsAsync(config.DatabaseName, config.SchemaName, config.TableName))
                    return OperationResult.ErrorResult($"[BprDevKit] Table [{config.SchemaName}].[{config.TableName}] does not exist in database [{config.DatabaseName}].");

                if (config.IsRename && await TableExistsAsync(config.DatabaseName, config.SchemaName, config.NewTableName!))
                    return OperationResult.ErrorResult($"[BprDevKit] Table [{config.SchemaName}].[{config.NewTableName}] already exists in database [{config.DatabaseName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Table updated successfully",
                    $"Table '{config.SchemaName}.{config.TableName}' updated successfully in database '{config.DatabaseName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering table: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropTable(TableOperation.Drop config)
        {
            try
            {
                var script = _sqlGenerator.GenerateDropTableScript(config);

                if (!await TableExistsAsync(config.DatabaseName, config.SchemaName, config.TableName))
                {
                    return config.IgnoreIfNotExists
                        ? OperationResult.SuccessResult($"Table '{config.SchemaName}.{config.TableName}' does not exist; nothing to drop.")
                        : OperationResult.ErrorResult($"[BprDevKit] Table [{config.SchemaName}].[{config.TableName}] does not exist in database [{config.DatabaseName}].");
                }

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Table dropped successfully",
                    $"Table '{config.SchemaName}.{config.TableName}' dropped successfully from database '{config.DatabaseName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping table: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads one table completely: header data, columns, indexes and the foreign keys it owns or is referenced by.
        /// </summary>
        public async Task<(OperationResult operationResult, TableOperation.Information? table)> GetTableInformation(TableOperation.Information config)
        {
            try
            {
                var header = await ExecuteQuery(_sqlGenerator.GenerateGetTableInfoScript(config));
                if (header.Rows.Count == 0)
                    return (OperationResult.ErrorResult($"[BprDevKit] Table [{config.SchemaName}].[{config.TableName}] does not exist in database [{config.DatabaseName}]."), null);

                var headerRow = header.Rows[0];
                var table = new TableOperation.Information
                {
                    DatabaseName = config.DatabaseName,
                    SchemaName = config.SchemaName,
                    TableName = config.TableName,
                    FileGroup = GetNullableString(headerRow, "FileGroup"),
                    Description = GetNullableString(headerRow, "Description"),
                    CreateDate = Convert.ToDateTime(headerRow["CreateDate"]),
                    ModifyDate = Convert.ToDateTime(headerRow["ModifyDate"])
                };

                var columns = await ExecuteQuery(_sqlGenerator.GenerateGetTableColumnsScript(config));
                table.Columns = columns.Rows.Cast<DataRow>().Select(row => MapColumn(config, row)).ToList();
                table.ColumnCount = table.Columns.Count;

                var indexes = await ExecuteQuery(_sqlGenerator.GenerateGetTableIndexesScript(config));
                table.Indexes = indexes.Rows.Cast<DataRow>().Select(row => MapIndex(config, row)).ToList();

                var foreignKeys = await ExecuteQuery(_sqlGenerator.GenerateGetTableForeignKeysScript(config));
                table.ForeignKeys = foreignKeys.Rows.Cast<DataRow>().Select(row => MapForeignKey(config.DatabaseName, row)).ToList();

                return (OperationResult.SuccessResult($"Retrieved information for table '{config.SchemaName}.{config.TableName}'."), table);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving table information: {ex.Message}", ex), null);
            }
        }

        /// <summary>
        /// Lists the user tables of a database (row count, column count, dates, description; no columns/indexes/keys).
        /// </summary>
        public async Task<(OperationResult operationResult, List<TableOperation.Information> tables)> GetTableList(string databaseName)
        {
            try
            {
                var table = await ExecuteQuery(_sqlGenerator.GenerateGetTableListScript(databaseName));

                var tables = table.Rows.Cast<DataRow>()
                    .Select(row => new TableOperation.Information
                    {
                        DatabaseName = databaseName,
                        SchemaName = Convert.ToString(row["SchemaName"]) ?? string.Empty,
                        TableName = Convert.ToString(row["TableName"]) ?? string.Empty,
                        RowCount = Convert.ToInt64(row["RowCount"]),
                        ColumnCount = Convert.ToInt32(row["ColumnCount"]),
                        CreateDate = Convert.ToDateTime(row["CreateDate"]),
                        ModifyDate = Convert.ToDateTime(row["ModifyDate"]),
                        Description = GetNullableString(row, "Description")
                    })
                    .ToList();

                return (OperationResult.SuccessResult($"Retrieved {tables.Count} table(s)"), tables);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving tables: {ex.Message}", ex), new List<TableOperation.Information>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> fileGroups)> GetFileGroups(string databaseName)
        {
            try
            {
                var results = await ExecuteListQuery(_sqlGenerator.GenerateGetFileGroupsScript(databaseName));
                return (OperationResult.SuccessResult($"Retrieved {results.Count} file group(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving file groups: {ex.Message}", ex), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> tables)> GetTables(string databaseName, string schemaName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetTablesScript(databaseName, schemaName);
                var results = await ExecuteListQuery(sql);

                return (OperationResult.SuccessResult($"Retrieved {results.Count} table(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving tables: {ex.Message}", ex), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> columns)> GetColumns(string databaseName, string schemaName, string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), new List<string>());

                if (string.IsNullOrWhiteSpace(tableName))
                    return (OperationResult.ErrorResult("Table name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetColumnsScript(databaseName, schemaName, tableName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No columns found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} column(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving columns: {ex.Message}"), new List<string>());
            }
        }
        #endregion

        #region Column Operations
        public async Task<OperationResult> CreateColumn(ColumnOperation.Create config)
        {
            try
            {
                var script = _sqlGenerator.GenerateCreateColumnScript(config);

                if (await ColumnExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.Column.ColumnName))
                    return OperationResult.ErrorResult($"[BprDevKit] Column [{config.Column.ColumnName}] already exists in table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Column created successfully",
                    $"Column '{config.Column.ColumnName}' added to table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating column: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> AlterColumn(ColumnOperation.Alter config)
        {
            try
            {
                var script = _sqlGenerator.GenerateAlterColumnScript(config);

                if (!await ColumnExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.ColumnName))
                    return OperationResult.ErrorResult($"[BprDevKit] Column [{config.ColumnName}] does not exist in table [{config.SchemaName}].[{config.TableName}].");

                if (config.IsRename && await ColumnExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.NewColumnName!))
                    return OperationResult.ErrorResult($"[BprDevKit] Column [{config.NewColumnName}] already exists in table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Column updated successfully",
                    $"Column '{config.ColumnName}' of table '{config.SchemaName}.{config.TableName}' updated successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering column: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropColumn(ColumnOperation.Drop config)
        {
            try
            {
                var script = _sqlGenerator.GenerateDropColumnScript(config);

                if (!await ColumnExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.ColumnName))
                    return OperationResult.ErrorResult($"[BprDevKit] Column [{config.ColumnName}] does not exist in table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Column dropped successfully",
                    $"Column '{config.ColumnName}' dropped from table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping column: {ex.Message}", ex);
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetColumnInformation(ColumnOperation.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetColumnInfoScript(config);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No column information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for column '{config.ColumnName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving column information: {ex.Message}"), null);
            }
        }
        #endregion

        #region Index Operations
        public async Task<OperationResult> CreateIndex(IndexOperation.Create config)
        {
            try
            {
                var script = _sqlGenerator.GenerateCreateIndexScript(config);

                return await ExecuteInTransaction(
                    new List<string> { script },
                    config.IsPrimaryKey ? "Primary key created successfully" : "Index created successfully",
                    config.IsPrimaryKey
                        ? $"Primary key created on table '{config.SchemaName}.{config.TableName}'."
                        : $"Index created on table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating index: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> AlterIndex(IndexOperation.Alter config)
        {
            try
            {
                var script = _sqlGenerator.GenerateAlterIndexScript(config);

                if (!await IndexExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.IndexName))
                    return OperationResult.ErrorResult($"[BprDevKit] Index [{config.IndexName}] does not exist on table [{config.SchemaName}].[{config.TableName}].");

                if (await IndexExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.NewIndexName))
                    return OperationResult.ErrorResult($"[BprDevKit] Index [{config.NewIndexName}] already exists on table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Index renamed successfully",
                    $"Index '{config.IndexName}' renamed to '{config.NewIndexName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error renaming index: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropIndex(IndexOperation.Drop config)
        {
            try
            {
                var script = _sqlGenerator.GenerateDropIndexScript(config);

                if (!await IndexExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.IndexName))
                    return OperationResult.ErrorResult($"[BprDevKit] Index [{config.IndexName}] does not exist on table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Index dropped successfully",
                    $"Index '{config.IndexName}' dropped from table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping index: {ex.Message}", ex);
            }
        }

        public async Task<(OperationResult operationResult, List<string> indexNames)> GetIndexNames(string databaseName, string schemaName, string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), new List<string>());

                if (string.IsNullOrWhiteSpace(tableName))
                    return (OperationResult.ErrorResult("Table name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetIndexNamesScript(databaseName, schemaName, tableName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No indexes found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} index(es)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving index names: {ex.Message}"), new List<string>());
            }
        }
        #endregion

        #region Foreign Key Operations
        public async Task<OperationResult> CreateForeignKey(ForeignKeyOperation.Create config)
        {
            try
            {
                var script = _sqlGenerator.GenerateCreateForeignKeyScript(config);

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Foreign key created successfully",
                    $"Foreign key created on table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating foreign key: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> AlterForeignKey(ForeignKeyOperation.Alter config)
        {
            try
            {
                var script = _sqlGenerator.GenerateAlterForeignKeyScript(config);

                if (!await ForeignKeyExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.FKName))
                    return OperationResult.ErrorResult($"[BprDevKit] Foreign key [{config.FKName}] does not exist on table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    config.Enabled ? "Foreign key enabled successfully" : "Foreign key disabled successfully",
                    $"Foreign key '{config.FKName}' {(config.Enabled ? "enabled" : "disabled")}.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error updating foreign key: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DropForeignKey(ForeignKeyOperation.Drop config)
        {
            try
            {
                var script = _sqlGenerator.GenerateDropForeignKeyScript(config);

                if (!await ForeignKeyExistsAsync(config.DatabaseName, config.SchemaName, config.TableName, config.FKName))
                    return OperationResult.ErrorResult($"[BprDevKit] Foreign key [{config.FKName}] does not exist on table [{config.SchemaName}].[{config.TableName}].");

                return await ExecuteInTransaction(
                    new List<string> { script },
                    "Foreign key dropped successfully",
                    $"Foreign key '{config.FKName}' dropped from table '{config.SchemaName}.{config.TableName}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping foreign key: {ex.Message}", ex);
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetForeignKeyInformation(ForeignKeyOperation.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetForeignKeyInfoScript(config);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No foreign key information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for foreign key on column '{config.ColumnName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving foreign key information: {ex.Message}"), null);
            }
        }
        #endregion

        #region Backup/Restore
        public async Task<OperationResult> BackupDatabase(BackupOperation config)
        {
            try
            {
                var script = _sqlGenerator.GenerateBackupScript(config);
                return await ExecuteDatabaseCommand(script, $"Database '{config.DatabaseName}' backed up successfully to '{config.BackupPath}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> RestoreDatabase(RestoreOperation config)
        {
            try
            {
                var script = _sqlGenerator.GenerateRestoreScript(config);
                return await ExecuteDatabaseCommand(script, $"Database '{config.DatabaseName}' restored successfully from '{config.RestorePath}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error: {ex.Message}", ex);
            }
        }
        #endregion

        #region Get Operations
        public async Task<(OperationResult operationResult, List<string> databases)> GetDatabases()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetDatabasesScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No databases found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} database(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting databases: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> collations)> GetCollations()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetCollationsScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No collations found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} collation(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting collations: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> dataTypes)> GetDataTypes()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetDataTypesScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No data types found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} data type(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting data types: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> fileGroups)> GetFileGroups()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetFileGroupsScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No file groups found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} file group(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting file groups: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> indexTypes)> GetIndexTypes()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetIndexTypesScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No index types found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} index type(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting index types: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, List<string> referentialActions)> GetReferentialActions()
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetReferentialActionsScript();
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No referential actions found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} referential action(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error getting referential actions: {ex.Message}"), new List<string>());
            }
        }
        #endregion

        #region View Operations
        public async Task<OperationResult> CreateView(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(viewName))
                    return OperationResult.ErrorResult("View name cannot be empty");

                if (string.IsNullOrWhiteSpace(selectStatement))
                    return OperationResult.ErrorResult("View definition cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateViewScript(databaseName, schemaName, viewName, selectStatement, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "View created successfully", $"View '{schemaName}.{viewName}' created successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating view: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterView(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(viewName))
                    return OperationResult.ErrorResult("View name cannot be empty");

                if (string.IsNullOrWhiteSpace(selectStatement))
                    return OperationResult.ErrorResult("View definition cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterViewScript(databaseName, schemaName, viewName, selectStatement, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "View altered successfully", $"View '{schemaName}.{viewName}' altered successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering view: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropView(string databaseName, string schemaName, string viewName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(viewName))
                    return OperationResult.ErrorResult("View name cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropViewScript(databaseName, schemaName, viewName)
                };

                return await ExecuteInTransaction(scripts, "View dropped successfully", $"View '{schemaName}.{viewName}' dropped successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping view: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? viewInfo)> GetViewInfo(string databaseName, string schemaName, string viewName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(viewName))
                    return (OperationResult.ErrorResult("View name cannot be empty"), null);

                var sql = _sqlGenerator.GenerateGetViewInfoScript(databaseName, schemaName, viewName);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No view information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for view '{schemaName}.{viewName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving view information: {ex.Message}"), null);
            }
        }

        public async Task<(OperationResult operationResult, List<string> views)> GetViews(string databaseName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetViewsScript(databaseName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No views found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} view(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving views: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, DataTable? viewTestResult)> TestView(string databaseName, string selectStatement)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(selectStatement))
                    return (OperationResult.ErrorResult("View definition cannot be empty"), null);

                var sql = _sqlGenerator.GenerateTestViewScript(databaseName, selectStatement);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No results for view test"), null);

                return (OperationResult.SuccessResult("View test executed successfully"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error testing view: {ex.Message}"), null);
            }
        }
        #endregion

        #region Stored Procedure Operations
        public async Task<OperationResult> CreateStoredProcedure(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(procName))
                    return OperationResult.ErrorResult("Stored procedure name cannot be empty");

                if (string.IsNullOrWhiteSpace(body))
                    return OperationResult.ErrorResult("Stored procedure body cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateProcedureScript(databaseName, schemaName, procName, body, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "Stored procedure created successfully", $"Stored procedure '{schemaName}.{procName}' created successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating stored procedure: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterStoredProcedure(string databaseName, string schemaName, string procName, string body, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(procName))
                    return OperationResult.ErrorResult("Stored procedure name cannot be empty");

                if (string.IsNullOrWhiteSpace(body))
                    return OperationResult.ErrorResult("Stored procedure body cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterProcedureScript(databaseName, schemaName, procName, body, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "Stored procedure altered successfully", $"Stored procedure '{schemaName}.{procName}' altered successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering stored procedure: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropStoredProcedure(string databaseName, string schemaName, string procName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(procName))
                    return OperationResult.ErrorResult("Stored procedure name cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropProcedureScript(databaseName, schemaName, procName)
                };

                return await ExecuteInTransaction(scripts, "Stored procedure dropped successfully", $"Stored procedure '{schemaName}.{procName}' dropped successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping stored procedure: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? procedureInfo)> GetStoredProcedureInfo(string databaseName, string schemaName, string procName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(procName))
                    return (OperationResult.ErrorResult("Stored procedure name cannot be empty"), null);

                var sql = _sqlGenerator.GenerateGetProcedureInfoScript(databaseName, schemaName, procName);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No stored procedure information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for stored procedure '{schemaName}.{procName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving stored procedure information: {ex.Message}"), null);
            }
        }

        public async Task<(OperationResult operationResult, List<string> procedures)> GetStoredProcedures(string databaseName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetProceduresScript(databaseName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No stored procedures found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} stored procedure(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving stored procedures: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, DataTable? procedureTestResult)> TestStoredProcedure(string databaseName, string procName, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(procName))
                    return (OperationResult.ErrorResult("Stored procedure name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(body))
                    return (OperationResult.ErrorResult("Stored procedure body cannot be empty"), null);

                var sql = _sqlGenerator.GenerateTestProcedureScript(databaseName, procName, body);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No results for stored procedure test"), null);

                return (OperationResult.SuccessResult($"Stored procedure '{procName}' test executed successfully"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error testing stored procedure: {ex.Message}"), null);
            }
        }
        #endregion

        #region Function Operations
        public async Task<OperationResult> CreateFunction(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(funcName))
                    return OperationResult.ErrorResult("Function name cannot be empty");

                if (string.IsNullOrWhiteSpace(body))
                    return OperationResult.ErrorResult("Function body cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateFunctionScript(databaseName, schemaName, funcName, body, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "Function created successfully", $"Function '{schemaName}.{funcName}' created successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating function: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterFunction(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(funcName))
                    return OperationResult.ErrorResult("Function name cannot be empty");

                if (string.IsNullOrWhiteSpace(body))
                    return OperationResult.ErrorResult("Function body cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterFunctionScript(databaseName, schemaName, funcName, body, withEncryption)
                };

                return await ExecuteInTransaction(scripts, "Function altered successfully", $"Function '{schemaName}.{funcName}' altered successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering function: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropFunction(string databaseName, string schemaName, string funcName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                if (string.IsNullOrWhiteSpace(funcName))
                    return OperationResult.ErrorResult("Function name cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropFunctionScript(databaseName, schemaName, funcName)
                };

                return await ExecuteInTransaction(scripts, "Function dropped successfully", $"Function '{schemaName}.{funcName}' dropped successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping function: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? functionInfo)> GetFunctionInfo(string databaseName, string schemaName, string funcName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(schemaName))
                    return (OperationResult.ErrorResult("Schema name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(funcName))
                    return (OperationResult.ErrorResult("Function name cannot be empty"), null);

                var sql = _sqlGenerator.GenerateGetFunctionInfoScript(databaseName, schemaName, funcName);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No function information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for function '{schemaName}.{funcName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving function information: {ex.Message}"), null);
            }
        }

        public async Task<(OperationResult operationResult, List<string> functions)> GetFunctions(string databaseName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetFunctionsScript(databaseName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No functions found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} function(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving functions: {ex.Message}"), new List<string>());
            }
        }

        public async Task<(OperationResult operationResult, DataTable? functionTestResult)> TestFunction(string databaseName, string funcName, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(funcName))
                    return (OperationResult.ErrorResult("Function name cannot be empty"), null);

                if (string.IsNullOrWhiteSpace(body))
                    return (OperationResult.ErrorResult("Function body cannot be empty"), null);

                var sql = _sqlGenerator.GenerateTestFunctionScript(databaseName, funcName, body);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No results for function test"), null);

                return (OperationResult.SuccessResult($"Function '{funcName}' test executed successfully"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error testing function: {ex.Message}"), null);
            }
        }
        #endregion

        #region Private Methods
        private async Task<SqlConnection> OpenConnectionAsync(string databaseName)
        {
            var connectionString = new SqlConnectionStringBuilder(_config.Server.BuildConnectionString())
            {
                InitialCatalog = databaseName
            }.ConnectionString;

            var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private async Task<bool> ExistsAsync(string databaseName, string sql, params (string Name, string Value)[] parameters)
        {
            using var connection = await OpenConnectionAsync(databaseName);
            using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = _config.Server.CommandTimeout
            };

            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);

            return await command.ExecuteScalarAsync() is not null;
        }

        private Task<bool> SchemaExistsAsync(string databaseName, string schemaName) =>
            ExistsAsync(databaseName, "SELECT 1 FROM sys.schemas WHERE name = @schemaName;", ("@schemaName", schemaName));

        private Task<bool> TableExistsAsync(string databaseName, string schemaName, string tableName) =>
            ExistsAsync(databaseName,
                @"SELECT 1 FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = @tableName;",
                ("@schemaName", schemaName), ("@tableName", tableName));

        private Task<bool> ColumnExistsAsync(string databaseName, string schemaName, string tableName, string columnName) =>
            ExistsAsync(databaseName,
                @"SELECT 1 FROM sys.columns AS c
INNER JOIN sys.tables AS t ON t.object_id = c.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = @tableName AND c.name = @columnName;",
                ("@schemaName", schemaName), ("@tableName", tableName), ("@columnName", columnName));

        private Task<bool> IndexExistsAsync(string databaseName, string schemaName, string tableName, string indexName) =>
            ExistsAsync(databaseName,
                @"SELECT 1 FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = @tableName AND i.index_id > 0 AND i.name = @indexName;",
                ("@schemaName", schemaName), ("@tableName", tableName), ("@indexName", indexName));

        private Task<bool> ForeignKeyExistsAsync(string databaseName, string schemaName, string tableName, string foreignKeyName) =>
            ExistsAsync(databaseName,
                @"SELECT 1 FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = @tableName AND fk.name = @foreignKeyName;",
                ("@schemaName", schemaName), ("@tableName", tableName), ("@foreignKeyName", foreignKeyName));

        private static SchemaOperation.Information MapSchema(string databaseName, DataRow row)
        {
            return new SchemaOperation.Information
            {
                DatabaseName = databaseName,
                SchemaName = Convert.ToString(row["SchemaName"]) ?? string.Empty,
                OwnerName = Convert.ToString(row["OwnerName"]) ?? string.Empty,
                ObjectCount = Convert.ToInt32(row["ObjectCount"])
            };
        }

        private static ColumnOperation.Information MapColumn(TableOperation.Information table, DataRow row)
        {
            var dataType = Convert.ToString(row["DataType"]) ?? string.Empty;
            var type = ColumnDataType.Find(dataType);
            var maxLength = Convert.ToInt32(row["MaxLength"]);
            var isUnicode = dataType.Equals("nchar", StringComparison.OrdinalIgnoreCase) || dataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase);

            return new ColumnOperation.Information
            {
                DatabaseName = table.DatabaseName,
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                ColumnName = Convert.ToString(row["ColumnName"]) ?? string.Empty,
                OrdinalPosition = Convert.ToInt32(row["OrdinalPosition"]),
                DataType = dataType,
                // max_length is in bytes; -1 means MAX.
                Length = type?.Kind == ColumnDataTypeKind.Length ? (isUnicode && maxLength > 0 ? maxLength / 2 : maxLength) : null,
                Precision = type?.Kind == ColumnDataTypeKind.PrecisionScale ? Convert.ToInt32(row["Precision"]) : null,
                Scale = (type?.Kind is ColumnDataTypeKind.PrecisionScale or ColumnDataTypeKind.FractionalSeconds) ? Convert.ToInt32(row["Scale"]) : null,
                IsNullable = Convert.ToBoolean(row["IsNullable"]),
                IsIdentity = Convert.ToBoolean(row["IsIdentity"]),
                IdentitySeed = row["IdentitySeed"] is DBNull ? 0 : Convert.ToInt32(row["IdentitySeed"]),
                IdentityIncrement = row["IdentityIncrement"] is DBNull ? 0 : Convert.ToInt32(row["IdentityIncrement"]),
                IsPrimaryKey = Convert.ToBoolean(row["IsPrimaryKey"]),
                Collation = GetNullableString(row, "Collation"),
                DefaultValue = GetNullableString(row, "DefaultValue"),
                ComputedFormula = GetNullableString(row, "ComputedFormula"),
                IsPersisted = Convert.ToBoolean(row["IsPersisted"]),
                Description = GetNullableString(row, "Description")
            };
        }

        private static IndexOperation.Information MapIndex(TableOperation.Information table, DataRow row)
        {
            return new IndexOperation.Information
            {
                DatabaseName = table.DatabaseName,
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                IndexName = Convert.ToString(row["IndexName"]) ?? string.Empty,
                IndexType = Convert.ToString(row["IndexType"]) ?? string.Empty,
                Columns = GetNullableString(row, "KeyColumns") ?? string.Empty,
                IncludeColumns = GetNullableString(row, "IncludeColumns"),
                IsUnique = Convert.ToBoolean(row["IsUnique"]),
                IsPrimaryKey = Convert.ToBoolean(row["IsPrimaryKey"]),
                IsUniqueConstraint = Convert.ToBoolean(row["IsUniqueConstraint"]),
                FillFactor = Convert.ToInt16(row["FillFactor"]),
                PadIndex = Convert.ToBoolean(row["PadIndex"]),
                AllowRowLocks = Convert.ToBoolean(row["AllowRowLocks"]),
                AllowPageLocks = Convert.ToBoolean(row["AllowPageLocks"]),
                FilterPredicate = GetNullableString(row, "FilterPredicate"),
                FileGroup = GetNullableString(row, "FileGroup") ?? string.Empty
            };
        }

        private static ForeignKeyOperation.Information MapForeignKey(string databaseName, DataRow row)
        {
            return new ForeignKeyOperation.Information
            {
                DatabaseName = databaseName,
                FKName = Convert.ToString(row["FKName"]) ?? string.Empty,
                SchemaName = Convert.ToString(row["SchemaName"]) ?? string.Empty,
                TableName = Convert.ToString(row["TableName"]) ?? string.Empty,
                ColumnName = GetNullableString(row, "ColumnName") ?? string.Empty,
                RefSchemaName = Convert.ToString(row["RefSchemaName"]) ?? string.Empty,
                RefTableName = Convert.ToString(row["RefTableName"]) ?? string.Empty,
                RefColumnName = GetNullableString(row, "RefColumnName") ?? string.Empty,
                OnDelete = (Convert.ToString(row["OnDelete"]) ?? "NO_ACTION").Replace('_', ' '),
                OnUpdate = (Convert.ToString(row["OnUpdate"]) ?? "NO_ACTION").Replace('_', ' '),
                IsNotForReplication = Convert.ToBoolean(row["IsNotForReplication"]),
                Enabled = Convert.ToBoolean(row["Enabled"])
            };
        }

        private static string DescribeSchemaChange(SchemaOperation.Alter config)
        {
            var changes = new List<string>();
            if (config.IsRename)
                changes.Add($"renamed to '{config.NewSchemaName}'");
            if (config.ChangesOwner)
                changes.Add($"owner changed to '{config.Owner}'");

            return $"Schema '{config.SchemaName}' {string.Join(" and ", changes)} in database '{config.DatabaseName}'.";
        }

        private async Task<bool> DatabaseExistsAsync(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;

            using var connection = new SqlConnection(_config.Server.BuildConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT 1 FROM sys.databases WHERE name = @databaseName;", connection)
            {
                CommandTimeout = _config.Server.CommandTimeout
            };
            command.Parameters.AddWithValue("@databaseName", databaseName);

            return await command.ExecuteScalarAsync() is not null;
        }

        private static bool HasDatabaseChanges(DatabaseOperation.Alter config)
        {
            return !string.IsNullOrWhiteSpace(config.Collation)
                || !string.IsNullOrWhiteSpace(config.RecoveryModel)
                || config.CompatibilityLevel.HasValue
                || config.IsReadOnly.HasValue
                || config.AutoShrink.HasValue
                || config.AutoClose.HasValue
                || !string.IsNullOrWhiteSpace(config.PageVerify);
        }

        private async Task<OperationResult> ExecuteDatabaseCommand(string sql, string successMessage)
        {
            using var connection = new SqlConnection(_config.Server.BuildConnectionString());
            await connection.OpenAsync();

            foreach (var batch in SplitSqlBatches(sql))
            {
                using var command = new SqlCommand(batch, connection)
                {
                    CommandTimeout = _config.Server.CommandTimeout
                };
                await command.ExecuteNonQueryAsync();
            }

            return OperationResult.SuccessResult(successMessage);
        }

        private async Task<OperationResult> ExecuteInTransaction(List<string> scripts, string successMessage, string customMessage)
        {
            var connectionString = _config.Server.BuildConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var script in scripts)
                {
                    foreach (var batch in SplitSqlBatches(script))
                    {
                        using var command = new SqlCommand(batch, connection, transaction)
                        {
                            CommandTimeout = _config.Server.CommandTimeout
                        };

                        await command.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();

                return OperationResult.SuccessResult(customMessage);
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // Ignore rollback exceptions
                }
                return OperationResult.ErrorResult($"Transaction failed and was rolled back: {ex.Message}", ex);
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                yield break;

            var batch = new StringBuilder();
            using var reader = new StringReader(sql);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (Regex.IsMatch(line, @"^\s*GO\s*;?\s*$", RegexOptions.IgnoreCase))
                {
                    var text = batch.ToString().Trim();
                    if (text.Length > 0)
                        yield return text;
                    batch.Clear();
                    continue;
                }

                batch.AppendLine(line);
            }

            var finalBatch = batch.ToString().Trim();
            if (finalBatch.Length > 0)
                yield return finalBatch;
        }

        private async Task<DataTable> ExecuteQuery(string sql)
        {
            DataTable result = new();
            try
            {
                var connectionString = _config.Server.BuildConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection)
                {
                    CommandTimeout = _config.Server.CommandTimeout
                };

                using var adapter = new SqlDataAdapter(command);
                adapter.Fill(result);
            }
            catch (Exception exp)
            {
                ExceptionHandler.HandleException(exp);
            }
            return result;
        }

        private async Task<List<string>> ExecuteListQuery(string sql)
        {
            List<string> result = new();
            try
            {
                var connectionString = _config.Server.BuildConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection)
                {
                    CommandTimeout = _config.Server.CommandTimeout
                };

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.HandleException(exp);
            }
            return result;
        }
        #endregion
    }
}