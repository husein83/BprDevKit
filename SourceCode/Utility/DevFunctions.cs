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
                        folderName = Constants.EncryptedServerConfigFileName;
                    }
                    break;

                case BprDevConfig.ModelGeneratorConfiguration:
                    {
                        folderName = Constants.EncryptedModelGeneratorConfigFileName;
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

        #region Helpers
        /// <summary>
        /// Returns a clean, human-readable date and time string suitable for file naming: yyyy-MM-dd_HH-mm-ss
        /// </summary>
        public static string GetReadableTimestamp(DateTime? dateTime = null)
        {
            return (dateTime ?? DateTime.Now).ToString("yyyy-MM-dd_HH-mm-ss");
        }
        #endregion
    }
}
