namespace BprDevKit.Localization
{
    /// <summary>
    /// Constants for translation dictionary keys.
    /// </summary>
    public static class TranslationKeys
    {
        // Common
        public const string AppName = "Common.AppName";
        public const string SqlWorkspace = "Common.SqlWorkspace";
        public const string Home = "Common.Home";
        public const string DatabaseManagement = "Common.DatabaseManagement";
        public const string Settings = "Common.Settings";
        public const string Save = "Common.Save";
        public const string Cancel = "Common.Cancel";
        public const string Delete = "Common.Delete";
        public const string Edit = "Common.Edit";
        public const string Create = "Common.Create";
        public const string Add = "Common.Add";
        public const string Refresh = "Common.Refresh";
        public const string Close = "Common.Close";
        public const string Actions = "Common.Actions";
        public const string Name = "Common.Name";
        public const string Description = "Common.Description";
        public const string Created = "Common.Created";
        public const string LastModified = "Common.LastModified";
        public const string Success = "Common.Success";
        public const string Error = "Common.Error";
        public const string Warning = "Common.Warning";
        public const string Information = "Common.Information";
        public const string ConfirmDelete = "Common.ConfirmDelete";
        public const string ConfirmDeleteText = "Common.ConfirmDeleteText";
        public const string YesDelete = "Common.YesDelete";
        public const string Search = "Common.Search";
        public const string Loading = "Common.Loading";
        public const string Language = "Common.Language";
        public const string Persian = "Common.Persian";
        public const string English = "Common.English";
        public const string Rename = "Common.Rename";
        public const string Remove = "Common.Remove";
        public const string Yes = "Common.Yes";
        public const string No = "Common.No";

        // Navigation & Workspace
        public const string ActiveServer = "Nav.ActiveServer";
        public const string SelectServer = "Nav.SelectServer";
        public const string NoServerSelected = "Nav.NoServerSelected";

        // Database Management Tabs
        public const string Databases = "Db.Databases";
        public const string Schemas = "Db.Schemas";
        public const string Tables = "Db.Tables";
        public const string Views = "Db.Views";
        public const string StoredProcedures = "Db.StoredProcedures";
        public const string Functions = "Db.Functions";
        public const string Columns = "Db.Columns";
        public const string Indexes = "Db.Indexes";
        public const string ForeignKeys = "Db.ForeignKeys";

        // Tables & Columns
        public const string TableName = "Table.Name";
        public const string SchemaName = "Table.Schema";
        public const string ColumnName = "Column.Name";
        public const string DataType = "Column.DataType";
        public const string Length = "Column.Length";
        public const string Precision = "Column.Precision";
        public const string Scale = "Column.Scale";
        public const string AllowNull = "Column.AllowNull";
        public const string IsIdentity = "Column.IsIdentity";
        public const string PrimaryKey = "Column.PrimaryKey";
        public const string DefaultValue = "Column.DefaultValue";
        public const string AutoName = "Column.AutoName";

        // Server Settings
        public const string ServerSettings = "Server.Settings";
        public const string AddNewServer = "Server.AddNewServer";
        public const string ApplicationSettings = "Server.ApplicationSettings";
        public const string ServerName = "Server.Name";
        public const string ServerAlias = "Server.Alias";
        public const string DatabaseEngine = "Server.DatabaseEngine";
        public const string DefaultDatabase = "Server.DefaultDatabase";
        public const string Authentication = "Server.Authentication";
        public const string WindowsAuth = "Server.WindowsAuth";
        public const string SqlAuth = "Server.SqlAuth";
        public const string Username = "Server.Username";
        public const string Password = "Server.Password";
        public const string Port = "Server.Port";
        public const string ConnectionTimeout = "Server.ConnectionTimeout";
        public const string CommandTimeout = "Server.CommandTimeout";
        public const string TrustServerCertificate = "Server.TrustServerCertificate";
        public const string TestConnection = "Server.TestConnection";
        public const string SaveServerConfig = "Server.SaveServerConfig";
        public const string SaveAppSettings = "Server.SaveAppSettings";
        public const string Logging = "Server.Logging";
        public const string LogLevel = "Server.LogLevel";
        public const string EnableConsoleLogging = "Server.EnableConsoleLogging";
        public const string EnableFileLogging = "Server.EnableFileLogging";
        public const string Enable = "Common.Enable";
        public const string Disable = "Common.Disable";
        public const string SaveChanges = "Common.SaveChanges";
        public const string Database = "Common.Database";
        public const string Type = "Common.Type";
        public const string ServerConfigWorkspace = "Server.ConfigWorkspace";
        public const string ServerTitle = "Server.Title";
        public const string ServerSubtitle = "Server.Subtitle";
        public const string ServerNoServersFound = "Server.NoServersFound";
        public const string ServerOrAlias = "Server.ServerOrAlias";
        public const string ServerSqliteNotice = "Server.SqliteNotice";
        public const string ServerWindowsAuthSwitch = "Server.WindowsAuthSwitch";
        public const string ServerGeneralLanguageTab = "Server.GeneralLanguageTab";
        public const string ServerLanguageHelp = "Server.LanguageHelp";
        public const string ServerConnectionTab = "Server.ConnectionTab";
        public const string ServerDatabaseDefaultsTab = "Server.DatabaseDefaultsTab";
        public const string ServerEditTitle = "Server.EditTitle";
        public const string ServerNewTitle = "Server.NewTitle";
        public const string ServerMaxSchemaName = "Server.MaxSchemaName";
        public const string ServerMaxTableName = "Server.MaxTableName";
        public const string ServerMaxColumnName = "Server.MaxColumnName";
        public const string ServerMaxIndexName = "Server.MaxIndexName";
        public const string ServerMaxForeignKeyName = "Server.MaxForeignKeyName";
        public const string ServerFileManagementDefaults = "Server.FileManagementDefaults";
        public const string ServerDataFileSize = "Server.DataFileSize";
        public const string ServerLogFileSize = "Server.LogFileSize";
        public const string ServerFileGrowth = "Server.FileGrowth";
        public const string ServerLogGrowth = "Server.LogGrowth";
        public const string ServerMaxLogFileSize = "Server.MaxLogFileSize";
        public const string ServerLogRetentionDays = "Server.LogRetentionDays";
        public const string ServerActionUndoneWarning = "Server.ActionUndoneWarning";
        public const string PersianFont = "Server.PersianFont";
        public const string EnglishFont = "Server.EnglishFont";
        public const string FontSettingsHelp = "Server.FontSettingsHelp";

        // Home Page
        public const string HomeEyebrow = "Home.Eyebrow";
        public const string HomeTitle = "Home.Title";
        public const string HomeSubtitle = "Home.Subtitle";
        public const string HomeDbCardDesc = "Home.DbCardDesc";
        public const string HomeSettingsCardDesc = "Home.SettingsCardDesc";

        // Database Management Workspace Parent
        public const string DbSubtitle = "Db.Subtitle";
        public const string DbServerSelected = "Db.ServerSelected";
        public const string DbServerConfigLabel = "Db.ServerConfigLabel";
        public const string DbSelectServerPlaceholder = "Db.SelectServerPlaceholder";
        public const string DbClearServer = "Db.ClearServer";
        public const string DbLoadingServers = "Db.LoadingServers";
        public const string DbNoServersWarning = "Db.NoServersWarning";
        public const string DbSelectServerToBegin = "Db.SelectServerToBegin";
        public const string DbTabsUnavailableNotice = "Db.TabsUnavailableNotice";
        public const string DbAreaUnderDevelopment = "Db.AreaUnderDevelopment";
        public const string DbAreaUnderDevelopmentDesc = "Db.AreaUnderDevelopmentDesc";

        // Database Management Tab
        public const string DbTabSubtitle = "DbTab.Subtitle";
        public const string DbTabCreateDb = "DbTab.CreateDb";
        public const string DbTabCollation = "DbTab.Collation";
        public const string DbTabOptional = "DbTab.Optional";
        public const string DbTabServerDefault = "DbTab.ServerDefault";
        public const string DbTabDataDirectory = "DbTab.DataDirectory";
        public const string DbTabDataDirHelp = "DbTab.DataDirHelp";
        public const string DbTabLogDirectory = "DbTab.LogDirectory";
        public const string DbTabLogDirHelp = "DbTab.LogDirHelp";
        public const string DbTabDataFile = "DbTab.DataFile";
        public const string DbTabLogFile = "DbTab.LogFile";
        public const string DbTabInitialSizeMB = "DbTab.InitialSizeMB";
        public const string DbTabMaxSizeMB = "DbTab.MaxSizeMB";
        public const string DbTabUnlimited = "DbTab.Unlimited";
        public const string DbTabGrowthMB = "DbTab.GrowthMB";
        public const string DbTabDbOptions = "DbTab.DbOptions";
        public const string DbTabRecoveryModel = "DbTab.RecoveryModel";
        public const string DbTabRecoverySimple = "DbTab.RecoverySimple";
        public const string DbTabRecoveryFull = "DbTab.RecoveryFull";
        public const string DbTabRecoveryBulkLogged = "DbTab.RecoveryBulkLogged";
        public const string DbTabPageVerify = "DbTab.PageVerify";
        public const string DbTabPageVerifyChecksum = "DbTab.PageVerifyChecksum";
        public const string DbTabPageVerifyTornPage = "DbTab.PageVerifyTornPage";
        public const string DbTabPageVerifyNone = "DbTab.PageVerifyNone";
        public const string DbTabCompatibilityLevel = "DbTab.CompatibilityLevel";
        public const string DbTabAdditionalFilegroup = "DbTab.AdditionalFilegroup";
        public const string DbTabFilegroupHelp = "DbTab.FilegroupHelp";
        public const string DbTabReadOnly = "DbTab.ReadOnly";
        public const string DbTabAutoShrink = "DbTab.AutoShrink";
        public const string DbTabAutoClose = "DbTab.AutoClose";
        public const string DbTabLoadingDatabases = "DbTab.LoadingDatabases";
        public const string DbTabNoDatabasesFound = "DbTab.NoDatabasesFound";
        public const string DbTabViewDetails = "DbTab.ViewDetails";
        public const string DbTabEditOptions = "DbTab.EditOptions";
        public const string DbTabDeleteDb = "DbTab.DeleteDb";
        public const string DbTabEditTitle = "DbTab.EditTitle";
        public const string DbTabKeepCurrentValue = "DbTab.KeepCurrentValue";
        public const string DbTabKeepCurrentValueHelp = "DbTab.KeepCurrentValueHelp";
        public const string DbTabDeleteConfirm = "DbTab.DeleteConfirm";
        public const string DbTabForceDisconnect = "DbTab.ForceDisconnect";
        public const string DbTabIgnoreIfNotExists = "DbTab.IgnoreIfNotExists";
        public const string DbTabDetailsTitle = "DbTab.DetailsTitle";

        // Schema Management Tab
        public const string SchemaTabSubtitle = "SchemaTab.Subtitle";
        public const string SchemaTabCreateSchema = "SchemaTab.CreateSchema";
        public const string SchemaTabSelectDbPrompt = "SchemaTab.SelectDbPrompt";
        public const string SchemaTabOwner = "SchemaTab.Owner";
        public const string SchemaTabDefaultOwner = "SchemaTab.DefaultOwner";
        public const string SchemaTabSelectDbToManage = "SchemaTab.SelectDbToManage";
        public const string SchemaTabLoadingSchemas = "SchemaTab.LoadingSchemas";
        public const string SchemaTabNoSchemasFound = "SchemaTab.NoSchemasFound";
        public const string SchemaTabTransferObjects = "SchemaTab.TransferObjects";
        public const string SchemaTabRename = "SchemaTab.Rename";
        public const string SchemaTabNewName = "SchemaTab.NewName";
        public const string SchemaTabCreateInDb = "SchemaTab.CreateInDb";
        public const string SchemaTabBuiltIn = "SchemaTab.BuiltIn";
        public const string SchemaTabViewObjects = "SchemaTab.ViewObjects";
        public const string SchemaTabEditHeading = "SchemaTab.EditHeading";
        public const string SchemaTabRenameWarning = "SchemaTab.RenameWarning";
        public const string SchemaTabCheckingContents = "SchemaTab.CheckingContents";
        public const string SchemaTabContainsObjectsWarning = "SchemaTab.ContainsObjectsWarning";
        public const string SchemaTabDeleteConfirm = "SchemaTab.DeleteConfirm";
        public const string SchemaTabObjectsInSchema = "SchemaTab.ObjectsInSchema";
        public const string SchemaTabLoadingObjects = "SchemaTab.LoadingObjects";
        public const string SchemaTabObjects = "SchemaTab.Objects";

        // SchemaObjectList
        public const string SchemaObjNoObjects = "SchemaObj.NoObjects";

        // Table Management Tab & Details
        public const string TableTabSubtitle = "TableTab.Subtitle";
        public const string TableTabCreateTable = "TableTab.CreateTable";
        public const string TableTabAllSchemas = "TableTab.AllSchemas";
        public const string TableTabSearchPlaceholder = "TableTab.SearchPlaceholder";
        public const string TableTabFileGroup = "TableTab.FileGroup";
        public const string TableTabDbDefault = "TableTab.DbDefault";
        public const string TableTabSelectDbToManage = "TableTab.SelectDbToManage";
        public const string TableTabLoadingTables = "TableTab.LoadingTables";
        public const string TableTabNoTablesFound = "TableTab.NoTablesFound";
        public const string TableTabNoMatchFilter = "TableTab.NoMatchFilter";
        public const string TableTabTableDetails = "TableTab.TableDetails";
        public const string TableTabCreateInDb = "TableTab.CreateInDb";
        public const string TableTabRows = "TableTab.Rows";
        public const string TableTabRenameOrDesc = "TableTab.RenameOrDesc";
        public const string TableTabDropTable = "TableTab.DropTable";
        public const string TableTabRowCountNote = "TableTab.RowCountNote";
        public const string TableTabLoadingDetails = "TableTab.LoadingDetails";
        public const string TableTabEditTitle = "TableTab.EditTitle";
        public const string TableTabClearDescHelp = "TableTab.ClearDescHelp";
        public const string TableTabRenameWarning = "TableTab.RenameWarning";
        public const string TableTabCheckingDependencies = "TableTab.CheckingDependencies";
        public const string TableTabDeleteConfirm = "TableTab.DeleteConfirm";
        public const string TableTabFkDependencyWarning = "TableTab.FkDependencyWarning";
        public const string TableTabDropDependentFks = "TableTab.DropDependentFks";

        // Panels: Columns, Indexes, ForeignKeys
        public const string ColPanelColumns = "ColPanel.Columns";
        public const string ColPanelAddColumn = "ColPanel.AddColumn";
        public const string ColPanelEditColumn = "ColPanel.EditColumn";
        public const string ColPanelCannotChangeType = "ColPanel.CannotChangeType";
        public const string ColPanelSize = "ColPanel.Size";
        public const string ColPanelDimensions = "ColPanel.Dimensions";
        public const string ColPanelDefaultExpr = "ColPanel.DefaultExpr";
        public const string ColPanelNotNullWarning = "ColPanel.NotNullWarning";
        public const string ColPanelAlterWarning = "ColPanel.AlterWarning";
        public const string ColPanelDropTitle = "ColPanel.DropTitle";
        public const string ColPanelDropWarning = "ColPanel.DropWarning";
        public const string ColPanelDropConfirmType = "ColPanel.DropConfirmType";
        public const string ColPanelDropButton = "ColPanel.DropButton";
        public const string ColPanelExtras = "ColPanel.Extras";
        public const string ColPanelDefaultFormula = "ColPanel.DefaultFormula";
        public const string ColPanelComputed = "ColPanel.Computed";
        public const string ColPanelPersisted = "ColPanel.Persisted";

        public const string IdxPanelIndexes = "IdxPanel.Indexes";
        public const string IdxPanelCreateIndex = "IdxPanel.CreateIndex";
        public const string IdxPanelKind = "IdxPanel.Kind";
        public const string IdxPanelIndex = "IdxPanel.Index";
        public const string IdxPanelPrimaryKey = "IdxPanel.PrimaryKey";
        public const string IdxPanelUnique = "IdxPanel.Unique";
        public const string IdxPanelClustered = "IdxPanel.Clustered";
        public const string IdxPanelKeyColumns = "IdxPanel.KeyColumns";
        public const string IdxPanelAddKeyColumn = "IdxPanel.AddKeyColumn";
        public const string IdxPanelIncludedColumns = "IdxPanel.IncludedColumns";
        public const string IdxPanelAllColumnsKey = "IdxPanel.AllColumnsKey";
        public const string IdxPanelFilter = "IdxPanel.Filter";
        public const string IdxPanelFillFactor = "IdxPanel.FillFactor";
        public const string IdxPanelTableFileGroup = "IdxPanel.TableFileGroup";
        public const string IdxPanelPkWarning = "IdxPanel.PkWarning";
        public const string IdxPanelDropTitle = "IdxPanel.DropTitle";
        public const string IdxPanelDropClusteredWarning = "IdxPanel.DropClusteredWarning";
        public const string IdxPanelDropRegularWarning = "IdxPanel.DropRegularWarning";
        public const string IdxPanelDropButton = "IdxPanel.DropButton";
        public const string IdxPanelNoIndexes = "IdxPanel.NoIndexes";
        public const string IdxPanelIncluded = "IdxPanel.Included";
        public const string IdxPanelFill = "IdxPanel.Fill";
        public const string IdxPanelRenameTitle = "IdxPanel.RenameTitle";
        public const string IdxPanelNewName = "IdxPanel.NewName";
        public const string IdxPanelKeyConstraintWarning = "IdxPanel.KeyConstraintWarning";
        public const string IdxPanelUniqueConstraint = "IdxPanel.UniqueConstraint";
        public const string IdxPanelType = "IdxPanel.Type";

        public const string FkPanelForeignKeys = "FkPanel.ForeignKeys";
        public const string FkPanelCreateFk = "FkPanel.CreateFk";
        public const string FkPanelReferencedTable = "FkPanel.ReferencedTable";
        public const string FkPanelSelectTablePrompt = "FkPanel.SelectTablePrompt";
        public const string FkPanelColumnPairs = "FkPanel.ColumnPairs";
        public const string FkPanelAddPair = "FkPanel.AddPair";
        public const string FkPanelOnDelete = "FkPanel.OnDelete";
        public const string FkPanelOnUpdate = "FkPanel.OnUpdate";
        public const string FkPanelNoAction = "FkPanel.NoAction";
        public const string FkPanelCascade = "FkPanel.Cascade";
        public const string FkPanelSetNull = "FkPanel.SetNull";
        public const string FkPanelSetDefault = "FkPanel.SetDefault";
        public const string FkPanelCreateOn = "FkPanel.CreateOn";
        public const string FkPanelSelectCol = "FkPanel.SelectCol";
        public const string FkPanelSelectRefCol = "FkPanel.SelectRefCol";
        public const string FkPanelCreateDisabled = "FkPanel.CreateDisabled";
        public const string FkPanelNote = "FkPanel.Note";
        public const string FkPanelDropTitle = "FkPanel.DropTitle";
        public const string FkPanelDropWarning = "FkPanel.DropWarning";
        public const string FkPanelNoFks = "FkPanel.NoFks";
        public const string FkPanelStatus = "FkPanel.Status";
        public const string FkPanelEnabled = "FkPanel.Enabled";
        public const string FkPanelDisabled = "FkPanel.Disabled";
        public const string FkPanelUntrusted = "FkPanel.Untrusted";
        public const string FkPanelDirection = "FkPanel.Direction";
        public const string FkPanelRelation = "FkPanel.Relation";
        public const string FkPanelOutgoing = "FkPanel.Outgoing";
        public const string FkPanelIncoming = "FkPanel.Incoming";
        public const string FkPanelEnable = "FkPanel.Enable";
        public const string FkPanelDisable = "FkPanel.Disable";
    }
}
