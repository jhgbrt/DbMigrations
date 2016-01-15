using System.Configuration;
using DbMigrations.Client.Configuration;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Application
{
    static class DatabaseFactory
    {
        public static IDatabase FromConfig(IDb db, Config config)
        {
            Queries q;
            if (config.ProviderName.StartsWith("Oracle"))
            {
                q = Queries.Oracle.Instance(config);
            }
            else if (config.ProviderName.Contains("SqLite"))
            {
                q = Queries.SqLite.Instance();
            }
            else
            {
                q = Queries.SqlServer.Instance(config);
            }
            var c = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); 
            var m = (DbMigrationsConfiguration)c.GetSection("migrationConfig");
            q.SaveToConfig(m);
            c.Save();
            return new Database(db, q);
        }
    }
}