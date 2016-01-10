using System.Linq;
using DbMigrations.Client.Databases.Oracle;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Databases.SqLite
{
    class SqlLiteDb : Database
    {
        public SqlLiteDb(IDb db, Config config) : base(db, "@", "Migrations")
        {
        }

        protected override bool MigrationsTableExists() =>
            Db.Sql(
                $"SELECT COUNT(*) " +
                $"FROM sqlite_master " +
                $"WHERE type = 'table' AND name = @TableName"
                )
                .WithParameters(new
                {
                    TableName = TableName.Split('.').Last()
                }).AsScalar<int>() > 0;

        protected override void CreateMigrationsTable() => Db.Sql(
            $"CREATE TABLE IF NOT EXISTS {TableName} (" +
            "      ScriptName nvarchar NOT NULL PRIMARY KEY, " +
            "      MD5 nvarchar NOT NULL, " +
            "      ExecutedOn datetime NOT NULL," +
            "      Content nvarchar NOT NULL" +
            "  )").AsNonQuery();

        protected override void InitializeTransaction()
        {
        }

        protected override string[] GetDropAllObjectsStatements() => Db.Sql(
            "select 'drop table ' || name || ';' as \"Statement\" from sqlite_master where type = 'table';"
            ).Select(d => (string)d.Statement).ToArray();
    }
}