using System;
using System.Collections.Generic;
using BprDevKit.DatabaseManagement.Models.Metadata;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// Interface for generating SQL scripts dynamically
    /// </summary>
    public interface ISqlGenerator
    {
        #region Database Operations
        string GenerateCreateDatabaseScript(DatabaseOperation.Create config);
        string GenerateAlterDatabaseScript(DatabaseOperation.Alter config);
        string GenerateDropDatabaseScript(DatabaseOperation.Drop config);
        string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config);
        string GenerateGetDatabasesScript();
        string GenerateGetCollationsScript();
        string GenerateGetFileGroupsScript();
        #endregion

        #region Schema Operations
        string GenerateCreateSchemaScript(SchemaOperation.Create config);
        string GenerateAlterSchemaScript(SchemaOperation.Alter config);
        string GenerateDropSchemaScript(SchemaOperation.Drop config);
        string GenerateGetSchemasScript(string databaseName);
        string GenerateGetSchemaListScript(string databaseName, string? schemaName = null);
        string GenerateGetSchemaInformationScript(SchemaOperation.Information config);
        string GenerateGetSchemaOwnersScript(string databaseName);
        #endregion

        #region Table Operations
        string GenerateCreateTableScript(TableOperation.Create config);
        string GenerateAlterTableScript(TableOperation.Alter config);
        string GenerateDropTableScript(TableOperation.Drop config);
        string GenerateGetTableInfoScript(TableOperation.Information config);
        string GenerateGetTablesScript(string databaseName, string schemaName);
        string GenerateGetTableListScript(string databaseName);
        string GenerateGetTableColumnsScript(TableOperation.Information config);
        string GenerateGetTableIndexesScript(TableOperation.Information config);
        string GenerateGetTableForeignKeysScript(TableOperation.Information config);
        string GenerateGetFileGroupsScript(string databaseName);
        #endregion

        #region Column Operations
        string GenerateCreateColumnScript(ColumnOperation.Create config);
        string GenerateAlterColumnScript(ColumnOperation.Alter config);
        string GenerateDropColumnScript(ColumnOperation.Drop config);
        string GenerateGetColumnInfoScript(ColumnOperation.Information config);
        string GenerateGetColumnsScript(string databaseName, string schemaName, string tableName);
        string GenerateGetDataTypesScript();
        #endregion

        #region Index Operations
        string GenerateCreateIndexScript(IndexOperation.Create config);
        string GenerateAlterIndexScript(IndexOperation.Alter config);
        string GenerateDropIndexScript(IndexOperation.Drop config);
        string GenerateGetIndexInfoScript(IndexOperation.Information config);
        string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName);
        string GenerateGetIndexTypesScript();
        #endregion

        #region Foreign Key Operations
        string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config);
        string GenerateAlterForeignKeyScript(ForeignKeyOperation.Alter config);
        string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config);
        string GenerateGetForeignKeyInfoScript(ForeignKeyOperation.Information config);
        string GenerateGetReferentialActionsScript();
        #endregion

        #region Check Constraint Operations
        string GenerateGetTableCheckConstraintsScript(TableOperation.Information config);
        string GenerateCreateCheckConstraintScript(CheckConstraintOperation.Create config);
        string GenerateDropCheckConstraintScript(CheckConstraintOperation.Drop config);
        string GenerateToggleCheckConstraintScript(CheckConstraintOperation.ToggleState config);
        #endregion

        #region View Operations
        string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true);
        string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true);
        string GenerateDropViewScript(string databaseName, string schemaName, string viewName);
        string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName);
        string GenerateGetViewsScript(string databaseName);
        string GenerateTestViewScript(string databaseName, string selectStatement);
        #endregion

        #region Stored Procedure Operations
        string GenerateCreateProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true);
        string GenerateAlterProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true);
        string GenerateDropProcedureScript(string databaseName, string schemaName, string procName);
        string GenerateGetProcedureInfoScript(string databaseName, string schemaName, string procName);
        string GenerateGetProceduresScript(string databaseName);
        string GenerateTestProcedureScript(string databaseName, string procName, string body);
        #endregion

        #region Function Operations
        string GenerateCreateFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true);
        string GenerateAlterFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true);
        string GenerateDropFunctionScript(string databaseName, string schemaName, string funcName);
        string GenerateGetFunctionInfoScript(string databaseName, string schemaName, string funcName);
        string GenerateGetFunctionsScript(string databaseName);
        string GenerateTestFunctionScript(string databaseName, string funcName, string body);
        #endregion

        #region Trigger Operations
        string GenerateCreateTriggerScript(TriggerOperation.Create config);
        string GenerateAlterTriggerScript(string databaseName, string schemaName, string triggerName, string body, bool withEncryption = true);
        string GenerateDropTriggerScript(string databaseName, string schemaName, string triggerName);
        string GenerateToggleTriggerScript(string databaseName, string schemaName, string triggerName, string tableSchema, string tableName, bool enable);
        string GenerateGetTriggerInfoScript(string databaseName, string schemaName, string triggerName);
        string GenerateGetTriggersScript(string databaseName);
        #endregion

        #region Backup/Restore
        string GenerateBackupScript(BackupOperation config);
        string GenerateRestoreScript(RestoreOperation config);
        #endregion

        #region Utility
        string GenerateUseDatabaseScript(string databaseName);
        string GenerateTransactionBeginScript();
        string GenerateTransactionCommitScript();
        string GenerateTransactionRollbackScript();
        string GenerateTryCatchBlock(string tryBlock, string catchBlock);
        #endregion
    }
}