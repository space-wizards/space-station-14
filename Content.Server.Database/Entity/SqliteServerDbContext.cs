using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database.Entity
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public SqliteServerDbContext()
        {
        }
        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
                options.UseSqlite("dummy connection string");
        }
    }
}
