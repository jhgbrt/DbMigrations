using System.Configuration;
using System.Linq;
using System.Xml;

namespace DbMigrations.Client.Configuration
{
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
            var template = q.Sql ?? string.Empty;
            if (!q.Arguments.Any()) return template;
            var args = (
                from a in q.Arguments
                let p = GetType().GetProperty(a)
                select p.GetValue(this)
                ).ToArray();

            return string.Format(template, args);
        }
    }

    public class QueryConfiguration : ConfigurationElement
    {
        public QueryConfiguration()
        {
            Arguments = new string[0];
        }

        const string sql = "sql";
        [ConfigurationProperty(sql)]
        public string Sql
        {
            get { return this[sql] as string; }
            set { this[sql] = value; }
        }

        const string parameters = "parameters";
        [ConfigurationProperty(parameters)]
        public string ParametersStr
        {
            get { return this[parameters] as string; }
            set { this[parameters] = value; }
        }

        public string[] Parameters
        {
            get
            {
                return string.IsNullOrEmpty(ParametersStr) ? new string[] { } : ParametersStr.Split(',');
            }
            set
            {
                ParametersStr = value == null ? null : string.Join(",", value);
            }
        }

        const string arguments = "arguments";

        [ConfigurationProperty(arguments)]
        public string ArgumentsStr
        {
            get { return this[arguments] as string; }
            set { this[arguments] = value; }

        }
        public string[] Arguments
        {
            get
            {
                return string.IsNullOrEmpty(ArgumentsStr) ? new string[] { } : ArgumentsStr.Split(',');
            }
            set
            {
                ArgumentsStr = value == null ? null : string.Join(",", value);
            }
        }

        protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            return true;
        }

        protected override bool SerializeToXmlElement(XmlWriter writer, string elementName)
        {
            if (writer == null)
                return true;
            writer.WriteStartElement(elementName);
            if (!string.IsNullOrEmpty(ArgumentsStr))
                writer.WriteAttributeString(arguments, ArgumentsStr);
            if (!string.IsNullOrEmpty(ParametersStr))
                writer.WriteAttributeString(parameters, ParametersStr);
            writer.WriteCData(Sql);
            writer.WriteEndElement();
            return false;
        }

        protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            var props = Properties;
            if (reader.AttributeCount > 0)
            {
                while (reader.MoveToNextAttribute())
                {
                    var propertyName = reader.Name;
                    var xmlValue = reader.Value;
                    var prop = props[propertyName];
                    var propertyValue = prop.Converter.ConvertFromInvariantString(xmlValue);
                    base[propertyName] = propertyValue;
                }
            }
            reader.MoveToContent();
            var content = reader.ReadElementContentAs(typeof(string), null);
            if (content != null)
                Sql = ((string)content).Trim();
        }
    }
}
