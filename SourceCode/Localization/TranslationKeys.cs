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
        public const string Status = "Common.Status";

        // Navigation & Workspace
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

        // Configuration Setting
        public const string ConfigurationSettingTitle = "ConfigSetting.Title";
        public const string ConfigurationSettingSubtitle = "ConfigSetting.Subtitle";
        public const string ConfigurationSettingDashboard = "ConfigSetting.Dashboard";
        public const string ServerConfigurationTab = "ConfigSetting.ServerTab";
        public const string ApplicationConfigurationTab = "ConfigSetting.ApplicationTab";
        public const string ModelGeneratorConfigurationTab = "ConfigSetting.ModelGeneratorTab";
        public const string ActiveLanguageAndLogs = "ConfigSetting.ActiveLanguageAndLogs";
        public const string ModelProfilesCount = "ConfigSetting.ModelProfilesCount";

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
        public const string Select = "Common.Select";
        public const string Database = "Common.Database";
        public const string Server = "Common.Server";
        public const string Type = "Common.Type";
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
        public const string ServerAdvancedSettings = "Server.AdvancedSettings";
        public const string ServerPooling = "Server.Pooling";
        public const string ServerMinPoolSize = "Server.MinPoolSize";
        public const string ServerMaxPoolSize = "Server.MaxPoolSize";
        public const string ServerEncryptConnection = "Server.EncryptConnection";
        public const string ServerTestingConnection = "Server.TestingConnection";
        public const string ServerTestConnectionFailed = "Server.TestConnectionFailed";
        public const string ServerVersion = "Server.Version";

        // Home Page
        public const string HomeEyebrow = "Home.Eyebrow";
        public const string HomeTitle = "Home.Title";
        public const string HomeSubtitle = "Home.Subtitle";
        public const string HomeDbCardDesc = "Home.DbCardDesc";
        public const string HomeScriptCardDesc = "Home.ScriptCardDesc";
        public const string HomeSettingsCardDesc = "Home.SettingsCardDesc";
        public const string HomeLogsCardDesc = "Home.LogsCardDesc";

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
        public const string TableTabFileGroup = "TableTab.FileGroup";
        public const string TableTabDbDefault = "TableTab.DbDefault";
        public const string TableTabSelectDbToManage = "TableTab.SelectDbToManage";
        public const string TableTabLoadingTables = "TableTab.LoadingTables";
        public const string TableTabNoTablesFound = "TableTab.NoTablesFound";
        public const string TableTabNoMatchFilter = "TableTab.NoMatchFilter";
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
        public const string ColPanelAddColumn = "ColPanel.AddColumn";
        public const string ColPanelEditColumn = "ColPanel.EditColumn";
        public const string ColPanelCannotChangeType = "ColPanel.CannotChangeType";
        public const string ColPanelSize = "ColPanel.Size";
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
        public const string IdxPanelDropRegularWarning = "IdxPanel.DropRegularWarning";
        public const string IdxPanelDropButton = "IdxPanel.DropButton";
        public const string IdxPanelNoIndexes = "IdxPanel.NoIndexes";
        public const string IdxPanelIncluded = "IdxPanel.Included";
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
        public const string FkPanelCreateOn = "FkPanel.CreateOn";
        public const string FkPanelSelectCol = "FkPanel.SelectCol";
        public const string FkPanelSelectRefCol = "FkPanel.SelectRefCol";
        public const string FkPanelCreateDisabled = "FkPanel.CreateDisabled";
        public const string FkPanelNote = "FkPanel.Note";
        public const string FkPanelDropTitle = "FkPanel.DropTitle";
        public const string FkPanelDropWarning = "FkPanel.DropWarning";
        public const string FkPanelNoFks = "FkPanel.NoFks";
        public const string FkPanelEnabled = "FkPanel.Enabled";
        public const string FkPanelDisabled = "FkPanel.Disabled";
        public const string FkPanelDirection = "FkPanel.Direction";
        public const string FkPanelRelation = "FkPanel.Relation";
        public const string FkPanelOutgoing = "FkPanel.Outgoing";
        public const string FkPanelIncoming = "FkPanel.Incoming";
        public const string FkPanelEnable = "FkPanel.Enable";
        public const string FkPanelDisable = "FkPanel.Disable";

        // Check Constraints Panel
        public const string CheckConstraints = "CheckConstraints";
        public const string CheckConstraintsCreate = "CheckConstraints.Create";
        public const string CheckConstraintsCreateOn = "CheckConstraints.CreateOn";
        public const string CheckConstraintsExpression = "CheckConstraints.Expression";
        public const string CheckConstraintsExpressionPlaceholder = "CheckConstraints.ExpressionPlaceholder";
        public const string CheckConstraintsNotForReplication = "CheckConstraints.NotForReplication";
        public const string CheckConstraintsNoConstraints = "CheckConstraints.NoConstraints";
        public const string CheckConstraintsDropTitle = "CheckConstraints.DropTitle";
        public const string CheckConstraintsDropWarning = "CheckConstraints.DropWarning";
        public const string CheckConstraintsEnable = "CheckConstraints.Enable";
        public const string CheckConstraintsDisable = "CheckConstraints.Disable";
        public const string CheckConstraintsStatus = "CheckConstraints.Status";

        // View Management Tab
        public const string ViewTabSubtitle = "ViewTab.Subtitle";
        public const string ViewTabCreateView = "ViewTab.CreateView";
        public const string ViewTabSelectDbToManage = "ViewTab.SelectDbToManage";
        public const string ViewTabLoadingViews = "ViewTab.LoadingViews";
        public const string ViewTabNoViewsFound = "ViewTab.NoViewsFound";
        public const string ViewTabNoMatchFilter = "ViewTab.NoMatchFilter";
        public const string ViewTabDefinition = "ViewTab.Definition";
        public const string ViewTabWithEncryption = "ViewTab.WithEncryption";
        public const string ViewTabTestQuery = "ViewTab.TestQuery";
        public const string ViewTabDropTitle = "ViewTab.DropTitle";
        public const string ViewTabDropConfirm = "ViewTab.DropConfirm";
        public const string ViewTabTestResults = "ViewTab.TestResults";

        // Stored Procedure Management Tab
        public const string ProcTabSubtitle = "ProcTab.Subtitle";
        public const string ProcTabCreateProc = "ProcTab.CreateProc";
        public const string ProcTabSelectDbToManage = "ProcTab.SelectDbToManage";
        public const string ProcTabLoadingProcs = "ProcTab.LoadingProcs";
        public const string ProcTabNoProcsFound = "ProcTab.NoProcsFound";
        public const string ProcTabNoMatchFilter = "ProcTab.NoMatchFilter";
        public const string ProcTabBody = "ProcTab.Body";
        public const string ProcTabWithEncryption = "ProcTab.WithEncryption";
        public const string ProcTabDropTitle = "ProcTab.DropTitle";
        public const string ProcTabDropConfirm = "ProcTab.DropConfirm";

        // Function Management Tab
        public const string FuncTabSubtitle = "FuncTab.Subtitle";
        public const string FuncTabCreateFunc = "FuncTab.CreateFunc";
        public const string FuncTabSelectDbToManage = "FuncTab.SelectDbToManage";
        public const string FuncTabLoadingFuncs = "FuncTab.LoadingFuncs";
        public const string FuncTabNoFuncsFound = "FuncTab.NoFuncsFound";
        public const string FuncTabNoMatchFilter = "FuncTab.NoMatchFilter";
        public const string FuncTabBody = "FuncTab.Body";
        public const string FuncTabWithEncryption = "FuncTab.WithEncryption";
        public const string FuncTabDropTitle = "FuncTab.DropTitle";
        public const string FuncTabDropConfirm = "FuncTab.DropConfirm";

        // Decryption & Encryption
        public const string CommonEncrypted = "Common.Encrypted";
        public const string CommonDecrypted = "Common.Decrypted";
        public const string CommonEncryptedNotice = "Common.EncryptedNotice";
        public const string CommonDecryptObject = "Common.DecryptObject";
        public const string CommonDecrypting = "Common.Decrypting";
        public const string CommonDecryptedSuccess = "Common.DecryptedSuccess";
        public const string CommonDecryptionFailed = "Common.DecryptionFailed";
        public const string CommonDacRequiredPrompt = "Common.DacRequiredPrompt";
        public const string CommonEnableRemoteDac = "Common.EnableRemoteDac";
        public const string CommonEnablingRemoteDac = "Common.EnablingRemoteDac";
        public const string CommonRemoteDacEnabled = "Common.RemoteDacEnabled";
        public const string CommonCannotSaveEncryptedEmpty = "Common.CannotSaveEncryptedEmpty";
        public const string CommonType = "Common.Type";
        public const string CommonCopy = "Common.Copy";
        public const string CommonCopied = "Common.Copied";
        public const string CommonFullScreen = "Common.FullScreen";
        public const string CommonExitFullScreen = "Common.ExitFullScreen";
        public const string Clear = "Common.Clear";

        // Triggers Management Tab
        public const string Triggers = "Triggers";
        public const string TriggerTabSubtitle = "TriggerTab.Subtitle";
        public const string TriggerTabCreateTrigger = "TriggerTab.CreateTrigger";
        public const string TriggerTabTableName = "TriggerTab.TableName";
        public const string TriggerTabTiming = "TriggerTab.Timing";
        public const string TriggerTabEvents = "TriggerTab.Events";
        public const string TriggerTabEnabled = "TriggerTab.Enabled";
        public const string TriggerTabDisabled = "TriggerTab.Disabled";
        public const string TriggerTabEnable = "TriggerTab.Enable";
        public const string TriggerTabDisable = "TriggerTab.Disable";
        public const string TriggerTabNoTriggersFound = "TriggerTab.NoTriggersFound";
        public const string TriggerTabNoMatchFilter = "TriggerTab.NoMatchFilter";
        public const string TriggerTabConfirmDrop = "TriggerTab.ConfirmDrop";
        public const string TriggerTabBody = "TriggerTab.Body";
        public const string TriggerTabWithEncryption = "TriggerTab.WithEncryption";
        public const string TriggerTabDropTitle = "TriggerTab.DropTitle";

        // Logs & Audit
        public const string Logs = "Nav.Logs";
        public const string LogsTitle = "Logs.Title";
        public const string LogsSubtitle = "Logs.Subtitle";
        public const string LogsLiveStream = "Logs.LiveStream";
        public const string LogsAutoScroll = "Logs.AutoScroll";
        public const string LogsClear = "Logs.Clear";
        public const string LogsOpenFolder = "Logs.OpenFolder";
        public const string LogsAllLevels = "Logs.AllLevels";
        public const string LogsLevel = "Logs.Level";
        public const string LogsSource = "Logs.Source";
        public const string LogsMessage = "Logs.Message";
        public const string LogsTime = "Logs.Time";
        public const string LogsServer = "Logs.Server";
        public const string LogsDatabase = "Logs.Database";
        public const string LogsDuration = "Logs.Duration";
        public const string LogsDetails = "Logs.Details";
        public const string LogsNoLogs = "Logs.NoLogs";
        public const string LogsTotalCount = "Logs.TotalCount";
        public const string LogsLiveSession = "Logs.LiveSession";
        public const string LogsImport = "Logs.Import";
        public const string LogsViewingFile = "Logs.ViewingFile";

        // SQL Script Generator
        public const string ScriptGenerator = "Nav.ScriptGenerator";
        public const string ScriptGenTitle = "ScriptGen.Title";
        public const string ScriptGenSubtitle = "ScriptGen.Subtitle";
        public const string ScriptGenSelectServer = "ScriptGen.SelectServer";
        public const string ScriptGenSelectDatabase = "ScriptGen.SelectDatabase";
        public const string ScriptGenSelectDatabasePrompt = "ScriptGen.SelectDatabasePrompt";
        public const string ScriptGenScriptMode = "ScriptGen.ScriptMode";
        public const string ScriptGenSchemaOnly = "ScriptGen.SchemaOnly";
        public const string ScriptGenDataOnly = "ScriptGen.DataOnly";
        public const string ScriptGenSchemaAndData = "ScriptGen.SchemaAndData";
        public const string ScriptGenOptions = "ScriptGen.Options";
        public const string ScriptGenIncludeIfNotExists = "ScriptGen.IncludeIfNotExists";
        public const string ScriptGenIncludeDropAndCreate = "ScriptGen.IncludeDropAndCreate";
        public const string ScriptGenIncludeForeignKeys = "ScriptGen.IncludeForeignKeys";
        public const string ScriptGenIncludeIndexes = "ScriptGen.IncludeIndexes";
        public const string ScriptGenIncludeCheckConstraints = "ScriptGen.IncludeCheckConstraints";
        public const string ScriptGenIncludeTriggers = "ScriptGen.IncludeTriggers";
        public const string ScriptGenIncludeComments = "ScriptGen.IncludeComments";
        public const string ScriptGenIncludeUseDatabase = "ScriptGen.IncludeUseDatabase";
        public const string ScriptGenMaxRows = "ScriptGen.MaxRows";
        public const string ScriptGenUnlimitedRows = "ScriptGen.UnlimitedRows";
        public const string ScriptGenSelectObjects = "ScriptGen.SelectObjects";
        public const string ScriptGenSelectAll = "ScriptGen.SelectAll";
        public const string ScriptGenDeselectAll = "ScriptGen.DeselectAll";
        public const string ScriptGenGenerateButton = "ScriptGen.GenerateButton";
        public const string ScriptGenGenerating = "ScriptGen.Generating";
        public const string ScriptGenGeneratedSuccess = "ScriptGen.GeneratedSuccess";
        public const string ScriptGenCopyClipboard = "ScriptGen.CopyClipboard";
        public const string ScriptGenCopied = "ScriptGen.Copied";
        public const string ScriptGenDownloadFile = "ScriptGen.DownloadFile";
        public const string ScriptGenNoObjectsSelected = "ScriptGen.NoObjectsSelected";
        public const string ScriptGenOutputTitle = "ScriptGen.OutputTitle";
        public const string ScriptGenFilterObjects = "ScriptGen.FilterObjects";
        public const string ScriptGenLines = "ScriptGen.Lines";
        public const string ScriptGenClear = "ScriptGen.Clear";
        public const string ScriptGenLoadingObjects = "ScriptGen.LoadingObjects";
        public const string ScriptGenNoMatchFilter = "ScriptGen.NoMatchFilter";
        public const string ScriptGenInitializing = "ScriptGen.Initializing";
        public const string ScriptGenRowsUnit = "ScriptGen.RowsUnit";
        public const string ScriptGenErrLoadServers = "ScriptGen.ErrLoadServers";
        public const string ScriptGenErrLoadDatabases = "ScriptGen.ErrLoadDatabases";
        public const string ScriptGenErrLoadObjects = "ScriptGen.ErrLoadObjects";
        public const string ScriptGenErrGenerationFailed = "ScriptGen.ErrGenerationFailed";
        public const string ScriptGenErrDownloadFailed = "ScriptGen.ErrDownloadFailed";

        // Logs Errors
        public const string LogsErrInvalidFile = "Logs.ErrInvalidFile";
        public const string LogsErrLoadFile = "Logs.ErrLoadFile";

        // Common Operation Failed
        public const string CommonOperationFailed = "Common.OperationFailed";

        // Server Management Notifications & Errors
        public const string ServerSavedSuccess = "Server.SavedSuccess";
        public const string ServerSaveError = "Server.SaveError";
        public const string ServerDeletedSuccess = "Server.DeletedSuccess";
        public const string ServerDeleteError = "Server.DeleteError";
        public const string ServerLoadError = "Server.LoadError";
        public const string ServerNameRequired = "Server.NameRequired";
        public const string ServerNotFound = "Server.NotFound";

        // Database Errors
        public const string DbErrLoadDatabases = "Db.ErrLoadDatabases";
        public const string DbErrCreateDatabase = "Db.ErrCreateDatabase";
        public const string DbErrLoadConfig = "Db.ErrLoadConfig";
        public const string DbErrUpdateDatabase = "Db.ErrUpdateDatabase";
        public const string DbErrDeleteDatabase = "Db.ErrDeleteDatabase";
        public const string DbErrLoadDetails = "Db.ErrLoadDetails";

        // Schema Errors
        public const string SchemaErrLoadSchemas = "Schema.ErrLoadSchemas";
        public const string SchemaErrCreateSchema = "Schema.ErrCreateSchema";
        public const string SchemaErrUpdateSchema = "Schema.ErrUpdateSchema";
        public const string SchemaErrDeleteSchema = "Schema.ErrDeleteSchema";
        public const string SchemaErrLoadObjects = "Schema.ErrLoadObjects";

        // Table Errors
        public const string TableErrLoadTables = "Table.ErrLoadTables";
        public const string TableErrCreateTable = "Table.ErrCreateTable";
        public const string TableErrLoadDetails = "Table.ErrLoadDetails";
        public const string TableErrReloadTable = "Table.ErrReloadTable";
        public const string TableErrUpdateTable = "Table.ErrUpdateTable";
        public const string TableErrDropTable = "Table.ErrDropTable";

        // Column Errors
        public const string ColErrSaveColumn = "Col.ErrSaveColumn";
        public const string ColErrDropColumn = "Col.ErrDropColumn";

        // Foreign Key Errors
        public const string FkPanelErrLoadRefColumns = "FkPanel.ErrLoadRefColumns";

        // General UI & Column Precision
        public const string Reset = "Reset";
        public const string CommonDefault = "Common.Default";
        public const string ColFractionalPrecision = "Col.FractionalPrecision";
        public const string ColVectorDimensions = "Col.VectorDimensions";
        public const string ScriptGenResetTooltip = "ScriptGen.ResetTooltip";

        // Phase 4: Data Model Generator
        public const string DataModelTitle = "DataModel.Title";
        public const string DataModelSubtitle = "DataModel.Subtitle";
        public const string DataModelSelectServer = "DataModel.SelectServer";
        public const string DataModelSelectDatabase = "DataModel.SelectDatabase";
        public const string DataModelSelectTables = "DataModel.SelectTables";
        public const string DataModelSelectObjects = "DataModel.SelectObjects";
        public const string DataModelNoObjectsSelected = "DataModel.NoObjectsSelected";
        public const string DataModelNoViewsFound = "DataModel.NoViewsFound";
        public const string DataModelNoProceduresFound = "DataModel.NoProceduresFound";
        public const string DataModelNoFunctionsFound = "DataModel.NoFunctionsFound";
        public const string DataModelLanguage = "DataModel.Language";
        public const string DataModelOptions = "DataModel.Options";
        public const string DataModelGenerate = "DataModel.Generate";
        public const string DataModelGenerating = "DataModel.Generating";
        public const string DataModelSelectAll = "DataModel.SelectAll";
        public const string DataModelDeselectAll = "DataModel.DeselectAll";
        public const string DataModelSearchPlaceholder = "DataModel.SearchPlaceholder";
        public const string DataModelNoTablesSelected = "DataModel.NoTablesSelected";
        public const string DataModelNoMatchFilter = "DataModel.NoMatchFilter";
        public const string DataModelTotalModels = "DataModel.TotalModels";
        public const string DataModelLines = "DataModel.Lines";
        public const string DataModelCharacters = "DataModel.Characters";
        public const string DataModelSizeKb = "DataModel.SizeKb";
        public const string DataModelCopyClipboard = "DataModel.CopyClipboard";
        public const string DataModelCopied = "DataModel.Copied";
        public const string DataModelDownloadFile = "DataModel.DownloadFile";
        public const string DataModelDownloadZip = "DataModel.DownloadZip";
        public const string DataModelOutputTitle = "DataModel.OutputTitle";
        public const string DataModelClear = "DataModel.Clear";
        public const string DataModelCommonSettings = "DataModel.CommonSettings";
        public const string DataModelPropertyNaming = "DataModel.PropertyNaming";
        public const string DataModelRemovePrefixes = "DataModel.RemovePrefixes";
        public const string DataModelSingularize = "DataModel.Singularize";
        public const string DataModelIncludeNavigation = "DataModel.IncludeNavigation";
        public const string DataModelIncludeDocComments = "DataModel.IncludeDocComments";
        public const string DataModelNamingOriginal = "DataModel.NamingOriginal";
        public const string DataModelNamingPascal = "DataModel.NamingPascal";
        public const string DataModelNamingCamel = "DataModel.NamingCamel";
        public const string DataModelNamingSnake = "DataModel.NamingSnake";

        // C# Options
        public const string DataModelCsTypeKind = "DataModel.CsTypeKind";
        public const string DataModelCsClass = "DataModel.CsClass";
        public const string DataModelCsRecord = "DataModel.CsRecord";
        public const string DataModelCsStruct = "DataModel.CsStruct";
        public const string DataModelCsAccessor = "DataModel.CsAccessor";
        public const string DataModelCsGetSet = "DataModel.CsGetSet";
        public const string DataModelCsGetInit = "DataModel.CsGetInit";
        public const string DataModelCsNamespace = "DataModel.CsNamespace";
        public const string DataModelCsNullableRef = "DataModel.CsNullableRef";
        public const string DataModelCsDataAnnotations = "DataModel.CsDataAnnotations";
        public const string DataModelCsJsonAttributes = "DataModel.CsJsonAttributes";
        public const string DataModelCsEfTableAttributes = "DataModel.CsEfTableAttributes";
        public const string DataModelCsGenerateDbContext = "DataModel.CsGenerateDbContext";
        public const string DataModelCsDbContextName = "DataModel.CsDbContextName";

        // Profiles
        public const string DataModelSelectProfileToProceed = "DataModel.SelectProfileToProceed";
        public const string DataModelManageProfilesInSettings = "DataModel.ManageProfilesInSettings";
        public const string DataModelStep1Server = "DataModel.Step1Server";
        public const string DataModelStep2Database = "DataModel.Step2Database";
        public const string DataModelStep3Profile = "DataModel.Step3Profile";
        public const string DataModelNoProfileSelected = "DataModel.NoProfileSelected";
        public const string DataModelCreateProfilePrompt = "DataModel.CreateProfilePrompt";
        public const string DataModelProfile = "DataModel.Profile";
        public const string DataModelProfiles = "DataModel.Profiles";
        public const string DataModelProfileSelect = "DataModel.ProfileSelect";
        public const string DataModelProfileSaveAs = "DataModel.ProfileSaveAs";
        public const string DataModelProfileName = "DataModel.ProfileName";
        public const string DataModelProfileSaveSuccess = "DataModel.ProfileSaveSuccess";
        public const string DataModelProfileDeleteSuccess = "DataModel.ProfileDeleteSuccess";
        public const string DataModelProfileGlobal = "DataModel.ProfileGlobal";

        // Structure & Regions
        public const string DataModelCsStructureTab = "DataModel.CsStructureTab";
        public const string DataModelCsNamespaceTab = "DataModel.CsNamespaceTab";
        public const string DataModelCsAttributesTab = "DataModel.CsAttributesTab";
        public const string DataModelCsNavFkTab = "DataModel.CsNavFkTab";
        public const string DataModelCsTransformTab = "DataModel.CsTransformTab";
        public const string DataModelCsCustomSnippetTab = "DataModel.CsCustomSnippetTab";
        public const string DataModelSchemaAccessTab = "DataModel.SchemaAccessTab";
        public const string DataModelEnableSchemaAccess = "DataModel.EnableSchemaAccess";
        public const string DataModelSchemaAccessHelp = "DataModel.SchemaAccessHelp";
        public const string DataModelAddSchemaRule = "DataModel.AddSchemaRule";
        public const string DataModelSourceSchema = "DataModel.SourceSchema";
        public const string DataModelAllowedTargetSchemas = "DataModel.AllowedTargetSchemas";
        public const string DataModelIsolatedSchema = "DataModel.IsolatedSchema";
        public const string DataModelFetchSchemasFromDb = "DataModel.FetchSchemasFromDb";
        public const string SelectServerFirstForDatabase = "DataModel.SelectServerFirstForDatabase";
        public const string AllDatabases = "DataModel.AllDatabases";

        public const string DataModelCsBaseClass = "DataModel.CsBaseClass";
        public const string DataModelCsInterfaces = "DataModel.CsInterfaces";
        public const string DataModelCsIsPartial = "DataModel.CsIsPartial";
        public const string DataModelCsUseNameof = "DataModel.CsUseNameof";
        public const string DataModelCsIncludeIndexes = "DataModel.CsIncludeIndexes";
        public const string DataModelCsUseRegions = "DataModel.CsUseRegions";
        public const string DataModelCsIncludeConstructor = "DataModel.CsIncludeConstructor";
        public const string DataModelCsIncludeConstants = "DataModel.CsIncludeConstants";
        public const string DataModelCsIncludeTableSchemaOverrides = "DataModel.CsIncludeTableSchemaOverrides";
        public const string DataModelCsOverrideProperties = "DataModel.CsOverrideProperties";
        public const string DataModelCsNavigationStyle = "DataModel.CsNavigationStyle";
        public const string DataModelCsNavEnterprise = "DataModel.CsNavEnterprise";
        public const string DataModelCsNavStandard = "DataModel.CsNavStandard";
        public const string DataModelCsUseFullNamespaceForNav = "DataModel.CsUseFullNamespaceForNav";
        public const string DataModelCsMakeCollectionsNullable = "DataModel.CsMakeCollectionsNullable";
        public const string DataModelCsCustomUsings = "DataModel.CsCustomUsings";
        public const string DataModelCsEnableCustomFk = "DataModel.CsEnableCustomFk";
        public const string DataModelCsEnableCustomPk = "DataModel.CsEnableCustomPk";
        public const string DataModelCsCustomColumnRules = "DataModel.CsCustomColumnRules";
        public const string DataModelCsStringInit = "DataModel.CsStringInit";
        public const string DataModelCsStringNullForgiving = "DataModel.CsStringNullForgiving";
        public const string DataModelCsStringEmpty = "DataModel.CsStringEmpty";
        public const string DataModelCsStringNone = "DataModel.CsStringNone";
        public const string DataModelCsCustomSnippet = "DataModel.CsCustomSnippet";
        public const string DataModelCsBackingField = "DataModel.CsBackingField";
        public const string DataModelCsOuterRegionName = "DataModel.CsOuterRegionName";
        public const string DataModelCsOuterRegionHelp = "DataModel.CsOuterRegionHelp";
        public const string DataModelCsIncludeTimestamp = "DataModel.CsIncludeTimestamp";
        public const string DataModelCsCustomClassAttributes = "DataModel.CsCustomClassAttributes";
        public const string DataModelCsDefaultPropertyAttributes = "DataModel.CsDefaultPropertyAttributes";
        public const string DataModelCsDefaultPropertyAttributesHelp = "DataModel.CsDefaultPropertyAttributesHelp";
        public const string DataModelCsCustomNavReferenceAttr = "DataModel.CsCustomNavReferenceAttr";
        public const string DataModelCsCustomNavInverseAttr = "DataModel.CsCustomNavInverseAttr";
        public const string DataModelCsNavigationPropertyAttributes = "DataModel.CsNavigationPropertyAttributes";
        public const string DataModelCsNavigationPropertyAttributesHelp = "DataModel.CsNavigationPropertyAttributesHelp";
        public const string DataModelCsIncludeSchemaInRelationName = "DataModel.CsIncludeSchemaInRelationName";

        // Annotations Management
        public const string DataModelCsAnnotationsManagement = "DataModel.CsAnnotationsManagement";
        public const string DataModelCsAnnotationsManagementDesc = "DataModel.CsAnnotationsManagementDesc";
        public const string DataModelCsKeyAttribute = "DataModel.CsKeyAttribute";
        public const string DataModelCsDatabaseGeneratedAttribute = "DataModel.CsDatabaseGeneratedAttribute";
        public const string DataModelCsRequiredAttribute = "DataModel.CsRequiredAttribute";
        public const string DataModelCsRequiredScope = "DataModel.CsRequiredScope";
        public const string DataModelCsRequiredScopeAll = "DataModel.CsRequiredScopeAll";
        public const string DataModelCsRequiredScopeStrings = "DataModel.CsRequiredScopeStrings";
        public const string DataModelCsRequiredScopeNonIdentity = "DataModel.CsRequiredScopeNonIdentity";
        public const string DataModelCsRequiredIncludePk = "DataModel.CsRequiredIncludePk";
        public const string DataModelCsRequiredErrorMsg = "DataModel.CsRequiredErrorMsg";
        public const string DataModelCsRequiredErrorMsgHint = "DataModel.CsRequiredErrorMsgHint";
        public const string DataModelCsStringLengthAttribute = "DataModel.CsStringLengthAttribute";
        public const string DataModelCsStringLengthKind = "DataModel.CsStringLengthKind";
        public const string DataModelCsColumnMode = "DataModel.CsColumnMode";
        public const string DataModelCsColumnModeDiffers = "DataModel.CsColumnModeDiffers";
        public const string DataModelCsColumnModeAlways = "DataModel.CsColumnModeAlways";
        public const string DataModelCsColumnModeNever = "DataModel.CsColumnModeNever";
        public const string DataModelCsColumnTypeName = "DataModel.CsColumnTypeName";
        public const string DataModelCsDisplayAttribute = "DataModel.CsDisplayAttribute";
        public const string DataModelCsDescriptionAttribute = "DataModel.CsDescriptionAttribute";
        public const string DataModelCsKeylessAttribute = "DataModel.CsKeylessAttribute";

        // Transformation Engine
        public const string DataModelTransformAddRule = "DataModel.TransformAddRule";
        public const string DataModelTransformRuleName = "DataModel.TransformRuleName";
        public const string DataModelTransformColPattern = "DataModel.TransformColPattern";
        public const string DataModelTransformTypePattern = "DataModel.TransformTypePattern";
        public const string DataModelTransformActionType = "DataModel.TransformActionType";
        public const string DataModelTransformNotMappedHelper = "DataModel.TransformNotMappedHelper";
        public const string DataModelTransformCustomGetterSetter = "DataModel.TransformCustomGetterSetter";
        public const string DataModelTransformTypeOverride = "DataModel.TransformTypeOverride";
        public const string DataModelTransformHelperPropName = "DataModel.TransformHelperPropName";
        public const string DataModelTransformTargetType = "DataModel.TransformTargetType";
        public const string DataModelTransformGetter = "DataModel.TransformGetter";
        public const string DataModelTransformSetter = "DataModel.TransformSetter";
        public const string DataModelTransformUsing = "DataModel.TransformUsing";
        public const string DataModelTransformNoRules = "DataModel.TransformNoRules";

        // Generic other languages
        public const string DataModelBaseClass = "DataModel.BaseClass";
        public const string DataModelCustomImports = "DataModel.CustomImports";
        public const string DataModelCustomSnippet = "DataModel.CustomSnippet";

        // Python Options
        public const string DataModelPyStyle = "DataModel.PyStyle";
        public const string DataModelPyPydantic = "DataModel.PyPydantic";
        public const string DataModelPyDataclass = "DataModel.PyDataclass";
        public const string DataModelPySqlAlchemy = "DataModel.PySqlAlchemy";
        public const string DataModelPyTypedDict = "DataModel.PyTypedDict";
        public const string DataModelPySnakeCase = "DataModel.PySnakeCase";
        public const string DataModelPyOptional = "DataModel.PyOptional";
        public const string DataModelPyDocstrings = "DataModel.PyDocstrings";

        // Dart Options
        public const string DataModelDartStyle = "DataModel.DartStyle";
        public const string DataModelDartPlain = "DataModel.DartPlain";
        public const string DataModelDartJsonSerializable = "DataModel.DartJsonSerializable";
        public const string DataModelDartFreezed = "DataModel.DartFreezed";
        public const string DataModelDartNullSafety = "DataModel.DartNullSafety";
        public const string DataModelDartFinalFields = "DataModel.DartFinalFields";
        public const string DataModelDartCopyWith = "DataModel.DartCopyWith";

        // Java Options
        public const string DataModelJavaStyle = "DataModel.JavaStyle";
        public const string DataModelJavaPojo = "DataModel.JavaPojo";
        public const string DataModelJavaRecord = "DataModel.JavaRecord";
        public const string DataModelJavaLombok = "DataModel.JavaLombok";
        public const string DataModelJavaJpa = "DataModel.JavaJpa";
        public const string DataModelJavaPackage = "DataModel.JavaPackage";
        public const string DataModelJavaJackson = "DataModel.JavaJackson";
        public const string DataModelJavaValidation = "DataModel.JavaValidation";
        public const string DataModelJavaSerializable = "DataModel.JavaSerializable";

        // Namespace & Constants
        public const string DataModelNamespaceMode = "DataModel.NamespaceMode";
        public const string DataModelNamespaceModeSingle = "DataModel.NamespaceModeSingle";
        public const string DataModelNamespaceModePerSchema = "DataModel.NamespaceModePerSchema";
        public const string DataModelNamespaceSingleHelp = "DataModel.NamespaceSingleHelp";
        public const string DataModelSchemaNamespacePattern = "DataModel.SchemaNamespacePattern";
        public const string DataModelExplicitSchemaNamespaces = "DataModel.ExplicitSchemaNamespaces";
        public const string DataModelAddConstant = "DataModel.AddConstant";
        public const string DataModelConstantModifier = "DataModel.ConstantModifier";
        public const string DataModelConstantType = "DataModel.ConstantType";
        public const string DataModelConstantName = "DataModel.ConstantName";
        public const string DataModelConstantValue = "DataModel.ConstantValue";

        // Override Members
        public const string DataModelCsIncludeOverrides = "DataModel.CsIncludeOverrides";
        public const string DataModelAddOverride = "DataModel.AddOverride";
        public const string DataModelOverrideKind = "DataModel.OverrideKind";
        public const string DataModelOverrideProperty = "DataModel.OverrideProperty";
        public const string DataModelOverrideMethod = "DataModel.OverrideMethod";
        public const string DataModelOverrideReturnType = "DataModel.OverrideReturnType";
        public const string DataModelOverrideName = "DataModel.OverrideName";
        public const string DataModelOverrideParams = "DataModel.OverrideParams";
        public const string DataModelOverrideExpression = "DataModel.OverrideExpression";
        public const string DataModelOverrideBody = "DataModel.OverrideBody";

        // Schema Selection
        public const string DataModelSelectSchema = "DataModel.SelectSchema";
        public const string DataModelSelectTargetSchemaPrompt = "DataModel.SelectTargetSchemaPrompt";
        public const string DataModelSchemaAccessServerDbRequired = "DataModel.SchemaAccessServerDbRequired";

        // Home Card
        public const string HomeDataModelCardDesc = "Home.DataModelCardDesc";

        // Database Backup & Restore
        public const string DbTabBackupDb = "Db.Tab.BackupDb";
        public const string DbTabRestoreDb = "Db.Tab.RestoreDb";
        public const string DbTabBackupPath = "Db.Tab.BackupPath";
        public const string DbTabBackupPathHelp = "Db.Tab.BackupPathHelp";
        public const string DbTabWithCompression = "Db.Tab.WithCompression";
        public const string DbTabCopyOnly = "Db.Tab.CopyOnly";
        public const string DbTabRestorePath = "Db.Tab.RestorePath";
        public const string DbTabRestorePathHelp = "Db.Tab.RestorePathHelp";
        public const string DbTabWithReplace = "Db.Tab.WithReplace";
        public const string DbTabCloseConnections = "Db.Tab.CloseConnections";
        public const string DbTabBackupSuccess = "Db.Tab.BackupSuccess";
        public const string DbTabRestoreSuccess = "Db.Tab.RestoreSuccess";
        public const string DbTabTakingBackup = "Db.Tab.TakingBackup";
        public const string DbTabRestoring = "Db.Tab.Restoring";
        public const string DbTabBrowse = "Db.Tab.Browse";
        public const string DbTabSelectFolder = "Db.Tab.SelectFolder";
        public const string DbTabSelectBakFile = "Db.Tab.SelectBakFile";
        public const string DbTabBackupFolder = "Db.Tab.BackupFolder";
        public const string DbTabBackupFileName = "Db.Tab.BackupFileName";
        public const string DbTabSelectThisFolder = "Db.Tab.SelectThisFolder";
        public const string DbTabParentFolder = "Db.Tab.ParentFolder";
        public const string DbTabDrives = "Db.Tab.Drives";
        public const string DbTabNoItems = "Db.Tab.NoItems";
        public const string DbTabFullBackupPath = "Db.Tab.FullBackupPath";

        // Configuration Backup & Restore
        public const string BackupRestoreTab = "Config.BackupRestoreTab";
        public const string BackupRestoreSettings = "Config.BackupRestoreSettings";
        public const string ConfigBackupExportTitle = "Config.BackupExportTitle";
        public const string ConfigBackupExportSubtitle = "Config.BackupExportSubtitle";
        public const string ConfigBackupDownloadBtn = "Config.BackupDownloadBtn";
        public const string ConfigBackupImportTitle = "Config.BackupImportTitle";
        public const string ConfigBackupImportSubtitle = "Config.BackupImportSubtitle";
        public const string ConfigBackupSelectFile = "Config.BackupSelectFile";
        public const string ConfigBackupPreview = "Config.BackupPreview";
        public const string ConfigBackupAppVersion = "Config.BackupAppVersion";
        public const string ConfigBackupExportDate = "Config.BackupExportDate";
        public const string ConfigBackupServersCount = "Config.BackupServersCount";
        public const string ConfigBackupProfilesCount = "Config.BackupProfilesCount";
        public const string ConfigBackupIncludeAppSettings = "Config.BackupIncludeAppSettings";
        public const string ConfigBackupRestoreServers = "Config.BackupRestoreServers";
        public const string ConfigBackupRestoreProfiles = "Config.BackupRestoreProfiles";
        public const string ConfigBackupRestoreApp = "Config.BackupRestoreApp";
        public const string ConfigBackupMergeOption = "Config.BackupMergeOption";
        public const string ConfigBackupReplaceOption = "Config.BackupReplaceOption";
        public const string ConfigBackupApplyBtn = "Config.BackupApplyBtn";
        public const string ConfigBackupInvalidFile = "Config.BackupInvalidFile";
        public const string ConfigBackupSuccess = "Config.BackupSuccess";

        // Data Model ZIP Packaging
        public const string ZipPackagingStructure = "DataModel.ZipPackagingStructure";
        public const string ZipPackagingFlat = "DataModel.ZipPackagingFlat";
        public const string ZipPackagingBySchema = "DataModel.ZipPackagingBySchema";
        public const string ZipPackagingByNamespace = "DataModel.ZipPackagingByNamespace";
        public const string ZipPackagingHelp = "DataModel.ZipPackagingHelp";
        public const string DataModelDownloadSeparateZips = "DataModel.DownloadSeparateZips";
    }
}
