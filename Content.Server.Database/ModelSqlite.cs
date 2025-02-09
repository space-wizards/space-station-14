using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;

namespace Content.Server.Database
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public SqliteServerDbContext(DbContextOptions<SqliteServerDbContext> options) : base(options)
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

            var ipConverter = new ValueConverter<IPAddress, string>(
                v => v.ToString(),
                v => IPAddress.Parse(v));

            modelBuilder.Entity<Player>()
                .Property(p => p.LastSeenAddress)
                .HasConversion(ipConverter);

            var ipMaskConverter = new ValueConverter<NpgsqlInet, string>(
                v => InetToString(v.Address, v.Netmask),
                v => StringToInet(v)
            );

            modelBuilder
                .Entity<ServerBan>()
                .Property(e => e.Address)
                .HasColumnType("TEXT")
                .HasConversion(ipMaskConverter);

            modelBuilder
                .Entity<ServerRoleBan>()
                .Property(e => e.Address)
                .HasColumnType("TEXT")
                .HasConversion(ipMaskConverter);

            var jsonStringConverter = new ValueConverter<JsonDocument, string>(
                v => JsonDocumentToString(v),
                v => StringToJsonDocument(v));

            var jsonByteArrayConverter = new ValueConverter<JsonDocument?, byte[]>(
                v => JsonDocumentToByteArray(v),
                v => ByteArrayToJsonDocument(v));

            modelBuilder.Entity<AdminLog>()
                .Property(log => log.Json)
                .HasConversion(jsonStringConverter);

            modelBuilder.Entity<Profile>()
                .Property(log => log.Markings)
                .HasConversion(jsonByteArrayConverter);

            // EF core can make this automatically unique on sqlite but not psql.
            modelBuilder.Entity<IPIntelCache>()
                .HasIndex(p => p.Address)
                .IsUnique();
        }

        public override int CountAdminLogs()
        {
            return AdminLog.Count();
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

        private static NpgsqlInet StringToInet(string inet) {
            var idx = inet.IndexOf('/', StringComparison.Ordinal);
            return new NpgsqlInet(
                IPAddress.Parse(inet.AsSpan(0, idx)),
                byte.Parse(inet.AsSpan(idx + 1), provider: CultureInfo.InvariantCulture)
            );
        }

        private static string JsonDocumentToString(JsonDocument document)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions {Indented = false});

            document.WriteTo(writer);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static JsonDocument StringToJsonDocument(string str)
        {
            return JsonDocument.Parse(str);
        }

        private static byte[] JsonDocumentToByteArray(JsonDocument? document)
        {
            if (document == null)
            {
                return Array.Empty<byte>();
            }

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions {Indented = false});

            document.WriteTo(writer);
            writer.Flush();

            return stream.ToArray();
        }

        private static JsonDocument ByteArrayToJsonDocument(byte[] str)
        {
            return JsonDocument.Parse(str);
        }
    }
}
