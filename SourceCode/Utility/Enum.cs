using System.ComponentModel.DataAnnotations;

namespace BprDevKit.Utility
{
    public enum BprDevConfig
    {
        ApplicationConfiguration,
        ServerConfiguration,
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public enum Language
    {
        English,
        Persian
    }

    public enum DatabaseServerType
    {
        [Display(Name = "Microsoft SQL Server")]
        SqlServer = 1,

        [Display(Name = "PostgreSQL")]
        PostgreSql = 2,

        [Display(Name = "MySQL")]
        MySql = 3,

        [Display(Name = "MariaDB")]
        MariaDb = 4,

        [Display(Name = "Oracle Database")]
        Oracle = 5,

        [Display(Name = "SQLite")]
        Sqlite = 6
    }

    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Success,
        YesOrNo
    }

    public enum EntityState
    {
        Unchanged,
        Modified,
        Added,
        Deleted
    }
}
