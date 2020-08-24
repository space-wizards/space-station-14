using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Preferences;
using Robust.Shared.Maths;
using static Content.Shared.Preferences.Sex;

namespace Content.Server.Preferences
{
    /// <summary>
    ///     Provides methods to retrieve and update character preferences.
    ///     Don't use this directly, go through <see cref="ServerPreferencesManager" /> instead.
    /// </summary>
    public class PreferencesDatabase
    {
        private readonly int _maxCharacterSlots;
        private readonly PrefsDb _prefsDb;

        // We use a single DbContext for the entire DB connection, and EFCore doesn't allow concurrent access.
        // So we need this semaphore to prevent bugs.
        private readonly SemaphoreSlim _prefsSemaphore = new SemaphoreSlim(1, 1);

        public PreferencesDatabase(IDatabaseConfiguration dbConfig, int maxCharacterSlots)
        {
            _maxCharacterSlots = maxCharacterSlots;
            _prefsDb = new PrefsDb(dbConfig);
        }

        public async Task<PlayerPreferences> GetPlayerPreferencesAsync(string username)
        {
            await _prefsSemaphore.WaitAsync();
            try
            {
                var prefs = await _prefsDb.GetPlayerPreferences(username);
                if (prefs is null) return null;

                var profiles = new ICharacterProfile[_maxCharacterSlots];
                foreach (var profile in prefs.HumanoidProfiles)
                {
                    profiles[profile.Slot] = ConvertProfiles(profile);
                }

                return new PlayerPreferences
                (
                    profiles,
                    prefs.SelectedCharacterSlot
                );
            }
            finally
            {
                _prefsSemaphore.Release();
            }
        }

        public async Task SaveSelectedCharacterIndexAsync(string username, int index)
        {
            await _prefsSemaphore.WaitAsync();
            try
            {
                index = MathHelper.Clamp(index, 0, _maxCharacterSlots - 1);
                await _prefsDb.SaveSelectedCharacterIndex(username, index);
            }
            finally
            {
                _prefsSemaphore.Release();
            }
        }

        public async Task SaveCharacterSlotAsync(string username, ICharacterProfile profile, int slot)
        {
            if (slot < 0 || slot >= _maxCharacterSlots)
                return;

            await _prefsSemaphore.WaitAsync();
            try
            {
                if (profile is null)
                {
                    await DeleteCharacterSlotAsync(username, slot);
                    return;
                }

                if (!(profile is HumanoidCharacterProfile humanoid))
                    // TODO: Handle other ICharacterProfile implementations properly
                    throw new NotImplementedException();
                var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
                var entity = new HumanoidProfile
                {
                    SlotName = humanoid.Name,
                    CharacterName = humanoid.Name,
                    Age = humanoid.Age,
                    Sex = humanoid.Sex.ToString(),
                    HairName = appearance.HairStyleName,
                    HairColor = appearance.HairColor.ToHex(),
                    FacialHairName = appearance.FacialHairStyleName,
                    FacialHairColor = appearance.FacialHairColor.ToHex(),
                    EyeColor = appearance.EyeColor.ToHex(),
                    SkinColor = appearance.SkinColor.ToHex(),
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
                await _prefsDb.SaveCharacterSlotAsync(username, entity);
            }
            finally
            {
                _prefsSemaphore.Release();
            }
        }


        private async Task DeleteCharacterSlotAsync(string username, int slot)
        {
            await _prefsDb.DeleteCharacterSlotAsync(username, slot);
        }

        public async Task<IEnumerable<KeyValuePair<string, ICharacterProfile>>> GetSelectedProfilesForPlayersAsync(
            List<string> usernames)
        {
            await _prefsSemaphore.WaitAsync();
            try
            {
                var profiles = await _prefsDb.GetProfilesForPlayersAsync(usernames);
                return profiles.Select(
                    p => new KeyValuePair<string, ICharacterProfile>(p.Key, ConvertProfiles(p.Value)));
            }
            finally
            {
                _prefsSemaphore.Release();
            }
        }

        private static HumanoidCharacterProfile ConvertProfiles(HumanoidProfile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => a.AntagName);
            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.Age,
                profile.Sex == "Male" ? Male : Female,
                new HumanoidCharacterAppearance
                (
                    profile.HairName,
                    Color.FromHex(profile.HairColor),
                    profile.FacialHairName,
                    Color.FromHex(profile.FacialHairColor),
                    Color.FromHex(profile.EyeColor),
                    Color.FromHex(profile.SkinColor)
                ),
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToList()
            );
        }
    }
}
