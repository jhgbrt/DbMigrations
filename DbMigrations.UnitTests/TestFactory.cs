using System;
using DbMigrations.Client.Model;

namespace DbMigrations.UnitTests
{
    public static class TestFactory
    {
        public static Migration ToMigration(this int i)
        {
            return new Migration(
                i.ToString().PadLeft(3, '0'), 
                i.ToString().GetHashCode().ToString(), 
                DateTime.UtcNow, 
                "SCRIPT CONTENT " + i);
        }

        public static Script ToScript(this int i, string checksum = null)
        {
            return new Script(
                "FOLDER", 
                i.ToString().PadLeft(3, '0'), 
                "SCRIPT CONTENT " + i, 
                checksum ?? i.ToString().GetHashCode().ToString()
                );
        }
    }

    public class MigrationScriptBuilder
    {
        private Script _script;
        private Migration _migration;
        private string _key;
        private MigrationScript _next;

        public MigrationScriptBuilder WithMigration(Migration m)
        {
            _key = m.ScriptName;
            _migration = m;
            return this;
        }
        public MigrationScriptBuilder WithoutMigration()
        {
            _migration = null;
            return this;
        }
        public MigrationScriptBuilder WithScript(Script s)
        {
            _key = s.ScriptName;
            _script = s;
            return this;
        }

        public MigrationScriptBuilder WithChecksum(string checksum)
        {
            _script = new Script(_script.Collection, _script.ScriptName, _script.Content, checksum);
            return this;
        }

        public MigrationScriptBuilder WithoutScript()
        {
            _script = null;
            return this;
        }

        public MigrationScriptBuilder WithNext(int i)
        {
            _next = Default(i).MigrationScript;
            return this;
        }

        private MigrationScriptBuilder(int i)
        {
            _key = i.ToString();
            _script = i.ToScript();
            _migration = i.ToMigration();
        }

        public static MigrationScriptBuilder Default(int i)
        {
            return new MigrationScriptBuilder(i);
        }

        public MigrationScript MigrationScript => new MigrationScript(_key, _migration, _script) {Next = _next};
    }
}