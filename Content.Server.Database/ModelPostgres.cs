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

            // pg_trgm accelerates regex (~*), ILIKE, and substring searches by building
            // trigram signatures for each message and using a GIN index to pre-filter rows
            // before the full per-row regex or LIKE evaluation. Without this every regex or
            // wildcard search performs a sequential scan on the message column.
            modelBuilder.HasPostgresExtension("pg_trgm");

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

            // Stored generated tsvector column on the log message for fast full-text search.
            // PostgreSQL materializes to_tsvector('english', message) at insert time so
            // SS14.Admin queries never recompute it per row. The GIN index enables sub-ms (ideally)
            // @@ lookups across millions of payload rows.
            // SQLite ignores this property (see ModelSqlite.cs).
            modelBuilder.Entity<AdminLogEventPayload>()
                .HasGeneratedTsVectorColumn(
                    p => p.SearchVector,
                    "english",
                    p => p.Message)
                .HasIndex(p => p.SearchVector)
                .HasDatabaseName("IX_admin_log_event_payload_search_vector_gin")
                .HasMethod("GIN");

            // Trigram GIN index on the raw message text. Used by regex (~*), ILIKE,
            // and exact substring searches.
            modelBuilder.Entity<AdminLogEventPayload>()
                .HasIndex(p => p.Message)
                .HasDatabaseName("IX_admin_log_event_payload_message_trgm")
                .HasMethod("GIN")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<AdminAuditEvent>()
                .HasGeneratedTsVectorColumn(
                    e => e.SearchVector,
                    "english",
                    e => e.Message)
                .HasIndex(e => e.SearchVector)
                .HasDatabaseName("IX_admin_audit_event_search_vector_gin")
                .HasMethod("GIN");

            // Trigram GIN index on the audit event message for the same reason.
            modelBuilder.Entity<AdminAuditEvent>()
                .HasIndex(e => e.Message)
                .HasDatabaseName("IX_admin_audit_event_message_trgm")
                .HasMethod("GIN")
                .HasOperators("gin_trgm_ops");
        }
    }
}
