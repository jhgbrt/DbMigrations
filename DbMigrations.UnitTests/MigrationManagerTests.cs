using System;
using DbMigrations.Client.Application;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;
using DbMigrations.Client.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class MigrationManagerTests
    {
        private readonly MigrationManager _migrationManager;
        private readonly IScriptFileRepository _scriptFileRepository;
        private readonly IMigrationRepository _migrationRepository;
        private readonly IDatabase _database;

        public MigrationManagerTests()
        {
            _scriptFileRepository = Substitute.For<IScriptFileRepository>();
            _migrationRepository = Substitute.For<IMigrationRepository>();
            _database = Substitute.For<IDatabase>();
            _migrationManager = new MigrationManager(
                    _scriptFileRepository,
                    _migrationRepository,
                    _database,
                    new Logger()
                );
        }

        [TestMethod]
        public void MigrateSchema_NoMigrations_ReturnsTrue()
        {
            var result = _migrationManager.MigrateSchema(true);
            Assert.IsTrue(result);
        }
        [TestMethod]
        public void MigrateSchema_NewMigration_ReturnsTrue()
        {
            _scriptFileRepository.GetScripts(ScriptKind.Migration).Returns(new[]
            {
                new Script("FOLDER", "SCRIPT1", "CONTENT", "CHECKSUM"),
            });
            var result = _migrationManager.MigrateSchema(true);
            Assert.IsTrue(result);
        }
        [TestMethod]
        public void MigrateSchema_ConsistentMigrations_ReturnsTrue()
        {
            _migrationRepository.GetMigrations().Returns(new[]
            {
                new Migration("SCRIPT1", "CHECKSUM", DateTime.UtcNow, "CONTENT"), 
            });
            _scriptFileRepository.GetScripts(ScriptKind.Migration).Returns(new[]
            {
                new Script("FOLDER", "SCRIPT1", "CONTENT", "CHECKSUM"),
            });
            var result = _migrationManager.MigrateSchema(true);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MigrateSchema_InconsistentMigrations_ReturnsFalse()
        {
            _migrationRepository.GetMigrations().Returns(new[]
            {
                new Migration("SCRIPT1", "CHECKSUM", DateTime.UtcNow, "CONTENT"), 
            });
            _scriptFileRepository.GetScripts(ScriptKind.Migration).Returns(new[]
            {
                new Script("FOLDER", "SCRIPT1", "CONTENT", "CHECKSUM*"),
            });
            var result = _migrationManager.MigrateSchema(true);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MigrateSchema_UnexpectedMigrations_ReturnsFalse()
        {
            _migrationRepository.GetMigrations().Returns(new[]
            {
                new Migration("SCRIPT1", "CHECKSUM", DateTime.UtcNow, "CONTENT"), 
                new Migration("SCRIPT4", "CHECKSUM", DateTime.UtcNow, "CONTENT"), 
            });
            _scriptFileRepository.GetScripts(ScriptKind.Migration).Returns(new[]
            {
                new Script("FOLDER", "SCRIPT1", "CONTENT", "CHECKSUM"),
                new Script("FOLDER", "SCRIPT2", "CONTENT", "CHECKSUM"),
                new Script("FOLDER", "SCRIPT3", "CONTENT", "CHECKSUM"),
                new Script("FOLDER", "SCRIPT4", "CONTENT", "CHECKSUM"),
            });
            var result = _migrationManager.MigrateSchema(false);
            Assert.IsFalse(result);
        }

    }
}