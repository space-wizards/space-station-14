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
            // Enforce that an address cannot be IPv6-mapped IPv4.
            // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
            modelBuilder.Entity<PostgresServerBan>()
                .HasCheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ff:0.0.0.0/96' >>= \"Address\"")
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
}
