using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using DbMigrations.Client.Infrastructure;

namespace DbMigrations.Client
{
    public class Config
    {
        private string _server;
        private string _user;
        private string _database;
        private string _password;
        private string _directory;
        private bool _useIntegratedSecurity = true;

        private readonly OptionSet _optionSet;

        private Config()
        {
            _optionSet = new OptionSet
            {
                {
                    "server=", "The db server (\"Data Source\" part of the connection string. " +
                               "For SQL Server, this is a string of the form " +
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
                    "connectionString=", "The complete connection string", s => ConnectionString = s
                },
                {
                    "providerName=", "The database provider invariant name. The default is " +
                                     "System.Data.SqlClient. For other provider names, the " +
                                     "corresponding dependencies (dll and possibly interop code) " +
                                     "containing the provider implementation " +
                                     "must be deployed alongside the migration utility " +
                                     "(e.g., for Oracle, make sure that Oracle.ManagedDataAccess.Client.dll " +
                                     "is present alongside the .exe or somewhere in the path). " +
                                     "At the moment, apart from System.Data.SqlClient (for SQL Server), the following providers are supported: \r\n" + 
                                     string.Join(Environment.NewLine, SupportedDataProviders.Names.Select(n => " - " + n)), s => ProviderName = s
                },
                {
                    "schema=", "Db schema for Migrations table (if different from the default for this user)", s => Schema = s
                },
                {
                    "reinitialize", "Will clear the database and re-run all migrations. Unless --force is specified, this is only allowed for local databases. Use with care!", s => ReInit = true
                },
                {
                    "force", "When used with --reinitialize, allows to restage a remote db. Use with care!", s => Force = true
                },
                {
                    "persistConfiguration", "Persist the configuration to the config file", s => PersistConfiguration = true
                },
                {
                    "pre=", "Comma-separated list of subfolders containing scripts to execute before migration (default none). " +
                            "It is highly recommended to only include generic (idem-potent) scripts in this folder. If you don't " +
                            "know what that means, don't use this feature.", s => PreMigration = s.Split(',')
                },
                {
                    "post=", "Comma-separated list of subfolders containing scripts to execute after migration. " +
                             "If not specified, all scripts except the migrations are considered post-migration " +
                             "scripts. Can be useful e.g. if certain folders are to be ignored in certain " +
                             "environments.", s => PostMigration = s.Split(',')
                }
            };
        }

        public string[] PostMigration { get; set; }

        public string[] PreMigration { get; set; }

        public bool ReInit { get; private set; }

        public string ProviderName { get; private set; }

        public string GetHelp()
        {
            var sb = new StringBuilder();
            _optionSet.WriteOptionDescriptions(new StringWriter(sb));
            return sb.ToString();
        }

        public bool IsValid(StringBuilder errors)
        {
            if (PersistConfiguration)
                return true;

            if (string.IsNullOrEmpty(_directory))
                errors.AppendLine("No folder");
            else if (!System.IO.Directory.Exists(_directory))
                errors.AppendLine($"Folder '{_directory}' not found");
            else if (!System.IO.Directory.Exists(Path.Combine(_directory, "Migrations")))
                errors.AppendLine($"Folder '{_directory}\\Migrations' not found. " +
                                   "By convention, the migration scripts should be " +
                                   "located in a subfolder called 'Migrations'");

            if (!_useIntegratedSecurity)
            {
                if (string.IsNullOrEmpty(_user)) errors.AppendLine("No user");
                if (string.IsNullOrEmpty(_password)) errors.AppendLine("No password");
            }

            if (ReInit && !Force)
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

        public bool Force { get; private set; }

        public static Config Create(string[] args)
        {
            var result = new Config();

            result._optionSet.Parse(args);

            if (string.IsNullOrEmpty(result.ProviderName))
                result.ProviderName = "System.Data.SqlClient";
            else
                EnsureProviderConfigurations();

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

        public string Directory => _directory;
        public string ConnectionString { get; private set; }
        public bool WhatIf { get; private set; }
        public bool Help { get; private set; }
        public string Schema { get; private set; }

        private DbProviderFactory DbProviderFactory => DbProviderFactories.GetFactory(ProviderName);

        private DbConnectionStringBuilder _connectionStringBuilder;

        private DbConnectionStringBuilder ConnectionStringBuilder
        {
            get
            {
                if (_connectionStringBuilder != null) return _connectionStringBuilder;
                var connectionStringBuilder = DbProviderFactory.CreateConnectionStringBuilder();
                if (connectionStringBuilder != null)
                {
                    connectionStringBuilder.ConnectionString = ConnectionString;
                    _connectionStringBuilder = connectionStringBuilder;
                }
                return _connectionStringBuilder;
            }
        }

        public string UserName => _user;

        public bool PersistConfiguration { get; private set; }

        private static void EnsureProviderConfigurations()
        {
            var dataSet = (DataSet)ConfigurationManager.GetSection("system.data");

            var dataproviders = SupportedDataProviders.DataProviders;

            var dataTable = dataSet.Tables[0];
            var dynamicRows = dataTable.Rows.OfType<DataRow>().Select(row => Dynamic.DataRow(row));

            var itemsToAdd = from item in dataproviders
                             join row in dynamicRows on item.InvariantName equals row.InvariantName into rows
                             where !rows.Any()
                             select item;

            foreach (var item in itemsToAdd)
            {
                dataTable.Rows.Add(item.Name, item.Description, item.InvariantName, item.AssemblyQualifiedName);
            }
        }
    }

    class DataProvider
    {
        public string Name;
        public string Description;
        public string InvariantName;
        public string AssemblyQualifiedName;
    }
    static class SupportedDataProviders
    {
        public static readonly DataProvider[] DataProviders =
            {
                new DataProvider {Name = "Microsoft SQL Server Compact Data Provider 4.0" , Description = ".NET Framework Data Provider for Microsoft SQL Server Compact", InvariantName = "System.Data.SqlServerCe.4.0", AssemblyQualifiedName = "System.Data.SqlServerCe.SqlCeProviderFactory, System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"},
                new DataProvider {Name = "SQLite Data Provider" , Description = ".Net Framework Data Provider for SQLite", InvariantName = "System.Data.SQLite", AssemblyQualifiedName = "System.Data.SQLite.SQLiteFactory, System.Data.SQLite"},
                new DataProvider {Name = "ODP.NET, Managed Driver", Description = "Oracle Data Provider for .NET, Managed Driver", InvariantName = "Oracle.ManagedDataAccess.Client", AssemblyQualifiedName = "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess"},
            };

        public static string[] Names
        {
            get { return DataProviders.Select(d => $"{d.InvariantName} ({d.Description})").ToArray(); }
        }
    }
}