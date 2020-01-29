using System;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database.Bans
{
    public sealed class PostgresBansDbContext : BansDbContext
    {
        public PostgresBansDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if(!InitializedWithOptions)
                options.UseNpgsql("dummy connection string");
        }

        public PostgresBansDbContext(DbContextOptions<BansDbContext> options) : base(options)
        {
        }
    }
    public sealed class SqliteBansDbContext : BansDbContext {
        public SqliteBansDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");
        }

        public SqliteBansDbContext(DbContextOptions<BansDbContext> options) : base(options)
        {
        }
    }
    public abstract class BansDbContext : DbContext
    {
        /// <summary>
        /// The "dotnet ef" CLI tool uses the parameter-less constructor.
        /// When that happens we want to supply the <see cref="DbContextOptions"/> via <see cref="DbContext.OnConfiguring"/>.
        /// To use the context within the application, the options need to be passed the constructor instead.
        /// </summary>
        protected readonly bool InitializedWithOptions;
        public BansDbContext()
        {
        }

        public BansDbContext(DbContextOptions<BansDbContext> options) : base(options)
        {
            InitializedWithOptions = true;
        }

        public DbSet<IPBan> IPBans { get; set; }
    }

    public class IPBan
    {
        public int IPBanId { get; set; }
        public string IpAddress { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset? ExpiresOn { get; set; }
    }
}
