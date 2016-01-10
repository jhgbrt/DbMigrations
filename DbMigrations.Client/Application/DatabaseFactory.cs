using DbMigrations.Client.Databases;
using DbMigrations.Client.Databases.Oracle;
using DbMigrations.Client.Databases.SqlServer;
using DbMigrations.Client.Databases.SqLite;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Application
{
    static class DatabaseFactory
    {
        public static IDatabase FromConfig(IDb db, Config config)
        {
            if (config.ProviderName.StartsWith("Oracle"))
            {
                return new OracleDb(db, config);
            }
            if (config.ProviderName.Contains("SQLite"))
            {
                return new SqlLiteDb(db, config);
            }
            return new SqlServerDb(db, config);
        }
    }
}