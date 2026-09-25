using BprDevKit.DatabaseManagement.Config;

namespace BprDevKit.Utility
{
    public static class UserData
    {
        public static ServerConfiguration? SelectedServerConfiguration { get; set; }

        public static DateTime? LastActionDateTime { get; set; }
    }
}
