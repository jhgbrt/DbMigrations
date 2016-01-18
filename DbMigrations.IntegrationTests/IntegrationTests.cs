using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using DbMigrations.Client;
using DbMigrations.Client.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.IntegrationTests
{

    [TestClass]
    [DeploymentItem(@"Scripts", "Scripts")]
    [DeploymentItem("x64", "x64")]
    [DeploymentItem("x86", "x86")]
    [DeploymentItem("Oracle.ManagedDataAccess.dll")]
    public class IntegrationTests
    {

        [TestMethod]
        public void RunOracleTest()
        {
            DbProviderFactories.GetFactory("Oracle.ManagedDataAccess.Client");
            RunTest(On.Oracle());
        }

        [TestMethod]
        public void RunSQLiteTest()
        {
            DbProviderFactories.GetFactory("System.Data.SQLite");
            RunTest(On.SqLite());
        }

        [TestMethod]
        public void RunSqlServerTest()
        {
            DbProviderFactories.GetFactory("System.Data.SqlClient");
            RunTest(On.SqlServer());
        }   
        
        [TestMethod]
        public void RunSqlServerCeTest()
        {
            DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
            RunTest(On.SqlServerCe());
        }

        private void RunTest(On target)
        {
            var migrations = target.MigrationFolder;
            var connectionString = target.ConnectionString;
            var masterConnectionString = target.MasterConnectionString;
            var providerName = target.ProviderName;
           var args = target.Arguments.Concat(new[] {$"--directory={migrations}"} ).ToArray();

            Console.WriteLine("CONNECTING");
            using (var db = new Db(masterConnectionString, providerName))
            {
                try
                {
                    db.Connect();
                }
                catch (Exception e)
                {
                    Assert.Inconclusive("Could not connect to the database ({0})", e.Message);
                }
            }

            Console.WriteLine("==== DROP - RECREATE ====");
            using (var db = new Db(masterConnectionString, providerName))
            {
                db.Execute(target.DropRecreate);
            }

            Console.WriteLine("==== EXECUTE MIGRATIONS ====");
            var result = Program.Main(args);

            Assert.AreEqual(0, result);

            using (var db = new Db(connectionString, providerName))
            {
                var customer = db.Sql("SELECT FIRST_NAME, LAST_NAME FROM CUSTOMER")
                    .Select(d => new { FirstName = (string)d.FIRST_NAME, LastName = (string)d.LAST_NAME })
                    .First();
                Assert.AreEqual(new { FirstName = "John", LastName = "Doe" }, customer);

            }

            File.WriteAllText($@"{migrations}\Migrations\003.sql", "CREATE TABLE Orders (Id int not null, Description int not null)");

            Console.WriteLine("==== ADDED MIGRATION, EXECUTE MIGRATIONS ====");

            result = Program.Main(args);

            Assert.AreEqual(0, result);

            File.WriteAllText($@"{migrations}\Migrations\003.sql", "--MODIFIED\r\n" +
                                                                   "CREATE TABLE Orders (Id int not null, Description2 int not null)");

            Console.WriteLine("==== MODIFIED MIGRATION, EXECUTE MIGRATIONS ====");

            result = Program.Main(args);

            Assert.AreEqual(1, result);

            Console.WriteLine("==== REINITIALIZE MIGRATIONS ====");

            args = args.Concat(new[] {"--reinitialize", "--force"}).ToArray();

            result = Program.Main(args);

            Assert.AreEqual(0, result);
        }
    }


}
