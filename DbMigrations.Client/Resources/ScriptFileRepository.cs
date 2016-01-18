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
        private readonly string[] _pre;
        private readonly string[] _post;

        public ScriptFileRepository(DirectoryInfo directory, string[] pre = null, string[] post = null)
        {
            _directory = directory;
            
            // by default there are no pre-migration scripts
            _pre = (pre ?? Enumerable.Empty<string>()).Except(new[] { "Migrations" }).ToArray();
            
            // by default everything but the migration subfolder and 
            // the pre-migration folders (if any) are considered 
            // post-migration scripts
            _post = post ?? directory
                .GetDirectories()
                .Where(d => !IsMigrationFolder(d))
                .Where(d => !IsPreMigrationFolder(d))
                .Select(x => x.Name)
                .ToArray();
        }
        
        private bool IsPreMigrationFolder(DirectoryInfo d)
        {
            return _pre.Contains(d.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        private bool IsPostMigrationFolder(DirectoryInfo d)
        {
            return _post.Contains(d.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        private bool IsMigrationFolder(DirectoryInfo d)
        {
            return d.Name.Equals("Migrations", StringComparison.InvariantCultureIgnoreCase);
        }

        public Script[] GetScripts(ScriptKind kind)
        {
            Func<DirectoryInfo, bool> selector;
            switch (kind)
            {
                case ScriptKind.PreMigration:
                    selector = IsPreMigrationFolder;
                    break;
                case ScriptKind.Migration:
                    selector = IsMigrationFolder;
                    break;
                case ScriptKind.PostMigration:
                    selector = IsPostMigrationFolder;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            var q = from d in _directory.EnumerateDirectories("*") 
                    where selector(d) 
                    from s in d.GetFiles("*.sql", SearchOption.AllDirectories) 
                    let collectionName = Path.GetFileName(d.FullName.Substring(_directory.FullName.Length + 1).Split(Path.PathSeparator).First()) 
                    let scriptName = s.FullName.Substring(d.FullName.Length + 1) 
                    let content = ReadFile(s.FullName) 
                    let checksum = content.Checksum() 
                    orderby d.FullName ascending, scriptName ascending 
                    select new Script(collectionName, scriptName, content, checksum);

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