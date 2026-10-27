using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BprDevKit.DataModelGenerator.Services
{
    public static class DataModelNamingHelper
    {
        public static string FormatClassName(string rawName, bool removePrefixes, string prefixList, bool singularize)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Unnamed";

            var clean = rawName.Trim();

            if (removePrefixes && !string.IsNullOrWhiteSpace(prefixList))
            {
                var prefixes = prefixList.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var prefix in prefixes)
                {
                    if (clean.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        clean = clean.Substring(prefix.Length);
                        break;
                    }
                }
            }

            clean = ToPascalCase(clean);

            if (singularize)
            {
                clean = Singularize(clean);
            }

            if (string.IsNullOrWhiteSpace(clean))
            {
                clean = ToPascalCase(rawName);
            }

            // Ensure valid identifier
            if (!char.IsLetter(clean[0]) && clean[0] != '_')
            {
                clean = "Item" + clean;
            }

            return clean;
        }

        public static string FormatPropertyName(string rawName, NamingConvention convention)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Property";

            return convention switch
            {
                NamingConvention.Original => SanitizeIdentifier(rawName),
                NamingConvention.PascalCase => ToPascalCase(rawName),
                NamingConvention.CamelCase => ToCamelCase(rawName),
                NamingConvention.SnakeCase => ToSnakeCase(rawName),
                _ => ToPascalCase(rawName)
            };
        }

        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var words = SplitIntoWords(input);
            if (words.Count == 0) return input;

            var sb = new StringBuilder();
            foreach (var word in words)
            {
                if (word.Length == 1)
                {
                    sb.Append(char.ToUpperInvariant(word[0]));
                }
                else
                {
                    sb.Append(char.ToUpperInvariant(word[0]));
                    sb.Append(word.Substring(1).ToLowerInvariant());
                }
            }

            var result = sb.ToString();
            return string.IsNullOrEmpty(result) ? input : result;
        }

        public static string ToCamelCase(string input)
        {
            var pascal = ToPascalCase(input);
            if (string.IsNullOrEmpty(pascal)) return string.Empty;
            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        public static string ToSnakeCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var words = SplitIntoWords(input);
            if (words.Count == 0) return input.ToLowerInvariant();

            return string.Join("_", words.Select(w => w.ToLowerInvariant()));
        }

        private static List<string> SplitIntoWords(string input)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return list;

            // Handle underscore or whitespace separators
            var parts = Regex.Split(input, @"[_\s\-]+");
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                // Split camelCase or PascalCase inside part (e.g. "CustomerID" -> "Customer", "ID")
                var matches = Regex.Matches(part, @"([A-Z]+(?=[A-Z][a-z0-9]|$)|[A-Z][a-z0-9]+|[a-z0-9]+)");
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (!string.IsNullOrEmpty(match.Value))
                            list.Add(match.Value);
                    }
                }
                else
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public static string Singularize(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

            // Common irregular plurals
            if (word.EndsWith("People", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 6) + "Person";
            if (word.EndsWith("Children", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 8) + "Child";
            if (word.EndsWith("Data", StringComparison.OrdinalIgnoreCase))
                return word;

            // Rules
            if (word.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && word.Length > 3)
                return word.Substring(0, word.Length - 3) + "y";

            if (word.EndsWith("sses", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 2);

            if (word.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("shes", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("ches", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 2);

            if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
                !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
                !word.EndsWith("us", StringComparison.OrdinalIgnoreCase) &&
                !word.EndsWith("is", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 1);

            return word;
        }

        public static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Field";
            var sb = new StringBuilder();
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    sb.Append(ch);
                }
            }
            var result = sb.ToString();
            if (string.IsNullOrEmpty(result)) return "Field";
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }
    }
}
