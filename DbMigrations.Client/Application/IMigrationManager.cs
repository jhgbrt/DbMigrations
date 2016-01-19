using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Application
{
    internal interface IMigrationManager
    {
        bool MigrateSchema(bool whatif, bool syncOnly, bool reInit);
        bool ExecuteScripts(bool whatIf, ScriptKind kind);
        bool HasScripts(ScriptKind kind);
    }
}