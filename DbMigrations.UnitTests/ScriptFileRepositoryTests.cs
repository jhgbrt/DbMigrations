using System.IO;
using System.Linq;
using DbMigrations.Client.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.UnitTests
{
    [TestClass]
    [DeploymentItem("Scripts", "Scripts")]
    public class ScriptFileRepositoryTests
    {
        [TestMethod]
        public void GetMigrationScripts_ReturnsMigrations()
        {
            var repo = new ScriptFileRepository(new DirectoryInfo(".\\Scripts\\UnitTests"));

            var migrations = repo.GetScripts(ScriptKind.Migration).Select(s => s.ScriptName).ToArray();

            CollectionAssert.AreEquivalent(
                new[]{"001.sql", "002.sql", "003.sql"}, 
                migrations
                );

        }

        [TestMethod]
        public void GetMigrationScripts_CalculatesCorrectChecksum()
        {
            var repo = new ScriptFileRepository(new DirectoryInfo(".\\Scripts\\UnitTests"));

            var script = repo.GetScripts(ScriptKind.Migration).First();

            var checksum = script.Checksum;

            Assert.AreEqual("9049690C31B0E6106EB20FB6ADCB6F12", checksum);
        }


        [TestMethod]
        public void GetOtherScripts_ReturnsScripts()
        {
            var repo = new ScriptFileRepository(new DirectoryInfo(".\\Scripts\\UnitTests"));

            var scripts = repo.GetScripts(ScriptKind.Other).Select(s => new{FolderName = s.Collection, s.ScriptName}).ToArray();

            var expected = new[] { @"01\001.sql", @"01\002.sql", @"02\001.sql", @"02\002.sql", @"02\003.sql" }.Select(s => new{FolderName = "DataLoads", ScriptName = s}).ToArray();

            CollectionAssert.AreEquivalent(
                expected, 
                scripts
                );
        }
    }
}