using System.Collections.Generic;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public interface IMigrationRepository
    {
        IList<Migration> GetMigrations();
        void ApplyMigration(Migration migration);
    }
}