using System;
using System.Collections.Generic;
using System.Linq;
using DbMigrations.Client.Model;
using DbMigrations.Client.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet;
using NSubstitute;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class DatabaseTests
    {
        private IDatabase _database;
        readonly IDb _db = Substitute.For<IDb>();
        private DbQueries _queries;

        [TestInitialize]
        public void Setup()
        {
            _queries = new DbQueries(
                "dummy",
                "@",
                "Migrations",
                "schema",
                "CONFIGURETX",
                "CREATE {0}",
                "COUNT",
                "DROP"
                );

            _database = new Database(_db, _queries);
        }

        [TestMethod]
        public void ClearAll()
        {
            SetupSelect("DROP", new[] { "DROP TABLE X" });
         
            _database.ClearAll();
            
            _db.Received(1).Execute("DROP TABLE X");
        }

        private void SetupSelect<T>(string query, T[] returnValue)
        {
            var cb = Substitute.For<ICommandBuilder>();
            cb.Select(Arg.Any<Func<dynamic, T>>()).Returns(returnValue);
            _db.Sql(query).Returns(cb);
        }

        [TestMethod]
        public void ApplyMigration()
        {
            var migration = new Migration("SCRIPTNAME", "MD5", DateTime.UtcNow, "CONTENT");
            _database.ApplyMigration(migration);
            _db.Received(1).Execute("CONTENT");
            _db.Received(1).Execute(_queries.InsertStatement, migration);
        }

        [TestMethod]
        public void EnsureMigrationsTable_MigrationTableExists_TableIsNotCreated()
        {
            SetupScalarQuery(_queries.CountMigrationTablesStatement, 1);
            
            _database.EnsureMigrationsTable();

            _db.DidNotReceive().Execute(_queries.CreateTableStatement);
        }
        [TestMethod]
        public void EnsureMigrationsTable_MigrationTableDoesNotExist_TableIsCreated()
        {
            SetupScalarQuery(_queries.CountMigrationTablesStatement, 0);

            _database.EnsureMigrationsTable();

            _db.Received(1).Execute(_queries.CreateTableStatement);
        }

        [TestMethod]
        public void GetMigrations_MigrationTableDoesNotExist_EmptyList()
        {
            SetupScalarQuery(_queries.CountMigrationTablesStatement, 0);

            _database.GetMigrations();

            _db.DidNotReceive().Sql(_queries.SelectStatement);
        }

        [TestMethod]
        public void GetMigrations_MigrationTableExists_EmptyList()
        {
            SetupScalarQuery(_queries.CountMigrationTablesStatement, 1);
            var migration = new Migration("NAME", "MD5", DateTime.UtcNow, "CONTENT");
            SetupSelect(_queries.SelectStatement, new []{migration});
            
            var result = _database.GetMigrations();

            _db.Received(1).Sql(_queries.SelectStatement);
            CollectionAssert.AreEqual(new[]{migration}, result.ToArray());
        }
        
        private void SetupScalarQuery<T>(string query, T returnValue)
        {
            var cb = Substitute.For<ICommandBuilder>();
            _db.Sql(query).Returns(cb);
            cb.WithParameters(Arg.Any<IDictionary<string, object>>()).Returns(cb);
            cb.WithParameters(Arg.Any<object>()).Returns(cb);
            cb.WithParameter(Arg.Any<string>(), Arg.Any<object>()).Returns(cb);
            cb.AsScalar<T>().Returns(returnValue);
        }
    }
}
