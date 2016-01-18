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
        private readonly DbSpecifics _dbSpecifics;

        public Database(IDb db, DbSpecifics dbSpecifics)
        {
            TableName = dbSpecifics.TableName.Split('.').Last();
            Schema = dbSpecifics.Schema;
            _dbSpecifics = dbSpecifics;
            _db = db;
        }

        private bool MigrationsTableExists()
        {
            var query = _dbSpecifics.CountMigrationTablesStatement;

            var escapeCharacter = _dbSpecifics.EscapeCharacter;
            
            var parameterNames = query.Parameters(escapeCharacter);
            
            var parameters = GetParameterValues(parameterNames);

            var count = _db
                .Sql(_dbSpecifics.CountMigrationTablesStatement)
                .WithParameters(parameters)
                .AsScalar<int>();

            return count > 0;
        }

        private IDictionary<string, object> GetParameterValues(IEnumerable<string> parameterNames)
        {
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

            return parameters.ToDictionary(x => x.name, x => x.value);
        }


        private void InitializeTransaction()
        {
            if (!string.IsNullOrEmpty(_dbSpecifics?.ConfigureTransactionStatement))
                _db.Sql(_dbSpecifics.ConfigureTransactionStatement).AsNonQuery();
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
                _db.Execute(script);
                scope.Complete();
            }
        }

        public void EnsureMigrationsTable()
        {
            if (MigrationsTableExists()) return;

            _db.Execute(_dbSpecifics.CreateTableStatement);
        }

        public void ClearAll()
        {
            var statements = (
                from d in _db.Sql(_dbSpecifics.DropAllObjectsStatement)
                select (string) d.Statement
                ).ToArray();

            foreach (var statement in statements)
            {
                _db.Execute(statement);
            }
        }

        public IList<Migration> GetMigrations()
        {
            if (!MigrationsTableExists())
                return new List<Migration>();

            var migrations = (
                from d in _db.Sql(_dbSpecifics.SelectStatement)
                select new Migration(d.ScriptName, d.MD5, d.ExecutedOn, d.Content)
                ).ToArray();

            return migrations;
        }

        public void Insert(Migration item)
        {
            _db.Execute(_dbSpecifics.InsertStatement, item);
        }

        public void ApplyMigration(Migration migration)
        {
            RunInTransaction(migration.Content);
            Insert(migration);
        }
    }
}