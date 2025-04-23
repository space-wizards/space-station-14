#!/usr/bin/env pwsh

$name = "AddVoiceProfileParameter"
dotnet ef migrations add --context SqliteServerDbContext -o Migrations/Sqlite $name
dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres $name
