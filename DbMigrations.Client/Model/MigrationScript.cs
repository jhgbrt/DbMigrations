using System;

namespace DbMigrations.Client.Model
{
    public class MigrationScript : IEquatable<MigrationScript>
    {

        public MigrationScript(string key, Migration migration, Script script)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (migration == null && script == null)
                    throw new ArgumentException("Both migration and script are null");
            if (migration != null && script != null && migration.ScriptName != script.ScriptName)
                throw new ArgumentException("Migration and script must have same name");
            Name = key;
            Migration = migration;
            Script = script;
        }
        public Migration Migration { get; }
        public Script Script { get; }

        public string Name { get; }

        public MigrationScript Next { get; set; }

        public bool IsConsistent => Script != null 
            && Migration != null 
            && Script.ScriptName == Migration.ScriptName 
            && Script.Checksum == Migration.MD5;

        public bool IsNewMigration => Script != null 
            && Migration == null 
            && (Next == null || Next.IsNewMigration);

        public bool HasChangedOnDisk => Script != null 
            && Migration != null 
            && Script.ScriptName == Migration.ScriptName 
            && Script.Checksum != Migration.MD5;

        public bool IsMissingOnDisk => Script == null && Migration != null;

        public override string ToString()
        {
            if (IsConsistent)
                return $"{Name} is consistent";
            if (IsNewMigration)
                return $"{Name} is valid new script on disk";
            if (IsMissingOnDisk)
                return $"{Name} expected but not found on disk";
            if (HasChangedOnDisk)
                return $"{Name} was changed on disk";
            if (IsUnexpectedExtraScript)
                return $"{Name} is a new script on disk, but comes alphabetically in wrong order";

            return $"{Migration} / {Script}";
        }

        public bool IsUnexpectedExtraScript => Script != null && Migration == null && Next != null;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MigrationScript) obj);
        }

        public bool Equals(MigrationScript other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Migration, other.Migration) && Equals(Script, other.Script);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Migration?.GetHashCode() ?? 0)*397 ^ (Script?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(MigrationScript left, MigrationScript right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MigrationScript left, MigrationScript right)
        {
            return !Equals(left, right);
        }
    }
}