using System;
using System.Collections.Generic;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Application
{
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

        public bool MigrateSchema(bool whatif, bool syncOnly = false, bool reInit = false)
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

            var ok = ApplyMigrations(whatif, syncOnly, migrations);

            if (!ok)
            {
                foreach (var migration in migrations)
                {
                    Logger.InfoLine(migration.ToString());
                    if (migration.IsConsistent)
                        continue;
                    if (!migration.HasChangedOnDisk)
                        continue;
                    WriteDiffs(migration);
                }
                Logger.Line();
                return false;
            }

            return true;
        }

        private void WriteDiffs(MigrationScript migration)
        {
            var diffs = Diff.Compute(migration.Migration.Content, migration.Script.Content);
            foreach (var diff in diffs)
            {
                switch (diff.Operation)
                {
                    case Operation.Equal:
                        Logger.Info(diff.Text);
                        break;
                    case Operation.Delete:
                        Logger.Write(ConsoleColor.Red, "(" + diff.Text + ")");
                        break;
                    case Operation.Insert:
                        Logger.Write(ConsoleColor.Green, diff.Text);
                        break;
                }
            }
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
        private static IEnumerable<MigrationScript> ZipWithScripts(IEnumerable<Migration> left, IEnumerable<Script> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            var result = left.FullOuterJoin(right, m => m.ScriptName, s => s.ScriptName)
                .OrderBy(j => j.Key)
                .Select(x => new MigrationScript(x.Key, x.Left, x.Right))
                .ToList();

            result.ConnectMigrations();

            return result;
        }

        private bool ApplyMigrations(bool whatif, bool syncOnly, IList<MigrationScript> migrations)
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
                    if (!whatif)
                        result = Apply(migration.Script, syncOnly, maxFileNameLength + 4);
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

        private bool Apply(Script script, bool syncOnly, int padding)
        {
            try
            {
                var migrationRecord = new Migration(script.ScriptName, script.Checksum, DateTime.UtcNow, script.Content);
                if (!syncOnly)
                {
                    Logger.Info($"{script.ScriptName} - applying... ".PadRight(padding));
                    _database.ApplyMigration(migrationRecord);
                }
                else
                {
                    Logger.Info($"{script.ScriptName} - inserting... ".PadRight(padding));
                    _database.Insert(migrationRecord);
                }
                Logger.Ok();
            }
            catch (Exception e)
            {
                Logger.Error("ERROR: " + e.Message);
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
            foreach (var item in result.Zip(result.Skip(1), (l, r) => new {item = l, next = r}))
            {
                item.item.Next = item.next;
            }
        }
    }
}