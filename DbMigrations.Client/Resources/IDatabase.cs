using System;
using System.Collections.Generic;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Model;

namespace DbMigrations.Client.Resources
{
    public interface IDatabase
    {
        void RunInTransaction(string script);
        void EnsureMigrationsTable();
        void ClearAll();

        IList<Migration> Select();
        void Insert(Migration item);
        bool TableExists { get; }
    }
}