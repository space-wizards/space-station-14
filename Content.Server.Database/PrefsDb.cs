using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public class PrefsDb
    {
        private readonly PreferencesDbContext _prefsCtx;

        public PrefsDb(IDatabaseConfiguration dbConfig)
        {
            _prefsCtx = dbConfig switch
            {
                SqliteConfiguration sqlite => (PreferencesDbContext) new SqlitePreferencesDbContext(
                    sqlite.Options),
                PostgresConfiguration postgres => new PostgresPreferencesDbContext(postgres.Options),
                _ => throw new NotImplementedException()
            };
            _prefsCtx.Database.Migrate();
        }

        public async Task<Prefs?> GetPlayerPreferences(Guid userId)
        {
            return await _prefsCtx
                .Preferences
                .Include(p => p.HumanoidProfiles).ThenInclude(h => h.Jobs)
                .Include(p => p.HumanoidProfiles).ThenInclude(h => h.Antags)
                .SingleOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task SaveSelectedCharacterIndex(Guid userId, int slot)
        {
            var prefs = _prefsCtx.Preferences.SingleOrDefault(p => p.UserId == userId);
            if (prefs is null)
                _prefsCtx.Preferences.Add(new Prefs
                {
                    UserId = userId,
                    SelectedCharacterSlot = slot
                });
            else
                prefs.SelectedCharacterSlot = slot;
            await _prefsCtx.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(Guid userId, HumanoidProfile newProfile)
        {
            var prefs = _prefsCtx
                .Preferences
                .Single(p => p.UserId == userId);
            var oldProfile = prefs
                .HumanoidProfiles
                .SingleOrDefault(h => h.Slot == newProfile.Slot);
            if (!(oldProfile is null)) prefs.HumanoidProfiles.Remove(oldProfile);
            prefs.HumanoidProfiles.Add(newProfile);
            await _prefsCtx.SaveChangesAsync();
        }

        public async Task DeleteCharacterSlotAsync(Guid userId, int slot)
        {
            var profile = _prefsCtx
                .Preferences
                .Single(p => p.UserId == userId)
                .HumanoidProfiles
                .RemoveAll(h => h.Slot == slot);
            await _prefsCtx.SaveChangesAsync();
        }

        public async Task<Dictionary<Guid, HumanoidProfile>> GetProfilesForPlayersAsync(List<Guid> userIds)
        {
            return await _prefsCtx.HumanoidProfile
                .Include(p => p.Jobs)
                .Include(a => a.Antags)
                .Join(_prefsCtx.Preferences,
                    profile => new {profile.Slot, profile.PrefsId},
                    prefs => new {Slot = prefs.SelectedCharacterSlot, prefs.PrefsId},
                    (profile, prefs) => new {prefs.UserId, profile})
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(arg => arg.UserId, arg => arg.profile);
        }
    }
}
