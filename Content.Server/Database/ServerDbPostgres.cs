using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class ServerDbPostgres : ServerDbBase
    {
        private readonly DbContextOptions<ServerDbContext> _options;
        private readonly Task _dbReadyTask;

        public ServerDbPostgres(DbContextOptions<ServerDbContext> options)
        {
            _options = options;
            
            _dbReadyTask = Task.Run(async () =>
            {
                await using var ctx = new PostgresServerDbContext(_options);
                await ctx.Database.MigrateAsync();
            });
        }

        protected override async Task<DbGuard> GetDb()
        {
            await _dbReadyTask;

            return new DbGuardImpl(new PostgresServerDbContext(_options));
        }

        private sealed class DbGuardImpl : DbGuard
        {
            public DbGuardImpl(ServerDbContext dbC)
            {
                DbContext = dbC;
            }

            public override ServerDbContext DbContext { get; }

            public override ValueTask DisposeAsync()
            {
                return DbContext.DisposeAsync();
            }
        }
    }
}
