using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Content.Server.Database
{
    public sealed class PostgresServerDbContext : ServerDbContext
    {
        // This is used by the "dotnet ef" CLI tool.
        public PostgresServerDbContext()
        {
        }

        static PostgresServerDbContext()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public DbSet<PostgresServerBan> Ban { get; set; } = default!;
        public DbSet<PostgresServerUnban> Unban { get; set; } = default!;
        public DbSet<PostgresConnectionLog> ConnectionLog { get; set; } = default!;


        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseNpgsql("dummy connection string");

            options.ReplaceService<IRelationalTypeMappingSource, CustomNpgsqlTypeMappingSource>();

            ((IDbContextOptionsBuilderInfrastructure) options).AddOrUpdateExtension(new SnakeCaseExtension());

            options.ConfigureWarnings(x =>
            {
                x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
            });
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
                .HasCheckConstraint("HaveEitherAddressOrUserIdOrHWId", "address IS NOT NULL OR user_id IS NOT NULL OR hwid IS NOT NULL");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // ReSharper disable once StringLiteralTypo
            modelBuilder.Entity<Player>()
                .HasCheckConstraint("LastSeenAddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= last_seen_address");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.LastSeenUserName);

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PostgresConnectionLog>()
                .HasCheckConstraint("AddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= address");

            foreach(var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach(var property in entity.GetProperties())
                {
                    if (property.FieldInfo?.FieldType == typeof(DateTime) || property.FieldInfo?.FieldType == typeof(DateTime?))
                        property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }

    [Table("server_ban")]
    public class PostgresServerBan
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        [Column(TypeName = "inet")] public (IPAddress, int)? Address { get; set; }
        public byte[]? HWId { get; set; }

        public DateTime BanTime { get; set; }

        public DateTime? ExpirationTime { get; set; }

        public string Reason { get; set; } = null!;
        public Guid? BanningAdmin { get; set; }

        public PostgresServerUnban? Unban { get; set; }
    }

    [Table("server_unban")]
    public class PostgresServerUnban
    {
        [Column("unban_id")] public int Id { get; set; }

        public int BanId { get; set; }
        public PostgresServerBan Ban { get; set; } = null!;

        public Guid? UnbanningAdmin { get; set; }

        public DateTime UnbanTime { get; set; }
    }

    [Table("connection_log")]
    public class PostgresConnectionLog
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;

        public DateTime Time { get; set; }

        public IPAddress Address { get; set; } = null!;
        public byte[]? HWId { get; set; }
    }
}
