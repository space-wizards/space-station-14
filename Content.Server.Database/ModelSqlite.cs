using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public DbSet<SqliteServerBan> Ban { get; set; } = default!;
        public DbSet<SqliteServerUnban> Unban { get; set; } = default!;
        public DbSet<SqlitePlayer> Player { get; set; } = default!;
        public DbSet<SqliteConnectionLog> ConnectionLog { get; set; } = default!;

        public SqliteServerDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SqlitePlayer>()
                .HasIndex(p => p.LastSeenUserName);
        }

        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }
    }

    [Table("ban")]
    public class SqliteServerBan
    {
        [Column("ban_id")] public int Id { get; set; }

        [Column("user_id")] public Guid? UserId { get; set; }
        [Column("address")] public string? Address { get; set; }

        [Column("ban_time")] public DateTime BanTime { get; set; }
        [Column("expiration_time")] public DateTime? ExpirationTime { get; set; }
        [Column("reason")] public string Reason { get; set; } = null!;
        [Column("banning_admin")] public Guid? BanningAdmin { get; set; }

        public SqliteServerUnban? Unban { get; set; }
    }

    [Table("unban")]
    public class SqliteServerUnban
    {
        [Column("unban_id")] public int Id { get; set; }

        [Column("ban_id")] public int BanId { get; set; }
        public SqliteServerBan Ban { get; set; } = null!;

        [Column("unbanning_admin")] public Guid? UnbanningAdmin { get; set; }
        [Column("unban_time")] public DateTime UnbanTime { get; set; }
    }

    [Table("player")]
    public class SqlitePlayer
    {
        [Column("player_id")] public int Id { get; set; }

        // Permanent data
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("first_seen_time")] public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        [Column("last_seen_user_name")] public string LastSeenUserName { get; set; } = null!;
        [Column("last_seen_time")] public DateTime LastSeenTime { get; set; }
        [Column("last_seen_address")] public string LastSeenAddress { get; set; } = null!;
    }

    [Table("connection_log")]
    public class SqliteConnectionLog
    {
        [Column("connection_log_id")] public int Id { get; set; }

        [Column("user_id")] public Guid UserId { get; set; }
        [Column("user_name")] public string UserName { get; set; } = null!;
        [Column("time")] public DateTime Time { get; set; }
        [Column("address")] public string Address { get; set; } = null!;
    }
}
