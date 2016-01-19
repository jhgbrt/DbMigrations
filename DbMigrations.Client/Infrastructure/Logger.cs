using System;
using System.Linq;
using static System.Console;
using static System.ConsoleColor;

namespace DbMigrations.Client.Infrastructure
{
    public class Logger
    {
        private static readonly object Lock = new object();
        public Logger InfoLine(string message) => Info(message + "\r\n");
        public Logger Info(string message) => Write(ForegroundColor, message);
        public Logger WarnLine(string message) => Warn(message).Line();
        public Logger Warn(string message) => Write(Yellow, message);
        public Logger ErrorLine(string message) => Error(message).Line();
        public Logger Error(string message) => Write(Red, message);
        public Logger Ok() => Write(Green, "OK");
        public Logger OkLine() => Ok().Line();

        public Logger Write(ConsoleColor foregroundColor, string message)
        {
            var originalColor = ForegroundColor;
            lock (Lock)
            {
                try
                {
                    ForegroundColor = foregroundColor;
                    Console.Write(message);
                }
                finally
                {
                    ForegroundColor = originalColor;
                }
            }
            return this;
        }

        public Logger Line()
        {
            WriteLine();
            return this;
        }

        public Logger Section(string message)
        {
            return Line()
                .InfoLine(message)
                .InfoLine(new string(Enumerable.Repeat('=', message.Length).ToArray()))
                .Line();
        }

        public void WriteHelp()
        {
                 InfoLine("Use this utility to execute DDL and SQL scripts against a database")
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
