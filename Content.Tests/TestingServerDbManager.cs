using System.Collections.Generic;
using System.Reflection;
using Content.Client;
using Content.Server;
using Robust.UnitTesting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Content.Server.Database;
using Content.Server.Database.Entity;

namespace Content.Tests
{
    public sealed class TestingServerDbManager : ServerDbManager
    {
        protected override ServerDbContext CreateDbContext()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            builder.UseSqlite(conn);
            return new SqliteServerDbContext(builder.Options);
        }
    }
}
