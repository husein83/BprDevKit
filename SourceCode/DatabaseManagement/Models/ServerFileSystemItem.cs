namespace BprDevKit.DatabaseManagement.Models
{
    public class ServerFileSystemItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long? SizeBytes { get; set; }
    }
}
