using System;
using System.IO;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public class ScriptFileRepository : IScriptFileRepository
    {
        readonly DirectoryInfo _directory;

        public ScriptFileRepository(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public Script[] GetScripts(ScriptKind kind)
        {
            Func<DirectoryInfo, bool> isMigrationFolder =
                d => d.Name.Equals("Migrations", StringComparison.InvariantCultureIgnoreCase);

            var q = 
                from d in _directory.EnumerateDirectories("*")
                where kind == ScriptKind.Migration ? isMigrationFolder(d) : !isMigrationFolder(d)
                from s in d.GetFiles("*.sql", SearchOption.AllDirectories)
                let folderName = d.FullName.Substring(_directory.FullName.Length + 1).Split(Path.PathSeparator).First()
                let scriptName = s.FullName.Substring(d.FullName.Length + 1)
                let content = ReadFile(s.FullName)
                let checksum = content.Checksum()
                orderby d.FullName ascending, scriptName ascending 
                select new Script(
                    folderName,
                    scriptName,
                    content,
                    checksum
                    );

            var scripts = q.ToArray();

            return scripts;
        }



        private static string ReadFile(string name)
        {
            string content;
            using (var stream = File.OpenRead(name))
            using (var reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }

    }
}