using DbMigrations.Client.Application;
using DbMigrations.Client.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class MigrationScriptTests
    {
        [TestMethod]
        public void ConnectMigrations_MissingItemInLeftList_ItemIsUnexpectedExtra()
        {
            var input = new[]
            {
                MigrationScriptBuilder.Default(1).MigrationScript,
                MigrationScriptBuilder.Default(2).WithoutMigration().MigrationScript,
                MigrationScriptBuilder.Default(3).MigrationScript
            };

            input.ConnectMigrations();

            Assert.IsTrue(input[1].IsUnexpectedExtraScript);
        }

        [TestClass]
        public class WhenScriptAndMigrationHaveSameNameAndChecksum
        {
            private readonly MigrationScript _ms = MigrationScriptBuilder.Default(1).MigrationScript;

            [TestMethod]
            public void IsConsistent_ReturnsTrue()
            {
                Assert.IsTrue(_ms.IsConsistent);
            }
            [TestMethod]
            public void IsNewMigration_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsNewMigration);
            }
            [TestMethod]
            public void HasChangedOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(_ms.HasChangedOnDisk);
            }
            [TestMethod]
            public void IsMissingOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsMissingOnDisk);
            }
            [TestMethod]
            public void IsUnexpectedExtraScript_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsUnexpectedExtraScript);
            }
        }

        [TestClass]
        public class WhenScriptAndMigrationHaveSameNameAndDifferentChecksum
        {
            private MigrationScript ms = MigrationScriptBuilder.Default(1).WithChecksum("*").MigrationScript;

            [TestMethod]
            public void IsConsistent_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsConsistent);
            }
            [TestMethod]
            public void IsNewMigration_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsNewMigration);
            }
            [TestMethod]
            public void HasChangedOnDisk_ReturnsTrue()
            {
                Assert.IsTrue(ms.HasChangedOnDisk);
            }
            [TestMethod]
            public void IsMissingOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsMissingOnDisk);
            }
            [TestMethod]
            public void IsUnexpectedExtraScript_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsUnexpectedExtraScript);
            }
        }
        [TestClass]
        public class WhenScriptButNoMigration
        {
            private readonly MigrationScript _ms = MigrationScriptBuilder.Default(1).WithoutMigration().MigrationScript;

            [TestMethod]
            public void IsConsistent_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsConsistent);
            }
            [TestMethod]
            public void IsNewMigration_ReturnsTrue()
            {
                Assert.IsTrue(_ms.IsNewMigration);
            }
            [TestMethod]
            public void HasChangedOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(_ms.HasChangedOnDisk);
            }
            [TestMethod]
            public void IsMissingOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsMissingOnDisk);
            }
            [TestMethod]
            public void IsUnexpectedExtraScript_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsUnexpectedExtraScript);
            }
        }

        [TestClass]
        public class WhenMigrationButNoScript
        {
            private readonly MigrationScript _ms = MigrationScriptBuilder.Default(1).WithoutScript().MigrationScript;

            [TestMethod]
            public void IsConsistent_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsConsistent);
            }
            [TestMethod]
            public void IsNewMigration_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsNewMigration);
            }
            [TestMethod]
            public void HasChangedOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(_ms.HasChangedOnDisk);
            }
            [TestMethod]
            public void IsMissingOnDisk_ReturnsTrue()
            {
                Assert.IsTrue(_ms.IsMissingOnDisk);
            }
            [TestMethod]
            public void IsUnexpectedExtraScript_ReturnsFalse()
            {
                Assert.IsFalse(_ms.IsUnexpectedExtraScript);
            }
        }

        [TestClass]
        public class WhenScriptButNoMigrationAndNextMigration
        {
            private MigrationScript ms = MigrationScriptBuilder.Default(1)
                .WithoutMigration()
                .WithNext(2)
                .MigrationScript;

            [TestMethod]
            public void IsConsistent_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsConsistent);
            }
            [TestMethod]
            public void IsNewMigration_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsNewMigration);
            }
            [TestMethod]
            public void HasChangedOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(ms.HasChangedOnDisk);
            }
            [TestMethod]
            public void IsMissingOnDisk_ReturnsFalse()
            {
                Assert.IsFalse(ms.IsMissingOnDisk);
            }
            [TestMethod]
            public void IsUnexpectedExtraScript_ReturnsTrue()
            {
                Assert.IsTrue(ms.IsUnexpectedExtraScript);
            }
        }
    }
}