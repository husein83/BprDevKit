namespace BprDevKit.DatabaseManagement.Models
{
    public class SchemaInfo
    {
        public string SchemaName { get; set; } = null!;
        public string SchemaType { get; set; } = null!;
        public DateTime CreateDate { get; set; }
    }
}
