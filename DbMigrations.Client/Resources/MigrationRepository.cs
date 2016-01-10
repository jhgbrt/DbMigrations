using System.Collections.Generic;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    internal class MigrationRepository : IMigrationRepository
    {
        private readonly IDatabase _database;
        public MigrationRepository(IDatabase database)
        {
            _database = database;
        }

        public IList<Migration> GetMigrations()
        {
            if (!_database.TableExists)
                return new List<Migration>();

            return _database.Select();
        }

        public void ApplyMigration(Migration migration)
        {
            _database.RunInTransaction(migration.Content);
            _database.Insert(migration);
        }


    }
}