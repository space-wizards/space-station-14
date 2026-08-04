using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Administration.GamePreset; //DS14
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.DeadSpace.Interfaces.Server;

namespace Content.Server.Database
{
    public abstract class ServerDbBase
    {
        private readonly ISawmill _opsLog;

        private IServerPlayTimeManager? _playTimeServer; // DS14-play-time-server-support

        public event Action<DatabaseNotification>? OnNotificationReceived;

        /// <param name="opsLog">Sawmill to trace log database operations to.</param>
        public ServerDbBase(ISawmill opsLog)
        {
            _opsLog = opsLog;

            // DS14-play-time-server-support-start
            if (IoCManager.Instance != null)
                IoCManager.Instance.TryResolveType(out _playTimeServer);
            // DS14-play-time-server-support-end
        }

        #region User ID Migration

        private enum UserIdMigrationMode
        {
            Login,
            Full,
        }

        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserIdMigrationLocalLocks = new();

        public async Task<UserIdMigrationReport> DryRunUserIdMigrationAsync(
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);
            return await BuildUserIdMigrationReportAsync(db.DbContext, oldUserId, newUserId, UserIdMigrationMode.Full, cancel);
        }

        public async Task<UserIdMigrationReport> ApplyUserIdMigrationAsync(
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel = default)
        {
            return await ApplyUserIdMigrationAsync(oldUserId, newUserId, UserIdMigrationMode.Full, cancel);
        }

        public async Task<UserIdMigrationReport> ApplyUserIdLoginMigrationAsync(
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel = default)
        {
            return await ApplyUserIdMigrationAsync(oldUserId, newUserId, UserIdMigrationMode.Login, cancel);
        }

        private async Task<UserIdMigrationReport> ApplyUserIdMigrationAsync(
            Guid oldUserId,
            Guid newUserId,
            UserIdMigrationMode mode,
            CancellationToken cancel)
        {
            var localLocks = await AcquireUserIdMigrationLocalLocksAsync(oldUserId, newUserId, cancel);
            try
            {
                await using var guard = await GetDb(cancel);
                await using var transaction = await guard.DbContext.Database.BeginTransactionAsync(cancel);
                await AcquireUserIdMigrationLocksAsync(guard.DbContext, [oldUserId, newUserId], cancel);

                var processedAt = mode == UserIdMigrationMode.Login
                    ? await GetUserIdLoginMigrationProcessedAtAsync(guard.DbContext, oldUserId, newUserId, cancel)
                    : null;

                if (processedAt != null &&
                    !await HasLoginMigrationTailRowsAsync(guard.DbContext, oldUserId, processedAt.Value, cancel))
                {
                    var processedReport = new UserIdMigrationReport(oldUserId, newUserId)
                    {
                        AlreadyProcessed = true
                    };
                    processedReport.Warnings.Add("Automatic login migration was already processed by this game database.");
                    return processedReport;
                }

                var report = await BuildUserIdMigrationReportAsync(guard.DbContext, oldUserId, newUserId, mode, cancel);
                if (!report.CanApply)
                    return report;

                if (!report.HasOldData)
                {
                    if (mode == UserIdMigrationMode.Login)
                    {
                        await RecordUserIdLoginMigrationAsync(guard.DbContext, oldUserId, newUserId, cancel);
                        await guard.DbContext.SaveChangesAsync(cancel);
                        await transaction.CommitAsync(cancel);
                    }

                    return report;
                }

                await ApplyUserIdMigrationCoreAsync(guard.DbContext, report, mode, cancel);
                if (mode is UserIdMigrationMode.Login or UserIdMigrationMode.Full)
                    await RecordUserIdLoginMigrationAsync(guard.DbContext, oldUserId, newUserId, cancel);

                await guard.DbContext.SaveChangesAsync(cancel);
                await transaction.CommitAsync(cancel);

                report.Applied = true;
                return report;
            }
            finally
            {
                ReleaseUserIdMigrationLocalLocks(localLocks);
            }
        }

        private static async Task<List<SemaphoreSlim>> AcquireUserIdMigrationLocalLocksAsync(
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            return await AcquireUserIdMigrationLocalLocksAsync([oldUserId, newUserId], cancel);
        }

        private static async Task<List<SemaphoreSlim>> AcquireUserIdMigrationLocalLocksAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancel)
        {
            var locks = new List<SemaphoreSlim>();

            try
            {
                foreach (var userId in userIds
                             .Where(userId => userId != Guid.Empty)
                             .Distinct()
                             .OrderBy(userId => userId))
                {
                    var userLock = UserIdMigrationLocalLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                    await userLock.WaitAsync(cancel);
                    locks.Add(userLock);
                }

                return locks;
            }
            catch
            {
                ReleaseUserIdMigrationLocalLocks(locks);
                throw;
            }
        }

        private static void ReleaseUserIdMigrationLocalLocks(List<SemaphoreSlim> locks)
        {
            foreach (var userLock in locks)
            {
                userLock.Release();
            }
        }

        private static async Task<DateTime?> GetUserIdLoginMigrationProcessedAtAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            return await db.UserIdLoginMigrations
                .Where(migration =>
                    migration.OldUserId == oldUserId &&
                    migration.NewUserId == newUserId)
                .Select(migration => (DateTime?)migration.ProcessedAt)
                .SingleOrDefaultAsync(cancel);
        }

        private static async Task RecordUserIdLoginMigrationAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var migration = await db.UserIdLoginMigrations.SingleOrDefaultAsync(migration =>
                migration.OldUserId == oldUserId &&
                migration.NewUserId == newUserId,
                cancel);

            if (migration == null)
            {
                db.UserIdLoginMigrations.Add(new UserIdLoginMigration
                {
                    OldUserId = oldUserId,
                    NewUserId = newUserId,
                    ProcessedAt = DateTime.UtcNow
                });
                return;
            }

            migration.ProcessedAt = DateTime.UtcNow;
        }

        private static async Task<bool> HasLoginMigrationTailRowsAsync(
            ServerDbContext db,
            Guid oldUserId,
            DateTime processedAt,
            CancellationToken cancel)
        {
            return await db.Player.AnyAsync(p => p.UserId == oldUserId && p.LastSeenTime > processedAt, cancel) ||
                   await db.Preference.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.Profile.AnyAsync(p => p.Preference.UserId == oldUserId, cancel) ||
                   await db.AssignedUserId.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.Admin.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.Set<AdminFlag>().AnyAsync(p => p.AdminId == oldUserId, cancel) ||
                   await db.Whitelist.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.Blacklist.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.BanExemption.AnyAsync(p => p.UserId == oldUserId, cancel) ||
                   await db.PlayTime.AnyAsync(p => p.PlayerId == oldUserId, cancel) ||
                   await db.RoleWhitelists.AnyAsync(p => p.PlayerUserId == oldUserId, cancel) ||
                   await db.BanPlayer.AnyAsync(p => p.UserId == oldUserId, cancel);
        }

        private static async Task AcquireUserIdMigrationLocksAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            await AcquireUserIdMigrationLocksAsync(db, [oldUserId, newUserId], cancel);
        }

        private static async Task AcquireUserIdMigrationLocksAsync(
            ServerDbContext db,
            IEnumerable<Guid> userIds,
            CancellationToken cancel)
        {
            if (!IsPostgres(db))
                return;

            var lockKeys = userIds
                .Where(userId => userId != Guid.Empty)
                .Select(GetUserIdMigrationLockKey)
                .Distinct()
                .OrderBy(key => key);

            foreach (var lockKey in lockKeys)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({lockKey})",
                    cancel);
            }
        }

        private static bool IsPostgres(ServerDbContext db)
        {
            return string.Equals(
                db.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal);
        }

        private static long GetUserIdMigrationLockKey(Guid userId)
        {
            var bytes = Encoding.UTF8.GetBytes($"user-id-migration:{userId:N}");
            var hash = SHA256.HashData(bytes);
            return BitConverter.ToInt64(hash, 0);
        }

        private async Task WithUserIdMigrationWriteLockAsync(
            Guid userId,
            Func<ServerDbContext, CancellationToken, Task> action,
            CancellationToken cancel = default)
        {
            var localLocks = await AcquireUserIdMigrationLocalLocksAsync([userId], cancel);
            try
            {
                await using var db = await GetDb(cancel);
                await using var transaction = await db.DbContext.Database.BeginTransactionAsync(cancel);
                await AcquireUserIdMigrationLocksAsync(db.DbContext, [userId], cancel);

                await action(db.DbContext, cancel);
                await transaction.CommitAsync(cancel);
            }
            finally
            {
                ReleaseUserIdMigrationLocalLocks(localLocks);
            }
        }

        private async Task<T> WithUserIdMigrationWriteLockAsync<T>(
            Guid userId,
            Func<ServerDbContext, CancellationToken, Task<T>> action,
            CancellationToken cancel = default)
        {
            return await WithUserIdMigrationWriteLockAsync([userId], action, cancel);
        }

        protected async Task<T> WithUserIdMigrationWriteLockAsync<T>(
            IEnumerable<Guid> userIds,
            Func<ServerDbContext, CancellationToken, Task<T>> action,
            CancellationToken cancel = default)
        {
            var userIdList = userIds.ToArray();
            var localLocks = await AcquireUserIdMigrationLocalLocksAsync(userIdList, cancel);
            try
            {
                await using var db = await GetDb(cancel);
                await using var transaction = await db.DbContext.Database.BeginTransactionAsync(cancel);
                await AcquireUserIdMigrationLocksAsync(db.DbContext, userIdList, cancel);

                var result = await action(db.DbContext, cancel);
                await transaction.CommitAsync(cancel);
                return result;
            }
            finally
            {
                ReleaseUserIdMigrationLocalLocks(localLocks);
            }
        }

        private async Task<UserIdMigrationReport> BuildUserIdMigrationReportAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            UserIdMigrationMode mode,
            CancellationToken cancel)
        {
            var report = new UserIdMigrationReport(oldUserId, newUserId);

            if (oldUserId == Guid.Empty)
                report.Errors.Add("Old user id is empty.");

            if (newUserId == Guid.Empty)
                report.Errors.Add("New user id is empty.");

            if (oldUserId == newUserId)
                report.Errors.Add("Old and new user ids are the same.");

            await AddConflictingUserIdMigrationErrorsAsync(db, report, oldUserId, newUserId, cancel);

            if (_playTimeServer?.UsePlayTimeServer() == true)
            {
                report.Warnings.Add("External playtime service is active; this migration updates local game database rows only. External playtime data must be migrated separately.");
            }
            else if (_playTimeServer != null)
            {
                report.Warnings.Add("External playtime service is registered but inactive; this command migrates the local game database only.");
            }

            if (mode == UserIdMigrationMode.Login)
            {
                report.Warnings.Add("Automatic login migration skips large historical audit tables; run the full migration command later to rewrite connection logs, admin logs, notes and round history.");
            }

            await AddTableCountAsync(
                report,
                "player",
                () => db.Player.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.Player.CountAsync(p => p.UserId == newUserId, cancel),
                mode == UserIdMigrationMode.Full
                    ? "merge old player record into MK player and remove the old record"
                    : "merge old player record into MK player; keep old record for historical tables");

            await AddTableCountAsync(
                report,
                "preference",
                () => db.Preference.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.Preference.CountAsync(p => p.UserId == newUserId, cancel),
                "move preferences; if both sides exist, merge profiles into free slots");

            await AddTableCountAsync(
                report,
                "profile",
                () => db.Profile.CountAsync(p => p.Preference.UserId == oldUserId, cancel),
                () => db.Profile.CountAsync(p => p.Preference.UserId == newUserId, cancel),
                "move character profiles with their jobs, traits, antags and loadouts");

            await AddTableCountAsync(
                report,
                "assigned_user_id",
                () => db.AssignedUserId.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.AssignedUserId.CountAsync(p => p.UserId == newUserId, cancel),
                "move guest username assignment or keep MK assignment if it already exists");

            await AddTableCountAsync(
                report,
                "admin",
                () => db.Admin.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.Admin.CountAsync(p => p.UserId == newUserId, cancel),
                "merge admin rank, flags and state");

            await AddTableCountAsync(
                report,
                "admin_flag",
                () => db.Set<AdminFlag>().CountAsync(p => p.AdminId == oldUserId, cancel),
                () => db.Set<AdminFlag>().CountAsync(p => p.AdminId == newUserId, cancel),
                "move admin flags and drop duplicates");

            await AddTableCountAsync(
                report,
                "whitelist",
                () => db.Whitelist.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.Whitelist.CountAsync(p => p.UserId == newUserId, cancel),
                "move whitelist status");

            await AddTableCountAsync(
                report,
                "blacklist",
                () => db.Blacklist.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.Blacklist.CountAsync(p => p.UserId == newUserId, cancel),
                "move blacklist status");

            await AddTableCountAsync(
                report,
                "server_ban_exemption",
                () => db.BanExemption.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.BanExemption.CountAsync(p => p.UserId == newUserId, cancel),
                "merge ban exemption flags");

            await AddTableCountAsync(
                report,
                "play_time",
                () => db.PlayTime.CountAsync(p => p.PlayerId == oldUserId, cancel),
                () => db.PlayTime.CountAsync(p => p.PlayerId == newUserId, cancel),
                "move playtime and sum duplicate trackers");

            await AddTableCountAsync(
                report,
                "role_whitelists",
                () => db.RoleWhitelists.CountAsync(p => p.PlayerUserId == oldUserId, cancel),
                () => db.RoleWhitelists.CountAsync(p => p.PlayerUserId == newUserId, cancel),
                "move role/job whitelists and drop duplicate roles");

            await AddTableCountAsync(
                report,
                "ban_player",
                () => db.BanPlayer.CountAsync(p => p.UserId == oldUserId, cancel),
                () => db.BanPlayer.CountAsync(p => p.UserId == newUserId, cancel),
                "move player ban selectors and drop duplicate selectors");

            if (mode == UserIdMigrationMode.Full)
            {
                await AddTableCountAsync(
                    report,
                    "admin_log_player",
                    () => db.AdminLogPlayer.CountAsync(p => p.PlayerUserId == oldUserId, cancel),
                    () => db.AdminLogPlayer.CountAsync(p => p.PlayerUserId == newUserId, cancel),
                    "move admin log player links and drop duplicate links");

                await AddTableCountAsync(
                    report,
                    "connection_log",
                    () => db.ConnectionLog.CountAsync(p => p.UserId == oldUserId, cancel),
                    () => db.ConnectionLog.CountAsync(p => p.UserId == newUserId, cancel),
                    "rewrite historical connection logs");

                await AddTableCountAsync(
                    report,
                    "uploaded_resource_log",
                    () => db.UploadedResourceLog.CountAsync(p => p.UserId == oldUserId, cancel),
                    () => db.UploadedResourceLog.CountAsync(p => p.UserId == newUserId, cancel),
                    "rewrite uploaded resource logs");

                await AddTableCountAsync(
                    report,
                    "admin_notes",
                    () => db.AdminNotes.CountAsync(p =>
                        p.PlayerUserId == oldUserId ||
                        p.CreatedById == oldUserId ||
                        p.LastEditedById == oldUserId ||
                        p.DeletedById == oldUserId, cancel),
                    () => db.AdminNotes.CountAsync(p =>
                        p.PlayerUserId == newUserId ||
                        p.CreatedById == newUserId ||
                        p.LastEditedById == newUserId ||
                        p.DeletedById == newUserId, cancel),
                    "rewrite note subject and audit user ids");

                await AddTableCountAsync(
                    report,
                    "admin_watchlists",
                    () => db.AdminWatchlists.CountAsync(p =>
                        p.PlayerUserId == oldUserId ||
                        p.CreatedById == oldUserId ||
                        p.LastEditedById == oldUserId ||
                        p.DeletedById == oldUserId, cancel),
                    () => db.AdminWatchlists.CountAsync(p =>
                        p.PlayerUserId == newUserId ||
                        p.CreatedById == newUserId ||
                        p.LastEditedById == newUserId ||
                        p.DeletedById == newUserId, cancel),
                    "rewrite watchlist subject and audit user ids");

                await AddTableCountAsync(
                    report,
                    "admin_messages",
                    () => db.AdminMessages.CountAsync(p =>
                        p.PlayerUserId == oldUserId ||
                        p.CreatedById == oldUserId ||
                        p.LastEditedById == oldUserId ||
                        p.DeletedById == oldUserId, cancel),
                    () => db.AdminMessages.CountAsync(p =>
                        p.PlayerUserId == newUserId ||
                        p.CreatedById == newUserId ||
                        p.LastEditedById == newUserId ||
                        p.DeletedById == newUserId, cancel),
                    "rewrite message subject and audit user ids");

                await AddTableCountAsync(
                    report,
                    "ban_admin_refs",
                    () => db.Ban.CountAsync(p => p.BanningAdmin == oldUserId || p.LastEditedById == oldUserId, cancel),
                    () => db.Ban.CountAsync(p => p.BanningAdmin == newUserId || p.LastEditedById == newUserId, cancel),
                    "rewrite ban author and editor refs");

                await AddTableCountAsync(
                    report,
                    "unban",
                    () => db.Unban.CountAsync(p => p.UnbanningAdmin == oldUserId, cancel),
                    () => db.Unban.CountAsync(p => p.UnbanningAdmin == newUserId, cancel),
                    "rewrite unban admin refs");

                await AddTableCountAsync(
                    report,
                    "player_round",
                    () => db.Round.CountAsync(r => r.Players.Any(p => p.UserId == oldUserId), cancel),
                    () => db.Round.CountAsync(r => r.Players.Any(p => p.UserId == newUserId), cancel),
                    "move round participation and drop duplicate round links");
            }

            if (await db.Preference.AnyAsync(p => p.UserId == oldUserId, cancel) &&
                await db.Preference.AnyAsync(p => p.UserId == newUserId, cancel))
            {
                report.Warnings.Add("Both users have preferences; old profiles will be appended to MK preferences and slot conflicts will be remapped.");
            }

            if (await db.Whitelist.AnyAsync(p => p.UserId == oldUserId, cancel) &&
                await db.Blacklist.AnyAsync(p => p.UserId == newUserId, cancel))
            {
                report.Warnings.Add("Old user is whitelisted but MK user is blacklisted; blacklist remains present after migration.");
            }

            if (await db.Blacklist.AnyAsync(p => p.UserId == oldUserId, cancel) &&
                await db.Whitelist.AnyAsync(p => p.UserId == newUserId, cancel))
            {
                report.Warnings.Add("Old user is blacklisted but MK user is whitelisted; blacklist is migrated too and should be reviewed.");
            }

            if (!report.HasOldData && report.Errors.Count == 0)
                report.Warnings.Add("No local game database rows were found for the old user id; apply will be a no-op.");

            return report;
        }

        private static async Task AddConflictingUserIdMigrationErrorsAsync(
            ServerDbContext db,
            UserIdMigrationReport report,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            if (oldUserId == Guid.Empty ||
                newUserId == Guid.Empty ||
                oldUserId == newUserId)
            {
                return;
            }

            var conflictingMigration = await db.UserIdLoginMigrations
                .AsNoTracking()
                .Where(migration =>
                    (migration.OldUserId == oldUserId ||
                     migration.NewUserId == oldUserId ||
                     migration.OldUserId == newUserId ||
                     migration.NewUserId == newUserId) &&
                    (migration.OldUserId != oldUserId ||
                     migration.NewUserId != newUserId))
                .Select(migration => new { migration.OldUserId, migration.NewUserId })
                .FirstOrDefaultAsync(cancel);

            if (conflictingMigration == null)
                return;

            report.Errors.Add(
                $"User ID migration conflicts with already processed migration {conflictingMigration.OldUserId} -> {conflictingMigration.NewUserId}.");
        }

        private static async Task AddTableCountAsync(
            UserIdMigrationReport report,
            string table,
            Func<Task<int>> oldCountTask,
            Func<Task<int>> newCountTask,
            string action)
        {
            var oldCount = await oldCountTask();
            var newCount = await newCountTask();
            report.Tables.Add(new UserIdMigrationTableReport(table, oldCount, newCount, action));
        }

        private static async Task ApplyUserIdMigrationCoreAsync(
            ServerDbContext db,
            UserIdMigrationReport report,
            UserIdMigrationMode mode,
            CancellationToken cancel)
        {
            var oldUserId = report.OldUserId;
            var newUserId = report.NewUserId;

            await MergePreferencesAsync(db, report, oldUserId, newUserId, cancel);

            var (oldPlayer, newPlayer) = await EnsureTargetPlayerAsync(db, oldUserId, newUserId, cancel);
            await MergeAdminsAsync(db, report, oldUserId, newUserId, cancel);
            await MergeSimpleUserTablesAsync(db, report, oldUserId, newUserId, cancel);
            await MergePlayTimeAsync(db, oldUserId, newUserId, cancel);
            await MoveRoleWhitelistsAsync(db, oldUserId, newUserId, cancel);
            await MoveBanPlayersAsync(db, oldUserId, newUserId, cancel);

            if (mode == UserIdMigrationMode.Full)
            {
                await MoveAdminLogPlayersAsync(db, oldUserId, newUserId, cancel);
                await MovePlayerForeignKeysAsync(db, oldUserId, newUserId, cancel);
                await MergePlayerRoundsAsync(oldPlayer, newPlayer);
            }

            if (oldPlayer != null && mode == UserIdMigrationMode.Full)
                db.Player.Remove(oldPlayer);
        }

        private static async Task MergePreferencesAsync(
            ServerDbContext db,
            UserIdMigrationReport report,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldPrefs = await db.Preference
                .Include(p => p.Profiles)
                .SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);

            if (oldPrefs == null)
                return;

            var newPrefs = await db.Preference
                .Include(p => p.Profiles)
                .SingleOrDefaultAsync(p => p.UserId == newUserId, cancel);

            if (newPrefs == null)
            {
                oldPrefs.UserId = newUserId;
                return;
            }

            var oldSelectedSlot = oldPrefs.SelectedCharacterSlot;
            var selectedSlotMap = new Dictionary<int, int>();
            var usedSlots = new HashSet<int>(newPrefs.Profiles.Select(p => p.Slot));

            foreach (var profile in oldPrefs.Profiles.OrderBy(p => p.Slot).ToArray())
            {
                var originalSlot = profile.Slot;
                var targetSlot = originalSlot;
                while (usedSlots.Contains(targetSlot))
                    targetSlot++;

                usedSlots.Add(targetSlot);
                selectedSlotMap[originalSlot] = targetSlot;

                if (targetSlot != originalSlot)
                    report.Warnings.Add($"Profile slot {originalSlot} was already used on MK preferences; old profile was moved to slot {targetSlot}.");

                profile.Slot = targetSlot;
                profile.Preference = newPrefs;
                profile.PreferenceId = newPrefs.Id;
            }

            var favorites = newPrefs.ConstructionFavorites
                .Concat(oldPrefs.ConstructionFavorites)
                .Distinct()
                .ToList();
            newPrefs.ConstructionFavorites = favorites;

            // DS14-start
            newPrefs.FavoriteAntags = newPrefs.FavoriteAntags
                .Concat(oldPrefs.FavoriteAntags)
                .Distinct()
                .ToList();
            // DS14-end

            if (IsDefaultAdminOocColor(newPrefs.AdminOOCColor) && !IsDefaultAdminOocColor(oldPrefs.AdminOOCColor))
            {
                newPrefs.AdminOOCColor = oldPrefs.AdminOOCColor;
            }
            else if (!string.Equals(newPrefs.AdminOOCColor, oldPrefs.AdminOOCColor, StringComparison.OrdinalIgnoreCase) &&
                     !IsDefaultAdminOocColor(oldPrefs.AdminOOCColor))
            {
                report.Warnings.Add("Both users have a non-default admin OOC color; MK color was kept.");
            }

            if (selectedSlotMap.TryGetValue(oldSelectedSlot, out var mappedSelectedSlot))
            {
                if (newPrefs.SelectedCharacterSlot != mappedSelectedSlot)
                    report.Warnings.Add($"Selected character slot was moved from MK slot {newPrefs.SelectedCharacterSlot} to migrated WizDen slot {mappedSelectedSlot}.");

                newPrefs.SelectedCharacterSlot = mappedSelectedSlot;
            }

            db.Preference.Remove(oldPrefs);
        }

        private static bool IsDefaultAdminOocColor(string color)
        {
            return string.Equals(color, Color.Red.ToHex(), StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<(Player? OldPlayer, Player? NewPlayer)> EnsureTargetPlayerAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldPlayer = await db.Player
                .Include(p => p.Rounds)
                .SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);

            if (oldPlayer == null)
                return (null, await db.Player.Include(p => p.Rounds).SingleOrDefaultAsync(p => p.UserId == newUserId, cancel));

            var newPlayer = await db.Player
                .Include(p => p.Rounds)
                .SingleOrDefaultAsync(p => p.UserId == newUserId, cancel);

            if (newPlayer == null)
            {
                newPlayer = new Player
                {
                    UserId = newUserId,
                    FirstSeenTime = oldPlayer.FirstSeenTime,
                    LastSeenUserName = oldPlayer.LastSeenUserName,
                    LastSeenTime = oldPlayer.LastSeenTime,
                    LastSeenAddress = oldPlayer.LastSeenAddress,
                    LastSeenHWId = CopyHwid(oldPlayer.LastSeenHWId),
                    LastReadRules = oldPlayer.LastReadRules,
                    Rounds = [],
                };

                db.Player.Add(newPlayer);
                await db.SaveChangesAsync(cancel);
                return (oldPlayer, newPlayer);
            }

            if (oldPlayer.FirstSeenTime < newPlayer.FirstSeenTime)
                newPlayer.FirstSeenTime = oldPlayer.FirstSeenTime;

            if (oldPlayer.LastSeenTime > newPlayer.LastSeenTime)
            {
                newPlayer.LastSeenUserName = oldPlayer.LastSeenUserName;
                newPlayer.LastSeenTime = oldPlayer.LastSeenTime;
                newPlayer.LastSeenAddress = oldPlayer.LastSeenAddress;
                newPlayer.LastSeenHWId = CopyHwid(oldPlayer.LastSeenHWId);
            }

            if (newPlayer.LastReadRules == null ||
                oldPlayer.LastReadRules > newPlayer.LastReadRules)
            {
                newPlayer.LastReadRules = oldPlayer.LastReadRules;
            }

            return (oldPlayer, newPlayer);
        }

        private static TypedHwid? CopyHwid(TypedHwid? hwid)
        {
            if (hwid == null)
                return null;

            return new TypedHwid
            {
                Hwid = hwid.Hwid.ToArray(),
                Type = hwid.Type,
            };
        }

        private static async Task MergeAdminsAsync(
            ServerDbContext db,
            UserIdMigrationReport report,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldAdmin = await db.Admin
                .Include(a => a.Flags)
                .SingleOrDefaultAsync(a => a.UserId == oldUserId, cancel);

            if (oldAdmin == null)
                return;

            var newAdmin = await db.Admin
                .Include(a => a.Flags)
                .SingleOrDefaultAsync(a => a.UserId == newUserId, cancel);

            if (newAdmin == null)
            {
                newAdmin = new Admin
                {
                    UserId = newUserId,
                    Title = oldAdmin.Title,
                    Deadminned = oldAdmin.Deadminned,
                    Suspended = oldAdmin.Suspended,
                    AdminRankId = oldAdmin.AdminRankId,
                    Flags = oldAdmin.Flags
                        .Select(flag => new AdminFlag
                        {
                            Flag = flag.Flag,
                            Negative = flag.Negative,
                        })
                        .ToList(),
                };

                db.Admin.Add(newAdmin);
                db.Admin.Remove(oldAdmin);
                return;
            }

            if (newAdmin.AdminRankId == null)
            {
                newAdmin.AdminRankId = oldAdmin.AdminRankId;
            }
            else if (oldAdmin.AdminRankId != null && newAdmin.AdminRankId != oldAdmin.AdminRankId)
            {
                report.Warnings.Add("Both users have different admin ranks; MK admin rank was kept.");
            }

            if (newAdmin.Title == null)
            {
                newAdmin.Title = oldAdmin.Title;
            }
            else if (oldAdmin.Title != null && !string.Equals(newAdmin.Title, oldAdmin.Title, StringComparison.Ordinal))
            {
                report.Warnings.Add("Both users have different admin titles; MK admin title was kept.");
            }

            newAdmin.Suspended |= oldAdmin.Suspended;
            if (newAdmin.Deadminned != oldAdmin.Deadminned)
                report.Warnings.Add("Both users have different deadmin state; MK deadmin state was kept.");

            var newFlags = newAdmin.Flags.ToDictionary(flag => flag.Flag, StringComparer.Ordinal);
            foreach (var oldFlag in oldAdmin.Flags)
            {
                if (!newFlags.TryGetValue(oldFlag.Flag, out var existingFlag))
                {
                    newAdmin.Flags.Add(new AdminFlag
                    {
                        AdminId = newUserId,
                        Flag = oldFlag.Flag,
                        Negative = oldFlag.Negative,
                    });
                    continue;
                }

                if (existingFlag.Negative != oldFlag.Negative)
                    report.Warnings.Add($"Admin flag {oldFlag.Flag} exists on both users with different negative state; MK flag was kept.");
            }

            db.Admin.Remove(oldAdmin);
        }

        private static async Task MergeSimpleUserTablesAsync(
            ServerDbContext db,
            UserIdMigrationReport report,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldWhitelist = await db.Whitelist.SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);
            if (oldWhitelist != null)
            {
                if (!await db.Whitelist.AnyAsync(p => p.UserId == newUserId, cancel))
                    db.Whitelist.Add(new Whitelist { UserId = newUserId });

                db.Whitelist.Remove(oldWhitelist);
            }

            var oldBlacklist = await db.Blacklist.SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);
            if (oldBlacklist != null)
            {
                if (!await db.Blacklist.AnyAsync(p => p.UserId == newUserId, cancel))
                    db.Blacklist.Add(new Blacklist { UserId = newUserId });

                db.Blacklist.Remove(oldBlacklist);
            }

            var oldBanExemption = await db.BanExemption.SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);
            if (oldBanExemption != null)
            {
                var newBanExemption = await db.BanExemption.SingleOrDefaultAsync(p => p.UserId == newUserId, cancel);
                if (newBanExemption == null)
                {
                    db.BanExemption.Add(new ServerBanExemption
                    {
                        UserId = newUserId,
                        Flags = oldBanExemption.Flags,
                    });
                }
                else
                {
                    newBanExemption.Flags |= oldBanExemption.Flags;
                }

                db.BanExemption.Remove(oldBanExemption);
            }

            var oldAssignedUser = await db.AssignedUserId.SingleOrDefaultAsync(p => p.UserId == oldUserId, cancel);
            if (oldAssignedUser != null)
            {
                var newAssignedUser = await db.AssignedUserId.SingleOrDefaultAsync(p => p.UserId == newUserId, cancel);
                if (newAssignedUser == null)
                {
                    oldAssignedUser.UserId = newUserId;
                }
                else
                {
                    if (!string.Equals(oldAssignedUser.UserName, newAssignedUser.UserName, StringComparison.OrdinalIgnoreCase))
                        report.Warnings.Add($"Both users have assigned usernames; kept MK assignment '{newAssignedUser.UserName}' and removed old assignment '{oldAssignedUser.UserName}'.");

                    db.AssignedUserId.Remove(oldAssignedUser);
                }
            }
        }

        private static async Task MergePlayTimeAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldTimes = await db.PlayTime
                .Where(p => p.PlayerId == oldUserId)
                .ToListAsync(cancel);

            if (oldTimes.Count == 0)
                return;

            var newTimes = await db.PlayTime
                .Where(p => p.PlayerId == newUserId)
                .ToDictionaryAsync(p => p.Tracker, cancel);

            foreach (var oldTime in oldTimes)
            {
                if (newTimes.TryGetValue(oldTime.Tracker, out var newTime))
                {
                    newTime.TimeSpent += oldTime.TimeSpent;
                    db.PlayTime.Remove(oldTime);
                    continue;
                }

                oldTime.PlayerId = newUserId;
            }
        }

        private static async Task MoveAdminLogPlayersAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldLinks = await db.AdminLogPlayer
                .Where(p => p.PlayerUserId == oldUserId)
                .ToListAsync(cancel);

            if (oldLinks.Count == 0)
                return;

            var newKeys = (await db.AdminLogPlayer
                    .Where(p => p.PlayerUserId == newUserId)
                    .Select(p => new { p.RoundId, p.LogId })
                    .ToListAsync(cancel))
                .Select(p => (p.RoundId, p.LogId))
                .ToHashSet();

            foreach (var oldLink in oldLinks)
            {
                if (!newKeys.Contains((oldLink.RoundId, oldLink.LogId)))
                {
                    db.AdminLogPlayer.Add(new AdminLogPlayer
                    {
                        RoundId = oldLink.RoundId,
                        LogId = oldLink.LogId,
                        PlayerUserId = newUserId,
                    });
                }

                db.AdminLogPlayer.Remove(oldLink);
            }
        }

        private static async Task MoveRoleWhitelistsAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldWhitelists = await db.RoleWhitelists
                .Where(p => p.PlayerUserId == oldUserId)
                .ToListAsync(cancel);

            if (oldWhitelists.Count == 0)
                return;

            var newRoles = (await db.RoleWhitelists
                .Where(p => p.PlayerUserId == newUserId)
                .Select(p => p.RoleId)
                .ToListAsync(cancel))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var oldWhitelist in oldWhitelists)
            {
                if (!newRoles.Contains(oldWhitelist.RoleId))
                {
                    db.RoleWhitelists.Add(new RoleWhitelist
                    {
                        PlayerUserId = newUserId,
                        RoleId = oldWhitelist.RoleId,
                    });
                }

                db.RoleWhitelists.Remove(oldWhitelist);
            }
        }

        private static async Task MoveBanPlayersAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            var oldBanPlayers = await db.BanPlayer
                .Where(p => p.UserId == oldUserId)
                .ToListAsync(cancel);

            if (oldBanPlayers.Count == 0)
                return;

            var newBanIds = await db.BanPlayer
                .Where(p => p.UserId == newUserId)
                .Select(p => p.BanId)
                .ToHashSetAsync(cancel);

            foreach (var oldBanPlayer in oldBanPlayers)
            {
                if (newBanIds.Contains(oldBanPlayer.BanId))
                {
                    db.BanPlayer.Remove(oldBanPlayer);
                    continue;
                }

                oldBanPlayer.UserId = newUserId;
            }
        }

        private static async Task MovePlayerForeignKeysAsync(
            ServerDbContext db,
            Guid oldUserId,
            Guid newUserId,
            CancellationToken cancel)
        {
            await db.ConnectionLog
                .Where(p => p.UserId == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.UserId, newUserId), cancel);

            await db.UploadedResourceLog
                .Where(p => p.UserId == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.UserId, newUserId), cancel);

            await db.AdminNotes
                .Where(p => p.PlayerUserId == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.PlayerUserId, (Guid?)newUserId), cancel);

            await db.AdminNotes
                .Where(p => p.CreatedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.CreatedById, (Guid?)newUserId), cancel);

            await db.AdminNotes
                .Where(p => p.LastEditedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.LastEditedById, (Guid?)newUserId), cancel);

            await db.AdminNotes
                .Where(p => p.DeletedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.DeletedById, (Guid?)newUserId), cancel);

            await db.AdminWatchlists
                .Where(p => p.PlayerUserId == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.PlayerUserId, (Guid?)newUserId), cancel);

            await db.AdminWatchlists
                .Where(p => p.CreatedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.CreatedById, (Guid?)newUserId), cancel);

            await db.AdminWatchlists
                .Where(p => p.LastEditedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.LastEditedById, (Guid?)newUserId), cancel);

            await db.AdminWatchlists
                .Where(p => p.DeletedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.DeletedById, (Guid?)newUserId), cancel);

            await db.AdminMessages
                .Where(p => p.PlayerUserId == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.PlayerUserId, (Guid?)newUserId), cancel);

            await db.AdminMessages
                .Where(p => p.CreatedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.CreatedById, (Guid?)newUserId), cancel);

            await db.AdminMessages
                .Where(p => p.LastEditedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.LastEditedById, (Guid?)newUserId), cancel);

            await db.AdminMessages
                .Where(p => p.DeletedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.DeletedById, (Guid?)newUserId), cancel);

            await db.Ban
                .Where(p => p.BanningAdmin == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.BanningAdmin, (Guid?)newUserId), cancel);

            await db.Ban
                .Where(p => p.LastEditedById == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.LastEditedById, (Guid?)newUserId), cancel);

            await db.Unban
                .Where(p => p.UnbanningAdmin == oldUserId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.UnbanningAdmin, (Guid?)newUserId), cancel);
        }

        private static Task MergePlayerRoundsAsync(Player? oldPlayer, Player? newPlayer)
        {
            if (oldPlayer == null || newPlayer == null)
                return Task.CompletedTask;

            var newRoundIds = newPlayer.Rounds.Select(round => round.Id).ToHashSet();
            foreach (var round in oldPlayer.Rounds.ToArray())
            {
                oldPlayer.Rounds.Remove(round);
                if (!newRoundIds.Add(round.Id))
                    continue;

                newPlayer.Rounds.Add(round);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Preferences
        public async Task<PlayerPreferences?> GetPlayerPreferencesAsync(
            NetUserId userId,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles).ThenInclude(h => h.Jobs)
                .Include(p => p.Profiles).ThenInclude(h => h.Antags)
                .Include(p => p.Profiles).ThenInclude(h => h.Traits)
                .Include(p => p.Profiles)
                    .ThenInclude(h => h.Loadouts)
                    .ThenInclude(l => l.Groups)
                    .ThenInclude(group => group.Loadouts)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

            if (prefs is null)
                return null;

            await RepairDuplicateJobPreferencesAsync(db.DbContext, prefs, userId, cancel); // DS14

            var maxSlot = prefs.Profiles.Max(p => p.Slot) + 1;
            var profiles = new Dictionary<int, ICharacterProfile>(maxSlot);
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            var constructionFavorites = new List<ProtoId<ConstructionPrototype>>(prefs.ConstructionFavorites.Count);
            foreach (var favorite in prefs.ConstructionFavorites)
                constructionFavorites.Add(new ProtoId<ConstructionPrototype>(favorite));

            // DS14-start
            var favoriteAntags = new List<ProtoId<AntagPrototype>>(prefs.FavoriteAntags.Count);
            foreach (var favorite in prefs.FavoriteAntags)
                favoriteAntags.Add(new ProtoId<AntagPrototype>(favorite));

            return new PlayerPreferences(profiles, prefs.SelectedCharacterSlot, Color.FromHex(prefs.AdminOOCColor),
                constructionFavorites, null, favoriteAntags);
            // DS14-end
        }

        // DS14-start
        private async Task RepairDuplicateJobPreferencesAsync(
            ServerDbContext db,
            Preference prefs,
            NetUserId userId,
            CancellationToken cancel)
        {
            var duplicateJobs = new List<Job>();
            foreach (var profile in prefs.Profiles)
            {
                var seenJobs = new HashSet<string>(StringComparer.Ordinal);

                foreach (var job in profile.Jobs.OrderByDescending(job => job.Id))
                {
                    if (!seenJobs.Add(job.JobName))
                        duplicateJobs.Add(job);
                }
            }

            if (duplicateJobs.Count == 0)
                return;

            var duplicateIds = duplicateJobs.Select(job => job.Id).ToArray();
            var removed = await db.Set<Job>()
                .Where(job => duplicateIds.Contains(job.Id))
                .ExecuteDeleteAsync(cancel);

            var duplicateIdSet = duplicateIds.ToHashSet();
            foreach (var profile in prefs.Profiles)
                profile.Jobs.RemoveAll(job => duplicateIdSet.Contains(job.Id));

            _opsLog.Warning($"Found {duplicateJobs.Count} duplicate job preference rows for user {userId}; deleted {removed} rows.");
        }
        // DS14-end

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                await SetSelectedCharacterSlotAsync(userId, index, db);
                await db.SaveChangesAsync();
            });
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                if (profile is null)
                {
                    await DeleteCharacterSlot(db, userId, slot);
                    await db.SaveChangesAsync();
                    return;
                }

                if (profile is not HumanoidCharacterProfile humanoid)
                {
                    // TODO: Handle other ICharacterProfile implementations properly
                    throw new NotImplementedException();
                }

                var oldProfile = db.Profile
                    .Include(p => p.Preference)
                    .Where(p => p.Preference.UserId == userId.UserId)
                    .Include(p => p.Jobs)
                    .Include(p => p.Antags)
                    .Include(p => p.Traits)
                    .Include(p => p.Loadouts)
                        .ThenInclude(l => l.Groups)
                        .ThenInclude(group => group.Loadouts)
                    .AsSplitQuery()
                    .SingleOrDefault(h => h.Slot == slot);

                var newProfile = ConvertProfiles(humanoid, slot, oldProfile);
                if (oldProfile == null)
                {
                    var prefs = await db
                        .Preference
                        .Include(p => p.Profiles)
                        .SingleAsync(p => p.UserId == userId.UserId);

                    prefs.Profiles.Add(newProfile);
                }

                await db.SaveChangesAsync();
            });
        }

        private static async Task DeleteCharacterSlot(ServerDbContext db, NetUserId userId, int slot)
        {
            var profile = await db.Profile.Include(p => p.Preference)
                .Where(p => p.Preference.UserId == userId.UserId && p.Slot == slot)
                .SingleOrDefaultAsync();

            if (profile == null)
            {
                return;
            }

            db.Profile.Remove(profile);
        }

        public async Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            return await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                var profile = ConvertProfiles((HumanoidCharacterProfile) defaultProfile, 0);
                var prefs = new Preference
                {
                    UserId = userId.UserId,
                    SelectedCharacterSlot = 0,
                    AdminOOCColor = Color.Red.ToHex(),
                    ConstructionFavorites = [],
                    FavoriteAntags = [], // DS14
                };

                prefs.Profiles.Add(profile);

                db.Preference.Add(prefs);

                await db.SaveChangesAsync();

                return new PlayerPreferences(new[] { new KeyValuePair<int, ICharacterProfile>(0, defaultProfile) }, 0, Color.FromHex(prefs.AdminOOCColor), []);
            });
        }

        public async Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                await DeleteCharacterSlot(db, userId, deleteSlot);
                await SetSelectedCharacterSlotAsync(userId, newSlot, db);
                await db.SaveChangesAsync();
            });
        }

        public async Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                var prefs = await db
                    .Preference
                    .Include(p => p.Profiles)
                    .SingleAsync(p => p.UserId == userId.UserId);
                prefs.AdminOOCColor = color.ToHex();
                await db.SaveChangesAsync();
            });
        }

        public async Task SaveConstructionFavoritesAsync(NetUserId userId, List<ProtoId<ConstructionPrototype>> constructionFavorites)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                var prefs = await db.Preference.SingleAsync(p => p.UserId == userId.UserId);

                var favorites = new List<string>(constructionFavorites.Count);
                foreach (var favorite in constructionFavorites)
                    favorites.Add(favorite.Id);
                prefs.ConstructionFavorites = favorites;

                await db.SaveChangesAsync();
            });
        }

        // DS14-start
        public async Task SaveAntagFavoritesAsync(NetUserId userId, List<ProtoId<AntagPrototype>> favoriteAntags)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                var prefs = await db.Preference.SingleAsync(p => p.UserId == userId.UserId);
                prefs.FavoriteAntags = favoriteAntags.Select(favorite => favorite.Id).ToList();
                await db.SaveChangesAsync();
            });
        }
        // DS14-end

        private static async Task SetSelectedCharacterSlotAsync(NetUserId userId, int newSlot, ServerDbContext db)
        {
            var prefs = await db.Preference.SingleAsync(p => p.UserId == userId.UserId);
            prefs.SelectedCharacterSlot = newSlot;
        }

        private static HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => new ProtoId<JobPrototype>(j.JobName), j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => new ProtoId<AntagPrototype>(a.AntagName));
            var traits = profile.Traits.Select(t => new ProtoId<TraitPrototype>(t.TraitName));

            var sex = Sex.Male;
            if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
                sex = sexVal;

            var spawnPriority = (SpawnPriorityPreference) profile.SpawnPriority;

            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
            if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
                gender = genderVal;

            // Corvax-TTS-Start
            var voice = profile.Voice;
            if (voice == String.Empty)
                voice = SharedHumanoidAppearanceSystem.DefaultSexVoice[sex];
            // Corvax-TTS-End

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            var markingsRaw = profile.Markings?.Deserialize<List<string>>();

            List<Marking> markings = new();
            if (markingsRaw != null)
            {
                foreach (var marking in markingsRaw)
                {
                    var parsed = Marking.ParseFromDbString(marking);

                    if (parsed is null) continue;

                    markings.Add(parsed);
                }
            }

            var loadouts = new Dictionary<string, RoleLoadout>();

            foreach (var role in profile.Loadouts)
            {
                var loadout = new RoleLoadout(role.RoleName)
                {
                    EntityName = role.EntityName,
                };

                foreach (var group in role.Groups)
                {
                    var groupLoadouts = loadout.SelectedLoadouts.GetOrNew(group.GroupName);
                    foreach (var profLoadout in group.Loadouts)
                    {
                        groupLoadouts.Add(new Loadout()
                        {
                            Prototype = profLoadout.LoadoutName,
                        });
                    }
                }

                loadouts[role.RoleName] = loadout;
            }

            // DS14-start
            var hairGradientEnabled = profile.HairGradientEnabled;
            var hairGradientColor = Color.FromHex(
                string.IsNullOrEmpty(profile.HairGradientColor) ? "#000000" : profile.HairGradientColor);
            // DS14-end

            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.FlavorText,
                profile.Species,
                voice, // Corvax-TTS
                profile.Age,
                sex,
                gender,
                new HumanoidCharacterAppearance
                (
                    profile.HairName,
                    Color.FromHex(profile.HairColor),
                    profile.FacialHairName,
                    Color.FromHex(profile.FacialHairColor),
                    Color.FromHex(profile.EyeColor),
                    Color.FromHex(profile.SkinColor),
                    markings,
                    hairGradientEnabled, // DS14
                    hairGradientColor // DS14
                ),
                spawnPriority,
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToHashSet(),
                traits.ToHashSet(),
                loadouts
            );
        }

        private static Profile ConvertProfiles(HumanoidCharacterProfile humanoid, int slot, Profile? profile = null)
        {
            profile ??= new Profile();
            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
            List<string> markingStrings = new();
            foreach (var marking in appearance.Markings)
            {
                markingStrings.Add(marking.ToString());
            }
            var markings = JsonSerializer.SerializeToDocument(markingStrings);

            profile.CharacterName = humanoid.Name;
            profile.FlavorText = humanoid.FlavorText;
            profile.Species = humanoid.Species;
            profile.Voice = humanoid.Voice; // Corvax-TTS
            profile.Age = humanoid.Age;
            profile.Sex = humanoid.Sex.ToString();
            profile.Gender = humanoid.Gender.ToString();
            profile.HairName = appearance.HairStyleId;
            profile.HairColor = appearance.HairColor.ToHex();
            profile.FacialHairName = appearance.FacialHairStyleId;
            profile.FacialHairColor = appearance.FacialHairColor.ToHex();
            profile.EyeColor = appearance.EyeColor.ToHex();
            profile.SkinColor = appearance.SkinColor.ToHex();
            // DS14-start
            profile.HairGradientEnabled = appearance.HairGradientEnabled;
            profile.HairGradientColor = appearance.HairGradientColor.ToHex();
            // DS14-end
            profile.SpawnPriority = (int) humanoid.SpawnPriority;
            profile.Markings = markings;
            profile.Slot = slot;
            profile.PreferenceUnavailable = (DbPreferenceUnavailableMode) humanoid.PreferenceUnavailable;

            profile.Jobs.Clear();
            profile.Jobs.AddRange(
                humanoid.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = (DbJobPriority) j.Value})
            );

            profile.Antags.Clear();
            profile.Antags.AddRange(
                humanoid.AntagPreferences
                    .Select(a => new Antag {AntagName = a})
            );

            profile.Traits.Clear();
            profile.Traits.AddRange(
                humanoid.TraitPreferences
                        .Select(t => new Trait {TraitName = t})
            );

            profile.Loadouts.Clear();

            foreach (var (role, loadouts) in humanoid.Loadouts)
            {
                var dz = new ProfileRoleLoadout()
                {
                    RoleName = role,
                    EntityName = loadouts.EntityName ?? string.Empty,
                };

                foreach (var (group, groupLoadouts) in loadouts.SelectedLoadouts)
                {
                    var profileGroup = new ProfileLoadoutGroup()
                    {
                        GroupName = group,
                    };

                    foreach (var loadout in groupLoadouts)
                    {
                        profileGroup.Loadouts.Add(new ProfileLoadout()
                        {
                            LoadoutName = loadout.Prototype,
                        });
                    }

                    dz.Groups.Add(profileGroup);
                }

                profile.Loadouts.Add(dz);
            }

            return profile;
        }
        #endregion

        #region User Ids
        public async Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            await using var db = await GetDb();

            var assigned = await db.DbContext.AssignedUserId.SingleOrDefaultAsync(p => p.UserName == name);
            return assigned?.UserId is { } g ? new NetUserId(g) : default(NetUserId?);
        }

        public async Task AssignUserIdAsync(string name, NetUserId netUserId)
        {
            await using var db = await GetDb();

            db.DbContext.AssignedUserId.Add(new AssignedUserId
            {
                UserId = netUserId.UserId,
                UserName = name
            });

            await db.DbContext.SaveChangesAsync();
        }
        #endregion

        #region Bans
        /*
         * BAN STUFF
         */
        /// <summary>
        ///     Looks up a ban by id.
        ///     This will return a pardoned ban as well.
        /// </summary>
        /// <param name="id">The ban id to look for.</param>
        /// <returns>The ban with the given id or null if none exist.</returns>
        public abstract Task<BanDef?> GetBanAsync(int id);

        /// <summary>
        ///     Looks up an user's most recent received un-pardoned ban.
        ///     This will NOT return a pardoned ban.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The legacy HWId of the user.</param>
        /// <param name="modernHWIds">The modern HWIDs of the user.</param>
        /// <returns>The user's latest received un-pardoned ban, or null if none exist.</returns>
        public abstract Task<BanDef?> GetBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            BanType type);

        /// <summary>
        ///     Looks up an user's ban history.
        ///     This will return pardoned bans as well.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The legacy HWId of the user.</param>
        /// <param name="modernHWIds">The modern HWIDs of the user.</param>
        /// <param name="includeUnbanned">Include pardoned and expired bans.</param>
        /// <returns>The user's ban history.</returns>
        public abstract Task<List<BanDef>> GetBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned,
            BanType type);

        public abstract Task<BanDef> AddBanAsync(BanDef ban);
        public abstract Task AddUnbanAsync(UnbanDef unban);

        public async Task SetBanPrisonAccess(int id, bool sendToPrison)
        {
            await using var db = await GetDb();

            var ban = await db.DbContext.Ban.SingleOrDefaultAsync(b => b.Id == id);
            if (ban is null || ban.Type != BanType.Server)
                return;

            ban.SendToPrison = sendToPrison;
            await db.DbContext.SaveChangesAsync();
        }

        public async Task<bool> TrySetActivePrisonBanExpiration(
            int id,
            DateTimeOffset expectedExpiration,
            DateTimeOffset expiration)
        {
            await using var db = await GetDb();

            var now = DateTime.UtcNow;
            var expected = expectedExpiration.UtcDateTime;
            var updated = expiration.UtcDateTime;
            var changed = await db.DbContext.Ban
                .Where(ban => ban.Id == id &&
                              ban.Type == BanType.Server &&
                              ban.SendToPrison &&
                              ban.Unban == null &&
                              ban.ExpirationTime == expected &&
                              ban.ExpirationTime > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(ban => ban.ExpirationTime, updated));

            return changed == 1;
        }

        public async Task EditBan(int id, string reason, NoteSeverity severity, DateTimeOffset? expiration, Guid editedBy, DateTimeOffset editedAt)
        {
            await using var db = await GetDb();

            var ban = await db.DbContext.Ban.SingleOrDefaultAsync(b => b.Id == id);
            if (ban is null)
                return;
            ban.Severity = severity;
            ban.Reason = reason;
            ban.ExpirationTime = expiration?.UtcDateTime;
            ban.LastEditedById = editedBy;
            ban.LastEditedAt = editedAt.UtcDateTime;
            await db.DbContext.SaveChangesAsync();
        }

        protected static async Task<ServerBanExemptFlags?> GetBanExemptionCore(
            DbGuard db,
            NetUserId? userId,
            CancellationToken cancel = default)
        {
            if (userId == null)
                return null;

            var exemption = await db.DbContext.BanExemption
                .SingleOrDefaultAsync(e => e.UserId == userId.Value.UserId, cancellationToken: cancel);

            return exemption?.Flags;
        }

        public async Task UpdateBanExemption(NetUserId userId, ServerBanExemptFlags flags)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                if (flags == 0)
                {
                    // Delete whatever is there.
                    await db.BanExemption.Where(u => u.UserId == userId.UserId).ExecuteDeleteAsync();
                    return;
                }

                var exemption = await db.BanExemption.SingleOrDefaultAsync(u => u.UserId == userId.UserId);
                if (exemption == null)
                {
                    exemption = new ServerBanExemption
                    {
                        UserId = userId
                    };

                    db.BanExemption.Add(exemption);
                }

                exemption.Flags = flags;
                await db.SaveChangesAsync();
            });
        }

        public async Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var flags = await GetBanExemptionCore(db, userId, cancel);
            return flags ?? ServerBanExemptFlags.None;
        }

        public abstract Task AddBiStatAsync(string gameMode, BiStatWinner winner, DateTime date); // DS14

        #endregion

        protected static List<Expression<Func<Ban, object>>> GetBanDefIncludes(BanType? type = null)
        {
            List<Expression<Func<Ban, object>>> list =
            [
                b => b.Players!,
                b => b.Rounds!,
                b => b.Hwids!,
                b => b.Unban!,
                b => b.Addresses!,
            ];

            if (type != BanType.Server)
                list.Add(b => b.Roles!);

            return list;
        }

        #region Playtime
        public async Task<List<PlayTime>> GetPlayTimes(Guid player, CancellationToken cancel)
        {
            // DS14-play-time-server-support-start
            if (_playTimeServer != null && _playTimeServer.UsePlayTimeServer())
                return await _playTimeServer.GetPlayTimesAsync(player, cancel);
            // DS14-play-time-server-support-end

            await using var db = await GetDb(cancel);

            return await db.DbContext.PlayTime
                .Where(p => p.PlayerId == player)
                .ToListAsync(cancel);
        }

        public async Task UpdatePlayTimes(IReadOnlyCollection<PlayTimeUpdate> updates)
        {
            if (updates.Count == 0)
                return;

            // DS14-play-time-server-support-start
            if (_playTimeServer != null && _playTimeServer.UsePlayTimeServer())
            {
                var data = updates.Select(x => new PlayTime()
                {
                    PlayerId = x.User.UserId,
                    Tracker = x.Tracker,
                    TimeSpent = x.Time
                });

                await _playTimeServer.UpdatePlayTimes(data);

                if (!_playTimeServer.SaveLocaly())
                {
                    return;
                }
            }
            // DS14-play-time-server-support-end

            // Ideally I would just be able to send a bunch of UPSERT commands, but EFCore is a pile of garbage.
            // So... In the interest of not making this take forever at high update counts...
            // Bulk-load play time objects for all players involved.
            // This allows us to semi-efficiently load all entities we need in a single DB query.
            // Then we can update & insert without further round-trips to the DB.

            var players = updates.Select(u => u.User.UserId).Distinct().ToArray();
            var localLocks = await AcquireUserIdMigrationLocalLocksAsync(players, CancellationToken.None);
            try
            {
                await using var db = await GetDb();
                await using var transaction = await db.DbContext.Database.BeginTransactionAsync();
                await AcquireUserIdMigrationLocksAsync(db.DbContext, players, CancellationToken.None);

                var dbTimes = (await db.DbContext.PlayTime
                        .Where(p => players.Contains(p.PlayerId))
                        .ToArrayAsync())
                    .GroupBy(p => p.PlayerId)
                    .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Tracker, p => p));

                foreach (var (user, tracker, time) in updates)
                {
                    if (dbTimes.TryGetValue(user.UserId, out var userTimes)
                        && userTimes.TryGetValue(tracker, out var ent))
                    {
                        // Already have a tracker in the database, update it.
                        ent.TimeSpent = time;
                        continue;
                    }

                    // No tracker, make a new one.
                    var playTime = new PlayTime
                    {
                        Tracker = tracker,
                        PlayerId = user.UserId,
                        TimeSpent = time
                    };

                    db.DbContext.PlayTime.Add(playTime);
                }

                await db.DbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            finally
            {
                ReleaseUserIdMigrationLocalLocks(localLocks);
            }
        }

        #endregion

        #region Player Records
        /*
         * PLAYER RECORDS
         */
        public async Task UpdatePlayerRecord(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableTypedHwid? hwId)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, _) =>
            {
                var record = await db.Player.SingleOrDefaultAsync(p => p.UserId == userId.UserId);
                if (record == null)
                {
                    db.Player.Add(record = new Player
                    {
                        FirstSeenTime = DateTime.UtcNow,
                        UserId = userId.UserId,
                    });
                }

                record.LastSeenTime = DateTime.UtcNow;
                record.LastSeenAddress = address;
                record.LastSeenUserName = userName;
                record.LastSeenHWId = hwId;

                await db.SaveChangesAsync();
            });
        }

        public async Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel)
        {
            await using var db = await GetDb();

            // Sort by descending last seen time.
            // So if, due to account renames, we have two people with the same username in the DB,
            // the most recent one is picked.
            var record = await db.DbContext.Player
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync(p => p.LastSeenUserName == userName, cancel);

            return record == null ? null : MakePlayerRecord(record);
        }

        public async Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb();

            var record = await db.DbContext.Player
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

            return record == null ? null : MakePlayerRecord(record);
        }

        protected async Task<bool> PlayerRecordExists(DbGuard db, NetUserId userId)
        {
            return await db.DbContext.Player.AnyAsync(p => p.UserId == userId);
        }

        [return: NotNullIfNotNull(nameof(player))]
        protected PlayerRecord? MakePlayerRecord(Player? player)
        {
            if (player == null)
                return null;

            return MakePlayerRecord(player.UserId, player);
        }

        protected PlayerRecord MakePlayerRecord(Guid userId, Player? player)
        {
            if (player == null)
            {
                // We don't have a record for this player in the database.
                // This is possible, for example, when banning people that never connected to the server.
                // Just return fallback data here, I guess.
                return new PlayerRecord(new NetUserId(userId), default, userId.ToString(), default, null, null);
            }

            return new PlayerRecord(
                new NetUserId(player.UserId),
                new DateTimeOffset(NormalizeDatabaseTime(player.FirstSeenTime)),
                player.LastSeenUserName,
                new DateTimeOffset(NormalizeDatabaseTime(player.LastSeenTime)),
                player.LastSeenAddress,
                player.LastSeenHWId);
        }

        #endregion

        #region Connection Logs
        /*
         * CONNECTION LOG
         */
        public abstract Task<int> AddConnectionLogAsync(NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableTypedHwid? hwId,
            float trust,
            ConnectionDenyReason? denied,
            int serverId);

        public async Task AddServerBanHitsAsync(int connection, IEnumerable<BanDef> bans)
        {
            await using var db = await GetDb();

            foreach (var ban in bans)
            {
                db.DbContext.ServerBanHit.Add(new ServerBanHit
                {
                    ConnectionId = connection, BanId = ban.Id!.Value
                });
            }

            await db.DbContext.SaveChangesAsync();
        }

        #endregion

        #region Admin Ranks
        /*
         * ADMIN RANKS
         */
        public async Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            return await db.DbContext.Admin
                .Include(p => p.Flags)
                .Include(p => p.AdminRank)
                .ThenInclude(p => p!.Flags)
                .AsSplitQuery() // tests fail because of a random warning if you dont have this!
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
        }

        public abstract Task<((Admin, string? lastUserName)[] admins, AdminRank[])>
            GetAllAdminAndRanksAsync(CancellationToken cancel);

        public async Task<AdminRank?> GetAdminRankDataForAsync(int id, CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            return await db.DbContext.AdminRank
                .Include(r => r.Flags)
                .SingleOrDefaultAsync(r => r.Id == id, cancel);
        }

        public async Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, ct) =>
            {
                var admin = await db.Admin.SingleAsync(a => a.UserId == userId.UserId, ct);
                db.Admin.Remove(admin);
                await db.SaveChangesAsync(ct);
            }, cancel);
        }

        public async Task AddAdminAsync(Admin admin, CancellationToken cancel)
        {
            await WithUserIdMigrationWriteLockAsync(admin.UserId, async (db, ct) =>
            {
                db.Admin.Add(admin);
                await db.SaveChangesAsync(ct);
            }, cancel);
        }

        public async Task UpdateAdminAsync(Admin admin, CancellationToken cancel)
        {
            await WithUserIdMigrationWriteLockAsync(admin.UserId, async (db, ct) =>
            {
                var existing = await db.Admin.Include(a => a.Flags).SingleAsync(a => a.UserId == admin.UserId, ct);
                existing.Flags = admin.Flags;
                existing.Title = admin.Title;
                existing.AdminRankId = admin.AdminRankId;
                existing.Deadminned = admin.Deadminned;
                existing.Suspended = admin.Suspended;
                await db.SaveChangesAsync(ct);
            }, cancel);
        }

        public async Task UpdateAdminDeadminnedAsync(NetUserId userId, bool deadminned, CancellationToken cancel)
        {
            await WithUserIdMigrationWriteLockAsync(userId.UserId, async (db, ct) =>
            {
                var adminRecord = db.Admin.Where(a => a.UserId == userId);
                await adminRecord.ExecuteUpdateAsync(
                    set => set.SetProperty(p => p.Deadminned, deadminned),
                    cancellationToken: ct);
            }, cancel);
        }

        public async Task RemoveAdminRankAsync(int rankId, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var admin = await db.DbContext.AdminRank.SingleAsync(a => a.Id == rankId, cancel);
            db.DbContext.AdminRank.Remove(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            db.DbContext.AdminRank.Add(rank);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task<int> AddNewRound(Server server, params Guid[] playerIds)
        {
            await using var db = await GetDb();

            var players = await db.DbContext.Player
                .Where(player => playerIds.Contains(player.UserId))
                .ToListAsync();

            var round = new Round
            {
                StartDate = DateTime.UtcNow,
                Players = players,
                ServerId = server.Id
            };

            db.DbContext.Round.Add(round);

            await db.DbContext.SaveChangesAsync();

            return round.Id;
        }

        public async Task<Round> GetRound(int id)
        {
            await using var db = await GetDb();

            var round = await db.DbContext.Round
                .Include(round => round.Players)
                .SingleAsync(round => round.Id == id);

            return round;
        }

        public async Task AddRoundPlayers(int id, Guid[] playerIds)
        {
            await using var db = await GetDb();

            // ReSharper disable once SuggestVarOrType_Elsewhere
            Dictionary<Guid, int> players = await db.DbContext.Player
                .Where(player => playerIds.Contains(player.UserId))
                .ToDictionaryAsync(player => player.UserId, player => player.Id);

            foreach (var player in playerIds)
            {
                await db.DbContext.Database.ExecuteSqlAsync($"""
INSERT INTO player_round (players_id, rounds_id) VALUES ({players[player]}, {id}) ON CONFLICT DO NOTHING
""");
            }

            await db.DbContext.SaveChangesAsync();
        }

        // DS14-start
        public async Task SetRoundGameModeHistoryAsync(int id, string? presetName, int? playerCount, string? mapName)
        {
            await using var db = await GetDb();

            var round = await db.DbContext.Round.SingleOrDefaultAsync(round => round.Id == id);
            if (round == null)
                return;

            round.GamePresetName = string.IsNullOrWhiteSpace(presetName) ? null : presetName;
            round.StartPlayerCount = playerCount;
            round.MapName = string.IsNullOrWhiteSpace(mapName) ? null : mapName;
            await db.DbContext.SaveChangesAsync();
        }

        public async Task<List<RoundGameModeRecord>> GetRoundGameModeHistoryAsync(int serverId, DateTime fromUtc)
        {
            await using var db = await GetDb();

            var rounds = await db.DbContext.Round
                .Where(round =>
                    round.ServerId == serverId &&
                    round.StartDate != null &&
                    round.StartDate >= fromUtc &&
                    round.GamePresetName != null &&
                    round.GamePresetName != string.Empty)
                .OrderByDescending(round => round.StartDate)
                .Select(round => new
                {
                    round.Id,
                    StartDate = round.StartDate!.Value,
                    GamePresetName = round.GamePresetName!,
                    PlayerCount = round.StartPlayerCount,
                    round.MapName
                })
                .ToListAsync();

            return rounds
                .Select(round => new RoundGameModeRecord(
                    round.Id,
                    NormalizeDatabaseTime(round.StartDate),
                    round.GamePresetName,
                    round.PlayerCount,
                    round.MapName))
                .ToList();
        }

        public async Task<AutoMapVoteConfigRecord?> GetAutoMapVoteConfigAsync(
            string serverId,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            var entity = await db.DbContext.AutoMapVoteConfigs
                .SingleOrDefaultAsync(config => config.ServerId == serverId, cancel);

            return entity == null
                ? null
                : MakeAutoMapVoteConfigRecord(entity);
        }

        public async Task UpsertAutoMapVoteConfigAsync(
            AutoMapVoteConfigRecord config,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            var entity = await db.DbContext.AutoMapVoteConfigs
                .SingleOrDefaultAsync(existing => existing.ServerId == config.ServerId, cancel);

            if (entity == null)
            {
                entity = new AutoMapVoteConfig
                {
                    ServerId = config.ServerId,
                };

                db.DbContext.AutoMapVoteConfigs.Add(entity);
            }

            ApplyAutoMapVoteConfig(entity, config);
            await db.DbContext.SaveChangesAsync(cancel);
        }

        private static AutoMapVoteConfigRecord MakeAutoMapVoteConfigRecord(AutoMapVoteConfig entity)
        {
            return new AutoMapVoteConfigRecord(
                entity.ServerId,
                entity.Enabled,
                entity.SmallMaxPlayers,
                entity.MediumMaxPlayers,
                entity.LargeMaxPlayers,
                entity.SmallMaps,
                entity.MediumMaps,
                entity.LargeMaps,
                entity.BlacklistMaps,
                entity.VoteDurationSeconds,
                entity.SmallPlayedMaps,
                entity.MediumPlayedMaps,
                entity.LargePlayedMaps,
                entity.SmallPoolQueueMaps,
                entity.MediumPoolQueueMaps,
                entity.LargePoolQueueMaps);
        }

        private static void ApplyAutoMapVoteConfig(AutoMapVoteConfig entity, AutoMapVoteConfigRecord config)
        {
            entity.Enabled = config.Enabled;
            entity.SmallMaxPlayers = config.SmallMaxPlayers;
            entity.MediumMaxPlayers = config.MediumMaxPlayers;
            entity.LargeMaxPlayers = config.LargeMaxPlayers;
            entity.SmallMaps = config.SmallMaps;
            entity.MediumMaps = config.MediumMaps;
            entity.LargeMaps = config.LargeMaps;
            entity.BlacklistMaps = config.BlacklistMaps;
            entity.VoteDurationSeconds = config.VoteDurationSeconds;
            entity.SmallPlayedMaps = config.SmallPlayedMaps;
            entity.MediumPlayedMaps = config.MediumPlayedMaps;
            entity.LargePlayedMaps = config.LargePlayedMaps;
            entity.SmallPoolQueueMaps = config.SmallPoolQueueMaps;
            entity.MediumPoolQueueMaps = config.MediumPoolQueueMaps;
            entity.LargePoolQueueMaps = config.LargePoolQueueMaps;
        }
        public abstract Task<GamePresetConfigRecord?> GetGamePresetConfigAsync(string serverId, CancellationToken cancel = default);
        public abstract Task UpsertGamePresetConfigAsync(GamePresetConfigRecord config, CancellationToken cancel = default);
        // DS14-end

        [return: NotNullIfNotNull(nameof(round))]
        protected RoundRecord? MakeRoundRecord(Round? round)
        {
            if (round == null)
                return null;

            return new RoundRecord(
                round.Id,
                NormalizeDatabaseTime(round.StartDate),
                MakeServerRecord(round.Server));
        }

        public async Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var existing = await db.DbContext.AdminRank
                .Include(r => r.Flags)
                .SingleAsync(a => a.Id == rank.Id, cancel);

            existing.Flags = rank.Flags;
            existing.Name = rank.Name;

            await db.DbContext.SaveChangesAsync(cancel);
        }
        #endregion

        #region Admin Logs

        public async Task<(Server, bool existed)> AddOrGetServer(string serverName)
        {
            await using var db = await GetDb();
            var server = await db.DbContext.Server
                .Where(server => server.Name.Equals(serverName))
                .SingleOrDefaultAsync();

            if (server != default)
                return (server, true);

            server = new Server
            {
                Name = serverName
            };

            db.DbContext.Server.Add(server);

            await db.DbContext.SaveChangesAsync();

            return (server, false);
        }

        [return: NotNullIfNotNull(nameof(server))]
        protected ServerRecord? MakeServerRecord(Server? server)
        {
            if (server == null)
                return null;

            return new ServerRecord(server.Id, server.Name);
        }

        public async Task AddAdminLogs(List<AdminLog> logs)
        {
            const int maxRetryAttempts = 5;
            var initialRetryDelay = TimeSpan.FromSeconds(5);

            DebugTools.Assert(logs.All(x => x.RoundId > 0), "Adding logs with invalid round ids.");

            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt < maxRetryAttempts)
            {
                try
                {
                    await using var db = await GetDb();
                    db.DbContext.AdminLog.AddRange(logs);
                    await db.DbContext.SaveChangesAsync();
                    _opsLog.Debug($"Successfully saved {logs.Count} admin logs.");
                    break;
                }
                catch (Exception ex)
                {
                    attempt += 1;
                    _opsLog.Error($"Attempt {attempt} failed to save logs: {ex}");

                    if (attempt >= maxRetryAttempts)
                    {
                        _opsLog.Error($"Max retry attempts reached. Failed to save {logs.Count} admin logs.");
                        return;
                    }

                    _opsLog.Warning($"Retrying in {retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(retryDelay);

                    retryDelay *= 2;
                }
            }
        }

        protected abstract IQueryable<AdminLog> StartAdminLogsQuery(ServerDbContext db, LogFilter? filter = null);

        private IQueryable<AdminLog> GetAdminLogsQuery(ServerDbContext db, LogFilter? filter = null)
        {
            // Save me from SQLite
            var query = StartAdminLogsQuery(db, filter);

            if (filter == null)
            {
                return query.OrderBy(log => log.Date);
            }

            if (filter.Round != null)
            {
                query = query.Where(log => log.RoundId == filter.Round);
            }

            if (filter.Types != null)
            {
                query = query.Where(log => filter.Types.Contains(log.Type));
            }

            if (filter.Impacts != null)
            {
                query = query.Where(log => filter.Impacts.Contains(log.Impact));
            }

            if (filter.Before != null)
            {
                query = query.Where(log => log.Date < filter.Before);
            }

            if (filter.After != null)
            {
                query = query.Where(log => log.Date > filter.After);
            }

            if (filter.IncludePlayers)
            {
                if (filter.AnyPlayers != null)
                {
                    query = query.Where(log =>
                        log.Players.Any(p => filter.AnyPlayers.Contains(p.PlayerUserId)) ||
                        log.Players.Count == 0 && filter.IncludeNonPlayers);
                }

                if (filter.AllPlayers != null)
                {
                    query = query.Where(log =>
                        log.Players.All(p => filter.AllPlayers.Contains(p.PlayerUserId)) ||
                        log.Players.Count == 0 && filter.IncludeNonPlayers);
                }
            }
            else
            {
                query = query.Where(log => log.Players.Count == 0);
            }

            if (filter.LastLogId != null)
            {
                query = filter.DateOrder switch
                {
                    DateOrder.Ascending => query.Where(log => log.Id > filter.LastLogId),
                    DateOrder.Descending => query.Where(log => log.Id < filter.LastLogId),
                    _ => throw new ArgumentOutOfRangeException(nameof(filter),
                        $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
                };
            }

            query = filter.DateOrder switch
            {
                DateOrder.Ascending => query.OrderBy(log => log.Date),
                DateOrder.Descending => query.OrderByDescending(log => log.Date),
                _ => throw new ArgumentOutOfRangeException(nameof(filter),
                    $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
            };

            const int hardLogLimit = 500_000;
            if (filter.Limit != null)
            {
                query = query.Take(Math.Min(filter.Limit.Value, hardLogLimit));
            }
            else
            {
                query = query.Take(hardLogLimit);
            }

            return query;
        }

        public async IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null)
        {
            await using var db = await GetDb();
            var query = GetAdminLogsQuery(db.DbContext, filter);

            await foreach (var log in query.Select(log => log.Message).AsAsyncEnumerable())
            {
                yield return log;
            }
        }

        public async IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null)
        {
            await using var db = await GetDb();
            var query = GetAdminLogsQuery(db.DbContext, filter);
            query = query.Include(log => log.Players);

            await foreach (var log in query.AsAsyncEnumerable())
            {
                var players = new Guid[log.Players.Count];
                for (var i = 0; i < log.Players.Count; i++)
                {
                    players[i] = log.Players[i].PlayerUserId;
                }

                yield return new SharedAdminLog(log.Id, log.Type, log.Impact, log.Date, log.Message, players);
            }
        }

        public async IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null)
        {
            await using var db = await GetDb();
            var query = GetAdminLogsQuery(db.DbContext, filter);

            await foreach (var json in query.Select(log => log.Json).AsAsyncEnumerable())
            {
                yield return json;
            }
        }

        public async Task<int> CountAdminLogs(int round)
        {
            await using var db = await GetDb();
            return await db.DbContext.AdminLog.CountAsync(log => log.RoundId == round);
        }

        #endregion

        #region Whitelist

        public async Task<bool> GetWhitelistStatusAsync(NetUserId player)
        {
            await using var db = await GetDb();

            return await db.DbContext.Whitelist.AnyAsync(w => w.UserId == player);
        }

        public async Task AddToWhitelistAsync(NetUserId player)
        {
            await WithUserIdMigrationWriteLockAsync(player.UserId, async (db, _) =>
            {
                db.Whitelist.Add(new Whitelist { UserId = player });
                await db.SaveChangesAsync();
            });
        }

        public async Task RemoveFromWhitelistAsync(NetUserId player)
        {
            await WithUserIdMigrationWriteLockAsync(player.UserId, async (db, _) =>
            {
                var entry = await db.Whitelist.SingleAsync(w => w.UserId == player);
                db.Whitelist.Remove(entry);
                await db.SaveChangesAsync();
            });
        }

        public async Task<DateTimeOffset?> GetLastReadRules(NetUserId player)
        {
            await using var db = await GetDb();

            return NormalizeDatabaseTime(await db.DbContext.Player
                .Where(dbPlayer => dbPlayer.UserId == player)
                .Select(dbPlayer => dbPlayer.LastReadRules)
                .SingleOrDefaultAsync());
        }

        public async Task SetLastReadRules(NetUserId player, DateTimeOffset? date)
        {
            await WithUserIdMigrationWriteLockAsync(player.UserId, async (db, _) =>
            {
                var dbPlayer = await db.Player.Where(dbPlayer => dbPlayer.UserId == player).SingleOrDefaultAsync();
                if (dbPlayer == null)
                {
                    return;
                }

                dbPlayer.LastReadRules = date?.UtcDateTime;
                await db.SaveChangesAsync();
            });
        }

        public async Task<bool> GetBlacklistStatusAsync(NetUserId player)
        {
            await using var db = await GetDb();

            return await db.DbContext.Blacklist.AnyAsync(w => w.UserId == player);
        }

        public async Task AddToBlacklistAsync(NetUserId player)
        {
            await WithUserIdMigrationWriteLockAsync(player.UserId, async (db, _) =>
            {
                db.Blacklist.Add(new Blacklist { UserId = player });
                await db.SaveChangesAsync();
            });
        }

        public async Task RemoveFromBlacklistAsync(NetUserId player)
        {
            await WithUserIdMigrationWriteLockAsync(player.UserId, async (db, _) =>
            {
                var entry = await db.Blacklist.SingleAsync(w => w.UserId == player);
                db.Blacklist.Remove(entry);
                await db.SaveChangesAsync();
            });
        }

        #endregion

        #region Uploaded Resources Logs

        public async Task AddUploadedResourceLogAsync(NetUserId user, DateTimeOffset date, string path, byte[] data)
        {
            await using var db = await GetDb();

            db.DbContext.UploadedResourceLog.Add(new UploadedResourceLog() { UserId = user, Date = date.UtcDateTime, Path = path, Data = data });
            await db.DbContext.SaveChangesAsync();
        }

        public async Task PurgeUploadedResourceLogAsync(int days)
        {
            await using var db = await GetDb();

            var date = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days));

            await foreach (var log in db.DbContext.UploadedResourceLog
                               .Where(l => date > l.Date)
                               .AsAsyncEnumerable())
            {
                db.DbContext.UploadedResourceLog.Remove(log);
            }

            await db.DbContext.SaveChangesAsync();
        }

        #endregion

        #region Admin Notes

        public virtual async Task<int> AddAdminNote(AdminNote note)
        {
            await using var db = await GetDb();
            db.DbContext.AdminNotes.Add(note);
            await db.DbContext.SaveChangesAsync();
            return note.Id;
        }

        public virtual async Task<int> AddAdminWatchlist(AdminWatchlist watchlist)
        {
            await using var db = await GetDb();
            db.DbContext.AdminWatchlists.Add(watchlist);
            await db.DbContext.SaveChangesAsync();
            return watchlist.Id;
        }

        public virtual async Task<int> AddAdminMessage(AdminMessage message)
        {
            await using var db = await GetDb();
            db.DbContext.AdminMessages.Add(message);
            await db.DbContext.SaveChangesAsync();
            return message.Id;
        }

        public async Task<AdminNoteRecord?> GetAdminNote(int id)
        {
            await using var db = await GetDb();
            var entity = await db.DbContext.AdminNotes
                .Where(note => note.Id == id)
                .Include(note => note.Round)
                .ThenInclude(r => r!.Server)
                .Include(note => note.CreatedBy)
                .Include(note => note.LastEditedBy)
                .Include(note => note.DeletedBy)
                .Include(note => note.Player)
                .SingleOrDefaultAsync();

            return entity == null ? null : MakeAdminNoteRecord(entity);
        }

        private AdminNoteRecord MakeAdminNoteRecord(AdminNote entity)
        {
            return new AdminNoteRecord(
                entity.Id,
                MakeRoundRecord(entity.Round),
                MakePlayerRecord(entity.Player),
                entity.PlaytimeAtNote,
                entity.Message,
                entity.Severity,
                MakePlayerRecord(entity.CreatedBy),
                NormalizeDatabaseTime(entity.CreatedAt),
                MakePlayerRecord(entity.LastEditedBy),
                NormalizeDatabaseTime(entity.LastEditedAt),
                NormalizeDatabaseTime(entity.ExpirationTime),
                entity.Deleted,
                MakePlayerRecord(entity.DeletedBy),
                NormalizeDatabaseTime(entity.DeletedAt),
                entity.Secret);
        }

        public async Task<AdminWatchlistRecord?> GetAdminWatchlist(int id)
        {
            await using var db = await GetDb();
            var entity = await db.DbContext.AdminWatchlists
                .Where(note => note.Id == id)
                .Include(note => note.Round)
                .ThenInclude(r => r!.Server)
                .Include(note => note.CreatedBy)
                .Include(note => note.LastEditedBy)
                .Include(note => note.DeletedBy)
                .Include(note => note.Player)
                .SingleOrDefaultAsync();

            return entity == null ? null : MakeAdminWatchlistRecord(entity);
        }

        public async Task<AdminMessageRecord?> GetAdminMessage(int id)
        {
            await using var db = await GetDb();
            var entity = await db.DbContext.AdminMessages
                .Where(note => note.Id == id)
                .Include(note => note.Round)
                .ThenInclude(r => r!.Server)
                .Include(note => note.CreatedBy)
                .Include(note => note.LastEditedBy)
                .Include(note => note.DeletedBy)
                .Include(note => note.Player)
                .SingleOrDefaultAsync();

            return entity == null ? null : MakeAdminMessageRecord(entity);
        }

        private AdminMessageRecord MakeAdminMessageRecord(AdminMessage entity)
        {
            return new AdminMessageRecord(
                entity.Id,
                MakeRoundRecord(entity.Round),
                MakePlayerRecord(entity.Player),
                entity.PlaytimeAtNote,
                entity.Message,
                MakePlayerRecord(entity.CreatedBy),
                NormalizeDatabaseTime(entity.CreatedAt),
                MakePlayerRecord(entity.LastEditedBy),
                NormalizeDatabaseTime(entity.LastEditedAt),
                NormalizeDatabaseTime(entity.ExpirationTime),
                entity.Deleted,
                MakePlayerRecord(entity.DeletedBy),
                NormalizeDatabaseTime(entity.DeletedAt),
                entity.Seen,
                entity.Dismissed);
        }

        public async Task<BanNoteRecord?> GetBanAsNoteAsync(int id)
        {
            await using var db = await GetDb();

            var ban = await BanRecordQuery(db.DbContext)
                .SingleOrDefaultAsync(b => b.Id == id);

            if (ban is null)
                return null;

            return await MakeBanNoteRecord(db.DbContext, ban);
        }

        public async Task<List<IAdminRemarksRecord>> GetAllAdminRemarks(Guid player)
        {
            await using var db = await GetDb();
            List<IAdminRemarksRecord> notes = new();
            notes.AddRange(
                (await (from note in db.DbContext.AdminNotes
                        where note.PlayerUserId == player &&
                              !note.Deleted &&
                              (note.ExpirationTime == null || DateTime.UtcNow < note.ExpirationTime)
                        select note)
                    .Include(note => note.Round)
                    .ThenInclude(r => r!.Server)
                    .Include(note => note.CreatedBy)
                    .Include(note => note.LastEditedBy)
                    .Include(note => note.Player)
                    .ToListAsync()).Select(MakeAdminNoteRecord));
            notes.AddRange(await GetActiveWatchlistsImpl(db, player));
            notes.AddRange(await GetMessagesImpl(db, player));
            notes.AddRange(await GetBansAsNotesForUser(db, player));
            return notes;
        }
        public async Task EditAdminNote(int id, string message, NoteSeverity severity, bool secret, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            await using var db = await GetDb();

            var note = await db.DbContext.AdminNotes.Where(note => note.Id == id).SingleAsync();
            note.Message = message;
            note.Severity = severity;
            note.Secret = secret;
            note.LastEditedById = editedBy;
            note.LastEditedAt = editedAt.UtcDateTime;
            note.ExpirationTime = expiryTime?.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task EditAdminWatchlist(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            await using var db = await GetDb();

            var note = await db.DbContext.AdminWatchlists.Where(note => note.Id == id).SingleAsync();
            note.Message = message;
            note.LastEditedById = editedBy;
            note.LastEditedAt = editedAt.UtcDateTime;
            note.ExpirationTime = expiryTime?.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task EditAdminMessage(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            await using var db = await GetDb();

            var note = await db.DbContext.AdminMessages.Where(note => note.Id == id).SingleAsync();
            note.Message = message;
            note.LastEditedById = editedBy;
            note.LastEditedAt = editedAt.UtcDateTime;
            note.ExpirationTime = expiryTime?.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task DeleteAdminNote(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            await using var db = await GetDb();

            var note = await db.DbContext.AdminNotes.Where(note => note.Id == id).SingleAsync();

            note.Deleted = true;
            note.DeletedById = deletedBy;
            note.DeletedAt = deletedAt.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task DeleteAdminWatchlist(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            await using var db = await GetDb();

            var watchlist = await db.DbContext.AdminWatchlists.Where(note => note.Id == id).SingleAsync();

            watchlist.Deleted = true;
            watchlist.DeletedById = deletedBy;
            watchlist.DeletedAt = deletedAt.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task DeleteAdminMessage(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            await using var db = await GetDb();

            var message = await db.DbContext.AdminMessages.Where(note => note.Id == id).SingleAsync();

            message.Deleted = true;
            message.DeletedById = deletedBy;
            message.DeletedAt = deletedAt.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task HideBanFromNotes(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            await using var db = await GetDb();

            var ban = await db.DbContext.Ban.Where(ban => ban.Id == id).SingleAsync();

            ban.Hidden = true;
            ban.LastEditedById = deletedBy;
            ban.LastEditedAt = deletedAt.UtcDateTime;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task<List<IAdminRemarksRecord>> GetVisibleAdminRemarks(Guid player)
        {
            await using var db = await GetDb();
            List<IAdminRemarksRecord> notesCol = new();
            notesCol.AddRange(
                (await (from note in db.DbContext.AdminNotes
                        where note.PlayerUserId == player &&
                              !note.Secret &&
                              !note.Deleted &&
                              (note.ExpirationTime == null || DateTime.UtcNow < note.ExpirationTime)
                        select note)
                    .Include(note => note.Round)
                    .ThenInclude(r => r!.Server)
                    .Include(note => note.CreatedBy)
                    .Include(note => note.Player)
                    .ToListAsync()).Select(MakeAdminNoteRecord));
            notesCol.AddRange(await GetMessagesImpl(db, player));
            notesCol.AddRange(await GetBansAsNotesForUser(db, player));
            return notesCol;
        }

        public async Task<List<AdminWatchlistRecord>> GetActiveWatchlists(Guid player)
        {
            await using var db = await GetDb();
            return await GetActiveWatchlistsImpl(db, player);
        }

        protected async Task<List<AdminWatchlistRecord>> GetActiveWatchlistsImpl(DbGuard db, Guid player)
        {
            var entities = await (from watchlist in db.DbContext.AdminWatchlists
                          where watchlist.PlayerUserId == player &&
                                !watchlist.Deleted &&
                                (watchlist.ExpirationTime == null || DateTime.UtcNow < watchlist.ExpirationTime)
                          select watchlist)
                .Include(note => note.Round)
                .ThenInclude(r => r!.Server)
                .Include(note => note.CreatedBy)
                .Include(note => note.LastEditedBy)
                .Include(note => note.Player)
                .ToListAsync();

            return entities.Select(MakeAdminWatchlistRecord).ToList();
        }

        private AdminWatchlistRecord MakeAdminWatchlistRecord(AdminWatchlist entity)
        {
            return new AdminWatchlistRecord(entity.Id, MakeRoundRecord(entity.Round), MakePlayerRecord(entity.Player), entity.PlaytimeAtNote, entity.Message, MakePlayerRecord(entity.CreatedBy), NormalizeDatabaseTime(entity.CreatedAt), MakePlayerRecord(entity.LastEditedBy), NormalizeDatabaseTime(entity.LastEditedAt), NormalizeDatabaseTime(entity.ExpirationTime), entity.Deleted, MakePlayerRecord(entity.DeletedBy), NormalizeDatabaseTime(entity.DeletedAt));
        }

        public async Task<List<AdminMessageRecord>> GetMessages(Guid player)
        {
            await using var db = await GetDb();
            return await GetMessagesImpl(db, player);
        }

        protected async Task<List<AdminMessageRecord>> GetMessagesImpl(DbGuard db, Guid player)
        {
            var entities = await (from message in db.DbContext.AdminMessages
                        where message.PlayerUserId == player && !message.Deleted &&
                              (message.ExpirationTime == null || DateTime.UtcNow < message.ExpirationTime)
                        select message).Include(note => note.Round)
                    .ThenInclude(r => r!.Server)
                    .Include(note => note.CreatedBy)
                    .Include(note => note.LastEditedBy)
                    .Include(note => note.Player)
                    .ToListAsync();

            return entities.Select(MakeAdminMessageRecord).ToList();
        }

        public async Task MarkMessageAsSeen(int id, bool dismissedToo)
        {
            await using var db = await GetDb();
            var message = await db.DbContext.AdminMessages.SingleAsync(m => m.Id == id);
            message.Seen = true;
            if (dismissedToo)
                message.Dismissed = true;
            await db.DbContext.SaveChangesAsync();
        }

        private static IQueryable<Ban> BanRecordQuery(ServerDbContext dbContext)
        {
            return dbContext.Ban
                .Include(ban => ban.Unban)
                .Include(ban => ban.Rounds!)
                .ThenInclude(r => r.Round)
                .ThenInclude(r => r!.Server)
                .Include(ban => ban.Addresses)
                .Include(ban => ban.Players)
                .Include(ban => ban.Roles)
                .Include(ban => ban.Hwids)
                .Include(ban => ban.CreatedBy)
                .Include(ban => ban.LastEditedBy)
                .Include(ban => ban.Unban);
        }

        private async Task<BanNoteRecord> MakeBanNoteRecord(ServerDbContext dbContext, Ban ban)
        {
            var playerRecords = await AsyncSelect(ban.Players,
                async bp => MakePlayerRecord(bp.UserId,
                    await dbContext.Player.SingleOrDefaultAsync(p => p.UserId == bp.UserId)));

            return new BanNoteRecord(
                ban.Id,
                ban.Type,
                [..ban.Rounds!.Select(br => MakeRoundRecord(br.Round!))],
                [..playerRecords],
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                MakePlayerRecord(ban.CreatedBy!),
                NormalizeDatabaseTime(ban.BanTime),
                MakePlayerRecord(ban.LastEditedBy!),
                NormalizeDatabaseTime(ban.LastEditedAt),
                NormalizeDatabaseTime(ban.ExpirationTime),
                ban.Hidden,
                ban.Unban?.UnbanningAdmin == null
                    ? null
                    : MakePlayerRecord(
                        ban.Unban.UnbanningAdmin.Value,
                        await dbContext.Player.SingleOrDefaultAsync(p => p.UserId == ban.Unban.UnbanningAdmin.Value)),
                NormalizeDatabaseTime(ban.Unban?.UnbanTime),
                [..ban.Roles!.Select(br => new BanRoleDef(br.RoleType, br.RoleId))]);
        }

        // These two are here because they get converted into notes later
        protected async Task<List<BanNoteRecord>> GetBansAsNotesForUser(DbGuard db, Guid user)
        {
            // You can't group queries, as player will not always exist. When it doesn't, the
            // whole query returns nothing
            var bans = await BanRecordQuery(db.DbContext)
                .AsSplitQuery()
                .Where(ban => ban.Players!.Any(bp => bp.UserId == user) && !ban.Hidden)
                .ToArrayAsync();

            var banNotes = new List<BanNoteRecord>();
            foreach (var ban in bans)
            {
                var banNote = await MakeBanNoteRecord(db.DbContext, ban);

                banNotes.Add(banNote);
            }

            return banNotes;
        }

        #endregion

        #region Job Whitelists

        public async Task<bool> AddJobWhitelist(Guid player, ProtoId<JobPrototype> job)
        {
            return await WithUserIdMigrationWriteLockAsync(player, async (db, _) =>
            {
                var exists = await db.RoleWhitelists
                    .Where(w => w.PlayerUserId == player)
                    .Where(w => w.RoleId == job.Id)
                    .AnyAsync();

                if (exists)
                    return false;

                var whitelist = new RoleWhitelist
                {
                    PlayerUserId = player,
                    RoleId = job
                };
                db.RoleWhitelists.Add(whitelist);
                await db.SaveChangesAsync();
                return true;
            });
        }

        public async Task<List<string>> GetJobWhitelists(Guid player, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);
            return await db.DbContext.RoleWhitelists
                .Where(w => w.PlayerUserId == player)
                .Select(w => w.RoleId)
                .ToListAsync(cancellationToken: cancel);
        }

        public async Task<bool> IsJobWhitelisted(Guid player, ProtoId<JobPrototype> job)
        {
            await using var db = await GetDb();
            return await db.DbContext.RoleWhitelists
                .Where(w => w.PlayerUserId == player)
                .Where(w => w.RoleId == job.Id)
                .AnyAsync();
        }

        public async Task<bool> RemoveJobWhitelist(Guid player, ProtoId<JobPrototype> job)
        {
            return await WithUserIdMigrationWriteLockAsync(player, async (db, _) =>
            {
                var entry = await db.RoleWhitelists
                    .Where(w => w.PlayerUserId == player)
                    .Where(w => w.RoleId == job.Id)
                    .SingleOrDefaultAsync();

                if (entry == null)
                    return false;

                db.RoleWhitelists.Remove(entry);
                await db.SaveChangesAsync();
                return true;
            });
        }

        #endregion

        # region IPIntel

        public async Task<bool> UpsertIPIntelCache(DateTime time, IPAddress ip, float score)
        {
            while (true)
            {
                try
                {
                    await using var db = await GetDb();

                    var existing = await db.DbContext.IPIntelCache
                        .Where(w => ip.Equals(w.Address))
                        .SingleOrDefaultAsync();

                    if (existing == null)
                    {
                        var newCache = new IPIntelCache
                        {
                            Time = time,
                            Address = ip,
                            Score = score,
                        };
                        db.DbContext.IPIntelCache.Add(newCache);
                    }
                    else
                    {
                        existing.Time = time;
                        existing.Score = score;
                    }

                    await Task.Delay(5000);

                    await db.DbContext.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateException)
                {
                    _opsLog.Warning("IPIntel UPSERT failed with a db exception... retrying.");
                }
            }
        }

        public async Task<IPIntelCache?> GetIPIntelCache(IPAddress ip)
        {
            await using var db = await GetDb();

            return await db.DbContext.IPIntelCache
                .SingleOrDefaultAsync(w => ip.Equals(w.Address));
        }

        public async Task<bool> CleanIPIntelCache(TimeSpan range)
        {
            await using var db = await GetDb();

            // Calculating this here cause otherwise sqlite whines.
            var cutoffTime = DateTime.UtcNow.Subtract(range);

            await db.DbContext.IPIntelCache
                .Where(w => w.Time <= cutoffTime)
                .ExecuteDeleteAsync();

            await db.DbContext.SaveChangesAsync();
            return true;
        }

        #endregion

        public abstract Task SendNotification(DatabaseNotification notification);

        // SQLite returns DateTime as Kind=Unspecified, Npgsql actually knows for sure it's Kind=Utc.
        // Normalize DateTimes here so they're always Utc. Thanks.
        protected abstract DateTime NormalizeDatabaseTime(DateTime time);

        [return: NotNullIfNotNull(nameof(time))]
        protected DateTime? NormalizeDatabaseTime(DateTime? time)
        {
            return time != null ? NormalizeDatabaseTime(time.Value) : time;
        }

        public async Task<bool> HasPendingModelChanges()
        {
            await using var db = await GetDb();
            return db.DbContext.Database.HasPendingModelChanges();
        }

        protected abstract Task<DbGuard> GetDb(
            CancellationToken cancel = default,
            [CallerMemberName] string? name = null);

        protected void LogDbOp(string? name)
        {
            _opsLog.Verbose($"Running DB operation: {name ?? "unknown"}");
        }

        protected abstract class DbGuard : IAsyncDisposable
        {
            public abstract ServerDbContext DbContext { get; }

            public abstract ValueTask DisposeAsync();
        }

        protected void NotificationReceived(DatabaseNotification notification)
        {
            OnNotificationReceived?.Invoke(notification);
        }

        public virtual void Shutdown()
        {

        }

        private static async Task<IEnumerable<TResult>> AsyncSelect<T, TResult>(
            IEnumerable<T>? enumerable,
            Func<T, Task<TResult>> selector)
        {
            var results = new List<TResult>();

            foreach (var item in enumerable ?? [])
            {
                results.Add(await selector(item));
            }

            return [..results];
        }
    }
}
