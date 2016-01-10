using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public interface IScriptFileRepository
    {
        Script[] GetScripts(ScriptKind kind);
    }
}