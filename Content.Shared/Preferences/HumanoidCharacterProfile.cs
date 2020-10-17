#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Text;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        private readonly Dictionary<string, JobPriority> _jobPriorities;
        private readonly List<string> _antagPreferences;
        public static int MinimumAge = 18;
        public static int MaximumAge = 120;
        public static int MaxNameLength = 32;

        private HumanoidCharacterProfile(
            string name,
            int age,
            Sex sex,
            HumanoidCharacterAppearance appearance,
            Dictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            List<string> antagPreferences)
        {
            Name = name;
            Age = age;
            Sex = sex;
            Appearance = appearance;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
            _antagPreferences = antagPreferences;
        }

        public HumanoidCharacterProfile(
            string name,
            int age,
            Sex sex,
            HumanoidCharacterAppearance appearance,
            IReadOnlyDictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            IReadOnlyList<string> antagPreferences)
            : this(name, age, sex, appearance, new Dictionary<string, JobPriority>(jobPriorities),
                preferenceUnavailable, new List<string>(antagPreferences))
        {
        }

        public static HumanoidCharacterProfile Default()
        {
            return Random();
        }

        public static HumanoidCharacterProfile Random()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var sex = random.Prob(0.5f) ? Sex.Male : Sex.Female;

            var firstName = random.Pick(sex == Sex.Male
                ? Names.MaleFirstNames
                : Names.FemaleFirstNames);
            var lastName = random.Pick(Names.LastNames);
            var name = $"{firstName} {lastName}";
            var age = random.Next(MinimumAge, MaximumAge);

            return new HumanoidCharacterProfile(name, age, sex, HumanoidCharacterAppearance.Random(sex),
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.OverflowJob, JobPriority.High}
                }, PreferenceUnavailableMode.StayInLobby, new List<string>());
        }

        public string Name { get; }
        public int Age { get; }
        public Sex Sex { get; }
        public ICharacterAppearance CharacterAppearance => Appearance;
        public HumanoidCharacterAppearance Appearance { get; }
        public IReadOnlyDictionary<string, JobPriority> JobPriorities => _jobPriorities;
        public IReadOnlyList<string> AntagPreferences => _antagPreferences;
        public PreferenceUnavailableMode PreferenceUnavailable { get; }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new HumanoidCharacterProfile(name, Age, Sex, Appearance, _jobPriorities, PreferenceUnavailable, _antagPreferences);
        }

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new HumanoidCharacterProfile(Name, Math.Clamp(age, MinimumAge, MaximumAge), Sex, Appearance, _jobPriorities, PreferenceUnavailable, _antagPreferences);
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new HumanoidCharacterProfile(Name, Age, sex, Appearance, _jobPriorities, PreferenceUnavailable, _antagPreferences);
        }

        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new HumanoidCharacterProfile(Name, Age, Sex, appearance, _jobPriorities, PreferenceUnavailable, _antagPreferences);
        }

        public HumanoidCharacterProfile WithJobPriorities(IReadOnlyDictionary<string, JobPriority> jobPriorities)
        {
            return new HumanoidCharacterProfile(
                Name,
                Age,
                Sex,
                Appearance,
                new Dictionary<string, JobPriority>(jobPriorities),
                PreferenceUnavailable,
                _antagPreferences);
        }

        public HumanoidCharacterProfile WithJobPriority(string jobId, JobPriority priority)
        {
            var dictionary = new Dictionary<string, JobPriority>(_jobPriorities);
            if (priority == JobPriority.Never)
            {
                dictionary.Remove(jobId);
            }
            else
            {
                dictionary[jobId] = priority;
            }

            return new HumanoidCharacterProfile(Name, Age, Sex, Appearance, dictionary, PreferenceUnavailable, _antagPreferences);
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new HumanoidCharacterProfile(Name, Age, Sex, Appearance, _jobPriorities, mode, _antagPreferences);
        }

        public HumanoidCharacterProfile WithAntagPreferences(IReadOnlyList<string> antagPreferences)
        {
            return new HumanoidCharacterProfile(
                Name,
                Age,
                Sex,
                Appearance,
                _jobPriorities,
                PreferenceUnavailable,
                new List<string>(antagPreferences));
        }

        public HumanoidCharacterProfile WithAntagPreference(string antagId, bool pref)
        {
            var list = new List<string>(_antagPreferences);
            if(pref)
            {
                if(!list.Contains(antagId))
                {
                    list.Add(antagId);
                }
            }
            else
            {
                if(list.Contains(antagId))
                {
                    list.Remove(antagId);
                }
            }
            return new HumanoidCharacterProfile(Name, Age, Sex, Appearance, _jobPriorities, PreferenceUnavailable, list);
        }

        /// <summary>
        ///     Makes this profile valid so there's no bad data like negative ages.
        /// </summary>
        public static HumanoidCharacterProfile EnsureValid(
            HumanoidCharacterProfile profile,
            IPrototypeManager prototypeManager)
        {
            var age = Math.Clamp(profile.Age, MinimumAge, MaximumAge);
            var sex = profile.Sex switch
            {
                Sex.Male => Sex.Male,
                Sex.Female => Sex.Female,
                _ => Sex.Male // Invalid enum values.
            };

            string name;
            if (string.IsNullOrEmpty(profile.Name))
            {
                name = "John Doe";
            }
            else if (profile.Name.Length > MaxNameLength)
            {
                name = profile.Name[..MaxNameLength];
            }
            else
            {
                name = profile.Name;
            }

            // TODO: Avoid Z̨͇̙͉͎̭͔̼̿͋A͚̖̞̗̞͈̓̾̀ͩͩ̔L̟ͮ̈͝G̙O͍͎̗̺̺ͫ̀̽͊̓͝ͅ tier shenanigans.
            // And other stuff like RTL overrides and such.
            // Probably also emojis...

            name = name.Trim();

            var appearance = HumanoidCharacterAppearance.EnsureValid(profile.Appearance);

            var prefsUnavailableMode = profile.PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            var priorities = new Dictionary<string, JobPriority>(profile.JobPriorities
                .Where(p => prototypeManager.HasIndex<JobPrototype>(p.Key) && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            var antags = profile.AntagPreferences
                .Where(prototypeManager.HasIndex<AntagPrototype>)
                .ToList();

            return new HumanoidCharacterProfile(name, age, sex, appearance, priorities, prefsUnavailableMode, antags);
        }

        public string Summary =>
            $"{Name}, {Age} years old {Sex.ToString().ToLower()} human.";

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterProfile other)) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (!_jobPriorities.SequenceEqual(other._jobPriorities)) return false;
            if (!_antagPreferences.SequenceEqual(other._antagPreferences)) return false;
            return Appearance.MemberwiseEquals(other.Appearance);
        }

        public override bool Equals(object? obj)
        {
            return obj is HumanoidCharacterProfile other && MemberwiseEquals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                Age,
                Sex,
                PreferenceUnavailable,
                _jobPriorities,
                _antagPreferences,
                Appearance);
        }
    }
}
