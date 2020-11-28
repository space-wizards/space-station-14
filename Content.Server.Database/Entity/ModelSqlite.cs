using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database.Entity
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }
    }
}
