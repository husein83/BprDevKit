using System;
using System.Collections.Generic;
using BprDevKit.DatabaseManagement.Models;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Interface for generating SQL scripts dynamically
    /// </summary>
    public interface ISqlGenerator
    {
        #region Database Operations
        string GenerateCreateDatabaseScript(DatabaseProcedure.Create config);
        string GenerateAlterDatabaseScript(DatabaseProcedure.Alter config);
        string GenerateDropDatabaseScript(DatabaseProcedure.Drop config);
        string GenerateGetDatabaseInfoScript(DatabaseProcedure.Information config);
        string GenerateGetDatabasesScript();
        string GenerateGetCollationsScript();
        string GenerateGetFileGroupsScript();
        #endregion

        #region Schema Operations
        string GenerateCreateSchemaScript(string databaseName, string schemaName);
        string GenerateDropSchemaScript(string databaseName, string schemaName);
        string GenerateGetSchemasScript(string databaseName);
        string GenerateGetSchemaObjectsScript(string databaseName, string schemaName);
        string GenerateRenameSchemaScript(string databaseName, string oldSchemaName, string newSchemaName);
        #endregion

        #region Table Operations
        string GenerateCreateTableScript(TableProcedure.Create config);
        string GenerateAlterTableScript(TableProcedure.Alter config);
        string GenerateDropTableScript(TableProcedure.Drop config);
        string GenerateGetTableInfoScript(TableProcedure.Information config);
        string GenerateGetTablesScript(string databaseName, string schemaName);
        string GenerateGetColumnsScript(string databaseName, string schemaName, string tableName);
        #endregion

        #region Column Operations
        string GenerateCreateColumnScript(ColumnProcedure.Create config);
        string GenerateAlterColumnScript(ColumnProcedure.Alter config);
        string GenerateDropColumnScript(ColumnProcedure.Drop config);
        string GenerateGetColumnInfoScript(ColumnProcedure.Information config);
        string GenerateGetDataTypesScript();
        #endregion

        #region Index Operations
        string GenerateCreateIndexScript(IndexProcedure.Create config);
        string GenerateAlterIndexScript(IndexProcedure.Alter config);
        string GenerateDropIndexScript(IndexProcedure.Drop config);
        string GenerateGetIndexInfoScript(IndexProcedure.Information config);
        string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName);
        string GenerateGetIndexTypesScript();
        #endregion

        #region Foreign Key Operations
        string GenerateCreateForeignKeyScript(ForeignKeyProcedure.Create config);
        string GenerateAlterForeignKeyScript(ForeignKeyProcedure.Alter config);
        string GenerateDropForeignKeyScript(ForeignKeyProcedure.Drop config);
        string GenerateGetForeignKeyInfoScript(ForeignKeyProcedure.Information config);
        string GenerateGetReferentialActionsScript();
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

        #region Backup/Restore
        string GenerateBackupScript(BackupProcedure config);
        string GenerateRestoreScript(RestoreProcedure config);
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