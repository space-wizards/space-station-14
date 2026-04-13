using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.AuditLog;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server.Database
{
    public abstract class ServerDbBase
    {
        private readonly ISawmill _opsLog;
        public event Action<DatabaseNotification>? OnNotificationReceived;
        private readonly ISerializationManager _serialization;

        /// <param name="opsLog">Sawmill to trace log database operations to.</param>
        public ServerDbBase(ISawmill opsLog, ISerializationManager serialization)
        {
            _serialization = serialization;
            _opsLog = opsLog;
        }

        #region Preferences
        public async Task<Preference?> GetPlayerPreferencesAsync(
            NetUserId userId,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            return await db.DbContext
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
        }

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            await using var db = await GetDb();

            await SetSelectedCharacterSlotAsync(userId, index, db.DbContext);

            await db.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Only intended for use in unit tests - drops the organ marking data from a profile in the given slot
        /// </summary>
        /// <param name="userId">The user whose profile to modify</param>
        /// <param name="slot">The slot index to modify</param>
        public async Task MakeCharacterSlotLegacyAsync(NetUserId userId, int slot)
        {
            await using var db = await GetDb();

            var oldProfile = await db.DbContext.Profile
                .Include(p => p.Preference)
                .Where(p => p.Preference.UserId == userId.UserId)
                .AsSplitQuery()
                .SingleOrDefaultAsync(h => h.Slot == slot);

            if (oldProfile == null)
                return;

            oldProfile.OrganMarkings = null;
            oldProfile.Markings = JsonSerializer.SerializeToDocument(new List<string>());

            await db.DbContext.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, HumanoidCharacterProfile? humanoid, int slot)
        {
            await using var db = await GetDb();

            if (humanoid is null)
            {
                await DeleteCharacterSlot(db.DbContext, userId, slot);
                await db.DbContext.SaveChangesAsync();
                return;
            }

            var oldProfile = db.DbContext.Profile
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
                var prefs = await db.DbContext
                    .Preference
                    .Include(p => p.Profiles)
                    .SingleAsync(p => p.UserId == userId.UserId);

                prefs.Profiles.Add(newProfile);
            }

            await db.DbContext.SaveChangesAsync();
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

        public async Task<Preference> InitPrefsAsync(NetUserId userId, HumanoidCharacterProfile defaultProfile)
        {
            await using var db = await GetDb();

            var profile = ConvertProfiles((HumanoidCharacterProfile) defaultProfile, 0);
            var prefs = new Preference
            {
                UserId = userId.UserId,
                SelectedCharacterSlot = 0,
                AdminOOCColor = Color.Red.ToHex(),
                ConstructionFavorites = [],
            };

            prefs.Profiles.Add(profile);

            db.DbContext.Preference.Add(prefs);

            await db.DbContext.SaveChangesAsync();

            return prefs;
        }

        public async Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            await using var db = await GetDb();

            await DeleteCharacterSlot(db.DbContext, userId, deleteSlot);
            await SetSelectedCharacterSlotAsync(userId, newSlot, db.DbContext);

            await db.DbContext.SaveChangesAsync();
        }

        public async Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            await using var db = await GetDb();
            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles)
                .SingleAsync(p => p.UserId == userId.UserId);
            prefs.AdminOOCColor = color.ToHex();

            await db.DbContext.SaveChangesAsync();

        }

        public async Task SaveConstructionFavoritesAsync(NetUserId userId, List<ProtoId<ConstructionPrototype>> constructionFavorites)
        {
            await using var db = await GetDb();
            var prefs = await db.DbContext.Preference.SingleAsync(p => p.UserId == userId.UserId);

            var favorites = new List<string>(constructionFavorites.Count);
            foreach (var favorite in constructionFavorites)
                favorites.Add(favorite.Id);
            prefs.ConstructionFavorites = favorites;

            await db.DbContext.SaveChangesAsync();
        }

        private static async Task SetSelectedCharacterSlotAsync(NetUserId userId, int newSlot, ServerDbContext db)
        {
            var prefs = await db.Preference.SingleAsync(p => p.UserId == userId.UserId);
            prefs.SelectedCharacterSlot = newSlot;
        }

        private Profile ConvertProfiles(HumanoidCharacterProfile humanoid, int slot, Profile? profile = null)
        {
            profile ??= new Profile();
            var appearance = humanoid.Appearance;
            var dataNode = _serialization.WriteValue(appearance.Markings, alwaysWrite: true, notNullableOverride: true);

            profile.CharacterName = humanoid.Name;
            profile.FlavorText = humanoid.FlavorText;
            profile.Species = humanoid.Species;
            profile.Age = humanoid.Age;
            profile.Sex = humanoid.Sex.ToString();
            profile.Gender = humanoid.Gender.ToString();
            profile.EyeColor = appearance.EyeColor.ToHex();
            profile.SkinColor = appearance.SkinColor.ToHex();
            profile.SpawnPriority = (int) humanoid.SpawnPriority;
            profile.OrganMarkings = JsonSerializer.SerializeToDocument(dataNode.ToJsonNode());

            // support for downgrades - at some point this should be removed
            var legacyMarkings = appearance.Markings
                .SelectMany(organ => organ.Value.Values)
                .SelectMany(i => i)
                .Select(marking => marking.ToLegacyDbString())
                .ToList();
            var flattenedMarkings = appearance.Markings.SelectMany(it => it.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var hairMarking = flattenedMarkings.FirstOrNull(kvp => kvp.Key == HumanoidVisualLayers.Hair)?.Value.FirstOrNull();
            var facialHairMarking = flattenedMarkings.FirstOrNull(kvp => kvp.Key == HumanoidVisualLayers.FacialHair)?.Value.FirstOrNull();
            profile.Markings =
                JsonSerializer.SerializeToDocument(legacyMarkings.Select(marking => marking.ToString()).ToList());
            profile.HairName = hairMarking?.MarkingId ?? HairStyles.DefaultHairStyle;
            profile.FacialHairName = facialHairMarking?.MarkingId ?? HairStyles.DefaultFacialHairStyle;
            profile.HairColor = (hairMarking?.MarkingColors[0] ?? Color.Black).ToHex();
            profile.FacialHairColor = (facialHairMarking?.MarkingColors[0] ?? Color.Black).ToHex();

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
            await using var db = await GetDb();

            if (flags == 0)
            {
                // Delete whatever is there.
                await db.DbContext.BanExemption.Where(u => u.UserId == userId.UserId).ExecuteDeleteAsync();
                return;
            }

            var exemption = await db.DbContext.BanExemption.SingleOrDefaultAsync(u => u.UserId == userId.UserId);
            if (exemption == null)
            {
                exemption = new ServerBanExemption
                {
                    UserId = userId
                };

                db.DbContext.BanExemption.Add(exemption);
            }

            exemption.Flags = flags;
            await db.DbContext.SaveChangesAsync();
        }

        public async Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var flags = await GetBanExemptionCore(db, userId, cancel);
            return flags ?? ServerBanExemptFlags.None;
        }

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

        #endregion

        #region Playtime
        public async Task<List<PlayTime>> GetPlayTimes(Guid player, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            return await db.DbContext.PlayTime
                .Where(p => p.PlayerId == player)
                .ToListAsync(cancel);
        }

        public async Task UpdatePlayTimes(IReadOnlyCollection<PlayTimeUpdate> updates)
        {
            await using var db = await GetDb();

            // Ideally I would just be able to send a bunch of UPSERT commands, but EFCore is a pile of garbage.
            // So... In the interest of not making this take forever at high update counts...
            // Bulk-load play time objects for all players involved.
            // This allows us to semi-efficiently load all entities we need in a single DB query.
            // Then we can update & insert without further round-trips to the DB.

            var players = updates.Select(u => u.User.UserId).Distinct().ToArray();
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
            await using var db = await GetDb();

            var record = await db.DbContext.Player.SingleOrDefaultAsync(p => p.UserId == userId.UserId);
            if (record == null)
            {
                db.DbContext.Player.Add(record = new Player
                {
                    FirstSeenTime = DateTime.UtcNow,
                    UserId = userId.UserId,
                });
            }

            record.LastSeenTime = DateTime.UtcNow;
            record.LastSeenAddress = address;
            record.LastSeenUserName = userName;
            record.LastSeenHWId = hwId;

            await db.DbContext.SaveChangesAsync();
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
            await using var db = await GetDb(cancel);

            var admin = await db.DbContext.Admin.SingleAsync(a => a.UserId == userId.UserId, cancel);
            db.DbContext.Admin.Remove(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminAsync(Admin admin, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            db.DbContext.Admin.Add(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminAsync(Admin admin, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var existing = await db.DbContext.Admin.Include(a => a.Flags).SingleAsync(a => a.UserId == admin.UserId, cancel);
            existing.Flags = admin.Flags;
            existing.Title = admin.Title;
            existing.AdminRankId = admin.AdminRankId;
            existing.Deadminned = admin.Deadminned;
            existing.Suspended = admin.Suspended;

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminDeadminnedAsync(NetUserId userId, bool deadminned, CancellationToken cancel)
        {
            await using var db = await GetDb(cancel);

            var adminRecord = db.DbContext.Admin.Where(a => a.UserId == userId);
            await adminRecord.ExecuteUpdateAsync(
                set => set.SetProperty(p => p.Deadminned, deadminned),
                cancellationToken: cancel);

            await db.DbContext.SaveChangesAsync(cancel);
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
                .Where(s => s.Name.Equals(serverName) && s.Id > 0)
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

        public async Task<List<ServerRecord>> GetAllServers()
        {
            await using var db = await GetDb();
            return await db.DbContext.Server
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new ServerRecord(s.Id, s.Name))
                .ToListAsync();
        }

        public async Task AddAdminLogs(List<AdminLogEventWriteData> logs, CancellationToken cancel = default)
        {
            const int maxRetryAttempts = 5;
            var initialRetryDelay = TimeSpan.FromSeconds(5);

            DebugTools.Assert(logs.All(x => x.RoundId > 0), "Adding logs with invalid round ids.");
            DebugTools.Assert(logs.All(x => x.ServerId > 0), "Adding logs with invalid server ids.");

            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt < maxRetryAttempts)
            {
                try
                {
                    await using var db = await GetDb(cancel);
                    await using var tx = await db.DbContext.Database.BeginTransactionAsync(cancel);

                    var headers = new List<AdminLogEvent>(logs.Count);
                    foreach (var log in logs)
                    {
                        var header = new AdminLogEvent
                        {
                            ServerId = log.ServerId,
                            RoundId = log.RoundId,
                            Type = log.Type,
                            Impact = log.Impact,
                            OccurredAt = log.OccurredAt,
                        };

                        headers.Add(header);
                    }

                    db.DbContext.AdminLogEvent.AddRange(headers);
                    await db.DbContext.SaveChangesAsync(cancel);

                    for (var i = 0; i < headers.Count; i++)
                    {
                        logs[i].LogId = headers[i].Id;
                    }

                    var dimKeys = new HashSet<(int ServerId, int RoundId, int EntityUid)>();
                    foreach (var log in logs)
                    {
                        foreach (var entity in log.Entities)
                            dimKeys.Add((log.ServerId, log.RoundId, entity.EntityUid));
                    }

                    // Single batch query to load all existing entity dimensions.
                    // Pre-compute distinct values so they're evaluated once, not per-row.
                    Dictionary<(int, int, int), AdminLogEntityDimension> existingDims;
                    if (dimKeys.Count == 0)
                    {
                        existingDims = new Dictionary<(int, int, int), AdminLogEntityDimension>();
                    }
                    else
                    {
                        var serverIds = dimKeys.Select(k => k.ServerId).Distinct().ToArray();
                        var roundIds = dimKeys.Select(k => k.RoundId).Distinct().ToArray();
                        var entityUids = dimKeys.Select(k => k.EntityUid).Distinct().ToArray();

                        existingDims = await db.DbContext.AdminLogEntityDimension
                            .Where(d => serverIds.Contains(d.ServerId)
                                     && roundIds.Contains(d.RoundId)
                                     && entityUids.Contains(d.EntityUid))
                            .ToDictionaryAsync(d => (d.ServerId, d.RoundId, d.EntityUid), cancel);
                    }

                    for (var i = 0; i < logs.Count; i++)
                    {
                        var log = logs[i];
                        var header = headers[i];

                        db.DbContext.AdminLogEventPayload.Add(new AdminLogEventPayload
                        {
                            EventId = header.Id,
                            Message = log.Message,
                            Json = log.Json,
                        });

                        foreach (var player in log.Players)
                        {
                            // Use the caller-supplied role if available,
                            // otherwise fall back to Actor as the default participation role.
                            var playerRole = log.PlayerRoles != null && log.PlayerRoles.TryGetValue(player, out var r)
                                ? r
                                : AdminLogEntityRole.Actor;

                            db.DbContext.AdminLogEventParticipant.Add(new AdminLogEventParticipant
                            {
                                EventId = header.Id,
                                ServerId = log.ServerId,
                                RoundId = log.RoundId,
                                OccurredAt = log.OccurredAt,
                                Type = log.Type,
                                Impact = log.Impact,
                                PlayerUserId = player,
                                Role = playerRole,
                            });
                        }

                        foreach (var entity in log.Entities)
                        {
                            db.DbContext.AdminLogEventParticipant.Add(new AdminLogEventParticipant
                            {
                                EventId = header.Id,
                                ServerId = log.ServerId,
                                RoundId = log.RoundId,
                                OccurredAt = log.OccurredAt,
                                Type = log.Type,
                                Impact = log.Impact,
                                EntityUid = entity.EntityUid,
                                Role = entity.Role,
                            });

                            var dimKey = (log.ServerId, log.RoundId, entity.EntityUid);
                            if (existingDims.TryGetValue(dimKey, out var dim))
                            {
                                dim.PrototypeId = entity.PrototypeId ?? dim.PrototypeId;
                                dim.EntityName = entity.EntityName ?? dim.EntityName;
                            }
                            else
                            {
                                var newDim = new AdminLogEntityDimension
                                {
                                    ServerId = log.ServerId,
                                    RoundId = log.RoundId,
                                    EntityUid = entity.EntityUid,
                                    PrototypeId = entity.PrototypeId,
                                    EntityName = entity.EntityName,
                                };
                                db.DbContext.AdminLogEntityDimension.Add(newDim);
                                // Track newly added dimensions so subsequent logs
                                // in this batch can update them instead of adding duplicates.
                                existingDims[dimKey] = newDim;
                            }
                        }
                    }
                    await db.DbContext.SaveChangesAsync(cancel);
                    await tx.CommitAsync(cancel);
                    _opsLog.Debug($"Successfully saved {logs.Count} admin logs.");
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    attempt += 1;
                    _opsLog.Error($"Attempt {attempt} failed to save logs: {ex}");

                    if (attempt >= maxRetryAttempts)
                    {
                        throw new InvalidOperationException($"Max retry attempts reached. Failed to save {logs.Count} admin logs.", ex);
                    }

                    _opsLog.Warning($"Retrying in {retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(retryDelay, cancel);

                    retryDelay *= 2;
                }
            }
        }

        public async Task AddAuditLogs(List<AdminAuditEventWriteData> logs, CancellationToken cancel = default)
        {
            const int maxRetryAttempts = 5;
            var initialRetryDelay = TimeSpan.FromSeconds(5);

            DebugTools.Assert(logs.All(x => x.ServerId > 0), "Adding audit logs with invalid server ids.");

            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt < maxRetryAttempts)
            {
                try
                {
                    await using var db = await GetDb(cancel);
                    await using var tx = await db.DbContext.Database.BeginTransactionAsync(cancel);

                    var headers = new List<AdminAuditEvent>(logs.Count);
                    foreach (var log in logs)
                    {
                        var header = new AdminAuditEvent
                        {
                            ServerId = log.ServerId,
                            RoundId = log.RoundId,
                            AdminUserId = log.AdminUserId,
                            Action = log.Action,
                            Severity = log.Severity,
                            OccurredAt = log.OccurredAt,
                            Message = log.Message,
                            TargetPlayerUserId = log.TargetPlayerUserId,
                            TargetEntityUid = log.TargetEntityUid,
                            TargetEntityName = log.TargetEntityName,
                            TargetEntityPrototype = log.TargetEntityPrototype,
                        };

                        headers.Add(header);
                    }

                    db.DbContext.AdminAuditEvent.AddRange(headers);
                    await db.DbContext.SaveChangesAsync(cancel);

                    for (var i = 0; i < headers.Count; i++)
                    {
                        db.DbContext.AdminAuditEventPayload.Add(new AdminAuditEventPayload
                        {
                            EventId = headers[i].Id,
                            Json = logs[i].Json ?? JsonSerializer.SerializeToDocument(new { }),
                        });
                    }

                    await db.DbContext.SaveChangesAsync(cancel);
                    await tx.CommitAsync(cancel);
                    _opsLog.Debug($"Successfully saved {logs.Count} admin audit logs.");
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    attempt += 1;
                    _opsLog.Error($"Attempt {attempt} failed to save audit logs: {ex}");

                    if (attempt >= maxRetryAttempts)
                    {
                        throw new InvalidOperationException($"Max retry attempts reached. Failed to save {logs.Count} admin audit logs.", ex);
                    }

                    _opsLog.Warning($"Retrying in {retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(retryDelay, cancel);

                    retryDelay *= 2;
                }
            }
        }

        protected abstract IQueryable<AdminLogEvent> StartAdminLogsQuery(ServerDbContext db, LogFilter? filter = null);

        /// <summary>
        /// Applies search filtering to an audit log query. Override in provider-specific
        /// subclasses to use native search features (e.g. PostgreSQL full-text search).
        /// </summary>
        protected virtual IQueryable<AdminAuditEvent> ApplyAuditLogSearch(
            IQueryable<AdminAuditEvent> query,
            string search,
            LogSearchMode searchMode)
        {
            switch (searchMode)
            {
                case LogSearchMode.Regex when SupportsRegex && IsValidRegex(search):
                    // Provider has native regex support and the pattern is valid.
#pragma warning disable RA0026
                    return query.Where(log =>
                        Regex.IsMatch(log.Message, search, RegexOptions.IgnoreCase));
#pragma warning restore RA0026
                case LogSearchMode.Regex when !SupportsRegex && IsValidRegex(search):
                    // Provider has no native regex (e.g. SQLite). Skip the text filter
                    // entirely so other filters (round, server, action, severity) still
                    // narrow the result set. Client-side SearchModeHelper applies the
                    // real regex to the loaded results.
                    return query;
                case LogSearchMode.Regex:
                    // Invalid regex pattern. Return no results rather than running a
                    // misleading plain-text search. The client UI already shows a
                    // validation error (red tint + tooltip).
                    return query.Where(_ => false);
                case LogSearchMode.Wildcard:
                    return query.Where(log => EF.Functions.Like(log.Message, search));
                case LogSearchMode.Exact:
                {
                    var escaped = EscapeLikePattern(search);
                    return query.Where(log =>
                        EF.Functions.Like(log.Message, $"%{escaped}%", "\\"));
                }
                default: // Keyword — tokenize into words, require all present
                {
                    foreach (var word in search.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var escaped = EscapeLikePattern(word);
                        query = query.Where(log =>
                            EF.Functions.Like(log.Message, $"%{escaped}%", "\\"));
                    }

                    return query;
                }
            }
        }

        protected virtual bool SupportsRegex => false;

        /// <summary>
        /// Escapes SQL LIKE special characters so the input is treated as a literal substring.
        /// </summary>
        protected static string EscapeLikePattern(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        /// <summary>
        /// Validates that a string is a legal .NET regex. This is a best-effort check:
        /// PostgreSQL uses a different regex engine, so a pattern that passes here could
        /// still fail at query time.
        /// </summary>
        protected static bool IsValidRegex(string pattern)
        {
            try
            {
                _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private IQueryable<AdminLogEvent> GetAdminLogsQuery(ServerDbContext db, LogFilter? filter = null)
        {
            // Save me from SQLite
            var query = StartAdminLogsQuery(db, filter);

            if (filter?.ServerId != null)
            {
                query = query.Where(log => log.ServerId == filter.ServerId);
            }

            if (filter == null)
            {
                return query.OrderBy(log => log.OccurredAt);
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
                query = query.Where(log => log.OccurredAt < filter.Before);
            }

            if (filter.After != null)
            {
                query = query.Where(log => log.OccurredAt > filter.After);
            }

            var participants = db.AdminLogEventParticipant.AsQueryable();

            if (filter.ServerId != null)
                participants = participants.Where(p => p.ServerId == filter.ServerId);

            if (filter.Round != null)
                participants = participants.Where(p => p.RoundId == filter.Round);

            if (filter.Types != null)
                participants = participants.Where(p => filter.Types.Contains(p.Type));

            if (filter.Impacts != null)
                participants = participants.Where(p => filter.Impacts.Contains(p.Impact));

            if (filter.Before != null)
                participants = participants.Where(p => p.OccurredAt < filter.Before);

            if (filter.After != null)
                participants = participants.Where(p => p.OccurredAt > filter.After);

            var hasPlayerFiltering = filter.AnyPlayers != null
                || filter.AllPlayers != null
                || !filter.IncludePlayers
                || filter.IncludeNonPlayers;

            if (hasPlayerFiltering)
            {
                var playerParticipants = participants.Where(p => p.PlayerUserId != null);
                var anyPlayerEventIds = playerParticipants.Select(p => p.EventId).Distinct();

                IQueryable<int> matchedPlayerEventIds = anyPlayerEventIds;

                if (filter.AnyPlayers != null)
                {
                    matchedPlayerEventIds = playerParticipants
                        .Where(p => filter.AnyPlayers.Contains(p.PlayerUserId!.Value))
                        .Select(p => p.EventId)
                        .Distinct();
                }

                if (filter.AllPlayers != null)
                {
                    // Use a single GROUP BY / HAVING COUNT(DISTINCT) set-intersection instead of
                    // the old loop that composed N nested IN(subquery) clauses (one per player).
                    // The old approach produced O(N)-deep query plans that could time out with 3+
                    // players; this translates to a single semi-join the planner handles well.
                    //
                    // cap at 10 players to prevent degenerate HAVING expressions.
                    var allPlayers = filter.AllPlayers.Length > 10
                        ? filter.AllPlayers[..10]
                        : filter.AllPlayers;

                    var requiredCount = allPlayers.Length;
                    matchedPlayerEventIds = playerParticipants
                        .Where(p => allPlayers.Contains(p.PlayerUserId!.Value))
                        .GroupBy(p => p.EventId)
                        .Where(g => g.Select(p => p.PlayerUserId).Distinct().Count() >= requiredCount)
                        .Select(g => g.Key);
                }

                query = query.Where(log =>
                    (filter.IncludePlayers && matchedPlayerEventIds.Contains(log.Id))
                    || (filter.IncludeNonPlayers && !anyPlayerEventIds.Contains(log.Id)));
            }

            var hasEntityFiltering = filter.AnyEntities != null
                || filter.AllEntities != null
                || filter.EntityRoles != null;

            if (hasEntityFiltering)
            {
                var entityParticipants = participants.Where(p => p.EntityUid != null);
                IQueryable<int> matchedEntityEventIds = entityParticipants
                    .Select(p => p.EventId)
                    .Distinct();

                if (filter.AnyEntities != null)
                {
                    matchedEntityEventIds = entityParticipants
                        .Where(p => filter.AnyEntities.Contains(p.EntityUid!.Value))
                        .Select(p => p.EventId)
                        .Distinct();
                }

                if (filter.AllEntities != null)
                {
                    // Use a single GROUP BY / HAVING COUNT(DISTINCT) set-intersection instead
                    // of composing N nested semi-joins. The old approach
                    // produced query plans that degraded rapidly with 3+ entities;
                    // this translates to a single grouped scan the planner handles well.
                    //
                    // Deduplicate input and cap at 10 entities to bound query complexity.
                    var allEntities = filter.AllEntities.Distinct().Take(10).ToArray();
                    var requiredCount = allEntities.Length;

                    matchedEntityEventIds = entityParticipants
                        .Where(p => allEntities.Contains(p.EntityUid!.Value))
                        .GroupBy(p => p.EventId)
                        .Where(g => g.Select(p => p.EntityUid).Distinct().Count() >= requiredCount)
                        .Select(g => g.Key);
                }

                if (filter.EntityRoles != null)
                {
                    var previousEntityEventIds = matchedEntityEventIds;
                    matchedEntityEventIds = entityParticipants
                        .Where(p => filter.EntityRoles.Contains(p.Role))
                        .Select(p => p.EventId)
                        .Distinct()
                        .Where(eventId => previousEntityEventIds.Contains(eventId));
                }

                query = query.Where(log => matchedEntityEventIds.Contains(log.Id));
            }

            if (filter.LastLogId != null)
            {
                // When both LastOccurredAt and LastLogId are provided, use a compound
                // (OccurredAt, Id) cursor so Postgres can seek directly into the
                // (ServerId, OccurredAt, Id) composite index instead of scanning.
                if (filter.LastOccurredAt != null)
                {
                    var cursorTime = filter.LastOccurredAt.Value;
                    var cursorId = filter.LastLogId.Value;

                    query = filter.DateOrder switch
                    {
                        DateOrder.Ascending => query.Where(log =>
                            log.OccurredAt > cursorTime ||
                            (log.OccurredAt == cursorTime && log.Id > cursorId)),
                        DateOrder.Descending => query.Where(log =>
                            log.OccurredAt < cursorTime ||
                            (log.OccurredAt == cursorTime && log.Id < cursorId)),
                        _ => throw new ArgumentOutOfRangeException(nameof(filter),
                            $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
                    };
                }
                else
                {
                    // Fallback: Id-only cursor for backward compatibility.
                    query = filter.DateOrder switch
                    {
                        DateOrder.Ascending => query.Where(log => log.Id > filter.LastLogId),
                        DateOrder.Descending => query.Where(log => log.Id < filter.LastLogId),
                        _ => throw new ArgumentOutOfRangeException(nameof(filter),
                            $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
                    };
                }
            }

            query = filter.DateOrder switch
            {
                DateOrder.Ascending => query.OrderBy(log => log.OccurredAt).ThenBy(log => log.Id),
                DateOrder.Descending => query.OrderByDescending(log => log.OccurredAt).ThenByDescending(log => log.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(filter), $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
            };

            const int hardLogLimit = 500_000;
            query = query.Take(Math.Min(filter.Limit ?? hardLogLimit, hardLogLimit));

            return query;
        }

        public async IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null)
        {
            var ct = filter?.CancellationToken ?? default;
            await using var db = await GetDb(ct);
            var query = GetAdminLogsQuery(db.DbContext, filter);

            await foreach (var message in query
                               .AsNoTracking()
                               .Select(log => log.Payload.Message)
                               .AsAsyncEnumerable()
                               .WithCancellation(ct))
            {
                yield return message;
            }
        }

        public async IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null)
        {
            var ct = filter?.CancellationToken ?? default;
            await using var db = await GetDb(ct);
            var query = GetAdminLogsQuery(db.DbContext, filter).AsNoTracking();

            var logs = await query
                .Select(log => new
                {
                    log.Id,
                    log.ServerId,
                    log.RoundId,
                    log.Type,
                    log.Impact,
                    log.OccurredAt,
                    Message = log.Payload.Message,
                    Json = log.Payload.Json
                })
                .ToListAsync(ct);

            var logIds = logs.Select(log => log.Id).ToArray();
            var participants = logIds.Length == 0
                ? []
                : await db.DbContext.AdminLogEventParticipant
                    .AsNoTracking()
                    .Where(p => logIds.Contains(p.EventId))
                    .Select(p => new
                    {
                        p.EventId,
                        p.PlayerUserId,
                        p.EntityUid,
                        p.Role
                    })
                    .ToListAsync(ct);

            var participantsByEvent = participants
                .GroupBy(p => p.EventId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            var roundIds = logs.Select(log => log.RoundId).Distinct().ToArray();
            var entityUids = participants
                .Where(p => p.EntityUid != null)
                .Select(p => p.EntityUid!.Value)
                .Distinct()
                .ToArray();

            var serverIds = logs.Select(log => log.ServerId).Distinct().ToArray();

            var dimensions = entityUids.Length == 0 || roundIds.Length == 0 || serverIds.Length == 0
                ? new Dictionary<(int ServerId, int RoundId, int EntityUid), AdminLogEntityDimension>()
                : await db.DbContext.AdminLogEntityDimension
                    .Where(dim => serverIds.Contains(dim.ServerId) && roundIds.Contains(dim.RoundId) && entityUids.Contains(dim.EntityUid))
                    .ToDictionaryAsync(dim => (dim.ServerId, dim.RoundId, dim.EntityUid), ct);

            var servers = await db.DbContext.Server
                .Where(server => serverIds.Contains(server.Id))
                .ToDictionaryAsync(server => server.Id, server => server.Name, ct);

            foreach (var log in logs)
            {
                var logParticipants = participantsByEvent.GetValueOrDefault(log.Id, []);
                var players = logParticipants.Where(p => p.PlayerUserId != null).Select(p => p.PlayerUserId!.Value).Distinct().ToArray();
                var entityRows = logParticipants.Where(p => p.EntityUid != null).ToArray();
                var entities = new SharedAdminLogEntity[entityRows.Length];
                var serverName = servers.GetValueOrDefault(log.ServerId, "unknown");

                for (var i = 0; i < entityRows.Length; i++)
                {
                    var row = entityRows[i];
                    dimensions.TryGetValue((log.ServerId, log.RoundId, row.EntityUid!.Value), out var dim);
                    entities[i] = new SharedAdminLogEntity(row.EntityUid!.Value, row.Role, dim?.PrototypeId, dim?.EntityName);
                }

                yield return new SharedAdminLog(log.Id, log.ServerId, serverName, log.Type, log.Impact, log.OccurredAt, log.Message, players, entities);
            }
        }

        public async IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null)
        {
            var ct = filter?.CancellationToken ?? default;
            await using var db = await GetDb(ct);
            var query = GetAdminLogsQuery(db.DbContext, filter);

            await foreach (var json in query
                               .AsNoTracking()
                               .Select(log => log.Payload.Json)
                               .AsAsyncEnumerable()
                               .WithCancellation(ct))
            {
                yield return json;
            }
        }

        public async Task<int> CountAdminLogs(int round, int? serverId = null, CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            var query = db.DbContext.AdminLogEvent.Where(log => log.RoundId == round);
            if (serverId != null)
                query = query.Where(log => log.ServerId == serverId);

            return await query.CountAsync(cancel);
        }

        private IQueryable<AdminAuditEvent> GetAuditLogsQuery(
            ServerDbContext db,
            AuditLogFilter filter,
            bool includePagination = true)
        {
            var query = db.AdminAuditEvent.AsQueryable();

            if (filter.ServerId != null)
            {
                query = query.Where(log => log.ServerId == filter.ServerId);
            }

            if (filter.Round != null)
            {
                query = query.Where(log => log.RoundId == filter.Round);
            }

            if (filter.Actions != null)
            {
                query = query.Where(log => filter.Actions.Contains(log.Action));
            }

            if (filter.Severities != null)
            {
                query = query.Where(log => filter.Severities.Contains(log.Severity));
            }

            if (filter.AdminUserId != null)
            {
                query = query.Where(log => log.AdminUserId == filter.AdminUserId);
            }

            if (filter.TargetPlayerUserId != null)
            {
                query = query.Where(log => log.TargetPlayerUserId == filter.TargetPlayerUserId);
            }

            if (filter.Before != null)
            {
                query = query.Where(log => log.OccurredAt < filter.Before);
            }

            if (filter.After != null)
            {
                query = query.Where(log => log.OccurredAt > filter.After);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = ApplyAuditLogSearch(query, filter.Search, filter.SearchMode);
            }

            if (!includePagination)
                return query;

            if (filter.LastLogId != null)
            {
                if (filter.LastOccurredAt != null)
                {
                    var cursorTime = filter.LastOccurredAt.Value;
                    var cursorId = filter.LastLogId.Value;

                    query = filter.DateOrder switch
                    {
                        DateOrder.Ascending => query.Where(log =>
                            log.OccurredAt > cursorTime ||
                            (log.OccurredAt == cursorTime && log.Id > cursorId)),
                        DateOrder.Descending => query.Where(log =>
                            log.OccurredAt < cursorTime ||
                            (log.OccurredAt == cursorTime && log.Id < cursorId)),
                        _ => throw new ArgumentOutOfRangeException(nameof(filter),
                            $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
                    };
                }
                else
                {
                    query = filter.DateOrder switch
                    {
                        DateOrder.Ascending => query.Where(log => log.Id > filter.LastLogId),
                        DateOrder.Descending => query.Where(log => log.Id < filter.LastLogId),
                        _ => throw new ArgumentOutOfRangeException(nameof(filter),
                            $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
                    };
                }
            }

            query = filter.DateOrder switch
            {
                DateOrder.Ascending => query.OrderBy(log => log.OccurredAt).ThenBy(log => log.Id),
                DateOrder.Descending => query.OrderByDescending(log => log.OccurredAt).ThenByDescending(log => log.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(filter), $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
            };

            const int hardLogLimit = 500_000;
            query = query.Take(Math.Min(filter.Limit ?? hardLogLimit, hardLogLimit));

            return query;
        }

        public async Task<List<SharedAdminAuditLog>> GetAuditLogs(AuditLogFilter filter)
        {
            var ct = filter.CancellationToken;
            await using var db = await GetDb(ct);

            var results = await GetAuditLogsQuery(db.DbContext, filter)
                .AsNoTracking()
                .Select(log => new SharedAdminAuditLog(
                    log.Id,
                    log.Action,
                    log.Severity,
                    log.OccurredAt,
                    log.AdminUserId,
                    string.Empty,
                    log.Message,
                    log.TargetPlayerUserId,
                    null,
                    log.TargetEntityUid,
                    log.TargetEntityName,
                    log.TargetEntityPrototype))
                .ToListAsync(ct);

            var userIds = new HashSet<Guid>();
            foreach (var log in results)
            {
                userIds.Add(log.AdminUserId);
                if (log.TargetPlayerUserId is { } targetPlayerUserId)
                    userIds.Add(targetPlayerUserId);
            }

            var nameMap = userIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await db.DbContext.Player
                    .AsNoTracking()
                    .Where(player => userIds.Contains(player.UserId))
                    .ToDictionaryAsync(player => player.UserId, player => player.LastSeenUserName, ct);

            for (var i = 0; i < results.Count; i++)
            {
                var log = results[i];
                results[i] = log with
                {
                    AdminUserName = nameMap.GetValueOrDefault(log.AdminUserId, log.AdminUserId.ToString()),
                    TargetPlayerUserName = log.TargetPlayerUserId is { } targetPlayerUserId
                        ? nameMap.GetValueOrDefault(targetPlayerUserId)
                        : null
                };
            }

            return results;
        }

        public async Task<int> CountAuditLogs(AuditLogFilter filter)
        {
            await using var db = await GetDb(filter.CancellationToken);
            return await GetAuditLogsQuery(db.DbContext, filter, includePagination: false)
                .CountAsync(filter.CancellationToken);
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
            await using var db = await GetDb();

            db.DbContext.Whitelist.Add(new Whitelist { UserId = player });
            await db.DbContext.SaveChangesAsync();
        }

        public async Task RemoveFromWhitelistAsync(NetUserId player)
        {
            await using var db = await GetDb();
            var entry = await db.DbContext.Whitelist.SingleAsync(w => w.UserId == player);
            db.DbContext.Whitelist.Remove(entry);
            await db.DbContext.SaveChangesAsync();
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
            await using var db = await GetDb();

            var dbPlayer = await db.DbContext.Player.Where(dbPlayer => dbPlayer.UserId == player).SingleOrDefaultAsync();
            if (dbPlayer == null)
            {
                return;
            }

            dbPlayer.LastReadRules = date?.UtcDateTime;
            await db.DbContext.SaveChangesAsync();
        }

        public async Task<bool> GetBlacklistStatusAsync(NetUserId player)
        {
            await using var db = await GetDb();

            return await db.DbContext.Blacklist.AnyAsync(w => w.UserId == player);
        }

        public async Task AddToBlacklistAsync(NetUserId player)
        {
            await using var db = await GetDb();

            db.DbContext.Blacklist.Add(new Blacklist() { UserId = player });
            await db.DbContext.SaveChangesAsync();
        }

        public async Task RemoveFromBlacklistAsync(NetUserId player)
        {
            await using var db = await GetDb();
            var entry = await db.DbContext.Blacklist.SingleAsync(w => w.UserId == player);
            db.DbContext.Blacklist.Remove(entry);
            await db.DbContext.SaveChangesAsync();
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
            await using var db = await GetDb();
            var exists = await db.DbContext.RoleWhitelists
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
            db.DbContext.RoleWhitelists.Add(whitelist);
            await db.DbContext.SaveChangesAsync();
            return true;
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
            await using var db = await GetDb();
            var entry = await db.DbContext.RoleWhitelists
                .Where(w => w.PlayerUserId == player)
                .Where(w => w.RoleId == job.Id)
                .SingleOrDefaultAsync();

            if (entry == null)
                return false;

            db.DbContext.RoleWhitelists.Remove(entry);
            await db.DbContext.SaveChangesAsync();
            return true;
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
