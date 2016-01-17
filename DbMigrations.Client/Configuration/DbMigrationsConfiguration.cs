using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DbMigrations.Client.Infrastructure;

namespace DbMigrations.Client.Configuration
{
    public class ConfigurationTextElement : ConfigurationElement
    {
        private string _value;
        protected override void DeserializeElement(XmlReader reader,
                                bool serializeCollectionKey)
        {
            _value = reader.ReadElementContentAs(typeof(string), null) as string;
        }

        public ConfigurationTextElement()
        {
            
        }

        public ConfigurationTextElement(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }

        protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            writer.WriteCData(_value);
        }
    }

    public class DbMigrationsConfiguration : ConfigurationSection
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

        const string countMigrationTables = "countMigrationTables";
        [ConfigurationProperty(countMigrationTables)]
        public QueryConfiguration CountMigrationTables
        {
            get { return this[countMigrationTables] as QueryConfiguration; }
            set { this[countMigrationTables] = value; }
        }

        const string createMigrationTable = "createMigrationTable";
        [ConfigurationProperty(createMigrationTable)]
        public QueryConfiguration CreateMigrationTable
        {
            get { return this[createMigrationTable] as QueryConfiguration; }
            set { this[createMigrationTable] = value; }
        }

        const string dropAllObjects = "dropAllObjects";
        [ConfigurationProperty(dropAllObjects)]
        public QueryConfiguration DropAllObjects
        {
            get { return this[dropAllObjects] as QueryConfiguration; }
            set { this[dropAllObjects] = value; }
        }

        const string configureTransaction = "initializeTransaction";
        [ConfigurationProperty(configureTransaction)]
        public QueryConfiguration ConfigureTransaction
        {
            get { return this[configureTransaction] as QueryConfiguration; }
            set { this[configureTransaction] = value; }
        }

        public string ToQuery(QueryConfiguration q)
        {
            var template = q.Sql.Value;

            var args = (
                from a in q.Arguments.AllKeys
                let p = GetType().GetProperty(a)
                select p.GetValue(this)
                ).ToArray();

            return string.Format(template, args);
        }
    }

    public class QueryConfiguration : ConfigurationElement
    {
        const string sql = "sql";
        [ConfigurationProperty(sql)]
        public ConfigurationTextElement Sql
        {
            get { return this[sql] as ConfigurationTextElement; }
            set { this[sql] = value; }
        }

        const string parameters = "parameters";
        [ConfigurationProperty(parameters)]
        public NameValueConfigurationCollection Parameters
        {
            get { return this[parameters] as NameValueConfigurationCollection; }
            set { this[parameters] = value; }
        }
        const string arguments = "arguments";
        [ConfigurationProperty(arguments)]
        public NameValueConfigurationCollection Arguments
        {
            get { return this[arguments] as NameValueConfigurationCollection; }
            set { this[arguments] = value; }
        }

    }
}
