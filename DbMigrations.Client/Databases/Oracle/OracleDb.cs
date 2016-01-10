using System.Linq;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Databases.Oracle
{
    internal class OracleDb : Database
    {
        public OracleDb(IDb db, Config config) : this(db, config.Schema??config.UserName)
        {
        }

        OracleDb(IDb db, string owner) : base(db, ":", $"{owner}.MIGRATIONS")
        {
            Owner = owner;
        }

        protected override bool MigrationsTableExists() =>
            Db.Sql(
                "SELECT COUNT(*) " +
                "FROM ALL_TABLES " +
                "WHERE UPPER(TABLE_NAME) = :TableName and OWNER = :Owner"
                ).WithParameters(
                    new
                    {
                        Owner,
                        TableName = TableName.Split('.').Last()
                    }).AsScalar<int>() > 0;

        protected override void CreateMigrationsTable() => Db.Sql(
            $"CREATE TABLE {TableName} (" +
            "     ScriptName nvarchar2(255) not null, " +
            "     MD5 nvarchar2(32) not null, " +
            "     ExecutedOn date not null, " +
            "     Content CLOB not null, " +
            "     CONSTRAINT PK_Migrations PRIMARY KEY (ScriptName)" +
            ")").AsNonQuery();

        protected override void InitializeTransaction()
        {
        }

        private string Owner { get; }

        protected override string[] GetDropAllObjectsStatements() => Db.Sql(
            "SELECT 'DROP TABLE '|| TABLE_NAME || ' CASCADE CONSTRAINTS' as Statement " +
            "FROM USER_TABLES " +
            "UNION ALL " +
            "SELECT 'DROP '||OBJECT_TYPE||' '|| OBJECT_NAME as Statement " +
            "FROM USER_OBJECTS " +
            "WHERE OBJECT_TYPE IN ('VIEW', 'PACKAGE', 'SEQUENCE', 'PROCEDURE', 'FUNCTION' )"
            ).Select(d => (string)d.Statement).ToArray();
    }
}