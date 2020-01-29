using System;
using System.Linq;
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

        public Prefs GetPlayerPreferences(string username)
        {
            return _prefsCtx
                .Preferences
                .Include(p => p.HumanoidProfiles)
                .ThenInclude(h => h.Jobs)
                .SingleOrDefault(p => p.Username == username);
        }

        public void SaveSelectedCharacterIndex(string username, int slot)
        {
            var prefs = _prefsCtx.Preferences.SingleOrDefault(p => p.Username == username);
            if (prefs is null)
                _prefsCtx.Preferences.Add(new Prefs
                {
                    Username = username,
                    SelectedCharacterSlot = slot
                });
            else
                prefs.SelectedCharacterSlot = slot;
            _prefsCtx.SaveChanges();
        }

        public void SaveCharacterSlot(string username, HumanoidProfile newProfile)
        {
            var prefs = _prefsCtx
                .Preferences
                .Single(p => p.Username == username);
            var oldProfile = prefs
                .HumanoidProfiles
                .SingleOrDefault(h => h.Slot == newProfile.Slot);
            if (!(oldProfile is null)) prefs.HumanoidProfiles.Remove(oldProfile);
            prefs.HumanoidProfiles.Add(newProfile);
            _prefsCtx.SaveChanges();
        }

        public void DeleteCharacterSlot(string username, int slot)
        {
            var profile = _prefsCtx
                .Preferences
                .Single(p => p.Username == username)
                .HumanoidProfiles
                .RemoveAll(h => h.Slot == slot);
            _prefsCtx.SaveChanges();
        }
    }
}
