#!/usr/bin/env bash

if [ -z "$1" ] ; then
    echo "Must specify migration name"
    exit 1
fi

dotnet ef migrations add --context SqliteServerDbContext -o Migrations/Sqlite "$1"
dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres "$1"
