using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CharacterAppearance;
using Content.Shared.Preferences;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public abstract class ServerDbBase
    {
        #region Preferences
        public async Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles).ThenInclude(h => h.Jobs)
                .Include(p => p.Profiles).ThenInclude(h => h.Antags)
                .AsSingleQuery()
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId);

            if (prefs is null) return null;

            var maxSlot = prefs.Profiles.Max(p => p.Slot) + 1;
            var profiles = new Dictionary<int, ICharacterProfile>(maxSlot);
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            return new PlayerPreferences(profiles, prefs.SelectedCharacterSlot, Color.FromHex(prefs.AdminOOCColor));
        }

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            await using var db = await GetDb();

            await SetSelectedCharacterSlotAsync(userId, index, db.DbContext);

            await db.DbContext.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            await using var db = await GetDb();

            if (profile is null)
            {
                await DeleteCharacterSlot(db.DbContext, userId, slot);
                await db.DbContext.SaveChangesAsync();
                return;
            }

            if (profile is not HumanoidCharacterProfile humanoid)
            {
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();
            }

            var entity = ConvertProfiles(humanoid, slot);

            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles)
                .SingleAsync(p => p.UserId == userId.UserId);

            var oldProfile = prefs
                .Profiles
                .SingleOrDefault(h => h.Slot == entity.Slot);

            if (oldProfile is not null)
            {
                prefs.Profiles.Remove(oldProfile);
            }

            prefs.Profiles.Add(entity);
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

        public async Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            await using var db = await GetDb();

            var profile = ConvertProfiles((HumanoidCharacterProfile) defaultProfile, 0);
            var prefs = new Preference
            {
                UserId = userId.UserId,
                SelectedCharacterSlot = 0,
                AdminOOCColor = Color.Red.ToHex()
            };

            prefs.Profiles.Add(profile);

            db.DbContext.Preference.Add(prefs);

            await db.DbContext.SaveChangesAsync();

            return new PlayerPreferences(new[] {new KeyValuePair<int, ICharacterProfile>(0, defaultProfile)}, 0, Color.FromHex(prefs.AdminOOCColor));
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

        private static async Task SetSelectedCharacterSlotAsync(NetUserId userId, int newSlot, ServerDbContext db)
        {
            var prefs = await db.Preference.SingleAsync(p => p.UserId == userId.UserId);
            prefs.SelectedCharacterSlot = newSlot;
        }

        private static HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => a.AntagName);

            var sex = Sex.Male;
            if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
                sex = sexVal;

            var clothing = ClothingPreference.Jumpsuit;
            if (Enum.TryParse<ClothingPreference>(profile.Clothing, true, out var clothingVal))
                clothing = clothingVal;

            var backpack = BackpackPreference.Backpack;
            if (Enum.TryParse<BackpackPreference>(profile.Backpack, true, out var backpackVal))
                backpack = backpackVal;

            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
            if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
                gender = genderVal;

            return new HumanoidCharacterProfile(
                profile.CharacterName,
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
                    Color.FromHex(profile.SkinColor)
                ),
                clothing,
                backpack,
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToList()
            );
        }

        private static Profile ConvertProfiles(HumanoidCharacterProfile humanoid, int slot)
        {
            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;

            var entity = new Profile
            {
                CharacterName = humanoid.Name,
                Age = humanoid.Age,
                Sex = humanoid.Sex.ToString(),
                Gender = humanoid.Gender.ToString(),
                HairName = appearance.HairStyleId,
                HairColor = appearance.HairColor.ToHex(),
                FacialHairName = appearance.FacialHairStyleId,
                FacialHairColor = appearance.FacialHairColor.ToHex(),
                EyeColor = appearance.EyeColor.ToHex(),
                SkinColor = appearance.SkinColor.ToHex(),
                Clothing = humanoid.Clothing.ToString(),
                Backpack = humanoid.Backpack.ToString(),
                Slot = slot,
                PreferenceUnavailable = (DbPreferenceUnavailableMode) humanoid.PreferenceUnavailable
            };
            entity.Jobs.AddRange(
                humanoid.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = (DbJobPriority) j.Value})
            );
            entity.Antags.AddRange(
                humanoid.AntagPreferences
                    .Select(a => new Antag {AntagName = a})
            );

            return entity;
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
        public abstract Task<ServerBanDef?> GetServerBanAsync(int id);

        /// <summary>
        ///     Looks up an user's most recent received un-pardoned ban.
        ///     This will NOT return a pardoned ban.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The HWId of the user.</param>
        /// <returns>The user's latest received un-pardoned ban, or null if none exist.</returns>
        public abstract Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId);

        /// <summary>
        ///     Looks up an user's ban history.
        ///     This will return pardoned bans as well.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The HWId of the user.</param>
        /// <returns>The user's ban history.</returns>
        public abstract Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId);

        public abstract Task AddServerBanAsync(ServerBanDef serverBan);
        public abstract Task AddServerUnbanAsync(ServerUnbanDef serverUnban);
        #endregion

        #region Player Records
        /*
         * PLAYER RECORDS
         */
        public async Task UpdatePlayerRecord(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId)
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
            record.LastSeenHWId = hwId.ToArray();

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

        protected abstract PlayerRecord MakePlayerRecord(Player player);
        #endregion

        #region Connection Logs
        /*
         * CONNECTION LOG
         */
        public abstract Task AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId);
        #endregion

        #region Admin Ranks
        /*
         * ADMIN RANKS
         */
        public async Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb();

            return await db.DbContext.Admin
                .Include(p => p.Flags)
                .Include(p => p.AdminRank)
                .ThenInclude(p => p!.Flags)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
        }

        public abstract Task<((Admin, string? lastUserName)[] admins, AdminRank[])>
            GetAllAdminAndRanksAsync(CancellationToken cancel);

        public async Task<AdminRank?> GetAdminRankDataForAsync(int id, CancellationToken cancel = default)
        {
            await using var db = await GetDb();

            return await db.DbContext.AdminRank
                .Include(r => r.Flags)
                .SingleOrDefaultAsync(r => r.Id == id, cancel);
        }

        public async Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDb();

            var admin = await db.DbContext.Admin.SingleAsync(a => a.UserId == userId.UserId, cancel);
            db.DbContext.Admin.Remove(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminAsync(Admin admin, CancellationToken cancel)
        {
            await using var db = await GetDb();

            db.DbContext.Admin.Add(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminAsync(Admin admin, CancellationToken cancel)
        {
            await using var db = await GetDb();

            var existing = await db.DbContext.Admin.Include(a => a.Flags).SingleAsync(a => a.UserId == admin.UserId, cancel);
            existing.Flags = admin.Flags;
            existing.Title = admin.Title;
            existing.AdminRankId = admin.AdminRankId;

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task RemoveAdminRankAsync(int rankId, CancellationToken cancel)
        {
            await using var db = await GetDb();

            var admin = await db.DbContext.AdminRank.SingleAsync(a => a.Id == rankId, cancel);
            db.DbContext.AdminRank.Remove(admin);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel)
        {
            await using var db = await GetDb();

            db.DbContext.AdminRank.Add(rank);

            await db.DbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel)
        {
            await using var db = await GetDb();

            var existing = await db.DbContext.AdminRank
                .Include(r => r.Flags)
                .SingleAsync(a => a.Id == rank.Id, cancel);

            existing.Flags = rank.Flags;
            existing.Name = rank.Name;

            await db.DbContext.SaveChangesAsync(cancel);
        }
        #endregion

        protected abstract Task<DbGuard> GetDb();

        protected abstract class DbGuard : IAsyncDisposable
        {
            public abstract ServerDbContext DbContext { get; }

            public abstract ValueTask DisposeAsync();
        }
    }
}
