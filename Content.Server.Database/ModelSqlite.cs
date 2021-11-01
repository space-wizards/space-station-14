using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

            ((IDbContextOptionsBuilderInfrastructure) options).AddOrUpdateExtension(new SnakeCaseExtension());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SqlitePlayer>()
                .HasIndex(p => p.LastSeenUserName);

            var converter = new ValueConverter<(IPAddress address, int mask), string>(
                v => InetToString(v.address, v.mask),
                v => StringToInet(v)
            );

            modelBuilder
                .Entity<SqliteServerBan>()
                .Property(e => e.Address)
                .HasColumnType("TEXT")
                .HasConversion(converter);
        }

        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }

        private static string InetToString(IPAddress address, int mask) {
            if (address.IsIPv4MappedToIPv6)
            {
                // Fix IPv6-mapped IPv4 addresses
                // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
                address = address.MapToIPv4();
                mask -= 96;
            }
            return $"{address}/{mask}";
        }

        private static (IPAddress, int) StringToInet(string inet) {
            var idx = inet.IndexOf('/', StringComparison.Ordinal);
            return (
                IPAddress.Parse(inet.AsSpan(0, idx)),
                int.Parse(inet.AsSpan(idx + 1), provider: CultureInfo.InvariantCulture)
            );
        }
    }

    [Table("ban")]
    public class SqliteServerBan
    {
        public int Id { get; set; }

        public Guid? UserId { get; set; }
        public (IPAddress address, int mask)? Address { get; set; }
        public byte[]? HWId { get; set; }

        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string Reason { get; set; } = null!;
        public Guid? BanningAdmin { get; set; }

        public SqliteServerUnban? Unban { get; set; }
    }

    [Table("unban")]
    public class SqliteServerUnban
    {
        [Column("unban_id")] public int Id { get; set; }

        public int BanId { get; set; }
        public SqliteServerBan Ban { get; set; } = null!;

        public Guid? UnbanningAdmin { get; set; }
        public DateTime UnbanTime { get; set; }
    }

    [Table("player")]
    public class SqlitePlayer
    {
        public int Id { get; set; }

        // Permanent data
        public Guid UserId { get; set; }
        public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        public string LastSeenUserName { get; set; } = null!;
        public DateTime LastSeenTime { get; set; }
        public string LastSeenAddress { get; set; } = null!;
        public byte[]? LastSeenHWId { get; set; }
    }

    [Table("connection_log")]
    public class SqliteConnectionLog
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime Time { get; set; }
        public string Address { get; set; } = null!;
        public byte[]? HWId { get; set; }
    }
}
