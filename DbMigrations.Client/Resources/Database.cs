using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    internal class Database : IDatabase
    {
        private readonly IDb _db;
        private readonly QueryConfiguration _queryConfiguration;

        public Database(IDb db, QueryConfiguration queryConfiguration)
        {
            TableName = queryConfiguration.TableName;
            Schema = queryConfiguration.Schema;
            _queryConfiguration = queryConfiguration;
            _db = db;
        }

        private bool MigrationsTableExists()
        {
            return _db
                .Sql(_queryConfiguration.CountMigrationTablesStatement)
                .WithParameters(new
                {
                    TableName = TableName.Split('.').Last(),
                    Schema
                }).AsScalar<int>() > 0;
        }


        private void InitializeTransaction()
        {
            if (!string.IsNullOrEmpty(_queryConfiguration?.ConfigureTransactionStatement))
                _db.Sql(_queryConfiguration.ConfigureTransactionStatement).AsNonQuery();
        }

        private string[] GetDropAllObjectsStatements()
        {
            return _db.Sql(_queryConfiguration.DropAllObjectsStatement).Select(d => (string) d.Statement).ToArray();
        }

        private string TableName { get; }
        private string Schema { get; }

        public void RunInTransaction(string script)
        {
            using (var scope = new TransactionScope())
            {
                InitializeTransaction();
                _db.Sql(script).AsNonQuery();
                scope.Complete();
            }
        }

        public void EnsureMigrationsTable()
        {
            if (TableExists) return;

            _db.Sql(_queryConfiguration.CreateTableStatement).AsNonQuery();
        }

        public bool TableExists => MigrationsTableExists();

        public void ClearAll()
        {
            var statements = GetDropAllObjectsStatements();
            foreach (var statement in statements)
            {
                _db.Execute(statement);
            }
        }

        private string SelectMigration => _queryConfiguration.SelectStatement;

        public IList<Migration> GetMigrations()
        {
            if (!TableExists)
                return new List<Migration>();
            return _db.Sql(SelectMigration).Select(d => new Migration(d.ScriptName, d.MD5, d.ExecutedOn, d.Content)).ToList();
        }

        private string InsertMigration => _queryConfiguration.InsertStatement;

        public void Insert(Migration item)
        {
            _db.Sql(InsertMigration).WithParameters(item).AsNonQuery();
        }

        public void ApplyMigration(Migration migration)
        {
            RunInTransaction(migration.Content);
            Insert(migration);
        }
    }
}