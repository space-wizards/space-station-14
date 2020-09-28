using System;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public DbSet<SqliteServerBan> Bans { get; set; } = default!;
        public DbSet<SqliteServerUnban> Unbans { get; set; } = default!;

        public SqliteServerDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");
        }

        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }
    }

    public class SqliteServerBan
    {
        public int Id { get; set; }

        public Guid? UserId { get; set; }
        public string? Address { get; set; }

        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string Reason { get; set; } = null!;
        public Guid? BanningAdmin { get; set; }

        public SqliteServerUnban? Unban { get; set; }
    }

    public class SqliteServerUnban
    {
        public int Id { get; set; }

        public int BanId { get; set; }
        public SqliteServerBan Ban { get; set; } = null!;

        public Guid? UnbanningAdmin { get; set; }
        public DateTime UnbanTime { get; set; }
    }
}
