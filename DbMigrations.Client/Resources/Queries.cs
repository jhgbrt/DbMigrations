namespace DbMigrations.Client.Resources
{
    class Queries
    {
        public static class SqlServer
        {
            public static Queries Instance(Config config)
            {
                var schema = config.Schema ?? "dbo";
                return new Queries("@", $"{schema}.Migrations", schema, 
                    "SET XACT_ABORT ON", 
                    CreateTableTemplate,
                    CountMigrationTablesStatement, 
                    DropAllObjectsStatement);
            }
            private static string CountMigrationTablesStatement => "SELECT COUNT(*) " +
                                                                   "FROM INFORMATION_SCHEMA.TABLES " +
                                                                   "WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @Schema";


            private static string CreateTableTemplate => "CREATE TABLE {0} (" +
                                                         "      ScriptName nvarchar(255) NOT NULL, " +
                                                         "      MD5 nvarchar(32) NOT NULL, " +
                                                         "      ExecutedOn datetime NOT NULL," +
                                                         "      Content nvarchar(max) NOT NULL" +
                                                         "      CONSTRAINT PK_Migrations PRIMARY KEY CLUSTERED (ScriptName ASC)" +
                                                         "  )";

            private static string DropAllObjectsStatement => "-- procedures\r\n" +
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
                                                             "where is_user_defined = 1";
        }
        public static class Oracle
        {
            public static Queries Instance(Config config)
            {
                var schema = config.Schema ?? config.UserName;
                var tableName = $"{schema}.MIGRATIONS";
                return new Queries(":", tableName, schema, string.Empty, CreateTableTemplate,
                    CountMigrationTablesStatement, DropAllObjectsStatement);
            }
            static string CountMigrationTablesStatement => "SELECT COUNT(*) " +
                                                           "FROM ALL_TABLES " +
                                                           "WHERE UPPER(TABLE_NAME) = :TableName and OWNER = :Schema";

            static string CreateTableTemplate = "CREATE TABLE {0} (" +
                                                "     ScriptName nvarchar2(255) not null, " +
                                                "     MD5 nvarchar2(32) not null, " +
                                                "     ExecutedOn date not null, " +
                                                "     Content CLOB not null, " +
                                                "     CONSTRAINT PK_Migrations PRIMARY KEY (ScriptName)" +
                                                ")";

            static string DropAllObjectsStatement = "SELECT 'DROP TABLE '|| TABLE_NAME || ' CASCADE CONSTRAINTS' as Statement " +
                                                    "FROM USER_TABLES " +
                                                    "UNION ALL " +
                                                    "SELECT 'DROP '||OBJECT_TYPE||' '|| OBJECT_NAME as Statement " +
                                                    "FROM USER_OBJECTS " +
                                                    "WHERE OBJECT_TYPE IN ('VIEW', 'PACKAGE', 'SEQUENCE', 'PROCEDURE', 'FUNCTION' )";

        }

        public static class SqLite
        {
            public static Queries Instance()
            {
                return new Queries("@", "Migrations", string.Empty, string.Empty
                    , CreateTableTemplate
                    , CountMigrationTablesStatement
                    , DropAllObjectsStatement);
            }

            private static string CountMigrationTablesStatement => "SELECT COUNT(*) " +
                                                                   "FROM sqlite_master " +
                                                                   "WHERE type = 'table' AND name = @TableName";

            private static string CreateTableTemplate => "CREATE TABLE IF NOT EXISTS {0} (" +
                                                         "      ScriptName nvarchar NOT NULL PRIMARY KEY, " +
                                                         "      MD5 nvarchar NOT NULL, " +
                                                         "      ExecutedOn datetime NOT NULL," +
                                                         "      Content nvarchar NOT NULL" +
                                                         "  )";

            private static string DropAllObjectsStatement => "select 'drop table ' || name || ';' as \"Statement\" from sqlite_master where type = 'table';";
        }

        public Queries(string escapeCharacter, string tableName, string schema, string configureTransactionStatement, string createTableTemplate, string countMigrationTablesStatement, string dropAllObjectsStatement)
        {
            EscapeCharacter = escapeCharacter;
            TableName = tableName;
            Schema = schema;
            ConfigureTransactionStatement = configureTransactionStatement;
            CreateTableTemplate = createTableTemplate;
            CountMigrationTablesStatement = countMigrationTablesStatement;
            DropAllObjectsStatement = dropAllObjectsStatement;
        }

        public string InsertTemplate { get; } = 
            "INSERT INTO {0} (ScriptName, MD5, ExecutedOn, Content) " +
            "VALUES ({1}ScriptName, {1}MD5, {1}ExecutedOn, {1}Content)";

        public string SelectTemplate { get; } =
            "SELECT ScriptName, MD5, ExecutedOn, Content " +
            "FROM {0} " +
            "ORDER BY ScriptName ASC";

        public string TableName { get; }
        public string Schema { get; set; }
        public string EscapeCharacter { get; }
        public string ConfigureTransactionStatement { get; }
        public string CreateTableTemplate { get; }
        public string CountMigrationTablesStatement { get; }
        public string DropAllObjectsStatement { get; set; }

        public string InsertStatement => string.Format(InsertTemplate, TableName, EscapeCharacter);
        public string SelectStatement => string.Format(SelectTemplate, TableName);
        public string CreateTableStatement => string.Format(CreateTableTemplate, TableName);
    }
}