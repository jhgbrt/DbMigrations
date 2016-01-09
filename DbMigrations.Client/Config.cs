using System;
using System.Data.Common;
using System.IO;
using System.Text;
using DbMigrations.Client.Infrastructure;

namespace DbMigrations.Client
{
    class Config
    {
        string _server;
        string _user;
        string _database;
        string _password;
        string _directory;
        bool _useIntegratedSecurity = true;

        private readonly OptionSet _optionSet;

        private Config()
        {
            _optionSet = new OptionSet
            {
                {
                    "server=", "The db server. For SQL Server, this is a string of the form " +
                               "'servername\\instance' (e.g. localhost\\sqlexpress), or just " +
                               "'servername' in case there's just a default instance). " +
                               "For Oracle, you can use either a " +
                               "registered TNS name, or (using the EZCONNECT feature), use a " +
                               "string of the form 'server[:port[/service]]', e.g. 'localhost:1521/XE'", s => _server = s
                },
                {
                    "user=", "db username (when no user/password specified, integrated security is assumed)", s =>
                    {
                        if (string.IsNullOrEmpty(s)) return;
                        _useIntegratedSecurity = false;
                        _user = s;
                    }
                },
                {
                    "password=", "db password (when no user/password specified, integrated security is assumed)", s =>
                    {
                        if (string.IsNullOrEmpty(s)) return;
                        _useIntegratedSecurity = false;
                        _password = s;
                    }
                },
                {
                    "database=", "database name (initial catalog)", s => _database = s
                },
                {
                    "directory=", "folder containing the migration scripts", s => _directory = s
                },
                {
                    "whatif", "Do not actually run the scripts, just report what would be executed).", s => WhatIf = true
                },
                {
                    "help", "Print help", s => Help = true
                },
                {
                    "providerName=", "The database provider invariant name. The default is " +
                                     "System.Data.SqlClient). For other provider names, the " +
                                     "corresponding .dll containing the provider implementation " +
                                     "must be deployed alongside the migration utility " +
                                     "(e.g., for Oracle: Oracle.ManagedDataAccess.Client.dll).", s => ProviderName = s
                },
                {
                    "schema=", "Db schema for Migrations table (if different from the default for this user)", s => Schema = s
                },
                {
                    "reinitialize", "Will clear the database and re-run all migrations. Use with care!", s => ReInit = true
                }
            };
        }

        public bool ReInit { get; set; }

        public string ProviderName { get; private set; }

        public string GetHelp()
        {
            var sb = new StringBuilder();
            _optionSet.WriteOptionDescriptions(new StringWriter(sb));
            return sb.ToString();
        }

        public bool IsValid(StringBuilder errors)
        {
            if (string.IsNullOrEmpty(_directory))
                errors.AppendLine("No folder");
            else if (!System.IO.Directory.Exists(_directory))
                errors.AppendLine(string.Format("Folder '{0}' not found", _directory));
            else if (!System.IO.Directory.Exists(Path.Combine(_directory, "Migrations")))
                errors.AppendLine(string.Format(@"Folder '{0}\Migrations' not found. By convention, the migration scripts should be located in a subfolder called 'Migrations'", _directory));

            if (!_useIntegratedSecurity)
            {
                if (string.IsNullOrEmpty(_user)) errors.AppendLine("No user");
                if (string.IsNullOrEmpty(_password)) errors.AppendLine("No password");
            }

            if (ReInit)
            {
                var ds = (string)ConnectionStringBuilder["Data Source"];
                if (!ds.Contains("localhost") && !ds.Contains(Environment.MachineName) && !ds.StartsWith(".\\"))
                {
                    errors.AppendLine("Reinitializing the db is only supported on local machine");
                }
            }


            if (errors.Length > 0)
                return false;

            return true;
        }

        public static Config Create(string[] args)
        {
            var result = new Config();

            result._optionSet.Parse(args);

            if (string.IsNullOrEmpty(result.ProviderName)) 
                result.ProviderName = "System.Data.SqlClient";

            if (!string.IsNullOrEmpty(result.ConnectionString))
                return result;

            var connectionStringBuilder = result.ConnectionStringBuilder;
            connectionStringBuilder["Data Source"] = result._server;
            connectionStringBuilder["Initial Catalog"] = result._database;
            if (result._useIntegratedSecurity)
            {
                connectionStringBuilder["Integrated Security"] = "True";
            }
            else
            {
                connectionStringBuilder["User ID"] = result._user;
                connectionStringBuilder["Password"] = result._password;

            }

            connectionStringBuilder["Connect Timeout"] = 10;
            result.ConnectionString = connectionStringBuilder.ConnectionString;
            return result;
        }

        public string Directory { get { return _directory; } }
        public string ConnectionString { get; private set; }
        public bool WhatIf { get; set; }
        public bool Help { get; set; }
        public string Schema { get; private set; }

        public DbProviderFactory DbProviderFactory
        {
            get { return DbProviderFactories.GetFactory(ProviderName); }
        }

        private DbConnectionStringBuilder _connectionStringBuilder;
        public DbConnectionStringBuilder ConnectionStringBuilder
        {
            get
            {
                if (_connectionStringBuilder == null)
                {
                    var connectionStringBuilder = DbProviderFactory.CreateConnectionStringBuilder();
                    connectionStringBuilder.ConnectionString = ConnectionString;
                    _connectionStringBuilder = connectionStringBuilder;
                }
                return _connectionStringBuilder;
            }
        }

        public string UserName
        {
            get { return _user; }
        }
    }
}