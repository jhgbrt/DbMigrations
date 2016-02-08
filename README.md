[![Build status](https://ci.appveyor.com/api/projects/status/9y7tm6ujwg6d05kv?svg=true)](https://ci.appveyor.com/project/jhgbrt/dbmigrations)

# DbMigrations

Use this utility to execute DDL and SQL scripts against a database

##How it works

The tool is a command-line utility that you point at a folder containing
sql scripts, which (by convention) need to be organized in a certain way.
Here's a small example to get a feel of it:

    migrate.exe --directory=path/to/scripts --server=.\sqlexpress --database=MyDb

This command runs all migration scripts in the folder pointed to against a
SQL Express database, using integrated security. 

##Organizing the scripts

By default, first all scripts in a folder called 'Migrations'
will be executed (in strict alphabetical order). Scripts may be
organized in subfolders, in which case the folders will also be
treated in alphabetical order. However, once a migration script
has been performed from a 'newer' folder, it is not allowed to
add new migrations in an 'earlier' folder.

##Tracking the migrations
Migrations are tracked inside the database, in a table called 'Migrations'.
Once a migration has been performed, it can not be changed. Also, new
versions of a migration package should always include all previous
migrations, with the same name in the same order.

The Migrations table has the following layout (note that the exact data type
depends on the database on which the migrations are run):

    ScriptName (string): the name of the script (relative to the Migrations folder)
    Checksum   (string): a MD5 hash of the script content
    Content    (string): the full content of the script
    ExecutedOn (date)  : the (local) date/time when this script was executed

##Additional (idempotent) scripts
For added convenience, the tool also supports running additional sql
scripts, in addition to the migration scripts themselfves.


After the migrations have been run, by default *all* scripts in *all* other
subfolders are executed. These scripts can be data loads, updates of
views or stored procedures, etc. The only limitation is that these
scripts should be **idempotent** at all times, and consistent with the
current state of the database as it is represented by the set of
migrations.

It is possible fine-tune this behaviour and have a pre-migration phase as well, using the
-pre and -post switches, where you specify a comma-seperated list of folders that must
be executed before resp. after the migrations. It is *highly* recommended to only
include 'generic', idem-potent scripts in the pre-migration phase, otherwise this folder
will very quickly become a source of severe headaches.

##Example

Given this folder structure:

    Scripts
    ├───01_DataLoads
    |   ├───01_Phase1
    |   |   ├───001_referencedata1.sql
    |   |   └───002_samples.sql
    |   └───02_Phase2
    |       └───001_referencedata2.sql
    ├───02_ViewsAndSprocs
    |   ├───001_views.sql
    |   └───002_sprocs.sql
    └───Migrations
        ├───001_migration1.sql
        └───002_migration2.sql

The scripts in this structure will be run in this order:

    Scripts\Migrations\001_migration1.sql
    Scripts\Migrations\002_migration2.sql
    Scripts\01_DataLoads\01_Phase1\001_referencedata1.sql
    Scripts\01_DataLoads\01_Phase1\002_samples.sql
    Scripts\01_DataLoads\02_Phase2\002_referencedata2.sql
    Scripts\02_ViewsAndSprocs\001_views.sql
    Scripts\02_ViewsAndSprocs\002_sprocs.sql

##Command line options

      --server=VALUE         
The db server. For SQL Server, this is a string
                               of the form 'servername\instance' (e.g.
                               localhost\sqlexpress), or just 'servername' in
                               case there's just a default instance). For
                               Oracle, you can use either a registered TNS
                               name, or (using the EZCONNECT feature), use a
                               string of the form 'server[:port[/service]]', e.
                               g. 'localhost:1521/XE'

      --user=VALUE           
The database username (when no user/password specified,
                               integrated security is assumed)

      --password=VALUE       

The database password (when no user/password specified,
                               integrated security is assumed)

      --database=VALUE       

The database name (initial catalog)

      --directory=VALUE      

Absolute or relative path to folder containing the migration scripts

      --whatif               

Do not actually run the scripts, just report what
                               would be executed).

      --help                 

Print help

      --connectionString=VALUE
The complete connection string

      --providerName=VALUE   
The database provider invariant name. The default
                               is System.Data.SqlClient). For other provider
                               names, the corresponding .dll containing the
                               provider implementation must be deployed
                               alongside the migration utility (e.g., for
                               Oracle: Oracle.ManagedDataAccess.Client.dll).

      --schema=VALUE         

Db schema for Migrations table (if different from the default for this user). 
For Sql server, the default schema is "dbo". For Oracle, the default schema 
is the same as the user name.

      --reinitialize         
Will clear the database and re-run all migrations.
                                Unless --force is specified, this is only
                               allowed for local databases. Use with care!

      --force                
When used with --reinitialize, allows to restage
                               a remote db. Use with care!

##Additional examples

    migrate.exe --directory=path/to/scripts --reinitialize \
                --server=localhost:1521/XE --user=(user) --password=(pass) \
                --schema=MYSCHEMA
                --providerName=Oracle.ManagedDataAccess.Client 

Drops all user objects and re-runs the migrations in the given folder against the 
schema MYSCHEMA of a locally installed Oracle XE instance. The user must exist, and 
have been granted sufficient rights in that schema.

    migrate.exe --directory=path/to/scripts --server=mydbserver --database=MyDb

(Incrementally) runs all migrations in the given directory against the default 
instance running on server mydbserver. Integrated Security is used, so the user 
under which the command is run should have sufficient rights. 
