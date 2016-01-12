using System.Collections.Generic;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public interface IDatabase
    {
        void RunInTransaction(string script);
        void EnsureMigrationsTable();
        void ClearAll();

        IList<Migration> GetMigrations();
        void Insert(Migration item);
        void ApplyMigration(Migration migration);
    }
}