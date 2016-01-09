using System;
using System.IO;
using System.Text;
using DbMigrations.Client.Application;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client
{
    public class Program
    {
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
                Logger.ErrorLine("Invalid configuration!");
                Logger.ErrorLine(stringBuilder.ToString());
                PrintHelp(config);
                return 1;
            }

            using (var db = new Db(config.ConnectionString, config.ProviderName))
            {
                var database = Database.From(db, config);
                var folder = new DirectoryInfo(config.Directory);

                IScriptFileRepository scripts = new ScriptFileRepository(folder);
                IMigrationManager manager = new MigrationManager(scripts, database);

                try
                {

                    Logger.InfoLine("db migration utility (c) Jeroen Haegebaert 2016");
                    Logger.WriteLine();

                    if (config.ReInit)
                    {
                        Logger.WarnLine("Reinitializing the database from scratch, are you sure? Y/[N]");
                        var readLine = Console.ReadLine();
                        if (readLine != "Y")
                            return 1;
                    }


                    Logger.WriteInfo("Connecting to the database... ");
                    db.Connect();
                    
                    Logger.OkLine();
                    Logger.WriteLine();
                    Logger.InfoLine("Performing database migrations");
                    Logger.InfoLine("==============================");
                    Logger.WriteLine();
                    if (!manager.MigrateSchema(config.WhatIf, config.ReInit))
                    {
                        Logger.InfoLine("Errors occurred. Use --help for for documentation.");
                        return 1;
                    }

                    Logger.WriteLine();
                    Logger.InfoLine("Running additional scripts");
                    Logger.InfoLine("==========================");
                    Logger.WriteLine();

                    if (!manager.ExecuteScripts(config.WhatIf))
                    {
                        Logger.InfoLine("Errors occurred. Use --help for for documentation.");
                        return 1;
                    }

                    Logger.WriteLine();
                    Logger.InfoLine("Migrations were successfully run");
                }
                catch (Exception e)
                {
                    Logger.ErrorLine(e.ToString());
                    return 1;
                }
                return 0;
            }
        }

        private static void PrintHelp(Config config)
        {
            Logger.InfoLine("Use this utility to execute DDL and SQL scripts against a database.");
            Logger.InfoLine("");
            Logger.InfoLine("Usage: migrate.exe [options]");
            Logger.InfoLine("Options:");
            Logger.InfoLine(config.GetHelp());
            if (!config.Help)
                return;
            Logger.InfoLine("Use this utility to execute DDL and SQL scripts against a database");
            Logger.InfoLine("");
            Logger.InfoLine("By convention, first all scripts in a folder called 'Migrations'");
            Logger.InfoLine("will be executed, in strict alphabetical order. Scripts may be");
            Logger.InfoLine("organized in subfolders, in which case the folders will also be");
            Logger.InfoLine("treated in alphabetical order. However, once a migration script");
            Logger.InfoLine("has been performed from a 'newer' folder, it is not allowed to");
            Logger.InfoLine("add new migrations in an 'earlier' folder.");
            Logger.InfoLine("");
            Logger.InfoLine("Migrations are tracked in the database with a checksum. Once a ");
            Logger.InfoLine("migration has been performed, it can not be changed. Also, new");
            Logger.InfoLine("versions of a migration package should always include all previous");
            Logger.InfoLine("migrations");
            Logger.InfoLine("");
            Logger.InfoLine("After the migrations have been run, *all* scripts in *all* other");
            Logger.InfoLine("subfolders are executed. These scripts can be data loads, updates of");
            Logger.InfoLine("views or stored procedures, etc. The only limitation is that these");
            Logger.InfoLine("scripts should be idempotent at all times, and consistent with the");
            Logger.InfoLine("current state of migrations");
            Logger.InfoLine("");
            Logger.InfoLine("Example folder structure:");
            Logger.InfoLine("");
            Logger.InfoLine("   Scripts");
            Logger.InfoLine("   ├───01_DataLoads");
            Logger.InfoLine("   |   ├───01_Phase1");
            Logger.InfoLine("   |   |   ├───001_referencedata1.sql");
            Logger.InfoLine("   |   |   └───002_samples.sql");
            Logger.InfoLine("   |   └───02_Phase2");
            Logger.InfoLine("   |       └───001_referencedata2.sql");
            Logger.InfoLine("   ├───02_ViewsAndSprocs");
            Logger.InfoLine("   |   ├───001_views.sql");
            Logger.InfoLine("   |   └───002_sprocs.sql");
            Logger.InfoLine("   └───Migrations");
            Logger.InfoLine("       ├───001_migration1.sql");
            Logger.InfoLine("       └───002_migration2.sql");
            Logger.InfoLine("");
            Logger.InfoLine("Scripts in this structure will be executed in this order:");
            Logger.InfoLine("");
            Logger.InfoLine(@"Scripts\Migrations\001_migration1.sql");
            Logger.InfoLine(@"Scripts\Migrations\002_migration2.sql");
            Logger.InfoLine(@"Scripts\01_DataLoads\01_Phase1\001_referencedata1.sql");
            Logger.InfoLine(@"Scripts\01_DataLoads\01_Phase1\002_samples.sql");
            Logger.InfoLine(@"Scripts\01_DataLoads\02_Phase2\002_referencedata2.sql");
            Logger.InfoLine(@"Scripts\02_ViewsAndSprocs\001_views.sql");
            Logger.InfoLine(@"Scripts\02_ViewsAndSprocs\002_sprocs.sql");
        }
    }
}
