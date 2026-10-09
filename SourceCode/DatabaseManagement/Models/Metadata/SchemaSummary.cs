using BprDevKit.DatabaseManagement.Models.Operations;

namespace BprDevKit.DatabaseManagement.Models.Metadata
{
    /// <summary>
    /// One row of the schema list: the schema, its owner and how many objects it contains.
    /// </summary>
    public class SchemaSummary
    {
        public string SchemaName { get; set; } = null!;
        public string OwnerName { get; set; } = null!;

        /// <summary>Tables, views, routines, synonyms, sequences, user-defined types and XML schema collections.</summary>
        public int ObjectCount { get; set; }

        public bool IsProtected => SchemaOperation.IsProtected(SchemaName);
    }
}
