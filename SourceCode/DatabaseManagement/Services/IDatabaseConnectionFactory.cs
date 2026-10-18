using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Factory for creating and opening database connections based on server configuration.
    /// </summary>
    public interface IDatabaseConnectionFactory
    {
        DbConnection CreateConnection(ServerConfiguration config, string? databaseName = null);
        Task<DbConnection> OpenConnectionAsync(ServerConfiguration config, string? databaseName = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of IDatabaseConnectionFactory supporting SQL Server natively and ready for additional providers.
    /// </summary>
    public class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public virtual DbConnection CreateConnection(ServerConfiguration config, string? databaseName = null)
        {
            ArgumentNullException.ThrowIfNull(config);

            var serverType = config.Server.DatabaseServerType;
            var connectionString = config.Server.BuildConnectionString(databaseName);

            return serverType switch
            {
                DatabaseServerType.SqlServer => new SqlConnection(connectionString),
                DatabaseServerType.PostgreSql => throw new NotSupportedException("PostgreSQL ADO.NET provider (Npgsql) is not yet registered. SQL Server is currently active."),
                DatabaseServerType.MySql or DatabaseServerType.MariaDb => throw new NotSupportedException("MySQL/MariaDB ADO.NET provider is not yet registered. SQL Server is currently active."),
                DatabaseServerType.Oracle => throw new NotSupportedException("Oracle ADO.NET provider is not yet registered. SQL Server is currently active."),
                DatabaseServerType.Sqlite => throw new NotSupportedException("SQLite ADO.NET provider is not yet registered. SQL Server is currently active."),
                _ => new SqlConnection(connectionString)
            };
        }

        public virtual async Task<DbConnection> OpenConnectionAsync(ServerConfiguration config, string? databaseName = null, CancellationToken cancellationToken = default)
        {
            var connection = CreateConnection(config, databaseName);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}
