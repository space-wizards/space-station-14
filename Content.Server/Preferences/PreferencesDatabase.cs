using System;
using System.Linq;
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

        public PreferencesDatabase(string databaseFilePath, int maxCharacterSlots)
        {
            _maxCharacterSlots = maxCharacterSlots;
            _prefsDb = new PrefsDb(databaseFilePath);
        }

        public PlayerPreferences GetPlayerPreferences(string username)
        {
            var prefs = _prefsDb.GetPlayerPreferences(username);
            if (prefs is null) return null;

            var profiles = new ICharacterProfile[_maxCharacterSlots];
            foreach (var profile in prefs.HumanoidProfiles)
            {
                var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => (JobPriority) j.Priority);

                profiles[profile.Slot] = new HumanoidCharacterProfile(
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
                    jobs
                );
            }

            return new PlayerPreferences
            (
                profiles,
                prefs.SelectedCharacterSlot
            );
        }

        public void SaveSelectedCharacterIndex(string username, int index)
        {
            index = index.Clamp(0, _maxCharacterSlots - 1);
            _prefsDb.SaveSelectedCharacterIndex(username, index);
        }

        public void SaveCharacterSlot(string username, ICharacterProfile profile, int slot)
        {
            if (slot < 0 || slot >= _maxCharacterSlots)
                return;
            if (profile is null)
            {
                DeleteCharacterSlot(username, slot);
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
                Slot = slot
            };
            entity.Jobs.AddRange(
                humanoid.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = (DbJobPriority) j.Value})
            );
            _prefsDb.SaveCharacterSlot(username, entity);
        }

        private void DeleteCharacterSlot(string username, int slot)
        {
            _prefsDb.DeleteCharacterSlot(username, slot);
        }
    }
}
