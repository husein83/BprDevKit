using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.Utility;

namespace BprDevKit.Config
{
    /// <summary>
    /// Package model for full application settings export and import.
    /// </summary>
    public class ConfigurationBackupPackage
    {
        public string AppVersion { get; set; } = Constants.Version;
        public DateTime ExportedAt { get; set; } = DateTime.Now;
        public string MachineName { get; set; } = Environment.MachineName;
        public ApplicationConfiguration? ApplicationConfig { get; set; }
        public List<ServerConfiguration>? ServerConfigs { get; set; }
        public List<DataModelProfile>? ModelGeneratorProfiles { get; set; }
    }

    /// <summary>
    /// Service to backup and restore all application configurations.
    /// </summary>
    public static class ConfigurationBackupService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public static ConfigurationBackupPackage CreateBackupPackage()
        {
            ApplicationConfiguration.LoadConfig();

            return new ConfigurationBackupPackage
            {
                AppVersion = Constants.Version,
                ExportedAt = DateTime.Now,
                MachineName = Environment.MachineName,
                ApplicationConfig = ApplicationConfiguration.Config,
                ServerConfigs = ServerConfiguration.LoadAllFromEncryptedFile(false),
                ModelGeneratorProfiles = ModelGeneratorConfiguration.LoadAllProfiles()
            };
        }

        public static string ExportToJson(ConfigurationBackupPackage? package = null)
        {
            package ??= CreateBackupPackage();
            return JsonSerializer.Serialize(package, JsonOptions);
        }

        public static (bool Success, string Message, ConfigurationBackupPackage? Package) ImportFromJson(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                    return (false, "Backup content is empty", null);

                var package = JsonSerializer.Deserialize<ConfigurationBackupPackage>(json, JsonOptions);
                if (package == null)
                    return (false, "Invalid configuration backup format", null);

                return (true, "Valid backup package", package);
            }
            catch (Exception ex)
            {
                return (false, $"Error parsing backup file: {ex.Message}", null);
            }
        }

        public static OperationResult RestorePackage(
            ConfigurationBackupPackage package,
            bool restoreServers = true,
            bool restoreModelProfiles = true,
            bool restoreAppSettings = true,
            bool mergeWithExisting = true)
        {
            try
            {
                var summary = new List<string>();

                // 1. Restore Servers
                if (restoreServers && package.ServerConfigs != null)
                {
                    if (mergeWithExisting)
                    {
                        var existingServers = ServerConfiguration.LoadAllFromEncryptedFile(false);
                        int added = 0, updated = 0;

                        foreach (var server in package.ServerConfigs)
                        {
                            var match = existingServers.FirstOrDefault(s =>
                                s.Server.ServerName.Equals(server.Server.ServerName, StringComparison.OrdinalIgnoreCase) &&
                                s.Server.DatabaseServerType == server.Server.DatabaseServerType);

                            if (match != null)
                            {
                                server.Id = match.Id;
                                server.CreateDate = match.CreateDate;
                                server.LastModifiedDate = DateTime.UtcNow;
                                existingServers.Remove(match);
                                existingServers.Add(server);
                                updated++;
                            }
                            else
                            {
                                server.Id ??= Guid.NewGuid();
                                server.CreateDate = DateTime.UtcNow;
                                existingServers.Add(server);
                                added++;
                            }
                        }
                        ServerConfiguration.SaveAll(existingServers);
                        summary.Add($"Servers: {added} added, {updated} updated");
                    }
                    else
                    {
                        ServerConfiguration.SaveAll(package.ServerConfigs);
                        summary.Add($"Servers: {package.ServerConfigs.Count} restored (overwrite)");
                    }
                }

                // 2. Restore Model Generator Profiles
                if (restoreModelProfiles && package.ModelGeneratorProfiles != null)
                {
                    if (mergeWithExisting)
                    {
                        var existingProfiles = ModelGeneratorConfiguration.LoadAllProfiles();
                        int added = 0, updated = 0;

                        foreach (var profile in package.ModelGeneratorProfiles)
                        {
                            var match = existingProfiles.FirstOrDefault(p =>
                                p.Id == profile.Id ||
                                p.ProfileName.Equals(profile.ProfileName, StringComparison.OrdinalIgnoreCase));

                            if (match != null)
                            {
                                profile.Id = match.Id;
                                profile.CreateDate = match.CreateDate;
                                profile.LastModifiedDate = DateTime.UtcNow;
                                existingProfiles.Remove(match);
                                existingProfiles.Add(profile);
                                updated++;
                            }
                            else
                            {
                                if (profile.Id == Guid.Empty) profile.Id = Guid.NewGuid();
                                profile.CreateDate = DateTime.UtcNow;
                                existingProfiles.Add(profile);
                                added++;
                            }
                        }
                        ModelGeneratorConfiguration.SaveAllProfiles(existingProfiles);
                        summary.Add($"Model Profiles: {added} added, {updated} updated");
                    }
                    else
                    {
                        ModelGeneratorConfiguration.SaveAllProfiles(package.ModelGeneratorProfiles);
                        summary.Add($"Model Profiles: {package.ModelGeneratorProfiles.Count} restored (overwrite)");
                    }
                }

                // 3. Restore Application Settings
                if (restoreAppSettings && package.ApplicationConfig != null)
                {
                    ApplicationConfiguration.SaveChanges(package.ApplicationConfig);
                    summary.Add("Application settings restored");
                }

                var msg = string.Join(" | ", summary);
                return OperationResult.SuccessResult(string.IsNullOrWhiteSpace(msg) ? "No changes applied" : msg);
            }
            catch (Exception ex)
            {
                return OperationResult.ErrorResult($"Error restoring configurations: {ex.Message}", ex);
            }
        }
    }
}
