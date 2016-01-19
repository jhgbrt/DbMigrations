using System.Configuration;

namespace DbMigrations.Client.Configuration
{
    public class DbMigrationsConfigurationSection : ConfigurationSection
    {
        const string invariantName = "invariantName";
        [ConfigurationProperty(invariantName)]
        public string InvariantName
        {
            get { return this[invariantName] as string; }
            set { this[invariantName] = value; }
        }

        const string tableName = "tableName";
        [ConfigurationProperty(tableName)]
        public string TableName
        {
            get { return this[tableName] as string; }
            set { this[tableName] = value; }
        }

        const string escapeChar = "escapeChar";
        [ConfigurationProperty(escapeChar, DefaultValue = "@")]
        public string EscapeChar
        {
            get { return this[escapeChar] as string; }
            set { this[escapeChar] = value; }
        }

        const string schema = "schema";
        [ConfigurationProperty(schema)]
        public string Schema
        {
            get { return this[schema] as string; }
            set { this[schema] = value; }
        }

        const string countMigrationTables = "count";
        [ConfigurationProperty(countMigrationTables)]
        public QueryConfigurationElement CountMigrationTables
        {
            get { return this[countMigrationTables] as QueryConfigurationElement; }
            set { this[countMigrationTables] = value; }
        }

        const string createMigrationTable = "create";
        [ConfigurationProperty(createMigrationTable)]
        public QueryConfigurationElement CreateMigrationTable
        {
            get { return this[createMigrationTable] as QueryConfigurationElement; }
            set { this[createMigrationTable] = value; }
        }

        const string dropAllObjects = "drop";
        [ConfigurationProperty(dropAllObjects)]
        public QueryConfigurationElement DropAllObjects
        {
            get { return this[dropAllObjects] as QueryConfigurationElement; }
            set { this[dropAllObjects] = value; }
        }

        const string configureTransaction = "initializeTransaction";
        [ConfigurationProperty(configureTransaction)]
        public QueryConfigurationElement ConfigureTransaction
        {
            get { return this[configureTransaction] as QueryConfigurationElement; }
            set { this[configureTransaction] = value; }
        }
    }
}
