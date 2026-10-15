using System;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Factory interface for creating provider-specific SQL generators.
    /// </summary>
    public interface ISqlGeneratorFactory
    {
        ISqlGenerator Create(DatabaseServerType serverType);
    }

    /// <summary>
    /// Default factory for creating SQL generators based on DatabaseServerType.
    /// </summary>
    public class SqlGeneratorFactory : ISqlGeneratorFactory
    {
        public virtual ISqlGenerator Create(DatabaseServerType serverType)
        {
            return serverType switch
            {
                DatabaseServerType.SqlServer => new SqlServerGenerator(),
                DatabaseServerType.PostgreSql => new PostgreSqlGenerator(),
                DatabaseServerType.MySql => new MySqlGenerator(),
                DatabaseServerType.MariaDb => new MariaDbGenerator(),
                DatabaseServerType.Oracle => new OracleGenerator(),
                DatabaseServerType.Sqlite => new SqliteGenerator(),
                _ => throw new NotSupportedException($"Database server type '{serverType}' is not supported.")
            };
        }
    }
}
