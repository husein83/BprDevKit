using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.DatabaseManagement.Services
{
    /// <summary>
    /// Result of an object decryption attempt.
    /// </summary>
    public sealed class DecryptionResult
    {
        public bool Success { get; set; }
        public string? DecryptedDefinition { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsDacRequired { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Contract for decrypting SQL Server database objects (Views, Procedures, Functions, Triggers)
    /// created with the WITH ENCRYPTION option.
    /// </summary>
    public interface ISqlObjectDecryptor
    {
        Task<DecryptionResult> DecryptObjectAsync(
            ServerConfiguration config,
            string databaseName,
            string schemaName,
            string objectName,
            string objectType,
            CancellationToken cancellationToken = default);

        Task<OperationResult> EnableRemoteDacAsync(
            ServerConfiguration config,
            CancellationToken cancellationToken = default);

        Task<OperationResult> DisableRemoteDacAsync(
            ServerConfiguration config,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Decrypts SQL Server encrypted objects (WITH ENCRYPTION) using Dedicated Administrator Connection (DAC)
    /// and XOR stream keystream reconstruction inside a rolled-back transaction.
    /// Automatically discovers dynamic DAC TCP ports to bypass SQL Server Browser requirements,
    /// and safely restores server configuration after operations.
    /// </summary>
    public class SqlObjectDecryptor : ISqlObjectDecryptor
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public SqlObjectDecryptor(IDatabaseConnectionFactory? connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new DatabaseConnectionFactory();
        }

        public async Task<DecryptionResult> DecryptObjectAsync(
            ServerConfiguration config,
            string databaseName,
            string schemaName,
            string objectName,
            string objectType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            if (string.IsNullOrWhiteSpace(databaseName))
                return new DecryptionResult { Success = false, Message = "Database name cannot be empty." };
            if (string.IsNullOrWhiteSpace(schemaName))
                return new DecryptionResult { Success = false, Message = "Schema name cannot be empty." };
            if (string.IsNullOrWhiteSpace(objectName))
                return new DecryptionResult { Success = false, Message = "Object name cannot be empty." };

            if (config.Server.DatabaseServerType != DatabaseServerType.SqlServer)
            {
                return new DecryptionResult
                {
                    Success = false,
                    Message = "Object decryption is currently supported for Microsoft SQL Server."
                };
            }

            bool wasRemoteDacInitiallyDisabled = false;
            SqlConnection? dacConnection = null;

            try
            {
                // 1. Establish DAC Connection with automatic port discovery & security state inspection
                var connResult = await OpenDacConnectionAsync(config, databaseName, cancellationToken);
                if (!connResult.Success || connResult.Connection == null)
                {
                    return new DecryptionResult
                    {
                        Success = false,
                        IsDacRequired = connResult.IsDacRequired,
                        Message = connResult.Message,
                        ErrorDetails = connResult.ErrorDetails
                    };
                }

                dacConnection = connResult.Connection;
                wasRemoteDacInitiallyDisabled = connResult.WasRemoteDacInitiallyDisabled;

                // 2. Query object_id and object type from sys.objects via DAC
                int objectId;
                string resolvedType;
                using (var cmd = dacConnection.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT object_id, RTRIM(type) AS type 
FROM sys.objects 
WHERE schema_id = SCHEMA_ID(@schemaName) AND name = @objectName;";
                    cmd.Parameters.Add(new SqlParameter("@schemaName", SqlDbType.NVarChar, 128) { Value = schemaName });
                    cmd.Parameters.Add(new SqlParameter("@objectName", SqlDbType.NVarChar, 128) { Value = objectName });

                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return new DecryptionResult
                        {
                            Success = false,
                            Message = $"Object [{schemaName}].[{objectName}] was not found in database [{databaseName}]."
                        };
                    }

                    objectId = reader.GetInt32(0);
                    resolvedType = reader.GetString(1);
                }

                // 3. Read original encrypted bytes from sys.sysobjvalues (valclass = 1)
                byte[] encryptedOriginal = await ReadEncryptedBytesAsync(dacConnection, objectId, cancellationToken);
                if (encryptedOriginal.Length == 0)
                {
                    return new DecryptionResult
                    {
                        Success = false,
                        Message = $"Object [{schemaName}].[{objectName}] does not contain encrypted metadata in sys.sysobjvalues."
                    };
                }

                int totalCharCount = encryptedOriginal.Length / 2;

                // 4. Build matching dummy statement (leading spaces guarantee characters 0..(totalCharCount-1) are spaces)
                string dummyStatement = BuildDummyStatement(schemaName, objectName, resolvedType, totalCharCount);
                if (dummyStatement.Length < totalCharCount)
                {
                    return new DecryptionResult
                    {
                        Success = false,
                        Message = $"Unable to build matching dummy statement (required length: {totalCharCount}, generated: {dummyStatement.Length})."
                    };
                }

                // 5. In a transaction: execute dummy statement, read dummy ciphertext, then ROLLBACK
                byte[] encryptedDummy;
                var tx = (SqlTransaction)await dacConnection.BeginTransactionAsync(cancellationToken);
                try
                {
                    using (var alterCmd = dacConnection.CreateCommand())
                    {
                        alterCmd.Transaction = tx;
                        alterCmd.CommandText = dummyStatement;
                        await alterCmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    encryptedDummy = await ReadEncryptedBytesAsync(dacConnection, objectId, cancellationToken, tx);
                }
                finally
                {
                    await tx.RollbackAsync(cancellationToken);
                }

                if (encryptedDummy.Length == 0)
                {
                    return new DecryptionResult
                    {
                        Success = false,
                        Message = "Failed to obtain dummy keystream ciphertext from SQL Server."
                    };
                }

                // 6. XOR Stream Cipher Decryption
                byte[] dummyPlainBytes = Encoding.Unicode.GetBytes(dummyStatement);
                int minLen = Math.Min(encryptedOriginal.Length, Math.Min(encryptedDummy.Length, dummyPlainBytes.Length));
                byte[] decryptedBytes = new byte[minLen];

                for (int i = 0; i < minLen; i++)
                {
                    decryptedBytes[i] = (byte)(encryptedOriginal[i] ^ encryptedDummy[i] ^ dummyPlainBytes[i]);
                }

                string decryptedSql = Encoding.Unicode.GetString(decryptedBytes).TrimEnd('\0');

                // 7. Post-process and normalize decrypted definition
                string cleanDefinition = NormalizeDecryptedDefinition(decryptedSql, schemaName, objectName, resolvedType);

                return new DecryptionResult
                {
                    Success = true,
                    DecryptedDefinition = cleanDefinition,
                    Message = "Object definition decrypted successfully."
                };
            }
            catch (Exception ex)
            {
                return new DecryptionResult
                {
                    Success = false,
                    Message = $"Decryption error: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
            finally
            {
                if (dacConnection != null)
                {
                    await dacConnection.DisposeAsync();
                }

                // Automatically restore 'remote admin connections' back to 0 if it was initially disabled
                if (wasRemoteDacInitiallyDisabled)
                {
                    await RestoreRemoteDacAsync(config);
                }
            }
        }

        public async Task<OperationResult> EnableRemoteDacAsync(
            ServerConfiguration config,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            try
            {
                await using var connection = await _connectionFactory.OpenConnectionAsync(config, "master", cancellationToken);
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE WITH OVERRIDE;
EXEC sp_configure 'remote admin connections', 1;
RECONFIGURE WITH OVERRIDE;";
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                return OperationResult.SuccessResult("Remote Dedicated Administrator Connections (DAC) have been successfully enabled.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Unable to enable Remote DAC automatically: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> DisableRemoteDacAsync(
            ServerConfiguration config,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            try
            {
                await using var connection = await _connectionFactory.OpenConnectionAsync(config, "master", cancellationToken);
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE WITH OVERRIDE;
EXEC sp_configure 'remote admin connections', 0;
RECONFIGURE WITH OVERRIDE;";
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                return OperationResult.SuccessResult("Remote Dedicated Administrator Connections (DAC) have been safely disabled.");
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Unable to disable Remote DAC: {ex.Message}", ex);
            }
        }

        private async Task<(bool Success, SqlConnection? Connection, bool IsDacRequired, bool WasRemoteDacInitiallyDisabled, string Message, string? ErrorDetails)>
            OpenDacConnectionAsync(ServerConfiguration config, string databaseName, CancellationToken cancellationToken)
        {
            int? dacPort = null;
            bool wasRemoteDacInitiallyDisabled = false;
            string host = string.IsNullOrWhiteSpace(config.Server.ServerName) ? "localhost" : config.Server.ServerName.Trim();
            if (host == "." || host == "(local)") host = "localhost";
            bool isLocal = IsLocalServer(host);

            // Step A: Inspect SQL Server via the normal already-working connection
            try
            {
                await using var normalConn = await _connectionFactory.OpenConnectionAsync(config, "master", cancellationToken);

                // 1. Verify sysadmin permission
                await using (var cmdAdmin = normalConn.CreateCommand())
                {
                    cmdAdmin.CommandText = "SELECT IS_SRVROLEMEMBER('sysadmin');";
                    var isSysAdmin = Convert.ToInt32(await cmdAdmin.ExecuteScalarAsync(cancellationToken)) == 1;
                    if (!isSysAdmin)
                    {
                        return (false, null, false, false,
                            "Dedicated Administrator Connection (DAC) requires 'sysadmin' role. The current login does not have sysadmin privileges.",
                            "SQL Server error: User must be a member of sysadmin fixed server role to use DAC.");
                    }
                }

                // 2. Check remote admin connections setting
                await using (var cmdConfig = normalConn.CreateCommand())
                {
                    cmdConfig.CommandText = "SELECT CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'remote admin connections';";
                    var val = await cmdConfig.ExecuteScalarAsync(cancellationToken);
                    if (val != null && Convert.ToInt32(val) == 0)
                    {
                        wasRemoteDacInitiallyDisabled = true;
                        if (!isLocal)
                        {
                            // Enable remote DAC temporarily for remote hosts
                            await using var cmdEnable = normalConn.CreateCommand();
                            cmdEnable.CommandText = @"
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE WITH OVERRIDE;
EXEC sp_configure 'remote admin connections', 1;
RECONFIGURE WITH OVERRIDE;";
                            await cmdEnable.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                // 3. Discover dynamic DAC TCP port from SQL Server error log
                await using (var cmdLog = normalConn.CreateCommand())
                {
                    cmdLog.CommandText = @"
DECLARE @log TABLE (LogDate DATETIME, ProcessInfo VARCHAR(50), [Text] NVARCHAR(MAX));
INSERT INTO @log EXEC xp_readerrorlog 0, 1, N'Dedicated admin connection';
SELECT TOP 1 [Text] FROM @log WHERE [Text] LIKE '%port%' ORDER BY LogDate DESC;";
                    var logText = Convert.ToString(await cmdLog.ExecuteScalarAsync(cancellationToken));
                    if (!string.IsNullOrEmpty(logText))
                    {
                        var match = Regex.Match(logText, @"\bport\s+(\d+)\b", RegexOptions.IgnoreCase);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedPort))
                        {
                            dacPort = parsedPort;
                        }
                    }
                }
            }
            catch
            {
                // Continue with fallbacks if error log inspection is not permitted
            }

            // Step B: Build candidate DAC connection strings in priority order
            var candidates = new List<string>();

            // 1. Direct TCP to the discovered DAC port (Bypasses SQL Server Browser service requirement completely!)
            if (dacPort.HasValue && dacPort.Value > 0)
            {
                candidates.Add(BuildDacPortConnectionString(config, host, dacPort.Value, databaseName));
                if (isLocal)
                {
                    candidates.Add(BuildDacPortConnectionString(config, "localhost", dacPort.Value, databaseName));
                    candidates.Add(BuildDacPortConnectionString(config, "127.0.0.1", dacPort.Value, databaseName));
                }
            }

            // 2. Standard admin: connection string
            candidates.Add(BuildStandardDacConnectionString(config, host, databaseName));
            if (isLocal)
            {
                candidates.Add(BuildStandardDacConnectionString(config, "localhost", databaseName));
                candidates.Add(BuildStandardDacConnectionString(config, "127.0.0.1", databaseName));
                candidates.Add(BuildStandardDacConnectionString(config, ".", databaseName));
            }

            // 3. Named Pipes (Local only)
            if (isLocal)
            {
                var instance = ExtractInstanceName(config.Server.ServerName);
                var pipeHost = string.IsNullOrEmpty(instance)
                    ? @"np:\\.\pipe\sql\query"
                    : $@"np:\\.\pipe\MSSQL${instance}\sql\query";
                candidates.Add(BuildPipeDacConnectionString(config, pipeHost, databaseName));
            }

            // Step C: Try connecting using candidates
            Exception? lastEx = null;
            foreach (var cs in candidates.Distinct())
            {
                try
                {
                    var conn = new SqlConnection(cs);
                    await conn.OpenAsync(cancellationToken);
                    return (true, conn, false, wasRemoteDacInitiallyDisabled, "DAC connection established.", null);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            return (false, null, true, wasRemoteDacInitiallyDisabled,
                "Could not establish DAC (Dedicated Administrator Connection) to SQL Server.",
                lastEx?.Message);
        }

        private async Task RestoreRemoteDacAsync(ServerConfiguration config)
        {
            try
            {
                await using var restoreConn = await _connectionFactory.OpenConnectionAsync(config, "master", CancellationToken.None);
                await using var cmd = restoreConn.CreateCommand();
                cmd.CommandText = @"
EXEC sp_configure 'remote admin connections', 0;
RECONFIGURE WITH OVERRIDE;";
                await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch
            {
                // Silently ignore restoration errors to avoid masking operation results
            }
        }

        private static string NormalizeDecryptedDefinition(string decryptedSql, string schemaName, string objectName, string type)
        {
            if (string.IsNullOrWhiteSpace(decryptedSql)) return string.Empty;
            return decryptedSql.TrimEnd('\0', ' ', '\r', '\n');
        }

        private static async Task<byte[]> ReadEncryptedBytesAsync(
            SqlConnection connection,
            int objectId,
            CancellationToken cancellationToken,
            SqlTransaction? transaction = null)
        {
            using var cmd = connection.CreateCommand();
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.CommandText = @"
SELECT imageval 
FROM sys.sysobjvalues 
WHERE objid = @objid AND valclass = 1 
ORDER BY subobjid;";
            cmd.Parameters.Add(new SqlParameter("@objid", SqlDbType.Int) { Value = objectId });

            var chunks = new List<byte[]>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                {
                    var bytes = (byte[])reader[0];
                    chunks.Add(bytes);
                }
            }

            int totalLength = chunks.Sum(c => c.Length);
            var result = new byte[totalLength];
            int offset = 0;
            foreach (var chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        private static string BuildDummyStatement(string schemaName, string objectName, string type, int targetCharLength)
        {
            string tail;
            switch (type.Trim().ToUpperInvariant())
            {
                case "V":
                    tail = $"ALTER VIEW [{schemaName}].[{objectName}] WITH ENCRYPTION AS SELECT 1 AS [_c];";
                    break;

                case "P":
                    tail = $"ALTER PROCEDURE [{schemaName}].[{objectName}] WITH ENCRYPTION AS RETURN 0;";
                    break;

                case "FN":
                case "FS":
                    tail = $"ALTER FUNCTION [{schemaName}].[{objectName}]() RETURNS INT WITH ENCRYPTION AS BEGIN RETURN 0; END;";
                    break;

                case "IF":
                    tail = $"ALTER FUNCTION [{schemaName}].[{objectName}]() RETURNS TABLE WITH ENCRYPTION AS RETURN (SELECT 1 AS [_c]);";
                    break;

                case "TF":
                case "FT":
                    tail = $"ALTER FUNCTION [{schemaName}].[{objectName}]() RETURNS @t TABLE ([_c] INT) WITH ENCRYPTION AS BEGIN RETURN; END;";
                    break;

                default:
                    tail = $"ALTER PROCEDURE [{schemaName}].[{objectName}] WITH ENCRYPTION AS RETURN 0;";
                    break;
            }

            // Using Sebastian Meine's Chosen Plaintext Attack:
            // Pad the BEGINNING of the statement with spaces so that characters 0..(targetCharLength-1)
            // are GUARANTEED to be spaces (' ', 0x0020).
            // T-SQL permits unlimited leading whitespace before ALTER.
            // This eliminates any header alignment, shift, or offset errors completely.
            return new string(' ', targetCharLength) + tail;
        }

        private static string BuildDacPortConnectionString(ServerConfiguration config, string host, int port, string databaseName)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"tcp:{host},{port}",
                InitialCatalog = databaseName,
                ConnectTimeout = 10,
                Pooling = false,
                TrustServerCertificate = true,
                ApplicationName = Constants.ApplicationName
            };

            if (config.Server.UseWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = config.Server.Username;
                builder.Password = config.Server.Password;
            }

            return builder.ConnectionString;
        }

        private static string BuildStandardDacConnectionString(ServerConfiguration config, string host, string databaseName)
        {
            var dacHost = host.StartsWith("admin:", StringComparison.OrdinalIgnoreCase) ? host : $"admin:{host}";

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = dacHost,
                InitialCatalog = databaseName,
                ConnectTimeout = 10,
                Pooling = false,
                TrustServerCertificate = true,
                ApplicationName = Constants.ApplicationName
            };

            if (config.Server.UseWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = config.Server.Username;
                builder.Password = config.Server.Password;
            }

            return builder.ConnectionString;
        }

        private static string BuildPipeDacConnectionString(ServerConfiguration config, string pipePath, string databaseName)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = pipePath,
                InitialCatalog = databaseName,
                ConnectTimeout = 10,
                Pooling = false,
                TrustServerCertificate = true,
                ApplicationName = Constants.ApplicationName
            };

            if (config.Server.UseWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = config.Server.Username;
                builder.Password = config.Server.Password;
            }

            return builder.ConnectionString;
        }

        private static bool IsLocalServer(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName)) return true;
            var clean = serverName.Trim();
            return clean == "."
                || clean.Equals("(local)", StringComparison.OrdinalIgnoreCase)
                || clean.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith(".\\", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith("(local)\\", StringComparison.OrdinalIgnoreCase)
                || clean == "127.0.0.1"
                || clean.StartsWith("127.0.0.1,", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith("127.0.0.1\\", StringComparison.OrdinalIgnoreCase)
                || clean.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractInstanceName(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName)) return string.Empty;
            var idx = serverName.IndexOf('\\');
            if (idx >= 0 && idx < serverName.Length - 1)
            {
                var instance = serverName.Substring(idx + 1);
                var portIdx = instance.IndexOf(',');
                return portIdx >= 0 ? instance.Substring(0, portIdx).Trim() : instance.Trim();
            }
            return string.Empty;
        }
    }
}
