#!/usr/bin/env pwsh

param([String]$name)

dotnet ef migrations add --context SqlitePreferencesDbContext -o Migrations/Sqlite $name
dotnet ef migrations add --context PostgresPreferencesDbContext -o Migrations/Postgres $name
