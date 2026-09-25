using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BprDevKit.DatabaseManagement.Config;
using BprDevKit.DatabaseManagement.Models;
using BprDevKit.DatabaseManagement.Services;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Main database operations service that handles transactions and uses SqlGenerator for dynamic SQL generation
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
        public async Task<OperationResult> CreateDatabase(DatabaseProcedure.Create config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateDatabaseScript(config)
                };

                return await ExecuteInTransaction(scripts, "Database created successfully", $"Database '{config.DatabaseName}' created successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating database: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterDatabase(DatabaseProcedure.Alter config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterDatabaseScript(config)
                };

                return await ExecuteInTransaction(scripts, "Database altered successfully", $"Database '{config.DatabaseName}' altered successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering database: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropDatabase(DatabaseProcedure.Drop config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropDatabaseScript(config)
                };

                return await ExecuteInTransaction(scripts, "Database dropped successfully", $"Database '{config.DatabaseName}' dropped successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping database: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetDatabaseInformation(DatabaseProcedure.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetDatabaseInfoScript(config);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No database information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for database '{config.DatabaseName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving database information: {ex.Message}"), null);
            }
        }
        #endregion

        #region Schema Operations
        public async Task<OperationResult> CreateSchema(string databaseName, string schemaName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateSchemaScript(databaseName, schemaName)
                };

                return await ExecuteInTransaction(scripts, "Schema created successfully", $"Schema '{schemaName}' created successfully in database '{databaseName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating schema: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropSchema(string databaseName, string schemaName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return OperationResult.ErrorResult("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(schemaName))
                    return OperationResult.ErrorResult("Schema name cannot be empty");

                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropSchemaScript(databaseName, schemaName)
                };

                return await ExecuteInTransaction(scripts, "Schema dropped successfully", $"Schema '{schemaName}' dropped successfully in database '{databaseName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping schema: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, List<string> schemas)> GetSchemas(string databaseName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (OperationResult.ErrorResult("Database name cannot be empty"), new List<string>());

                var sql = _sqlGenerator.GenerateGetSchemasScript(databaseName);
                var results = await ExecuteListQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No schemas found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} schema(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving schemas: {ex.Message}"), new List<string>());
            }
        }
        #endregion

        #region Table Operations
        public async Task<OperationResult> CreateTable(TableProcedure.Create config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateTableScript(config)
                };

                return await ExecuteInTransaction(scripts, "Table created successfully", $"Table '{config.SchemaName}.{config.TableName}' created successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating table: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterTable(TableProcedure.Alter config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterTableScript(config)
                };

                return await ExecuteInTransaction(scripts, "Table altered successfully", $"Table '{config.SchemaName}.{config.TableName}' altered successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering table: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropTable(TableProcedure.Drop config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropTableScript(config)
                };

                return await ExecuteInTransaction(scripts, "Table dropped successfully", $"Table '{config.SchemaName}.{config.TableName}' dropped successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping table: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetTableInformation(TableProcedure.Information config)
        {
            try
            {
                var sql = _sqlGenerator.GenerateGetTableInfoScript(config);
                var results = await ExecuteQuery(sql);

                if (results == null)
                    return (OperationResult.ErrorResult("No table information found"), null);

                return (OperationResult.SuccessResult($"Retrieved information for table '{config.SchemaName}.{config.TableName}'"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving table information: {ex.Message}"), null);
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

                if (results == null)
                    return (OperationResult.ErrorResult("No tables found"), new List<string>());

                return (OperationResult.SuccessResult($"Retrieved {results.Count} table(s)"), results);
            }
            catch (Exception ex)
            {
                return (OperationResult.ErrorResult($"Error retrieving tables: {ex.Message}"), new List<string>());
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
        public async Task<OperationResult> CreateColumn(ColumnProcedure.Create config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateColumnScript(config)
                };

                return await ExecuteInTransaction(scripts, "Column created successfully", $"Column '{config.ColumnName}' created successfully in table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating column: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterColumn(ColumnProcedure.Alter config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterColumnScript(config)
                };

                return await ExecuteInTransaction(scripts, "Column altered successfully", $"Column '{config.ColumnName}' altered successfully in table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering column: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropColumn(ColumnProcedure.Drop config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropColumnScript(config)
                };

                return await ExecuteInTransaction(scripts, "Column dropped successfully", $"Column '{config.ColumnName}' dropped successfully from table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping column: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetColumnInformation(ColumnProcedure.Information config)
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
        public async Task<OperationResult> CreateIndex(IndexProcedure.Create config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateIndexScript(config)
                };

                return await ExecuteInTransaction(scripts, "Index created successfully", $"Index '{config.IndexName ?? "(auto-generated)"}' created successfully on table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating index: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterIndex(IndexProcedure.Alter config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterIndexScript(config)
                };

                return await ExecuteInTransaction(scripts, "Index altered successfully", $"Index '{config.IndexName}' altered successfully on table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering index: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropIndex(IndexProcedure.Drop config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropIndexScript(config)
                };

                return await ExecuteInTransaction(scripts, "Index dropped successfully", $"Index '{config.IndexName}' dropped successfully from table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping index: {ex.Message}");
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
        public async Task<OperationResult> CreateForeignKey(ForeignKeyProcedure.Create config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateCreateForeignKeyScript(config)
                };

                return await ExecuteInTransaction(scripts, "Foreign key created successfully", $"Foreign key created successfully on table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error creating foreign key: {ex.Message}");
            }
        }

        public async Task<OperationResult> AlterForeignKey(ForeignKeyProcedure.Alter config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateAlterForeignKeyScript(config)
                };

                return await ExecuteInTransaction(scripts, "Foreign key altered successfully", $"Foreign key altered successfully on table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error altering foreign key: {ex.Message}");
            }
        }

        public async Task<OperationResult> DropForeignKey(ForeignKeyProcedure.Drop config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateDropForeignKeyScript(config)
                };

                return await ExecuteInTransaction(scripts, "Foreign key dropped successfully", $"Foreign key dropped successfully from table '{config.SchemaName}.{config.TableName}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error dropping foreign key: {ex.Message}");
            }
        }

        public async Task<(OperationResult operationResult, DataTable? information)> GetForeignKeyInformation(ForeignKeyProcedure.Information config)
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
        public async Task<OperationResult> BackupDatabase(BackupProcedure config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateBackupScript(config)
                };

                return await ExecuteInTransaction(scripts, "Database backed up successfully", $"Database '{config.DatabaseName}' backed up successfully to '{config.BackupPath}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error: {ex.Message}");
            }
        }

        public async Task<OperationResult> RestoreDatabase(RestoreProcedure config)
        {
            try
            {
                var scripts = new List<string>
                {
                    _sqlGenerator.GenerateRestoreScript(config)
                };

                return await ExecuteInTransaction(scripts, "Database restored successfully", $"Database '{config.DatabaseName}' restored successfully from '{config.RestorePath}'");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error: {ex.Message}");
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
        private async Task<OperationResult> ExecuteInTransaction(List<string> scripts, string successMessage, string customMessage)
        {
            var connectionString = _config.Server.BuildConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var script in scripts)
                {
                    using var command = new SqlCommand(script, connection)
                    {
                        CommandTimeout = _config.Server.CommandTimeout
                    };

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return OperationResult.SuccessResult(customMessage);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback exceptions
                }
                return OperationResult.ErrorResult($"Transaction failed and was rolled back: {ex.Message}", ex);
            }
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