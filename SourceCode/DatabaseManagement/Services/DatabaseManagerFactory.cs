using System;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    public sealed class DatabaseManagerFactory
    {
        private readonly ISqlGeneratorFactory _sqlGeneratorFactory;

        public DatabaseManagerFactory(ISqlGeneratorFactory sqlGeneratorFactory)
        {
            _sqlGeneratorFactory = sqlGeneratorFactory ?? throw new ArgumentNullException(nameof(sqlGeneratorFactory));
        }

        public DatabaseManager Create(ServerConfiguration serverConfiguration)
        {
            ArgumentNullException.ThrowIfNull(serverConfiguration);
            var sqlGenerator = _sqlGeneratorFactory.Create(serverConfiguration.Server.DatabaseServerType);
            return new DatabaseManager(sqlGenerator, serverConfiguration);
        }
    }
}