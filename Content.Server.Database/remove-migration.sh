#!/usr/bin/env bash

dotnet ef migrations remove --context SqliteServerDbContext
dotnet ef migrations remove --context PostgresServerDbContext
