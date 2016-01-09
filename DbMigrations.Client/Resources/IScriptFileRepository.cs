using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public enum ScriptKind
    {
        Migration,
        Other
    }
    public interface IScriptFileRepository
    {
        Script[] GetScripts(ScriptKind kind);
    }
}