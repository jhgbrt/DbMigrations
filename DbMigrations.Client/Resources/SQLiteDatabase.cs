using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    class SQLiteDatabase : IDatabase
    {
        private readonly IDb _db;

        public SQLiteDatabase(IDb db)
        {
            _db = db;
        }

        public void RunInTransaction(string script)
        {
            using (var scope = new TransactionScope())
            {
                _db.Sql(script).AsNonQuery();
                scope.Complete();
            }
        }

        public void EnsureMigrationsTable()
        {
            var queryFmt = "CREATE TABLE IF NOT EXISTS {0} (" +
                        "      ScriptName nvarchar NOT NULL PRIMARY KEY, " +
                        "      MD5 nvarchar NOT NULL, " +
                        "      ExecutedOn datetime NOT NULL," +
                        "      Content nvarchar NOT NULL" +
                        "  )";

            var query = string.Format(queryFmt, TableName);

            _db.Execute(query);
        }

        const string TableName = "Migrations";

        private static readonly Func<dynamic, Migration> Select =
            d => new Migration(d.ScriptName, d.MD5, d.ExecutedOn, d.Content);

        public IList<Migration> GetMigrations()
        {
            if (!TableExists())
                return new List<Migration>();

            var query = $"SELECT ScriptName, MD5, ExecutedOn, Content FROM {TableName} ORDER BY ScriptName ASC";

            return _db.Sql(query).Select(Select).ToList();
        }

        private bool TableExists()
        {
            var tableExists = "SELECT COUNT(*) FROM sqlite_master where type='table' and name = 'Migrations'";
            return _db.Sql(tableExists).AsScalar<int>() > 0;
        }

        public void ApplyMigration(Migration migration)
        {
            RunInTransaction(migration.Content);
            InsertMigration(migration);
        }

        public void ClearAll()
        {

            var statement = "declare @n char(1)\r\n" +
                            "set @n = char(10)\r\n" +
                            "\r\n" +
                            "declare @stmt nvarchar(max)\r\n" +
                            "\r\n" +
                            "-- procedures\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'drop procedure [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                            "from sys.procedures\r\n" +
                            "\r\n" +
                            "-- check constraints\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "\'alter table [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \']    drop constraint [\' + name + \']\'\r\n" +
                            "from sys.check_constraints\r\n" +
                            "\r\n" +
                            "-- views\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'drop view [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                            "from sys.views\r\n" +
                            "\r\n" +
                            "-- foreign keys\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'alter table [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \'] drop constraint [\' + name + \']\'\r\n" +
                            "from sys.foreign_keys\r\n" +
                            "\r\n" +
                            "-- tables\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'drop table [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                            "from sys.tables\r\n" +
                            "\r\n" +
                            "-- functions\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'drop function [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                            "from sys.objects\r\n" +
                            "where type in ( \'FN\', \'IF\', \'TF\' )\r\n" +
                            "\r\n" +
                            "-- user defined types\r\n" +
                            "Select @stmt = isnull( @stmt + @n, \'\' ) +\r\n" +
                            "    \'drop type [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                            "from sys.types\r\n" +
                            "where is_user_defined = 1\r\n" +
                            "\r\n" +
                            "Select @stmt";

            var dropEverything = _db.Sql(statement).AsScalar<string>();
            _db.Execute(dropEverything);
        }

        private void InsertMigration(Migration migration)
        {
            var query =
                $"INSERT INTO {TableName} (ScriptName, MD5, ExecutedOn, Content) VALUES (@ScriptName, @MD5, @ExecutedOn, @Content)";
            _db.Sql(query).WithParameters(migration).AsNonQuery();
        }
    }
}