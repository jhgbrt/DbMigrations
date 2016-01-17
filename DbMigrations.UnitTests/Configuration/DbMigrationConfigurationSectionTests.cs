﻿using System.Configuration;
using System.Linq;
using DbMigrations.Client.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DbMigrations.UnitTests.Configuration
{
    [TestClass]
    public class DbMigrationConfigurationSectionTests
    {
        DbMigrationsConfiguration config = (DbMigrationsConfiguration)ConfigurationManager.GetSection("migrationConfig");
        [TestMethod]
        public void CanReadConfigSection()
        {
            Assert.IsNotNull(config);
        }

        [TestMethod]
        public void InvariantNameProperty()
        {
            Assert.AreEqual("Invariant.Name", config.InvariantName);
        }

        [TestMethod]
        public void TableNameProperty()
        {
            Assert.AreEqual("MyTableName", config.TableName);

        }
        [TestMethod]
        public void SchemaProperty()
        {
            Assert.AreEqual("MySchema", config.Schema);
        }

        [TestMethod]
        public void EscapeCharProperty()
        {
            Assert.AreEqual("x", config.EscapeChar);
        }

        [TestMethod]
        public void CountMigrationTablesProperty()
        {
            Assert.IsNotNull(config.CountMigrationTables);
        }

        [TestMethod]
        public void CountMigrationTablesHasSql()
        {
            Assert.AreEqual("SELECT *", config.CountMigrationTables.Sql.Value);
        }

        [TestMethod]
        public void SqlParameterIsNotNull()
        {
            Assert.IsNotNull(config.CountMigrationTables.Parameters);
        }
        [TestMethod]
        public void SqlParameterHasElement()
        {
            Assert.AreEqual("MyParameter", config.CountMigrationTables.Parameters.AllKeys[0]);
        }

        [TestMethod]
        public void SqlArgumentIsNotNull()
        {
            Assert.IsNotNull(config.CountMigrationTables.Arguments);
        }
        [TestMethod]
        public void SqlArgumentHasElement()
        {
            Assert.AreEqual("MyArgument", config.CountMigrationTables.Arguments.AllKeys[0]);
        }
    }
}