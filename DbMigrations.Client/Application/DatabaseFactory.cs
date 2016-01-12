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
                return new Database(db, Queries.Oracle.Instance(config));
            }
            if (config.ProviderName.Contains("SqLite"))
            {
                return new Database(db, Queries.SqLite.Instance());
            }
            return new Database(db, Queries.SqlServer.Instance(config));
        }
    }
}