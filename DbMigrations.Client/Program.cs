using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using DbMigrations.Client.Application;
using DbMigrations.Client.Configuration;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;
using Net.Code.ADONet;

namespace DbMigrations.Client
{
    public static class Program
    {
        private static readonly Logger Logger = new Logger();
        public static int Main(string[] args)
        {

            var config = Config.Create(args);

            if (config.Help)
            {
                PrintHelp(config);
                return 0;
            }

            Logger.InfoLine("db migration utility (c) 2016").Line();

            var queries = DbQueries.Get(config);
            if (queries != null && config.PersistConfiguration)
            {
                SaveConfiguration(queries);
                Logger.InfoLine($"Configuration for {config.ProviderName} saved to config file.");
                return 0;
            }

            var stringBuilder = new StringBuilder();
            if (!config.IsValid(stringBuilder))
            {
                Logger
                    .ErrorLine("Invalid configuration!")
                    .ErrorLine(stringBuilder.ToString());
                PrintHelp(config);
                return 1;
            }

            if (config.ReInit && !config.Force)
            {
                Logger.WarnLine("Reinitializing the database from scratch. This will " +
                                "DROP all tables from the database! Are you sure? Y/[N]");
                var readLine = Console.ReadLine();
                if (readLine != "Y")
                    return 1;
            }

            if (config.Sync && !config.Force)
            {
                Logger.WarnLine("This will sync the database with the migration folder, without executing " +
                                "any scripts and without checking if the database state is actually consistent. " +
                                "Are you sure you know what you're doing? Y/[N]");
                var readLine = Console.ReadLine();
                if (readLine != "Y")
                    return 1;
            }

            using (var db = new Db(config.ConnectionString, config.ProviderName))
            {
                var manager = CreateMigrationManager(config, db, queries);
                try
                {
                    Logger.Info("Connecting to the database... ");
                    db.Connect();
                    Logger.OkLine();

                    Func<IMigrationManager, Config, bool>[] workflow = 
                    {
                        (m,c) => PreMigration(m, c),
                        (m,c) => Migration(m, c),
                        (m,c) => PostMigration(m, c)
                    };

                    var result = workflow.All(action => action(manager, config));
                    
                    if (!result)
                    {
                        Logger.InfoLine("Errors occurred. Use --help for for documentation.");
                        return 1;
                    }
                    
                    Logger.Line().InfoLine("Migrations were successfully run");
                }
                catch (Exception e)
                {
                    Logger.ErrorLine(e.ToString());
                    return 1;
                }
                return 0;
            }
        }

        private static bool PreMigration(IMigrationManager manager, Config config)
        {
            if (config.Sync || !manager.HasScripts(ScriptKind.PreMigration)) return true;
            Logger.Section("Running pre-migration scripts");
            return manager.ExecuteScripts(config.WhatIf, ScriptKind.PreMigration);
        }

        private static bool Migration(IMigrationManager manager, Config config)
        {
            Logger.Section("Performing database migrations");
            return manager.MigrateSchema(config.WhatIf, config.Sync, config.ReInit);
        }

        private static bool PostMigration(IMigrationManager manager, Config config)
        {
            if (config.Sync || !manager.HasScripts(ScriptKind.PostMigration)) return true;
            Logger.Section("Running post-migration scripts");
            return manager.ExecuteScripts(config.WhatIf, ScriptKind.PostMigration);
        }

        private static void SaveConfiguration(DbQueries dbQueries)
        {
            var c = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var m = (DbMigrationsConfigurationSection) c.GetSection("migrationConfig");
            if (m == null)
            {
                m = new DbMigrationsConfigurationSection();
                c.Sections.Add("migrationConfig", m);
            }
            dbQueries.SaveToConfig(m);
            c.Save(ConfigurationSaveMode.Minimal);
        }

        private static IMigrationManager CreateMigrationManager(Config config, IDb db, DbQueries queryConfig)
        {
            var database = new Database(db, queryConfig);
            var folder = new DirectoryInfo(config.Directory);
            var scripts = new ScriptFileRepository(folder, config.PreMigration, config.PostMigration);
            var manager = new MigrationManager(scripts, database, Logger);
            return manager;
        }

        private static void PrintHelp(Config config)
        {
            Logger
                .InfoLine("Use this utility to execute DDL and SQL scripts against a database.")
                .InfoLine("")
                .InfoLine("Usage: migrate.exe [options]")
                .InfoLine("Options:")
                .InfoLine(config.GetHelp());

            if (!config.Help)
                return;

            Logger.WriteHelp();
        }
    }
}
