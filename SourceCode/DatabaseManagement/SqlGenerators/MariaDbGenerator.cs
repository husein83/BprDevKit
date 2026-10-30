namespace BprDevKit.DatabaseManagement.SqlGenerators
{
    /// <summary>
    /// SQL generator for MariaDB databases (inherits MySQL generator standard logic).
    /// </summary>
    public class MariaDbGenerator : MySqlGenerator
    {
        public override string ProviderName => "MariaDB";
    }
}
