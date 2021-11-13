using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Content.Server.Database
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public DbSet<SqliteServerBan> Ban { get; set; } = default!;
        public DbSet<SqliteServerUnban> Unban { get; set; } = default!;
        public DbSet<SqliteConnectionLog> ConnectionLog { get; set; } = default!;

        public SqliteServerDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");

            ((IDbContextOptionsBuilderInfrastructure) options).AddOrUpdateExtension(new SnakeCaseExtension());

            options.ConfigureWarnings(x =>
            {
                x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.LastSeenUserName);

            var ipConverter = new ValueConverter<IPAddress, string>(
                v => v.ToString(),
                v => IPAddress.Parse(v));

            modelBuilder.Entity<Player>()
                .Property(p => p.LastSeenAddress)
                .HasConversion(ipConverter);

            var ipMaskConverter = new ValueConverter<(IPAddress address, int mask), string>(
                v => InetToString(v.address, v.mask),
                v => StringToInet(v)
            );

            modelBuilder
                .Entity<SqliteServerBan>()
                .Property(e => e.Address)
                .HasColumnType("TEXT")
                .HasConversion(ipMaskConverter);
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
