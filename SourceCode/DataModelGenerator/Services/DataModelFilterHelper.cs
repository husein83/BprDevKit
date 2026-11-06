using System;
using System.Linq;
using BprDevKit.DatabaseManagement.Models.Operations;
using BprDevKit.DataModelGenerator.Models;

namespace BprDevKit.DataModelGenerator.Services
{
    public static class DataModelFilterHelper
    {
        /// <summary>
        /// Checks whether the column represents a rowversion / timestamp concurrency token.
        /// </summary>
        public static bool IsRowVersion(ColumnOperation.Information col)
        {
            if (string.IsNullOrWhiteSpace(col.DataType)) return false;
            var dt = col.DataType.Trim();
            return string.Equals(dt, "rowversion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dt, "timestamp", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks whether the column represents a hierarchy/tree-node column (e.g. hierarchyid in SQL Server, or TreeNode/Hierarchy naming).
        /// </summary>
        public static bool IsHierarchyOrTreeNode(ColumnOperation.Information col)
        {
            if (!string.IsNullOrWhiteSpace(col.DataType))
            {
                var dt = col.DataType.Trim();
                if (string.Equals(dt, "hierarchyid", StringComparison.OrdinalIgnoreCase)
                    || dt.IndexOf("hierarchy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(col.ColumnName))
            {
                var name = col.ColumnName.Trim();
                if (name.IndexOf("treenode", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("tree_node", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("hierarchyid", StringComparison.OrdinalIgnoreCase) >= 0
                    || (name.IndexOf("hierarchy", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("node", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the column is a computed / persisted column.
        /// </summary>
        public static bool IsComputed(ColumnOperation.Information col)
        {
            return col.IsComputed || !string.IsNullOrWhiteSpace(col.ComputedFormula);
        }

        /// <summary>
        /// Checks whether the text matches any comma-, semicolon-, or newline-separated wildcard pattern.
        /// </summary>
        public static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern)) return false;
            if (pattern == "*") return true;

            var parts = pattern.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                var cur = p.Trim();
                if (cur == "*") return true;

                if (cur.StartsWith("*") && cur.EndsWith("*") && cur.Length > 2)
                {
                    var sub = cur.Substring(1, cur.Length - 2);
                    if (text.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                else if (cur.StartsWith("*") && cur.Length > 1)
                {
                    var suffix = cur.Substring(1);
                    if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (cur.EndsWith("*") && cur.Length > 1)
                {
                    var prefix = cur.Substring(0, cur.Length - 1);
                    if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (string.Equals(text, cur, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether a column should be included in data model generation based on user options.
        /// </summary>
        public static bool ShouldIncludeColumn(ColumnOperation.Information col, DataModelOptions options)
        {
            // 1. Excluded Column Patterns
            if (!string.IsNullOrWhiteSpace(options.ExcludedColumnPatterns) && MatchesPattern(col.ColumnName, options.ExcludedColumnPatterns))
            {
                return false;
            }

            // 2. RowVersion / Timestamp toggle
            if (!options.IncludeRowVersionColumns && IsRowVersion(col))
            {
                return false;
            }

            // 3. HierarchyId / Tree-Node toggle
            if (!options.IncludeHierarchyIdColumns && IsHierarchyOrTreeNode(col))
            {
                return false;
            }

            // 4. Computed column handling: Exclude mode
            if (options.ComputedColumnMode == ComputedColumnHandling.Exclude && IsComputed(col))
            {
                return false;
            }

            return true;
        }
    }
}

