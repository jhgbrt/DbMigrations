<#
.SYNOPSIS
    Generates a .sql script for applying any pending migrations, and adds 
    that script to the database project

.DESCRIPTION
    Generates a .sql script to apply any pending migrations to the database.

.PARAMETER MigrationsProjectName
    Specifies the project that contains the migration configuration type to be
    used. If omitted, the default project selected in package manager console
    is used.

.PARAMETER DatabaseProjectName
    Specifies the project that contains the database migration scripts. 
    If omitted, the first project which name ends with "Database" is used.

.EXAMPLE
	Add-MigrationScript
	# Generates a migration script (using Update-Database -Script) and adds it to the database project
	
#>
function Add-MigrationScript 
{
    param (
        [parameter(Position = 0)]
        [string] $MigrationsProjectName,
        [parameter(Position = 1)]
        [string] $DatabaseProjectName
    )

    if ($MigrationsProjectName)
    {
        $migrationsProject = Get-Project $MigrationsProjectName
    }
    else
    {
        $migrationsProject = Get-MigrationProject
    }

    Write-Host "Using migrations project: $($migrationsProject.Name)"

    $migrations = $migrationsProject.ProjectItems.Item("Migrations")
    $lastMigration = $migrations.ProjectItems | Where-Object {$_.Name -ne "Configuration.cs" } | Sort-Object {$_.Name} | Select-Object -Last 1 Name
    $lastMigrationScriptName = [System.IO.Path]::ChangeExtension($lastMigration.Name, "sql")


    if ($DatabaseProjectName) {
        $databaseProject = Get-Project $DatabaseProjectName
    } else {
        $databaseProject = Get-SolutionProjects | Where {$_.Name.EndsWith("Database")} | Select -First 1
    }

    if (!$databaseProject) {
        throw "No database project found. Please specify the DatabaseProjectName to use, or add a project with name ending on 'Database' to the solution"
    }

    Write-Host "Using database project: $($databaseProject.Name)"

    $databaseProjectPath = Split-Path $databaseProject.FullName
    $fileName = [System.IO.Path]::Combine($databaseProjectPath, "Scripts", "Migrations", $lastMigrationScriptName);

    Write-Host "Last migration: $($lastMigration.Name)"
    Write-Host "Migration script that will be created: $($lastMigrationScriptName)" 
    Write-Host "SQL script will be saved as: $($fileName)"

    Write-Host "Running Update-Database -Script"
    Update-Database -Script -Verbose -ProjectName $MigrationsProjectName
    Write-Host "Script generated"

    $activeDocument = $DTE.ActiveDocument
    $activeDocument.Save($fileName)

    Write-Host "File saved: $fileName"

    $databaseMigration = $databaseProject.ProjectItems.Item("Scripts").ProjectItems.Item("Migrations")
    $item = $databaseMigration.ProjectItems.AddFromFile($fileName)
    $item.Properties.Item("CopyToOutputDirectory").Value = 2

    Write-Host "File $fileName added to project $($databaseProject.Name)" 
}


function Get-SolutionProjects() {
    $projects = New-Object System.Collections.Stack
    
    $DTE.Solution.Projects | %{
        $projects.Push($_)
    }
    
    while ($projects.Count -ne 0)
    {
        $project = $projects.Pop();
        
        # NOTE: This line is similar to doing a "yield return" in C#
        $project

        if ($project.ProjectItems)
        {
            $project.ProjectItems | ?{ $_.SubProject } | %{
                $projects.Push($_.SubProject)
            }
        }
    }
}

function Get-MigrationProject()
{
    $project = Get-SolutionProjects | Where {Is-MigrationProject($_)} | Select -First 1
    return $project;
}

function Is-MigrationProject($project) {
    try {
        $config = $project.ProjectItems.Item("Migrations").ProjectItems.Item("Configuration.cs")
        return $true
    } catch {
        return $false
    }
}

Export-ModuleMember @( 'Add-MigrationScript' ) 
