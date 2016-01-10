using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    internal abstract class Database : IDatabase
    {
        protected readonly IDb Db;
        protected Database(IDb db, string esc, string tableName)
        {
            Esc = esc;
            TableName = tableName;
            Db = db;
        }

        private string Esc { get; }
        protected abstract bool MigrationsTableExists();
        protected abstract void CreateMigrationsTable();

        protected virtual void InitializeTransaction()
        {
        }

        protected abstract string[] GetDropAllObjectsStatements();
        protected string TableName { get; }

        public void RunInTransaction(string script)
        {
            using (var scope = new TransactionScope())
            {
                InitializeTransaction();
                Db.Sql(script).AsNonQuery();
                scope.Complete();
            }
        }

        public void EnsureMigrationsTable()
        {
            if (TableExists) return;

            CreateMigrationsTable();
        }

        public bool TableExists => MigrationsTableExists();

        public void ClearAll()
        {
            var statements = GetDropAllObjectsStatements();
            foreach (var statement in statements)
            {
                Db.Execute(statement);
            }
        }

        private string SelectMigration =>
            $"SELECT ScriptName, MD5, ExecutedOn, Content " +
            $"FROM {TableName} " +
            $"ORDER BY ScriptName ASC";

        public IList<Migration> Select()
        {
            return Db.Sql(SelectMigration).Select(d => new Migration(d.ScriptName, d.MD5, d.ExecutedOn, d.Content)).ToList();
        }

        private string InsertMigration =>
            $"INSERT INTO {TableName} (ScriptName, MD5, ExecutedOn, Content) " +
            $"VALUES ({Esc}ScriptName, {Esc}MD5, {Esc}ExecutedOn, {Esc}Content)";

        public void Insert(Migration item)
        {
            Db.Sql(InsertMigration).WithParameters(item).AsNonQuery();
        }
    }
}