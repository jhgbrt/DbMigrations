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

        public DateTime ExecutedOn { get; }

        [StringLength(255)]
        public string Content { get; }

        [StringLength(int.MaxValue)]
        public string ScriptName { get; }

        [StringLength(32)]
        public string MD5 { get; }

        public override string ToString()
        {
            return $"{ScriptName} ({MD5})";
        }
    }
}