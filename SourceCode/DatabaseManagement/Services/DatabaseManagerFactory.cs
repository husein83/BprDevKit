using BprDevKit.Config;

namespace BprDevKit.DatabaseManagement.Services
{
    public sealed class DatabaseManagerFactory
    {
        private readonly ISqlGenerator _sqlGenerator;

        public DatabaseManagerFactory(ISqlGenerator sqlGenerator)
        {
            _sqlGenerator = sqlGenerator;
        }

        public DatabaseManager Create(ServerConfiguration serverConfiguration)
        {
            ArgumentNullException.ThrowIfNull(serverConfiguration);
            return new DatabaseManager(_sqlGenerator, serverConfiguration);
        }
    }
}