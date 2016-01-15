using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbMigrations.Client.Configuration
{
    public class ConfigurationTextElement<T> : ConfigurationElement
    {
        private T _value;
        protected override void DeserializeElement(XmlReader reader,
                                bool serializeCollectionKey)
        {
            _value = (T)reader.ReadElementContentAs(typeof(T), null);
        }

        public T Value
        {
            get { return _value; }
        }
    }

    public class DbMigrationsConfiguration : ConfigurationSection
    {
        const string invariantName = "invariantName";
        [ConfigurationProperty(invariantName)]
        public string InvariantName
        {
            get { return (string) this[invariantName]; }
            set { this[invariantName] = value; }
        }

        const string tableName = "tableName";
        [ConfigurationProperty(tableName)]
        public string TableName
        {
            get { return (string)this[tableName]; }
            set { this[tableName] = value; }
        }

        const string escapeChar = "escapeChar";
        [ConfigurationProperty(escapeChar, DefaultValue = "@")]
        public string EscapeChar
        {
            get { return (string)this[escapeChar]; }
            set { this[escapeChar] = value; }
        }

        const string schema = "schema";
        [ConfigurationProperty(schema)]
        public string Schema
        {
            get { return (string)this[schema]; }
            set { this[schema] = value; }
        }

        const string countMigrationTables = "countMigrationTables";
        [ConfigurationProperty(countMigrationTables)]
        public QueryConfiguration CountMigrationTables
        {
            get { return (QueryConfiguration) this[countMigrationTables]; }
            set { this[countMigrationTables] = value; }
        }

        const string createMigrationTable = "createMigrationTable";
        [ConfigurationProperty(createMigrationTable)]
        public QueryConfiguration CreateMigrationTable
        {
            get { return (QueryConfiguration)this[createMigrationTable]; }
            set { this[createMigrationTable] = value; }
        }

        const string dropAllObjects = "dropAllObjects";
        [ConfigurationProperty(dropAllObjects)]
        public QueryConfiguration DropAllObjects
        {
            get { return (QueryConfiguration)this[dropAllObjects]; }
            set { this[dropAllObjects] = value; }
        }

        const string configureTransaction = "configureTransaction";
        [ConfigurationProperty(dropAllObjects)]
        public QueryConfiguration ConfigureTransaction
        {
            get { return (QueryConfiguration)this[configureTransaction]; }
            set { this[configureTransaction] = value; }
        }

    }

    public class QueryConfiguration : ConfigurationElement
    {
        const string sql = "sql";
        [ConfigurationProperty(sql)]
        public ConfigurationTextElement<string> Sql
        {
            get { return (ConfigurationTextElement<string>) this[sql]; }
            set { this[sql] = value; }
        }

        const string parameters = "parameters";
        [ConfigurationProperty(parameters)]
        public NameValueConfigurationCollection Parameters
        {
            get { return (NameValueConfigurationCollection)this[parameters]; }
            set { this[parameters] = value; }
        }
        const string arguments = "arguments";
        [ConfigurationProperty(arguments)]
        public NameValueConfigurationCollection Arguments
        {
            get { return (NameValueConfigurationCollection)this[arguments]; }
            set { this[arguments] = value; }
        }
    }
}
