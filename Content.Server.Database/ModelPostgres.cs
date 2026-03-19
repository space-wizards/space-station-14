using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Content.Server.Database
{
    public sealed class PostgresServerDbContext : ServerDbContext
    {
        public PostgresServerDbContext(DbContextOptions<PostgresServerDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            ((IDbContextOptionsBuilderInfrastructure) options).AddOrUpdateExtension(new SnakeCaseExtension());

            options.ConfigureWarnings(x =>
            {
                x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
#if DEBUG
                // for tests
                x.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
#endif
            });

#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ReSharper disable StringLiteralTypo
            // Enforce that an address cannot be IPv6-mapped IPv4.
            // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
            modelBuilder.Entity<BanAddress>().ToTable(t =>
                t.HasCheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address"));

            modelBuilder.Entity<Player>().ToTable(t =>
                t.HasCheckConstraint("LastSeenAddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= last_seen_address"));

            modelBuilder.Entity<ConnectionLog>().ToTable(t =>
                t.HasCheckConstraint("AddressNotIPv6MappedIPv4",
                    "NOT inet '::ffff:0.0.0.0/96' >>= address"));

            // ReSharper restore StringLiteralTypo

            foreach(var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach(var property in entity.GetProperties())
                {
                    if (property.FieldInfo?.FieldType == typeof(DateTime) || property.FieldInfo?.FieldType == typeof(DateTime?))
                        property.SetColumnType("timestamp with time zone");
                }
            }

            // Postgres-only GIN tsvector index on the log message for fast full-text search.
            // Without this, every search requires computing to_tsvector() on the fly across
            // potentially millions of payload rows.
            modelBuilder.Entity<AdminLogEventPayload>()
                .HasIndex(p => p.Message)
                .HasDatabaseName("IX_admin_log_event_payload_message_gin")
                .HasMethod("GIN")
                .HasAnnotation("Npgsql:TsVectorConfig", "english");
        }

        public override IQueryable<AdminLogEvent> SearchLogs(IQueryable<AdminLogEvent> query, string searchText)
        {
            return query.Where(log => EF.Functions.ToTsVector("english", log.Payload.Message).Matches(EF.Functions.PlainToTsQuery("english", searchText)));
        }

        public override int CountAdminLogs()
        {
            // Use a fast statistical row estimate from pg_class instead of COUNT(*).
            // reltuples is a float4, so we round rather than truncate to reduce drift at large counts.
            // Database.ExecuteSqlRaw routes through EF's managed connection and avoids the
            // race condition that arises when opening a raw NpgsqlConnection manually.
            var result = Database
                .SqlQueryRaw<double>("SELECT reltuples::double precision AS \"Value\" FROM pg_class WHERE relname = 'admin_log_event'")
                .AsEnumerable()
                .FirstOrDefault();

            return (int) Math.Round(result);
        }
    }
}
