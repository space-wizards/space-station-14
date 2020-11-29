namespace Content.Server.Database
{
    using Entity.Models;
    using Content.Shared.Preferences;
    using System.Linq;
    using Robust.Shared.Maths;
    using System.Collections.Generic;

    public static class ProfileConversionExtension
    {
        public static Profile ConvertProfile(this HumanoidCharacterProfile humanoidProfile, int slot)
        {
            var appearance = (HumanoidCharacterAppearance) humanoidProfile.CharacterAppearance;

            var entity = new Profile
            {
                CharacterName = humanoidProfile.Name,
                Age = humanoidProfile.Age,
                Sex = humanoidProfile.Sex.ToString(),
                HairName = appearance.HairStyleName,
                HairColor = appearance.HairColor.ToHex(),
                FacialHairName = appearance.FacialHairStyleName,
                FacialHairColor = appearance.FacialHairColor.ToHex(),
                EyeColor = appearance.EyeColor.ToHex(),
                SkinColor = appearance.SkinColor.ToHex(),
                Slot = slot,
                PreferenceUnavailable = humanoidProfile.PreferenceUnavailable,
                Jobs = humanoidProfile.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = j.Value})
                    .ToList(),
                Antags = humanoidProfile.AntagPreferences
                    .Select(a => new Antag {AntagName = a})
                    .ToList(),
            };

            return entity;
        }

        public static HumanoidCharacterProfile ConvertProfile(this Profile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => j.Priority);
            var antags = profile.Antags.Select(a => a.AntagName);
            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.Age,
                // TODO: do something about this
                profile.Sex == "Male" ? Sex.Male : Sex.Female,
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
                profile.PreferenceUnavailable,
                antags.ToList()
            );
        }
    }
}