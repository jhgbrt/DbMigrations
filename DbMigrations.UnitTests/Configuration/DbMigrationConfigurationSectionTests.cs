using System.Configuration;
using System.Linq;
using DbMigrations.Client.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DbMigrations.UnitTests.Configuration
{
    [TestClass]
    public class DbMigrationConfigurationSectionTests
    {
        DbMigrationsConfigurationSection config = (DbMigrationsConfigurationSection)ConfigurationManager.GetSection("migrationConfig");
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
            Assert.AreEqual("SELECT count(*) FROM information_schema.tables WHERE TableName = @TableName AND Schema = @Schema", config.CountMigrationTables.Sql.Trim());
        }

        [TestMethod]
        public void CreateMigrationTableProperty()
        {
            Assert.IsNotNull(config.CreateMigrationTable);
        }

        [TestMethod]
        public void CreateMigrationTableHasSql()
        {
            Assert.AreEqual("CREATE TABLE {TableName} (ScriptName nvarchar2(100))", config.CreateMigrationTable.Sql);
        }
        [TestMethod]
        public void DropAllObjectsProperty()
        {
            Assert.IsNotNull(config.DropAllObjects);
        }

        [TestMethod]
        public void DropAllObjectsHasSql()
        {
            Assert.AreEqual("SELECT x AS Statement", config.DropAllObjects.Sql);
        }
        [TestMethod]
        public void InitTxProperty()
        {
            Assert.IsNotNull(config.ConfigureTransaction);
        }

        [TestMethod]
        public void InitTxHasSql()
        {
            Assert.AreEqual("SELECT 'INIT TX'", config.ConfigureTransaction.Sql);
        }
    }
}
