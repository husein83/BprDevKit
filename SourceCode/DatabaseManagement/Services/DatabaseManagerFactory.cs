using System;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    public sealed class DatabaseManagerFactory
    {
        private readonly ISqlGeneratorFactory _sqlGeneratorFactory;
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public DatabaseManagerFactory(ISqlGeneratorFactory sqlGeneratorFactory, IDatabaseConnectionFactory connectionFactory)
        {
            _sqlGeneratorFactory = sqlGeneratorFactory ?? throw new ArgumentNullException(nameof(sqlGeneratorFactory));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public DatabaseManager Create(ServerConfiguration serverConfiguration)
        {
            ArgumentNullException.ThrowIfNull(serverConfiguration);
            var sqlGenerator = _sqlGeneratorFactory.Create(serverConfiguration.Server.DatabaseServerType);
            return new DatabaseManager(sqlGenerator, serverConfiguration, _connectionFactory);
        }
    }
}