This package adds development-time functionality to EF Migrations.

Specifically, it adds a workflow to generate .sql scripts from migrations, and add those to a database project.

Prerequisites:

- A project should be present that uses EF Migrations
- A project should be present that contains the database scripts. By convention, the name of this project should end with "Database" (can be overridden)
- The database project should contain a folder called "Scripts" with a subfolder called "Migrations"

Workflow:

# enable EF migrations in the nuget package manager console
# ensure the Default project selected is the EF Migrations project
Enable-Migrations

# Add an EF Migration after a change to the model
Add-Migration

# Generate a database script and add the resulting .sql script 
# to the Database project
Add-MigrationScript
