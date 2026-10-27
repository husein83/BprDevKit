using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DataModelGenerator.Services
{
    public class DataModelGeneratorService
    {
        public List<GeneratedModelFile> GenerateModels(
            IReadOnlyList<TableOperation.Information> tables,
            DataModelOptions options)
        {
            var results = new List<GeneratedModelFile>();
            if (tables == null || tables.Count == 0) return results;

            foreach (var table in tables)
            {
                GeneratedModelFile file = options.Language switch
                {
                    DataModelLanguage.Python => PythonModelGenerator.Generate(table, options, tables),
                    DataModelLanguage.Dart => DartModelGenerator.Generate(table, options, tables),
                    DataModelLanguage.Java => JavaModelGenerator.Generate(table, options, tables),
                    DataModelLanguage.CSharp or _ => CSharpModelGenerator.Generate(table, options, tables)
                };

                results.Add(file);
            }

            // Optionally generate DbContext for C#
            if (options.Language == DataModelLanguage.CSharp && options.CSharp.GenerateDbContext)
            {
                var contextName = string.IsNullOrWhiteSpace(options.CSharp.DbContextName) ? "AppDbContext" : options.CSharp.DbContextName.Trim();
                var dbContextFile = CSharpModelGenerator.GenerateDbContext(
                    contextName,
                    options.CSharp.Namespace,
                    tables,
                    options);
                results.Add(dbContextFile);
            }

            return results;
        }

        public string CombineAllModels(IReadOnlyList<GeneratedModelFile> files, DataModelLanguage language)
        {
            if (files == null || files.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            var separator = language switch
            {
                DataModelLanguage.Python => "# ============================================================================\n",
                _ => "// ============================================================================\n"
            };

            foreach (var file in files)
            {
                sb.AppendLine(separator);
                sb.AppendLine($"{GetCommentPrefix(language)} File: {file.FileName} ({file.SchemaName}.{file.TableName})");
                sb.AppendLine(separator);
                sb.AppendLine(file.Content);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        public byte[] CreateZipArchive(IReadOnlyList<GeneratedModelFile> files, ZipPackagingStructure packaging = ZipPackagingStructure.Flat)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    string entryName;
                    switch (packaging)
                    {
                        case ZipPackagingStructure.BySchema:
                            var schemaFolder = string.IsNullOrWhiteSpace(file.SchemaName) ? "Default" : CleanPathSegment(file.SchemaName);
                            entryName = $"{schemaFolder}/{file.FileName}";
                            break;

                        case ZipPackagingStructure.ByNamespace:
                            var ns = string.IsNullOrWhiteSpace(file.Namespace)
                                ? (string.IsNullOrWhiteSpace(file.SchemaName) ? "Default" : file.SchemaName)
                                : file.Namespace;
                            var nsFolder = CleanPathSegment(ns.Replace('.', '/'));
                            entryName = $"{nsFolder}/{file.FileName}";
                            break;

                        case ZipPackagingStructure.Flat:
                        default:
                            entryName = file.FileName;
                            break;
                    }

                    var uniqueName = entryName;
                    int counter = 1;
                    while (!usedEntryNames.Add(uniqueName))
                    {
                        var dir = Path.GetDirectoryName(entryName)?.Replace('\\', '/');
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(entryName);
                        var ext = Path.GetExtension(entryName);
                        uniqueName = string.IsNullOrEmpty(dir)
                            ? $"{nameWithoutExt}_{counter}{ext}"
                            : $"{dir}/{nameWithoutExt}_{counter}{ext}";
                        counter++;
                    }

                    var entry = archive.CreateEntry(uniqueName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write(file.Content);
                }
            }

            return memoryStream.ToArray();
        }

        private static string CleanPathSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment)) return "Default";
            var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Except(new[] { '/', '\\' }).ToArray();
            var cleaned = string.Join("_", segment.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return cleaned.Trim('/', '\\');
        }

        private static string GetCommentPrefix(DataModelLanguage language) =>
            language == DataModelLanguage.Python ? "#" : "//";
    }
}
