using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Connections
{
    /// <summary>
    /// Diagnostics result for database connection test.
    /// </summary>
    public sealed class ConnectionTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public string? ServerVersion { get; set; }
        public string? Database { get; set; }
        public string? Provider { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Factory for creating and opening database connections based on server configuration.
    /// </summary>
    public interface IDatabaseConnectionFactory
    {
        DbConnection CreateConnection(ServerConfiguration config, string? databaseName = null);
        Task<DbConnection> OpenConnectionAsync(ServerConfiguration config, string? databaseName = null, CancellationToken cancellationToken = default);
        Task<ConnectionTestResult> TestConnectionAsync(ServerConfiguration config, CancellationToken cancellationToken = default);
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
                DatabaseServerType.PostgreSql => new Npgsql.NpgsqlConnection(connectionString),
                DatabaseServerType.MySql or DatabaseServerType.MariaDb => new MySqlConnector.MySqlConnection(connectionString),
                DatabaseServerType.Sqlite => new Microsoft.Data.Sqlite.SqliteConnection(connectionString),
                DatabaseServerType.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
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

        public virtual async Task<ConnectionTestResult> TestConnectionAsync(ServerConfiguration config, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var serverType = config.Server.DatabaseServerType;
            var caps = DatabaseProviderMetadata.GetCapabilities(serverType);

            try
            {
                await using var connection = await OpenConnectionAsync(config, cancellationToken: cancellationToken);
                stopwatch.Stop();

                string serverVersion = connection.ServerVersion;
                string database = string.IsNullOrWhiteSpace(connection.Database) ? "Default" : connection.Database;

                // Query database version dynamically
                try
                {
                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = serverType switch
                    {
                        DatabaseServerType.SqlServer => "SELECT @@VERSION",
                        DatabaseServerType.PostgreSql => "SELECT version()",
                        DatabaseServerType.MySql or DatabaseServerType.MariaDb => "SELECT VERSION()",
                        DatabaseServerType.Sqlite => "SELECT sqlite_version()",
                        DatabaseServerType.Oracle => "SELECT banner FROM v$version WHERE ROWNUM = 1",
                        _ => "SELECT 1"
                    };
                    cmd.CommandTimeout = Math.Min(10, Math.Max(5, config.Server.CommandTimeout));
                    var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                    if (scalar is string fullVer && !string.IsNullOrWhiteSpace(fullVer))
                    {
                        var firstLine = fullVer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(firstLine))
                        {
                            serverVersion = firstLine.Trim();
                        }
                    }
                }
                catch
                {
                    // If version query fails, retain connection.ServerVersion
                }

                return new ConnectionTestResult
                {
                    Success = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    ServerVersion = serverVersion,
                    Database = database,
                    Provider = caps.DisplayName,
                    Message = $"Successfully connected to '{database}' on '{config.Server.ServerName}' ({stopwatch.ElapsedMilliseconds} ms)."
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new ConnectionTestResult
                {
                    Success = false,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Provider = caps.DisplayName,
                    Message = ex.Message,
                    ErrorDetails = ex.ToString()
                };
            }
        }
    }
}
