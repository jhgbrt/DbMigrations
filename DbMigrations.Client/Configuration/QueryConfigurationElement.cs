using System.Configuration;
using System.Xml;

namespace DbMigrations.Client.Configuration
{
    public class QueryConfigurationElement : ConfigurationElement
    {
        const string sql = "sql";
        [ConfigurationProperty(sql)]
        public string Sql
        {
            get { return this[sql] as string; }
            set { this[sql] = value; }
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