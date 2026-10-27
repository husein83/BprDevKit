using System;
using System.Collections.Generic;

namespace BprDevKit.ScriptGenerator.Models
{
    public enum ScriptType
    {
        SchemaOnly = 0,
        DataOnly = 1,
        SchemaAndData = 2
    }

    public sealed class ScriptingOptions
    {
        public ScriptType ScriptType { get; set; } = ScriptType.SchemaAndData;

        public bool IncludeIfNotExists { get; set; } = true;

        public bool IncludeDropAndCreate { get; set; } = false;

        public bool IncludeForeignKeys { get; set; } = true;

        public bool IncludeIndexes { get; set; } = true;

        public bool IncludeCheckConstraints { get; set; } = true;

        public bool IncludeTriggers { get; set; } = true;

        public bool IncludeComments { get; set; } = true;

        public bool IncludeUseDatabase { get; set; } = true;

        public int MaxDataRowsPerTable { get; set; } = 1000;

        public HashSet<string> SelectedSchemas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> SelectedTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> SelectedViews { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> SelectedProcedures { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> SelectedFunctions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> SelectedTriggers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public int TotalSelectedCount =>
            SelectedSchemas.Count +
            SelectedTables.Count +
            SelectedViews.Count +
            SelectedProcedures.Count +
            SelectedFunctions.Count +
            SelectedTriggers.Count;
    }
}
