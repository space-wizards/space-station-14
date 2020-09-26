using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class ServerDbPostgres : ServerDbBase
    {
        private readonly DbContextOptions<PreferencesDbContext> _options;
        private readonly Task _dbReadyTask;

        public ServerDbPostgres(DbContextOptions<PreferencesDbContext> options)
        {
            _options = options;
            
            _dbReadyTask = Task.Run(async () =>
            {
                await using var ctx = new PostgresPreferencesDbContext(_options);
                await ctx.Database.MigrateAsync();
            });
        }

        protected override async Task<DbGuard> GetDb()
        {
            await _dbReadyTask;

            return new DbGuardImpl(new PostgresPreferencesDbContext(_options));
        }

        private sealed class DbGuardImpl : DbGuard
        {
            public DbGuardImpl(PreferencesDbContext dbC)
            {
                DbContext = dbC;
            }

            public override PreferencesDbContext DbContext { get; }

            public override ValueTask DisposeAsync()
            {
                return DbContext.DisposeAsync();
            }
        }
    }
}
