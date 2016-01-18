using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using DbMigrations.Client.Application;
using DbMigrations.Client.Configuration;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

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

            var stringBuilder = new StringBuilder();
            if (!config.IsValid(stringBuilder))
            {
                Logger
                    .ErrorLine("Invalid configuration!")
                    .ErrorLine(stringBuilder.ToString());
                PrintHelp(config);
                return 1;
            }

            var dbSpecifics = DbSpecifics.Get(config);
            if (dbSpecifics != null && config.PersistConfiguration)
            {
                SaveConfiguration(dbSpecifics);
                return 0;
            }

            using (var db = new Db(config.ConnectionString, config.ProviderName))
            {
                var manager = CreateMigrationManager(config, db, dbSpecifics);
                try
                {
                    Logger
                        .InfoLine("db migration utility (c) Jeroen Haegebaert 2016")
                        .Line();

                    if (config.ReInit && !config.Force)
                    {
                        Logger.WarnLine("Reinitializing the database from scratch, are you sure? Y/[N]");
                        var readLine = Console.ReadLine();
                        if (readLine != "Y")
                            return 1;
                    }


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

                    
                    Logger
                        .Line()
                        .InfoLine("Migrations were successfully run");
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
            if (!manager.HasScripts(ScriptKind.PreMigration)) return true;
            Logger.Section("Running pre-migration scripts");
            return manager.ExecuteScripts(config.WhatIf, ScriptKind.PreMigration);
        }

        private static bool Migration(IMigrationManager manager, Config config)
        {
            Logger.Section("Performing database migrations");
            return manager.MigrateSchema(config.WhatIf, config.ReInit);
        }

        private static bool PostMigration(IMigrationManager manager, Config config)
        {
            if (!manager.HasScripts(ScriptKind.PostMigration)) return true;
            Logger.Section("Running post-migration scripts");
            return manager.ExecuteScripts(config.WhatIf, ScriptKind.PostMigration);
        }

        private static void SaveConfiguration(DbSpecifics dbSpecifics)
        {
            var c = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var m = (DbMigrationsConfigurationSection) c.GetSection("migrationConfig");
            if (m == null)
            {
                m = new DbMigrationsConfigurationSection();
                c.Sections.Add("migrationConfig", m);
            }
            dbSpecifics.SaveToConfig(m);
            c.Save(ConfigurationSaveMode.Minimal);
        }

        private static IMigrationManager CreateMigrationManager(Config config, IDb db, DbSpecifics queryConfig)
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

            Logger
                .InfoLine("Use this utility to execute DDL and SQL scripts against a database")
                .InfoLine("")
                .InfoLine("By convention, first all scripts in a folder called 'Migrations'")
                .InfoLine("will be executed, in strict alphabetical order. Scripts may be")
                .InfoLine("organized in subfolders, in which case the folders will also be")
                .InfoLine("treated in alphabetical order. However, once a migration script")
                .InfoLine("has been performed from a 'newer' folder, it is not allowed to")
                .InfoLine("add new migrations in an 'earlier' folder.")
                .InfoLine("")
                .InfoLine("Migrations are tracked in the database with a checksum. Once a ")
                .InfoLine("migration has been performed, it can not be changed. Also, new")
                .InfoLine("versions of a migration package should always include all previous")
                .InfoLine("migrations")
                .Line()
                .InfoLine("After the migrations have been run, *all* scripts in *all* other")
                .InfoLine("subfolders are executed. These scripts can be data loads, updates of")
                .InfoLine("views or stored procedures, etc. The only limitation is that these")
                .InfoLine("scripts should be idempotent at all times, and consistent with the")
                .InfoLine("current state of migrations")
                .InfoLine("")
                .InfoLine("Example folder structure:")
                .InfoLine("")
                .InfoLine("   Scripts")
                .InfoLine("   ├───01_DataLoads")
                .InfoLine("   |   ├───01_Phase1")
                .InfoLine("   |   |   ├───001_referencedata1.sql")
                .InfoLine("   |   |   └───002_samples.sql")
                .InfoLine("   |   └───02_Phase2")
                .InfoLine("   |       └───001_referencedata2.sql")
                .InfoLine("   ├───02_ViewsAndSprocs")
                .InfoLine("   |   ├───001_views.sql")
                .InfoLine("   |   └───002_sprocs.sql")
                .InfoLine("   └───Migrations")
                .InfoLine("       ├───001_migration1.sql")
                .InfoLine("       └───002_migration2.sql")
                .Line()
                .InfoLine("Scripts in this structure will be executed in this order:")
                .InfoLine("")
                .InfoLine(@"Scripts\Migrations\001_migration1.sql")
                .InfoLine(@"Scripts\Migrations\002_migration2.sql")
                .InfoLine(@"Scripts\01_DataLoads\01_Phase1\001_referencedata1.sql")
                .InfoLine(@"Scripts\01_DataLoads\01_Phase1\002_samples.sql")
                .InfoLine(@"Scripts\01_DataLoads\02_Phase2\002_referencedata2.sql")
                .InfoLine(@"Scripts\02_ViewsAndSprocs\001_views.sql")
                .InfoLine(@"Scripts\02_ViewsAndSprocs\002_sprocs.sql");
        }
    }
}
