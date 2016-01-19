using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using DbMigrations.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using Net.Code.ADONet;

namespace DbMigrations.IntegrationTests
{

    [TestClass]
    [DeploymentItem(@"Scripts", "Scripts")]
    [DeploymentItem("x64", "x64")]
    [DeploymentItem("x86", "x86")]
    [DeploymentItem("Oracle.ManagedDataAccess.dll")]
    [DeploymentItem("System.Data.SQLite.dll")]
    public class IntegrationTests
    {

        [TestMethod]
        public void RunOracleTest()
        {
            RunTest(On.Oracle());
        }

        [TestMethod]
        public void RunSQLiteTest()
        {
            RunTest(On.SqLite());
        }

        [TestMethod]
        public void RunSqlServerTest()
        {
            RunTest(On.SqlServer());
        }   
        
        [TestMethod]
        public void RunSqlServerCeTest()
        {
            RunTest(On.SqlServerCe());
        }

        private void RunTest(On target)
        {
            var migrations = target.MigrationFolder;
            var connectionString = target.ConnectionString;
            var masterConnectionString = target.MasterConnectionString;
            var providerName = target.ProviderName;
            var args = target.Arguments.Concat(new[] {$"--directory={migrations}"}).ToArray();

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

            Assert.AreEqual(0, result, "Error while applying initial migrations");

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

            Assert.AreEqual(0, result, "Error while applying migrations after adding a new migration");

            File.WriteAllText($@"{migrations}\Migrations\003.sql", "--MODIFIED\r\n" +
                                                                   "CREATE TABLE Orders (Id int not null, Description2 int not null)");

            Console.WriteLine("==== MODIFIED MIGRATION, EXECUTE MIGRATIONS ====");

            result = Program.Main(args);

            Assert.AreEqual(1, result, "Expected an error when trying to execute migrations when when file is modified.");

            Console.WriteLine("==== REINITIALIZE MIGRATIONS ====");

            result = Program.Main(args.Concat(new[] {"--reinitialize", "--force", "--pre=Pre"}).ToArray());

            Assert.AreEqual(0, result, "Error while re-initializing migrations");
            
            File.WriteAllText($@"{migrations}\Migrations\004.sql", "THIS SCRIPT IS NOT ACTUALLY EXECUTED");

            Console.WriteLine("==== ADDED MIGRATION, SYNC WITHOUT APPLY ====");

            result = Program.Main(args.Concat(new[] {"--sync", "--force" }).ToArray());

            Assert.AreEqual(0, result, "Error while synchronizing the database with existing migrations");
            
            using (var db = new Db(connectionString, providerName))
            {
                var count = db.Sql("SELECT COUNT(*) FROM Migrations").AsScalar<int>();
                Assert.AreEqual(4, count);
            }

        }
    }


}
