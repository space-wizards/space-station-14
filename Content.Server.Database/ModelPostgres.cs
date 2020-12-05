using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Content.Server.Database
{
    public sealed class PostgresServerDbContext : ServerDbContext
    {
        // This is used by the "dotnet ef" CLI tool.
        public PostgresServerDbContext()
        {
        }

        public DbSet<PostgresServerBan> Ban { get; set; } = default!;
        public DbSet<PostgresServerUnban> Unban { get; set; } = default!;
        public DbSet<PostgresPlayer> Player { get; set; } = default!;
        public DbSet<PostgresConnectionLog> ConnectionLog { get; set; } = default!;


        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseNpgsql("dummy connection string");

            options.ReplaceService<IRelationalTypeMappingSource, CustomNpgsqlTypeMappingSource>();
        }

        public PostgresServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PostgresServerBan>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PostgresServerBan>()
                .HasIndex(p => p.Address);

            modelBuilder.Entity<PostgresServerBan>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PostgresServerUnban>()
                .HasIndex(p => p.BanId)
                .IsUnique();

            // ReSharper disable once CommentTypo
            // ReSharper disable once StringLiteralTypo
            // Enforce that an address cannot be IPv6-mapped IPv4.
            // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
            modelBuilder.Entity<PostgresServerBan>()
                .HasCheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address")
                .HasCheckConstraint("HaveEitherAddressOrUserId", "address IS NOT NULL OR user_id IS NOT NULL");

            modelBuilder.Entity<PostgresPlayer>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // ReSharper disable once StringLiteralTypo
            modelBuilder.Entity<PostgresPlayer>()
                .HasCheckConstraint("LastSeenAddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= last_seen_address");

            modelBuilder.Entity<PostgresPlayer>()
                .HasIndex(p => p.LastSeenUserName);

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasCheckConstraint("AddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= address");
        }
    }

    [Table("server_ban")]
    public class PostgresServerBan
    {
        [Column("server_ban_id")] public int Id { get; set; }

        [Column("user_id")] public Guid? UserId { get; set; }
        [Column("address", TypeName = "inet")] public (IPAddress, int)? Address { get; set; }

        [Column("ban_time", TypeName = "timestamp with time zone")]
        public DateTime BanTime { get; set; }

        [Column("expiration_time", TypeName = "timestamp with time zone")]
        public DateTime? ExpirationTime { get; set; }

        [Column("reason")] public string Reason { get; set; } = null!;
        [Column("banning_admin")] public Guid? BanningAdmin { get; set; }

        public PostgresServerUnban? Unban { get; set; }
    }

    [Table("server_unban")]
    public class PostgresServerUnban
    {
        [Column("unban_id")] public int Id { get; set; }

        [Column("ban_id")] public int BanId { get; set; }
        [Column("ban")] public PostgresServerBan Ban { get; set; } = null!;

        [Column("unbanning_admin")] public Guid? UnbanningAdmin { get; set; }

        [Column("unban_time", TypeName = "timestamp with time zone")]
        public DateTime UnbanTime { get; set; }
    }

    [Table("player")]
    public class PostgresPlayer
    {
        [Column("player_id")] public int Id { get; set; }

        // Permanent data
        [Column("user_id")] public Guid UserId { get; set; }

        [Column("first_seen_time", TypeName = "timestamp with time zone")]
        public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        [Column("last_seen_user_name")] public string LastSeenUserName { get; set; } = null!;

        [Column("last_seen_time", TypeName = "timestamp with time zone")]
        public DateTime LastSeenTime { get; set; }

        [Column("last_seen_address")] public IPAddress LastSeenAddress { get; set; } = null!;
    }

    [Table("connection_log")]
    public class PostgresConnectionLog
    {
        [Column("connection_log_id")] public int Id { get; set; }

        [Column("user_id")] public Guid UserId { get; set; }
        [Column("user_name")] public string UserName { get; set; } = null!;

        [Column("time", TypeName = "timestamp with time zone")]
        public DateTime Time { get; set; }

        [Column("address")] public IPAddress Address { get; set; } = null!;
    }
}
