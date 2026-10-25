using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.Services.DataModels
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

        public byte[] CreateZipArchive(IReadOnlyList<GeneratedModelFile> files)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var entry = archive.CreateEntry(file.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write(file.Content);
                }
            }

            return memoryStream.ToArray();
        }

        private static string GetCommentPrefix(DataModelLanguage language) =>
            language == DataModelLanguage.Python ? "#" : "//";
    }
}
