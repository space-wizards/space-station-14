namespace Content.Server.Database.Entity
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using Models;

    public abstract class ServerDbContext : DbContext
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }

        public DbSet<Preference> Preferences { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<AssignedUser> AssignedUsers { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<AdminRank> AdminRanks { get; set; } = null!;
        public DbSet<ServerBan> Bans { get; set; } = null!;
        public DbSet<ServerUnban> Unbans { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<ConnectionLog> ConnectionLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());


            if (!Database.IsNpgsql())
            {
                var converter = new ValueConverter<(IPAddress, int), string>(
                    v => InetToString(v.Item1, v.Item2),
                    v => StringToInet(v)
                );

                modelBuilder
                    .Entity<ServerBan>()
                    .Property(e => e.Address)
                    .HasColumnType("TEXT")
                    .HasConversion(converter);
            } else {
                modelBuilder
                    .Entity<ServerBan>()
                    .Property(e => e.Address)
                    .HasColumnType("inet");
                //"timestamp with time zone"
            }
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
}
