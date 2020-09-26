using System.Threading;
using System.Threading.Tasks;
using Content.Server.Preferences;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    /// <summary>
    ///     Provides methods to retrieve and update character preferences.
    ///     Don't use this directly, go through <see cref="ServerPreferencesManager" /> instead.
    /// </summary>
    public sealed class ServerDbSqlite : ServerDbBase
    {
        // For SQLite we use a single DB context via SQLite.
        // This doesn't allow concurrent access so that's what the semaphore is for.
        // That said, this is bloody SQLite, I don't even think EFCore bothers to truly async it.
        private readonly SemaphoreSlim _prefsSemaphore = new SemaphoreSlim(1, 1);

        private readonly Task _dbReadyTask;
        private readonly SqlitePreferencesDbContext _prefsCtx;

        public ServerDbSqlite(DbContextOptions<PreferencesDbContext> options)
        {
            _prefsCtx = new SqlitePreferencesDbContext(options);

            _dbReadyTask = Task.Run(() => _prefsCtx.Database.Migrate());
        }

        protected override async Task<DbGuard> GetDb()
        {
            await _dbReadyTask;
            await _prefsSemaphore.WaitAsync();

            return new DbGuardImpl(this);
        }

        private sealed class DbGuardImpl : DbGuard
        {
            private readonly ServerDbSqlite _db;

            public DbGuardImpl(ServerDbSqlite db)
            {
                _db = db;
            }

            public override PreferencesDbContext DbContext => _db._prefsCtx;

            public override ValueTask DisposeAsync()
            {
                _db._prefsSemaphore.Release();
                return default;
            }
        }
    }
}
