using System.Collections.Generic;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public interface IDatabase
    {
        void RunInTransaction(string script);
        void EnsureMigrationsTable();
        IList<Migration> GetMigrations();
        void ApplyMigration(Migration migration);
        void ClearAll();
    }
}