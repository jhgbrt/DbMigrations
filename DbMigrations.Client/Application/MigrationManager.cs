using System;
using System.Collections.Generic;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Application
{
    class BufferedWriter
    {
        readonly Logger _logger;
        readonly Queue<string> _lines = new Queue<string>();
        private int _flush;
        private int _context = 2;

        public BufferedWriter(Logger logger)
        {
            _logger = logger;
        }

        public void Write(string text)
        {
            foreach (var line in text.Lines())
            {
                if (_flush > 0)
                {
                    _logger.Write(ConsoleColor.Gray, line);
                    _flush--;
                }
                else
                {
                    _lines.Enqueue(line);
                }
            }
            while (_lines.Count > _context) _lines.Dequeue();
        }

        public void Write(string text, ConsoleColor color)
        {
            while (_lines.Any())
            {
                _logger.Write(ConsoleColor.Gray, _lines.Dequeue());
            }
            _logger.Write(color, text);
            _flush = _context;
        }

    }

    public class MigrationManager : IMigrationManager
    {
        private Logger Logger { get; }
        private readonly IScriptFileRepository _scriptFileRepository;
        private readonly IDatabase _database;

        public MigrationManager(
            IScriptFileRepository scriptFileRepository, 
            IDatabase database, 
            Logger logger)
        {
            _scriptFileRepository = scriptFileRepository;
            _database = database;
            Logger = logger;
        }

        public bool MigrateSchema(bool whatif, bool reInit = false)
        {
            if (reInit)
            {
                _database.ClearAll();
            }

            if (!whatif)
                _database.EnsureMigrationsTable();

            var migrations = GetMigrations();

            if (!migrations.Any())
            {
                Logger.InfoLine("Database is consistent; no migrations to execute.");
                return true;
            }
            
            var ok = ApplyMigrations(whatif, migrations);

            if (!ok)
            {
                foreach (var migration in migrations)
                {
                    Logger.InfoLine(migration.ToString());
                    if (migration.IsConsistent) 
                        continue;
                    if (migration.HasChangedOnDisk)
                    {
                        var writer = new BufferedWriter(Logger);
                        var diffs = Diff.Compute(migration.Migration.Content, migration.Script.Content);
                        foreach (var diff in diffs)
                        {
                            switch (diff.Operation)
                            {
                                case Operation.Equal:
                                    writer.Write(diff.Text);
                                    break;
                                case Operation.Delete:
                                    writer.Write("(" + diff.Text + ")", ConsoleColor.DarkRed);
                                    break;
                                case Operation.Insert:
                                    writer.Write(diff.Text, ConsoleColor.DarkGreen);
                                    break;
                            }
                        }
                        
                    }
                }
                Logger.Line();
                return false;
            }

            return true;
        }
       
        private List<MigrationScript> GetMigrations()
        {
            var migrations = _database.GetMigrations();

            var scripts = _scriptFileRepository.GetScripts(ScriptKind.Migration);

            var migrationScripts = ZipWithScripts(migrations, scripts).ToList();

            return migrationScripts;
        }


        /// <summary>
        /// Zip db migrations with scripts on disk
        /// </summary>
        public static IEnumerable<MigrationScript> ZipWithScripts(IEnumerable<Migration> left, IEnumerable<Script> right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");

            var result = left.FullOuterJoin(right, m => m.ScriptName, s => s.ScriptName)
                .OrderBy(j => j.Key)
                .Select(x => new MigrationScript(x.Key, x.Left, x.Right))
                .ToList();

            result.ConnectMigrations();
            
            return result;
        }

        private bool ApplyMigrations(bool whatif, IList<MigrationScript> migrations)
        {
            var maxFileNameLength = migrations.Select(s => s.Name.Length).Max();

            foreach (var migration in migrations)
            {
                bool result = true;
                Logger.Info($"[{migration.Script?.Collection}] ");
                if (migration.IsConsistent)
                {
                    Logger.Info($"{migration.Name} was already applied.");
                }
                else if (migration.IsNewMigration)
                {
                    var script = migration.Script;
                    Logger.Info($"{migration.Name} - applying... ".PadRight(maxFileNameLength + 4));
                    if (!whatif)
                    {
                        try
                        {
                            _database.ApplyMigration(new Migration(script.ScriptName, script.Checksum, DateTime.UtcNow, script.Content));
                            Logger.Ok();
                        }
                        catch (Exception e)
                        {
                            Logger.Error("ERROR: " + e.Message);
                            result = false;
                        }
                    }
                }
                else
                {
                    Logger.Error($"ERROR: {migration}".PadRight(maxFileNameLength + 4));
                    result = false;
                }
                Logger.Line();
                if (!result)
                    return false;
            }
            return true;
        }

        public bool HasScripts(ScriptKind kind)
        {
            var scripts = _scriptFileRepository.GetScripts(kind);
            return scripts.Any();
        }

        public bool ExecuteScripts(bool whatif, ScriptKind kind)
        {
            var scripts = _scriptFileRepository.GetScripts(kind);

            var maxFolderNameLength = scripts.Select(s => s.Collection.Length).DefaultIfEmpty().Max();
            var maxFileNameLength = scripts.Select(s => s.ScriptName.Length).DefaultIfEmpty().Max();

            foreach (var script in scripts)
            {
                Logger.Info($"[{script.Collection}".PadRight(maxFolderNameLength + 1) + "] ");
                Logger.Info((script.ScriptName + "... ").PadRight(maxFileNameLength + 4));
                try
                {
                    if (!whatif)
                    {
                        _database.RunInTransaction(script.Content);
                        Logger.Ok();
                    }
                }
                catch (Exception e)
                {
                    Logger.ErrorLine("ERROR: " + e.Message);
                    return false;
                }
                Console.WriteLine();
            }
            return true;
        }
    }

    public static class Ex
    {
        public static void ConnectMigrations(this IList<MigrationScript> result)
        {
            foreach (var item in result.Zip(result.Skip(1), (l, r) => new { item = l, next = r }))
            {
                item.item.Next = item.next;
            }
        }
    }
}