using System.Configuration;
using DbMigrations.Client.Configuration;
using DbMigrations.Client.Infrastructure;

namespace DbMigrations.Client.Resources
{
    class QueryConfiguration
    {
        public static QueryConfiguration GetQueryConfiguration(Config config)
        {
            var c = (DbMigrationsConfiguration)ConfigurationManager.GetSection("migrationConfig");
            if (!string.IsNullOrEmpty(c?.InvariantName))
                return FromConfigurationSection(c);

            if (config.ProviderName.StartsWith("Oracle"))
            {
                return Oracle.Instance(config);
            }
            if (config.ProviderName.Contains("SqLite"))
            {
                return SqLite.Instance();
            }

            return SqlServer.Instance(config);
        }

        private static QueryConfiguration FromConfigurationSection(DbMigrationsConfiguration config)
        {
            if (string.IsNullOrEmpty(config?.InvariantName))
                return null;

            return new QueryConfiguration(
                config.InvariantName,
                config.EscapeChar,
                config.TableName,
                config.Schema,
                config.ToQuery(config.ConfigureTransaction),
                config.ToQuery(config.CreateMigrationTable),
                config.ToQuery(config.CountMigrationTables),
                config.ToQuery(config.DropAllObjects)
                );
        }

        private static class SqlServer
        {
            public static QueryConfiguration Instance(Config config)
            {
                var schema = config.Schema ?? "dbo";
                return new QueryConfiguration("System.Data.SqlClient", "@", $"{schema}.Migrations", schema, 
                    "SET XACT_ABORT ON", 
                    CreateTableTemplate,
                    CountMigrationTablesStatement, 
                    DropAllObjectsStatement);
            }

            private static string CountMigrationTablesStatement
                = "SELECT COUNT(*) \r\n" +
                  "FROM INFORMATION_SCHEMA.TABLES \r\n" +
                  "WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @Schema\r\n";


            private static string CreateTableTemplate
                = "CREATE TABLE {TableName} (\r\n" +
                  "      ScriptName nvarchar(255) NOT NULL, \r\n" +
                  "      MD5 nvarchar(32) NOT NULL, \r\n" +
                  "      ExecutedOn datetime NOT NULL,\r\n" +
                  "      Content nvarchar(max) NOT NULL\r\n" +
                  "      CONSTRAINT PK_Migrations PRIMARY KEY CLUSTERED (ScriptName ASC)\r\n" +
                  "  )";

            private static string DropAllObjectsStatement
                = "-- procedures\r\n" +
                  "SELECT \'DROP PROCEDURE [\' + schema_name(schema_id) + \'].[\' + name + \']\' [Statement]\r\n" +
                  "FROM sys.procedures\r\n" +
                  "UNION ALL\r\n" +
                  "-- check constraints\r\n" +
                  "SELECT \'ALTER TABLE [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \']    drop constraint [\' + name + \']\'\r\n" +
                  "FROM sys.check_constraints\r\n" +
                  "UNION ALL\r\n" +
                  "-- views\r\n" +
                  "SELECT \'DROP VIEW [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                  "FROM sys.views\r\n" +
                  "UNION ALL\r\n" +
                  "-- foreign keys\r\n" +
                  "SELECT \'ALTER TABLE [\' + schema_name(schema_id) + \'].[\' + object_name( parent_object_id ) + \'] drop constraint [\' + name + \']\'\r\n" +
                  "FROM sys.foreign_keys\r\n" +
                  "UNION ALL\r\n" +
                  "-- tables\r\n" +
                  "SELECT \'DROP TABLE [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                  "FROM sys.tables\r\n" +
                  "UNION ALL\r\n" +
                  "-- functions\r\n" +
                  "SELECT \'DROP FUNCTION [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                  "FROM sys.objects\r\n" +
                  "WHERE type IN ( \'FN\', \'IF\', \'TF\' )\r\n" +
                  "UNION ALL\r\n" +
                  "-- user defined types\r\n" +
                  "SELECT \'DROP TYPE [\' + schema_name(schema_id) + \'].[\' + name + \']\'\r\n" +
                  "FROM sys.types\r\n" +
                  "WHERE is_user_defined = 1";
        }

        private static class Oracle
        {
            public static QueryConfiguration Instance(Config config)
            {
                var schema = config.Schema ?? config.UserName;
                var tableName = $"{schema}.MIGRATIONS";
                return new QueryConfiguration("Oracle.ManagedDataAccess.Client", ":", tableName, schema, string.Empty, CreateTableTemplate,
                    CountMigrationTablesStatement, DropAllObjectsStatement);
            }

            private static string CountMigrationTablesStatement
                = "SELECT COUNT(*) \r\n" +
                  "FROM ALL_TABLES \r\n" +
                  "WHERE UPPER(TABLE_NAME) = :TableName and OWNER = :Schema";

            private static string CreateTableTemplate
                = "CREATE TABLE {TableName} (\r\n" +
                  "     ScriptName nvarchar2(255) not null, \r\n" +
                  "     MD5 nvarchar2(32) not null, \r\n" +
                  "     ExecutedOn date not null, \r\n" +
                  "     Content CLOB not null, \r\n" +
                  "     CONSTRAINT PK_Migrations PRIMARY KEY (ScriptName)\r\n" +
                  ")";

            static string DropAllObjectsStatement
                = "SELECT 'DROP TABLE '|| TABLE_NAME || ' CASCADE CONSTRAINTS' as Statement \r\n" +
                  "FROM USER_TABLES \r\n" +
                  "UNION ALL \r\n" +
                  "SELECT 'DROP '||OBJECT_TYPE||' '|| OBJECT_NAME as Statement \r\n" +
                  "FROM USER_OBJECTS \r\n" +
                  "WHERE OBJECT_TYPE IN ('VIEW', 'PACKAGE', 'SEQUENCE', 'PROCEDURE', 'FUNCTION' )";

        }

        private static class SqLite
        {
            public static QueryConfiguration Instance()
            {
                return new QueryConfiguration(
                    "System.Data.SqLite",
                    "@", "Migrations", string.Empty, string.Empty
                    , CreateTableTemplate
                    , CountMigrationTablesStatement
                    , DropAllObjectsStatement);
            }

            private static string CountMigrationTablesStatement
                = "SELECT COUNT(*) \r\n" +
                  "FROM sqlite_master \r\n" +
                  "WHERE type = 'table' AND name = @TableName";

            private static string CreateTableTemplate
                = "CREATE TABLE IF NOT EXISTS {TableName} (\r\n" +
                  "      ScriptName nvarchar NOT NULL PRIMARY KEY, \r\n" +
                  "      MD5 nvarchar NOT NULL, \r\n" +
                  "      ExecutedOn datetime NOT NULL,\r\n" +
                  "      Content nvarchar NOT NULL\r\n" +
                  "  )";

            private static string DropAllObjectsStatement 
                = "SELECT 'DROP TABLE ' || name || ';' AS \"Statement\" \r\n" +
                  "FROM SQLITE_MASTER \r\n" +
                  "WHERE TYPE = 'table';";
        }

        private QueryConfiguration(string invariantName, string escapeCharacter, string tableName, string schema, string configureTransactionStatement, string createTableTemplate, string countMigrationTablesStatement, string dropAllObjectsStatement)
        {
            InvariantName = invariantName;
            EscapeCharacter = escapeCharacter;
            TableName = tableName;
            Schema = schema;
            ConfigureTransactionStatement = configureTransactionStatement;
            CreateTableTemplate = createTableTemplate;
            CountMigrationTablesStatement = countMigrationTablesStatement;
            DropAllObjectsStatement = dropAllObjectsStatement;
        }

        private string InsertTemplate { get; } =
            "INSERT INTO {TableName} (ScriptName, MD5, ExecutedOn, Content) " +
            "VALUES ({EscapeCharacter}ScriptName, {EscapeCharacter}MD5, {EscapeCharacter}ExecutedOn, {EscapeCharacter}Content)";

        private string SelectTemplate { get; } =
            "SELECT ScriptName, MD5, ExecutedOn, Content " +
            "FROM {TableName} " +
            "ORDER BY ScriptName ASC";

        public string TableName { get; }
        public string Schema { get; }
        private string InvariantName { get; }
        public string EscapeCharacter { get; }
        public string ConfigureTransactionStatement { get; }
        private string CreateTableTemplate { get; }
        public string CountMigrationTablesStatement { get; }
        public string DropAllObjectsStatement { get; }
        public string InsertStatement => InsertTemplate.FormatWith(new {TableName, EscapeCharacter});
        public string SelectStatement => SelectTemplate.FormatWith(new {TableName});
        public string CreateTableStatement => CreateTableTemplate.FormatWith(new {TableName});
        public void SaveToConfig(DbMigrationsConfiguration config)
        {
            config.InvariantName = InvariantName;
            config.TableName = TableName;
            config.Schema = Schema;
            config.EscapeChar = EscapeCharacter;
            config.CountMigrationTables.Sql = CountMigrationTablesStatement;
            config.DropAllObjects.Sql = DropAllObjectsStatement;
            config.ConfigureTransaction.Sql = ConfigureTransactionStatement;
            config.CreateMigrationTable.Sql = CreateTableTemplate;
        }
    }
}
