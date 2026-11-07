using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BprDevKit.DataModelGenerator;
using BprDevKit.DataModelGenerator.Models;
using BprDevKit.Utility;

namespace BprDevKit.Config
{
    /// <summary>
    /// Represents a saved configuration profile for Data Model Generator.
    /// Supports multiple profiles per server and database.
    /// </summary>
    public class DataModelProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProfileName { get; set; } = string.Empty;
        public Guid? ServerId { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }
        public DataModelOptions Options { get; set; } = new();

        #region Static Load & Save Operations
        public static List<DataModelProfile> LoadAll() => ModelGeneratorConfiguration.LoadAllProfiles();
        public static List<DataModelProfile> LoadAllProfiles() => ModelGeneratorConfiguration.LoadAllProfiles();
        public static void Save(DataModelProfile profile) => ModelGeneratorConfiguration.SaveProfile(profile);
        public static void SaveProfile(DataModelProfile profile) => ModelGeneratorConfiguration.SaveProfile(profile);
        public static void SaveAll(List<DataModelProfile> profiles) => ModelGeneratorConfiguration.SaveAllProfiles(profiles);
        public static void SaveAllProfiles(List<DataModelProfile> profiles) => ModelGeneratorConfiguration.SaveAllProfiles(profiles);
        public static bool Delete(Guid id) => ModelGeneratorConfiguration.DeleteProfile(id);
        public static bool DeleteProfile(Guid id) => ModelGeneratorConfiguration.DeleteProfile(id);
        public static List<DataModelProfile> GetProfilesFor(Guid? serverId, string? databaseName) => ModelGeneratorConfiguration.GetProfilesFor(serverId, databaseName);
        #endregion
    }

    /// <summary>
    /// Manages loading and saving encrypted model generator configuration profiles.
    /// </summary>
    public static class ModelGeneratorConfiguration
    {
        public static List<DataModelProfile> LoadAllProfiles()
        {
            try
            {
                string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ModelGeneratorConfiguration, out bool isExistsFile);
                if (isExistsFile)
                {
                    string encrypted = File.ReadAllText(filePath);
                    string decrypted = encrypted.DecryptContent();
                    if (decrypted.TryDevDeserialize<List<DataModelProfile>>(out var list) && list != null)
                    {
                        foreach (var p in list)
                        {
                            var cs = p.Options?.CSharp;
                            if (cs != null)
                            {
                                if (cs.CustomSnippets == null) cs.CustomSnippets = new();
                                if (cs.CustomSnippets.Count == 0 && !string.IsNullOrWhiteSpace(cs.CustomClassBodySnippet))
                                {
                                    cs.CustomSnippets.Add(new CustomCodeSnippetDefinition
                                    {
                                        TargetObjectType = "All",
                                        Title = "Custom Code",
                                        Code = cs.CustomClassBodySnippet,
                                        IsEnabled = true
                                    });
                                    cs.CustomClassBodySnippet = string.Empty;
                                }

                                if (cs.ClassAttributes == null) cs.ClassAttributes = new();
                                if (cs.ClassAttributes.Count == 0 && !string.IsNullOrWhiteSpace(cs.CustomClassAttributes))
                                {
                                    var lines = cs.CustomClassAttributes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                    foreach (var line in lines)
                                    {
                                        var clean = line.Trim();
                                        if (string.IsNullOrWhiteSpace(clean)) continue;
                                        cs.ClassAttributes.Add(new ClassAttributeDefinition
                                        {
                                            TargetObjectType = "All",
                                            Attribute = clean.StartsWith("[") ? clean : $"[{clean}]",
                                            IsEnabled = true
                                        });
                                    }
                                    cs.CustomClassAttributes = string.Empty;
                                }
                            }
                        }
                        return list;
                    }
                }
            }
            catch
            {
                // Fallback to empty list on corrupted or missing file
            }

            return new List<DataModelProfile>();
        }

        public static void SaveAllProfiles(List<DataModelProfile> profiles)
        {
            if (profiles.TryDevSerialize(out var jsonContent))
            {
                string filePath = DevFunctions.GetConfigurationPath(BprDevConfig.ModelGeneratorConfiguration, out bool isExistsFile);
                string encrypted = jsonContent.EncryptContent();
                File.WriteAllText(filePath, encrypted);
            }
        }

        public static void SaveProfile(DataModelProfile profile)
        {
            var all = LoadAllProfiles();
            var existingIndex = all.FindIndex(p => p.Id == profile.Id);
            if (existingIndex >= 0)
            {
                profile.LastModifiedDate = DateTime.UtcNow;
                all[existingIndex] = profile;
            }
            else
            {
                profile.CreateDate = DateTime.UtcNow;
                all.Add(profile);
            }
            SaveAllProfiles(all);
        }

        public static bool DeleteProfile(Guid id)
        {
            var all = LoadAllProfiles();
            var count = all.RemoveAll(p => p.Id == id);
            if (count > 0)
            {
                SaveAllProfiles(all);
                return true;
            }
            return false;
        }

        public static List<DataModelProfile> GetProfilesFor(Guid? serverId, string? databaseName)
        {
            var all = LoadAllProfiles();
            if (!serverId.HasValue && string.IsNullOrWhiteSpace(databaseName))
            {
                return all;
            }

            return all.Where(p =>
                (!p.ServerId.HasValue || p.ServerId == serverId) &&
                (string.IsNullOrWhiteSpace(p.DatabaseName) || string.Equals(p.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase))
            ).OrderBy(p => p.ProfileName).ToList();
        }
    }
}
