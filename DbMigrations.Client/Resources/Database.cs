using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            TableName = queryConfiguration.TableName.Split('.').Last();
            Schema = queryConfiguration.Schema;
            _queryConfiguration = queryConfiguration;
            _db = db;
        }

        private bool MigrationsTableExists()
        {
            var query = _queryConfiguration.CountMigrationTablesStatement;

            var escapeCharacter = _queryConfiguration.EscapeCharacter;

            var parameterNames = query.Parameters(escapeCharacter);

            var parameterWithCorrespondingProperties = (
                from name in parameterNames
                let prop = GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)
                select new {prop, name}
                ).ToList();

            if (parameterWithCorrespondingProperties.Any(item => item.prop == null))
            {
                var invalidProperties = parameterWithCorrespondingProperties
                    .Where(item => item.prop == null)
                    .Select(x => x.name)
                    .ToList();
                var message =
                    $"The following {(invalidProperties.Count > 1 ? "properties are" : "property is")} not supported in the count query: '{string.Join(",", invalidProperties)}'.";
                throw new Exception(message);
            }

            var parameters = 
                from x in parameterWithCorrespondingProperties
                let value = x.prop.GetValue(this)
                select new {x.name, value};

            var commandBuilder = parameters.Aggregate(
                _db.Sql(query)
                , (cb, p) => cb.WithParameter(p.name, p.value)
                );

            return commandBuilder.AsScalar<int>() > 0;
        }


        private void InitializeTransaction()
        {
            if (!string.IsNullOrEmpty(_queryConfiguration?.ConfigureTransactionStatement))
                _db.Sql(_queryConfiguration.ConfigureTransactionStatement).AsNonQuery();
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private string TableName { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
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
            if (MigrationsTableExists()) return;

            _db.Sql(_queryConfiguration.CreateTableStatement).AsNonQuery();
        }

        public void ClearAll()
        {
            var statements = (
                from d in _db.Sql(_queryConfiguration.DropAllObjectsStatement)
                select (string) d.Statement).ToArray();

            foreach (var statement in statements)
            {
                _db.Execute(statement);
            }
        }

        public IList<Migration> GetMigrations()
        {
            return MigrationsTableExists() 
                ? _db.Sql(_queryConfiguration.SelectStatement).Select(d => new Migration(d.ScriptName, d.MD5, d.ExecutedOn, d.Content)).ToList() 
                : new List<Migration>();
        }

        public void Insert(Migration item)
        {
            _db.Sql(_queryConfiguration.InsertStatement).WithParameters(item).AsNonQuery();
        }

        public void ApplyMigration(Migration migration)
        {
            RunInTransaction(migration.Content);
            Insert(migration);
        }
    }
}