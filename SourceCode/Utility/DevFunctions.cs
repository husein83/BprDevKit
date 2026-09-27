using BprDevKit.Config;

namespace BprDevKit.Utility
{
    public sealed class DevFunctions
    {
        #region ConfigurationFunction
        public static string GetConfigurationPath(BprDevConfig devConfig, out bool isExistsFile)
        {
            string folderName = string.Empty;
            switch (devConfig)
            {
                case BprDevConfig.ApplicationConfiguration:
                    {
                        folderName = Constants.EncryptedApplicationConfigFileName;
                    }
                    break;

                case BprDevConfig.ServerConfiguration:
                    {
                        if (ApplicationConfiguration.Config.Security.EnableConfigFileEncryption)
                            folderName = Constants.EncryptedServerConfigFileName;
                        else
                            folderName = Constants.ServerConfigFileName;
                    }
                    break;

                default:
                    break;
            }
            string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigFolderName);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            string filePath = Path.Combine(configDir, folderName);
            isExistsFile = File.Exists(filePath);

            return filePath;
        }
        #endregion
    }
}
