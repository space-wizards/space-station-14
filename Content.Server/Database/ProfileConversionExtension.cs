namespace Content.Server.Database
{
    using Entity.Models;
    using Content.Shared.Preferences;
    using System.Linq;
    using Robust.Shared.Maths;
    using System.Collections.Generic;

    public static class ProfileConversionExtension
    {
        public static void ConvertProfile(this HumanoidCharacterProfile humanoidProfile, Profile target)
        {
            var appearance = (HumanoidCharacterAppearance) humanoidProfile.CharacterAppearance;

            target.CharacterName = humanoidProfile.Name;
            target.Age = humanoidProfile.Age;
            target.Sex = humanoidProfile.Sex.ToString();
            target.HairName = appearance.HairStyleName;
            target.HairColor = appearance.HairColor.ToHex();
            target.FacialHairName = appearance.FacialHairStyleName;
            target.FacialHairColor = appearance.FacialHairColor.ToHex();
            target.EyeColor = appearance.EyeColor.ToHex();
            target.SkinColor = appearance.SkinColor.ToHex();
            target.PreferenceUnavailable = humanoidProfile.PreferenceUnavailable;
            target.Jobs = humanoidProfile.JobPriorities
                .Where(j => j.Value != JobPriority.Never)
                .Select(j => new Job {JobName = j.Key, Priority = j.Value})
                .ToList();
            target.Antags = humanoidProfile.AntagPreferences
                .Select(a => new Antag {AntagName = a})
                .ToList();
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