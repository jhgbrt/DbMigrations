using System.Linq;
using DbMigrations.Client.Databases.Oracle;
using DbMigrations.Client.Infrastructure;
using DbMigrations.Client.Resources;

namespace DbMigrations.Client.Databases.SqlServer
{
    class SqlServerDb : Database
    {
        public string Schema { get; }

        public SqlServerDb(IDb db, Config config) : this(db, config.Schema ?? "dbo")
        {
        }

        SqlServerDb(IDb db, string schema) : base(db, "@", $"{schema}.Migrations")
        {
            Schema = schema;
        }

        protected override bool MigrationsTableExists() =>
            Db.Sql(
                "SELECT COUNT(*) " +
                "FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @Schema"
                )
                .WithParameters(new
                {
                    Schema,
                    TableName = TableName.Split('.').Last()
                }).AsScalar<int>() > 0;

        protected override void CreateMigrationsTable() => Db.Execute(
            $"CREATE TABLE {TableName} (" +
            "      ScriptName nvarchar(255) NOT NULL, " +
            "      MD5 nvarchar(32) NOT NULL, " +
            "      ExecutedOn datetime NOT NULL," +
            "      Content nvarchar(max) NOT NULL" +
            "      CONSTRAINT PK_Migrations PRIMARY KEY CLUSTERED (ScriptName ASC)" +
            "  )");

        protected override void InitializeTransaction()
        {
            Db.Sql("SET XACT_ABORT ON").AsNonQuery();
        }

        protected override string[] GetDropAllObjectsStatements() => Db.Sql(
            "-- procedures\r\n" +
            "Select \'drop procedure [\' + schema_name(schema_id) + \'].[\' + name + \']\' [Statement]\r\n" +
            "from sys.procedures\r\n" +
            "union all\r\n" +
            "-- check constraints\r\n" +
            "Select \'alter table [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \']    drop constraint [\' + name + \']\'\r\n" +
            "from sys.check_constraints\r\n" +
            "union all\r\n" +
            "-- views\r\n" +
            "Select \'drop view [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
            "from sys.views\r\n" +
            "union all\r\n" +
            "-- foreign keys\r\n" +
            "Select \'alter table [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \'] drop constraint [\' + name + \']\'\r\n" +
            "from sys.foreign_keys\r\n" +
            "union all\r\n" +
            "-- tables\r\n" +
            "Select \'drop table [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
            "from sys.tables\r\n" +
            "union all\r\n" +
            "-- functions\r\n" +
            "Select \'drop function [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
            "from sys.objects\r\n" +
            "where type in ( \'FN\', \'IF\', \'TF\' )\r\n" +
            "union all\r\n" +
            "-- user defined types\r\n" +
            "Select \'drop type [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
            "from sys.types\r\n" +
            "where is_user_defined = 1").Select(d => (string)d.Statement).ToArray();
    }
}