using DbMigrations.Client.Infrastructure;

namespace DbMigrations.Client.Resources
{
    static class Database
    {
        public static IDatabase From(IDb db, Config config)
        {
            if (db.ProviderName.StartsWith("Oracle"))
                return new OracleDatabase(db, config.Schema ?? config.UserName);
            if (db.ProviderName.Contains("SQLite"))
                return new SQLiteDatabase(db);
            return new SqlDatabase(db, config.Schema);
        }
    }
}