using System;
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

        public DbSet<PostgresServerBan> Bans { get; set; } = default!;
        public DbSet<PostgresServerUnban> Unbans { get; set; } = default!;
        public DbSet<PostgresPlayer> Player { get; set; } = default!;
        public DbSet<PostgresConnectionLog> ConnectionLog { get; set; } = default!;


        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if(!InitializedWithOptions)
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
                .Property(p => p.Address)
                .HasColumnType("inet");

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
                .HasCheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= \"Address\"")
                .HasCheckConstraint("HaveEitherAddressOrUserId", "\"Address\" IS NOT NULL OR \"UserId\" IS NOT NULL");

            modelBuilder.Entity<PostgresServerBan>()
                .Property(p => p.BanTime)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<PostgresServerBan>()
                .Property(p => p.ExpirationTime)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<PostgresServerUnban>()
                .Property(p => p.UnbanTime)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<PostgresPlayer>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // ReSharper disable once StringLiteralTypo
            modelBuilder.Entity<PostgresPlayer>()
                .HasCheckConstraint("LastSeenAddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= \"LastSeenAddress\"");

            modelBuilder.Entity<PostgresPlayer>()
                .Property(p => p.LastSeenTime)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<PostgresConnectionLog>()
                .Property(p => p.Time)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasCheckConstraint("AddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= \"Address\"");

        }
    }

    public class PostgresServerBan
    {
        public int Id { get; set; }

        public Guid? UserId { get; set; }
        public (IPAddress, int)? Address { get; set; }

        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string Reason { get; set; } = null!;
        public Guid? BanningAdmin { get; set; }

        public PostgresServerUnban? Unban { get; set; }
    }

    public class PostgresServerUnban
    {
        public int Id { get; set; }

        public int BanId { get; set; }
        public PostgresServerBan Ban { get; set; } = null!;

        public Guid? UnbanningAdmin { get; set; }
        public DateTime UnbanTime { get; set; }
    }

    public class PostgresPlayer
    {
        public int Id { get; set; }

        // Permanent data
        public Guid UserId { get; set; }
        public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        public string LastSeenUserName { get; set; } = null!;
        public DateTime LastSeenTime { get; set; }
        public IPAddress LastSeenAddress { get; set; } = null!;
    }

    public class PostgresConnectionLog
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime Time { get; set; }
        public IPAddress Address { get; set; } = null!;
    }
}
