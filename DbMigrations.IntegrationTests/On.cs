using System;

namespace DbMigrations.IntegrationTests
{

    public class On
    {
        public static On SqLite()
        {
            var sqliteAssembly = typeof (System.Data.SQLite.SQLiteConnection).Assembly;
            Console.WriteLine(sqliteAssembly.ToString());
            var initSqlStatement = ""; // a database is always created from scratch (since mstest always executes in a new folder)

            var providerName = "System.Data.SQLite";
            var server = @"db.db";
            var database = "MIGRATIONTEST";
            var masterConnectionString = $@"Data Source={server};";
            string connectionString = $@"Data Source={server};";
            var args = new[]
            {
                $@"--directory={@".\Scripts\SQLite"}",
                $@"--server={server}",
                $@"--database={database}",
                $@"--providerName={providerName}"
            };

            return new On("SqLite", server, database, @".\Scripts\SQLite", providerName, masterConnectionString, connectionString, initSqlStatement, args);

        }

        public static On SqlServer()
        {
            var initSqlStatement = "if exists (SELECT * FROM sys.databases WHERE Name = \'{0}\') \r\n" +
                                   "begin\r\n" +
                                   "\texec msdb.dbo.sp_delete_database_backuphistory \'{0}\'\r\n" +
                                   "\talter database {0} SET  SINGLE_USER WITH ROLLBACK IMMEDIATE\r\n" +
                                   "\tdrop database {0}\r\n" +
                                   "end\r\n" +
                                   "create database {0}\r\n";

            var server = @"localhost";
            var database = "MIGRATIONTEST";
            var masterConnectionString = $@"Data Source={server};Initial Catalog=master;Integrated Security=True";
            string connectionString = $@"Data Source={server};Initial Catalog={database};Integrated Security=True";
            var args = new[]
            {
                $@"--directory={@".\Scripts\SqlServer"}",
                $@"--server={server}",
                $@"--database={database}"
            };

            return new On("SqlServer", server, database, @".\Scripts\SqlServer", "System.Data.SqlClient", masterConnectionString, connectionString, initSqlStatement, args);
        }

        public static On Oracle()
        {
            var initSqlStatement = "DECLARE\r\n" +
                                   "\r\n" +
                                   "    c INTEGER := 0;\r\n" +
                                   "\r\n" +
                                   "BEGIN\r\n" +
                                   "    SELECT count(*) INTO c FROM sys.dba_users WHERE USERNAME = \'{0}\';\r\n" +
                                   "    IF c = 1 THEN\r\n" +
                                   "            execute immediate (\'drop user {0} cascade\');\r\n" +
                                   "            execute immediate (\'drop tablespace {0}_TS\');\r\n" +
                                   "            execute immediate (\'drop tablespace {0}_TS_TMP\');\r\n" +
                                   "    END IF;\r\n" +
                                   "    \r\n" +
                                   "    execute immediate (\'create tablespace {0}_TS datafile \'\'{0}.dat\'\' size 10M reuse autoextend on\');\r\n" +
                                   "    execute immediate (\'create temporary tablespace {0}_TS_TMP tempfile \'\'{0}_TMP.dat\'\' size 10M reuse autoextend on\');\r\n" +
                                   "    execute immediate (\'create user {0} identified by pass default tablespace {0}_TS temporary tablespace {0}_TS_TMP\');\r\n" +
                                   "    execute immediate (\'grant create session to {0}\');\r\n" +
                                   "    execute immediate (\'grant create table to {0}\');\r\n" +
                                   "    execute immediate (\'GRANT UNLIMITED TABLESPACE TO {0}\');\r\n" +
                                   "\r\n" +
                                   "END;";

            var server = @"localhost:1521/XE";
            var database = "MIGRATIONTEST";
            var masterConnectionString = $@"Data Source={server};DBA Privilege=SYSDBA;User Id=sys;Password=sys";
            var connectionString = $@"Data Source={server};User Id={database};Password=pass";
            var args = new[]
            {
                $@"--server={server}",
                $@"--user={database}",
                @"--password=pass",
                @"--providerName=Oracle.ManagedDataAccess.Client"
            };
            return new On("Oracle", server, database, @".\Scripts\Oracle", "Oracle.ManagedDataAccess.Client", masterConnectionString, connectionString, initSqlStatement, args);
        }


        private On(string name, string server, string database, string migrationFolder, string providerName, string masterConnectionString, string connectionString, string initSql, string[] args)
        {
            Name = name;
            Server = server;
            Database = database;
            MigrationFolder = migrationFolder;
            ProviderName = providerName;
            MasterConnectionString = masterConnectionString;
            ConnectionString = connectionString;
            DropRecreate = string.Format(initSql, database);
            Arguments = args;
        }

        public string Server { get; }

        public string Database { get; }

        public string MigrationFolder { get; }

        public string ProviderName { get; }

        public string MasterConnectionString { get; }

        public string ConnectionString { get; }

        public string[] Arguments { get; }

        public string DropRecreate { get; }
        public string Name { get;  }
    }
}