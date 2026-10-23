using System;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Base class for SQL generators providing common provider details and default throwing stubs.
    /// </summary>
    public abstract class SqlGeneratorBase : ISqlGenerator
    {
        public virtual string ProviderName => GetType().Name.Replace("Generator", string.Empty);

        protected virtual string NotSupported(string operationName) =>
            throw new NotImplementedException($"Operation '{operationName}' is not yet implemented for {ProviderName}.");

        #region Database Operations
        public virtual string GenerateCreateDatabaseScript(DatabaseOperation.Create config) => NotSupported(nameof(GenerateCreateDatabaseScript));
        public virtual string GenerateAlterDatabaseScript(DatabaseOperation.Alter config) => NotSupported(nameof(GenerateAlterDatabaseScript));
        public virtual string GenerateDropDatabaseScript(DatabaseOperation.Drop config) => NotSupported(nameof(GenerateDropDatabaseScript));
        public virtual string GenerateGetDatabaseInfoScript(DatabaseOperation.Information config) => NotSupported(nameof(GenerateGetDatabaseInfoScript));
        public virtual string GenerateGetDatabasesScript() => NotSupported(nameof(GenerateGetDatabasesScript));
        public virtual string GenerateGetCollationsScript() => NotSupported(nameof(GenerateGetCollationsScript));
        public virtual string GenerateGetFileGroupsScript() => NotSupported(nameof(GenerateGetFileGroupsScript));
        #endregion

        #region Schema Operations
        public virtual string GenerateCreateSchemaScript(SchemaOperation.Create config) => NotSupported(nameof(GenerateCreateSchemaScript));
        public virtual string GenerateAlterSchemaScript(SchemaOperation.Alter config) => NotSupported(nameof(GenerateAlterSchemaScript));
        public virtual string GenerateDropSchemaScript(SchemaOperation.Drop config) => NotSupported(nameof(GenerateDropSchemaScript));
        public virtual string GenerateGetSchemasScript(string databaseName) => NotSupported(nameof(GenerateGetSchemasScript));
        public virtual string GenerateGetSchemaListScript(string databaseName, string? schemaName = null) => NotSupported(nameof(GenerateGetSchemaListScript));
        public virtual string GenerateGetSchemaInformationScript(SchemaOperation.Information config) => NotSupported(nameof(GenerateGetSchemaInformationScript));
        public virtual string GenerateGetSchemaOwnersScript(string databaseName) => NotSupported(nameof(GenerateGetSchemaOwnersScript));
        #endregion

        #region Table Operations
        public virtual string GenerateCreateTableScript(TableOperation.Create config) => NotSupported(nameof(GenerateCreateTableScript));
        public virtual string GenerateAlterTableScript(TableOperation.Alter config) => NotSupported(nameof(GenerateAlterTableScript));
        public virtual string GenerateDropTableScript(TableOperation.Drop config) => NotSupported(nameof(GenerateDropTableScript));
        public virtual string GenerateGetTableInfoScript(TableOperation.Information config) => NotSupported(nameof(GenerateGetTableInfoScript));
        public virtual string GenerateGetTablesScript(string databaseName, string schemaName) => NotSupported(nameof(GenerateGetTablesScript));
        public virtual string GenerateGetTableListScript(string databaseName) => NotSupported(nameof(GenerateGetTableListScript));
        public virtual string GenerateGetTableColumnsScript(TableOperation.Information config) => NotSupported(nameof(GenerateGetTableColumnsScript));
        public virtual string GenerateGetTableIndexesScript(TableOperation.Information config) => NotSupported(nameof(GenerateGetTableIndexesScript));
        public virtual string GenerateGetTableForeignKeysScript(TableOperation.Information config) => NotSupported(nameof(GenerateGetTableForeignKeysScript));
        public virtual string GenerateGetFileGroupsScript(string databaseName) => NotSupported(nameof(GenerateGetFileGroupsScript));
        #endregion

        #region Column Operations
        public virtual string GenerateCreateColumnScript(ColumnOperation.Create config) => NotSupported(nameof(GenerateCreateColumnScript));
        public virtual string GenerateAlterColumnScript(ColumnOperation.Alter config) => NotSupported(nameof(GenerateAlterColumnScript));
        public virtual string GenerateDropColumnScript(ColumnOperation.Drop config) => NotSupported(nameof(GenerateDropColumnScript));
        public virtual string GenerateGetColumnInfoScript(ColumnOperation.Information config) => NotSupported(nameof(GenerateGetColumnInfoScript));
        public virtual string GenerateGetColumnsScript(string databaseName, string schemaName, string tableName) => NotSupported(nameof(GenerateGetColumnsScript));
        public virtual string GenerateGetDataTypesScript() => NotSupported(nameof(GenerateGetDataTypesScript));
        #endregion

        #region Index Operations
        public virtual string GenerateCreateIndexScript(IndexOperation.Create config) => NotSupported(nameof(GenerateCreateIndexScript));
        public virtual string GenerateAlterIndexScript(IndexOperation.Alter config) => NotSupported(nameof(GenerateAlterIndexScript));
        public virtual string GenerateDropIndexScript(IndexOperation.Drop config) => NotSupported(nameof(GenerateDropIndexScript));
        public virtual string GenerateGetIndexInfoScript(IndexOperation.Information config) => NotSupported(nameof(GenerateGetIndexInfoScript));
        public virtual string GenerateGetIndexNamesScript(string databaseName, string schemaName, string tableName) => NotSupported(nameof(GenerateGetIndexNamesScript));
        public virtual string GenerateGetIndexTypesScript() => NotSupported(nameof(GenerateGetIndexTypesScript));
        #endregion

        #region Foreign Key Operations
        public virtual string GenerateCreateForeignKeyScript(ForeignKeyOperation.Create config) => NotSupported(nameof(GenerateCreateForeignKeyScript));
        public virtual string GenerateAlterForeignKeyScript(ForeignKeyOperation.Alter config) => NotSupported(nameof(GenerateAlterForeignKeyScript));
        public virtual string GenerateDropForeignKeyScript(ForeignKeyOperation.Drop config) => NotSupported(nameof(GenerateDropForeignKeyScript));
        public virtual string GenerateGetForeignKeyInfoScript(ForeignKeyOperation.Information config) => NotSupported(nameof(GenerateGetForeignKeyInfoScript));
        public virtual string GenerateGetReferentialActionsScript() => NotSupported(nameof(GenerateGetReferentialActionsScript));
        #endregion

        #region Check Constraint Operations
        public virtual string GenerateGetTableCheckConstraintsScript(TableOperation.Information config) => NotSupported(nameof(GenerateGetTableCheckConstraintsScript));
        public virtual string GenerateCreateCheckConstraintScript(CheckConstraintOperation.Create config) => NotSupported(nameof(GenerateCreateCheckConstraintScript));
        public virtual string GenerateDropCheckConstraintScript(CheckConstraintOperation.Drop config) => NotSupported(nameof(GenerateDropCheckConstraintScript));
        public virtual string GenerateToggleCheckConstraintScript(CheckConstraintOperation.ToggleState config) => NotSupported(nameof(GenerateToggleCheckConstraintScript));
        #endregion

        #region View Operations
        public virtual string GenerateCreateViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true) => NotSupported(nameof(GenerateCreateViewScript));
        public virtual string GenerateAlterViewScript(string databaseName, string schemaName, string viewName, string selectStatement, bool withEncryption = true) => NotSupported(nameof(GenerateAlterViewScript));
        public virtual string GenerateDropViewScript(string databaseName, string schemaName, string viewName) => NotSupported(nameof(GenerateDropViewScript));
        public virtual string GenerateGetViewInfoScript(string databaseName, string schemaName, string viewName) => NotSupported(nameof(GenerateGetViewInfoScript));
        public virtual string GenerateGetViewsScript(string databaseName) => NotSupported(nameof(GenerateGetViewsScript));
        public virtual string GenerateTestViewScript(string databaseName, string selectStatement) => NotSupported(nameof(GenerateTestViewScript));
        #endregion

        #region Stored Procedure Operations
        public virtual string GenerateCreateProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true) => NotSupported(nameof(GenerateCreateProcedureScript));
        public virtual string GenerateAlterProcedureScript(string databaseName, string schemaName, string procName, string body, bool withEncryption = true) => NotSupported(nameof(GenerateAlterProcedureScript));
        public virtual string GenerateDropProcedureScript(string databaseName, string schemaName, string procName) => NotSupported(nameof(GenerateDropProcedureScript));
        public virtual string GenerateGetProcedureInfoScript(string databaseName, string schemaName, string procName) => NotSupported(nameof(GenerateGetProcedureInfoScript));
        public virtual string GenerateGetProceduresScript(string databaseName) => NotSupported(nameof(GenerateGetProceduresScript));
        public virtual string GenerateTestProcedureScript(string databaseName, string procName, string body) => NotSupported(nameof(GenerateTestProcedureScript));
        #endregion

        #region Function Operations
        public virtual string GenerateCreateFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true) => NotSupported(nameof(GenerateCreateFunctionScript));
        public virtual string GenerateAlterFunctionScript(string databaseName, string schemaName, string funcName, string body, bool withEncryption = true) => NotSupported(nameof(GenerateAlterFunctionScript));
        public virtual string GenerateDropFunctionScript(string databaseName, string schemaName, string funcName) => NotSupported(nameof(GenerateDropFunctionScript));
        public virtual string GenerateGetFunctionInfoScript(string databaseName, string schemaName, string funcName) => NotSupported(nameof(GenerateGetFunctionInfoScript));
        public virtual string GenerateGetFunctionsScript(string databaseName) => NotSupported(nameof(GenerateGetFunctionsScript));
        public virtual string GenerateTestFunctionScript(string databaseName, string funcName, string body) => NotSupported(nameof(GenerateTestFunctionScript));
        #endregion

        #region Trigger Operations
        public virtual string GenerateCreateTriggerScript(TriggerOperation.Create config) => NotSupported(nameof(GenerateCreateTriggerScript));
        public virtual string GenerateAlterTriggerScript(string databaseName, string schemaName, string triggerName, string body, bool withEncryption = true) => NotSupported(nameof(GenerateAlterTriggerScript));
        public virtual string GenerateDropTriggerScript(string databaseName, string schemaName, string triggerName) => NotSupported(nameof(GenerateDropTriggerScript));
        public virtual string GenerateToggleTriggerScript(string databaseName, string schemaName, string triggerName, string tableSchema, string tableName, bool enable) => NotSupported(nameof(GenerateToggleTriggerScript));
        public virtual string GenerateGetTriggerInfoScript(string databaseName, string schemaName, string triggerName) => NotSupported(nameof(GenerateGetTriggerInfoScript));
        public virtual string GenerateGetTriggersScript(string databaseName) => NotSupported(nameof(GenerateGetTriggersScript));
        #endregion

        #region Backup/Restore
        public virtual string GenerateBackupScript(BackupOperation config) => NotSupported(nameof(GenerateBackupScript));
        public virtual string GenerateRestoreScript(RestoreOperation config) => NotSupported(nameof(GenerateRestoreScript));
        #endregion

        #region Utility
        public virtual string GenerateUseDatabaseScript(string databaseName) => NotSupported(nameof(GenerateUseDatabaseScript));
        public virtual string GenerateTransactionBeginScript() => NotSupported(nameof(GenerateTransactionBeginScript));
        public virtual string GenerateTransactionCommitScript() => NotSupported(nameof(GenerateTransactionCommitScript));
        public virtual string GenerateTransactionRollbackScript() => NotSupported(nameof(GenerateTransactionRollbackScript));
        public virtual string GenerateTryCatchBlock(string tryBlock, string catchBlock) => NotSupported(nameof(GenerateTryCatchBlock));
        #endregion
    }
}
