// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using Robust.Shared.Configuration;

namespace Content.Server.SS220.PrimeWhitelist;

internal sealed class PrimelistDb
{
    private readonly DbContextOptions<PrimelistDbContext> _options;
    private readonly ISawmill _sawmill = Logger.GetSawmill("prime");

    public PrimelistDb(DbContextOptions<PrimelistDbContext> options)
    {
        _options = options;
    }

    private async Task<DbGuard> GetDb()
    {
        return new DbGuard(new PrimelistDbContext(_options));
    }

    public async Task<PrimelistWhitelist?> GetPrimelistRecord(string ckey)
    {
        await using var db = await GetDb();
        var query = db.PgDbContext.Whitelist
            .Where(p =>p.Ckey==ckey&&p.IsValid).OrderByDescending(p=>p.Date);
        try
        {
            var whitelist = await query.FirstOrDefaultAsync();
            return whitelist;
        }
        catch (Exception e)
        {
            _sawmill.Debug($"Exception occured: ${e}");
            return null;
        }
    }

    public sealed class PrimelistDbContext : DbContext
    {
        public PrimelistDbContext(DbContextOptions<PrimelistDbContext> options) : base(options)
        {
        }

        public DbSet<PrimelistWhitelist> Whitelist { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PrimelistWhitelist>()
                .HasIndex(p => p.Id)
                .IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
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
    }

    private sealed class DbGuard
    {
        public DbGuard(PrimelistDbContext dbC)
        {
            PgDbContext = dbC;
        }

        public PrimelistDbContext PgDbContext { get; }

        public ValueTask DisposeAsync()
        {
            return PgDbContext.DisposeAsync();
        }
    }


    [Table("ckey_whitelist")]
    public record PrimelistWhitelist
    {
        [Column("id")] public int Id { get; set; }
        [Column("date")] public DateTime Date { get; set; }
        [Column("ckey")] public string Ckey { get; set; } = null!;
        [Column("adminwho")] public string AdminWho { get; set; } = null!;
        [Column("port")] public uint Port { get; set; }
        [Column("date_start")] public DateTime DateStart { get; set; }
        [Column("date_end")] public DateTime? DateEnd { get; set; }
        [Column("is_valid")] public bool IsValid { get; set;  }
    }
}

public sealed class Primelist
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private PrimelistDb _db = default!;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("primelist");
        var host = _cfg.GetCVar(CCVars.PrimelistDatabaseIp);
        var port = _cfg.GetCVar(CCVars.PrimelistDatabasePort);
        var db = _cfg.GetCVar(CCVars.PrimelistDatabaseName);
        var user = _cfg.GetCVar(CCVars.PrimelistDatabaseUsername);
        var pass = _cfg.GetCVar(CCVars.PrimelistDatabasePassword);

        var builder = new DbContextOptionsBuilder<PrimelistDb.PrimelistDbContext>();
        var connectionString = new MySqlConnectionStringBuilder()
        {
            Server = host,
            Port = Convert.ToUInt32(port),
            Database = db,
            UserID = user,
            Password = pass,
        }.ConnectionString;

        _sawmill.Debug($"Using MySQL \"{host}:{port}/{db}\"");
        builder.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11 , 2)));
        _db = new PrimelistDb(builder.Options);
    }

    public async Task<bool> IsPrimelisted(string accountName)
    {
        var record = await _db.GetPrimelistRecord(accountName);
        if (record == null)
        {
            _sawmill.Debug($"{accountName} is not in primelist");
            return false;
        }
        var now = DateTime.UtcNow;
        record.DateEnd ??= DateTime.MaxValue;
        var check = now >= record.DateStart && (now <= record.DateEnd || record.DateEnd == DateTime.MinValue);
        if (check)
            return true;
        _sawmill.Debug($"{accountName} record is outdated, from {record.DateStart} to {record.DateEnd}");
        return false;
    }
}
