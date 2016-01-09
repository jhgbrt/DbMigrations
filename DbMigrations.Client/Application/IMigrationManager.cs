namespace DbMigrations.Client.Application
{
    internal interface IMigrationManager
    {
        bool MigrateSchema(bool whatif, bool reInit);
        bool ExecuteScripts(bool whatIf);
    }
}