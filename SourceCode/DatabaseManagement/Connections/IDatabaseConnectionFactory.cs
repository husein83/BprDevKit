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

        public virtual async Task<ConnectionTestResult> TestConnectionAsync(ServerConfiguration config, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var serverType = config.Server.DatabaseServerType;
            var caps = DatabaseProviderMetadata.GetCapabilities(serverType);

            try
            {
                if (serverType != DatabaseServerType.SqlServer)
                {
                    return new ConnectionTestResult
                    {
                        Success = false,
                        LatencyMs = 0,
                        Provider = caps.DisplayName,
                        Message = $"Provider '{caps.DisplayName}' configuration is valid, but native ADO.NET driver is not registered yet.",
                        ErrorDetails = $"Constructed Connection String: {config.Server.BuildConnectionString()}"
                    };
                }

                await using var connection = await OpenConnectionAsync(config, cancellationToken: cancellationToken);
                stopwatch.Stop();

                string serverVersion = connection.ServerVersion;
                string database = connection.Database;

                // Query @@VERSION for detailed edition/version information
                try
                {
                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT @@VERSION";
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
                    // If @@VERSION query fails, retain connection.ServerVersion
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
