using BprDevKit.Utility;

namespace BprDevKit.Config
{
    #region ApplicationConfiguration
    /// <summary>
    /// Configuration management for application settings
    /// </summary>
    public sealed class ApplicationConfiguration
    {
        #region Property
        public Language Language { get; set; } = Language.English;

        // Security settings
        public SecuritySettings Security { get; set; } = new();

        // Logging settings
        public LoggingSettings Logging { get; set; } = new();
        #endregion

        #region Public
        public static ApplicationConfiguration Config { get; private set; } = new();

        public static void LoadConfig()
        {
            string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ApplicationConfiguration, out bool isExistsFile);
            if (isExistsFile)
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = encryptedContent.DecryptContent();
                if (decryptedContent.TryDevDeserialize<ApplicationConfiguration>(out var result))
                {
                    Config = result;
                }
            }
        }

        /// <summary>
        /// Save application configuration to JSON file
        /// </summary>
        public static void SaveChanges(ApplicationConfiguration applicationConfiguration)
        {
            if (applicationConfiguration.TryDevSerialize(out var jsonContent))
            {
                string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ApplicationConfiguration, out bool isExistsFile);
                string encryptedContent = jsonContent.EncryptContent();
                File.WriteAllText(filePath, encryptedContent);
                Config = applicationConfiguration;
            }
        }
        #endregion

        #region Private

        #endregion
    }
    #endregion

    #region ApplicationConfigurationItem
    /// <summary>
    /// Security settings
    /// </summary>
    public class SecuritySettings
    {

    }

    /// <summary>
    /// Logging settings
    /// </summary>
    public class LoggingSettings
    {
        public Utility.LogLevel LogLevel { get; set; } = BprDevKit.Utility.LogLevel.Information;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = false;
        public string LogFolder => BprDevKit.Utility.Constants.LogFolderName;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int RetentionDays { get; set; } = 7;
    }
    #endregion
}
