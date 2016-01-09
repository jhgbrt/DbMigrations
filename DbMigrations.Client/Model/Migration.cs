using System;
using System.ComponentModel.DataAnnotations;

namespace DbMigrations.Client.Model
{
    public class Migration
    {
        public Migration(string scriptName, string md5, DateTime executedOn, string content)
        {
            ScriptName = scriptName;
            MD5 = md5;
            ExecutedOn = executedOn;
            Content = content;
        }

        public DateTime ExecutedOn { get; set; }

        [StringLength(255)]
        public string Content { get; private set; }

        [StringLength(int.MaxValue)]
        public string ScriptName { get; private set; }

        [StringLength(32)]
        public string MD5 { get; private set; }

        public override string ToString()
        {
            return $"{ScriptName} ({MD5})";
        }
    }
}