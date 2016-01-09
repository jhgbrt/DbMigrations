using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    internal class OracleDatabase : IDatabase
    {
        private readonly IDb _db;
        private readonly string _schema;
        
        public OracleDatabase(IDb db, string schema)
        {
            _db = db;
            _schema = schema;
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
            if (TableExists()) return;
            var queryFmt =
                "CREATE TABLE {0} (" +
                "     SCRIPT_NAME nvarchar2(255) not null, " +
                "     MD5 nvarchar2(32) not null, " +
                "     EXECUTED_ON date not null, " +
                "     CONTENT CLOB not null, " +
                "     CONSTRAINT PK_MIGRATIONS PRIMARY KEY (SCRIPT_NAME)" +
                ")";

            var query = string.Format(queryFmt , TableName);

            _db.Execute(query);
        }

        private string TableName => string.IsNullOrEmpty(_schema) ? "MIGRATIONS" : _schema + "." + "MIGRATIONS";

        private void InsertMigration(Migration migration)
        {
            var query =
                $@"INSERT INTO {TableName} (SCRIPT_NAME, MD5, EXECUTED_ON, CONTENT) VALUES (:ScriptName, :MD5, :ExecutedOn, :Content)";
            _db.Sql(query).WithParameters(migration).AsNonQuery();
        }

        private static Migration Select(dynamic d) => new Migration(d.SCRIPT_NAME, d.MD5, d.EXECUTED_ON, d.CONTENT);

        public IList<Migration> GetMigrations()
        {
            if (!TableExists())
                return new List<Migration>();

            var query = $"SELECT SCRIPT_NAME, MD5, EXECUTED_ON, CONTENT FROM {TableName} ORDER BY SCRIPT_NAME ASC";

            return _db.Sql(query).Select(Select).ToList();
        }

        private bool TableExists()
        {
            var tableExists = "SELECT COUNT(*) FROM ALL_TABLES where TABLE_NAME = 'MIGRATIONS' and OWNER = :Schema";
            return _db.Sql(tableExists).WithParameter("Schema", _schema).AsScalar<int>() > 0;
        }

        public void ApplyMigration(Migration migration)
        {
            RunInTransaction(migration.Content);
            InsertMigration(migration);
        }

        public void ClearAll()
        {
            throw new NotSupportedException();
        }
    }
}