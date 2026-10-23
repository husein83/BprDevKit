using System;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    public sealed class DatabaseManagerFactory
    {
        private readonly ISqlGeneratorFactory _sqlGeneratorFactory;
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ISqlObjectDecryptor _decryptor;
        private readonly BprDevKit.Utility.Logging.IBprLogger _logger;

        public DatabaseManagerFactory(
            ISqlGeneratorFactory sqlGeneratorFactory, 
            IDatabaseConnectionFactory connectionFactory,
            ISqlObjectDecryptor? decryptor = null,
            BprDevKit.Utility.Logging.IBprLogger? logger = null)
        {
            _sqlGeneratorFactory = sqlGeneratorFactory ?? throw new ArgumentNullException(nameof(sqlGeneratorFactory));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _decryptor = decryptor ?? new SqlObjectDecryptor(connectionFactory);
            _logger = logger ?? BprDevKit.Utility.Logging.BprLogger.Instance;
        }

        public DatabaseManager Create(ServerConfiguration serverConfiguration)
        {
            ArgumentNullException.ThrowIfNull(serverConfiguration);
            var sqlGenerator = _sqlGeneratorFactory.Create(serverConfiguration.Server.DatabaseServerType);
            return new DatabaseManager(sqlGenerator, serverConfiguration, _connectionFactory, _decryptor, _logger);
        }
    }
}