namespace DbMigrations.Client.Model
{
    public class Script
    {
        public Script(string collection, string scriptName, string content, string checksum)
        {
            Collection = collection;
            ScriptName = scriptName;
            Content = content;
            Checksum = checksum;
        }

        public string Collection { get; private set; }

        public string ScriptName { get; private set; }
        public string Content { get; private set; }
        public string Checksum { get; private set; }

        public override string ToString()
        {
            return $"{ScriptName} ({Checksum})";
        }

    }
}