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
using Robust.Shared.Localization.Macros;
using Robust.Shared.Localization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
    /// </summary>
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile, IGenderable
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
            Gender gender,
            HumanoidCharacterAppearance appearance,
            ClothingPreference clothing,
            BackpackPreference backpack,
            Dictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            List<string> antagPreferences)
        {
            Name = name;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            Clothing = clothing;
            Backpack = backpack;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
            _antagPreferences = antagPreferences;
        }

        /// <summary>Copy constructor but with overridable references (to prevent useless copies)</summary>
        private HumanoidCharacterProfile(
            HumanoidCharacterProfile other,
            Dictionary<string, JobPriority> jobPriorities,
            List<string> antagPreferences)
            : this(other.Name, other.Age, other.Sex, other.Gender, other.Appearance, other.Clothing, other.Backpack,
                jobPriorities, other.PreferenceUnavailable, antagPreferences)
        {
        }

        /// <summary>Copy constructor</summary>
        private HumanoidCharacterProfile(HumanoidCharacterProfile other)
            : this(other, new Dictionary<string, JobPriority>(other.JobPriorities), new List<string>(other.AntagPreferences))
        {
        }

        public HumanoidCharacterProfile(
            string name,
            int age,
            Sex sex,
            Gender gender,
            HumanoidCharacterAppearance appearance,
            ClothingPreference clothing,
            BackpackPreference backpack,
            IReadOnlyDictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            IReadOnlyList<string> antagPreferences)
            : this(name, age, sex, gender, appearance, clothing, backpack, new Dictionary<string, JobPriority>(jobPriorities),
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
            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;

            var firstName = random.Pick(sex == Sex.Male
                ? Names.MaleFirstNames
                : Names.FemaleFirstNames);
            var lastName = random.Pick(Names.LastNames);
            var name = $"{firstName} {lastName}";
            var age = random.Next(MinimumAge, MaximumAge);

            return new HumanoidCharacterProfile(name, age, sex, gender, HumanoidCharacterAppearance.Random(sex), ClothingPreference.Jumpsuit, BackpackPreference.Backpack,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.OverflowJob, JobPriority.High}
                }, PreferenceUnavailableMode.StayInLobby, new List<string>());
        }

        public string Name { get; private set; }
        public int Age { get; private set; }
        public Sex Sex { get; private set; }
        public Gender Gender { get; private set; }
        public ICharacterAppearance CharacterAppearance => Appearance;
        public HumanoidCharacterAppearance Appearance { get; private set; }
        public ClothingPreference Clothing { get; private set; }
        public BackpackPreference Backpack { get; private set; }
        public IReadOnlyDictionary<string, JobPriority> JobPriorities => _jobPriorities;
        public IReadOnlyList<string> AntagPreferences => _antagPreferences;
        public PreferenceUnavailableMode PreferenceUnavailable { get; private set; }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new(this) { Name = name };
        }

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new(this) { Age = age };
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new(this) { Sex = sex };
        }

        public HumanoidCharacterProfile WithGender(Gender gender)
        {
            return new(this) { Gender = gender };
        }

        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new(this) { Appearance = appearance };
        }

        public HumanoidCharacterProfile WithClothingPreference(ClothingPreference clothing)
        {
            return new(this) { Clothing = clothing };
        }
        public HumanoidCharacterProfile WithBackpackPreference(BackpackPreference backpack)
        {
            return new(this) { Backpack = backpack };
        }
        public HumanoidCharacterProfile WithJobPriorities(IEnumerable<KeyValuePair<string, JobPriority>> jobPriorities)
        {
            return new(this, new Dictionary<string, JobPriority>(jobPriorities), _antagPreferences);
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
            return new(this, dictionary, _antagPreferences);
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new(this) { PreferenceUnavailable = mode };
        }

        public HumanoidCharacterProfile WithAntagPreferences(IEnumerable<string> antagPreferences)
        {
            return new(this, _jobPriorities, new List<string>(antagPreferences));
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
            return new(this, _jobPriorities, list);
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
            var gender = profile.Gender switch
            {
                Gender.Epicene => Gender.Epicene,
                Gender.Female => Gender.Female,
                Gender.Male => Gender.Male,
                Gender.Neuter => Gender.Neuter,
                _ => Gender.Epicene // Invalid enum values.
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

            var clothing = profile.Clothing switch
            {
                ClothingPreference.Jumpsuit => ClothingPreference.Jumpsuit,
                ClothingPreference.Jumpskirt => ClothingPreference.Jumpskirt,
                _ => ClothingPreference.Jumpsuit // Invalid enum values.
            };

            var backpack = profile.Backpack switch
            {
                BackpackPreference.Backpack => BackpackPreference.Backpack,
                BackpackPreference.Satchel => BackpackPreference.Satchel,
                BackpackPreference.Duffelbag => BackpackPreference.Duffelbag,
                _ => BackpackPreference.Backpack // Invalid enum values.
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

            return new HumanoidCharacterProfile(name, age, sex, gender, appearance, clothing, backpack, priorities, prefsUnavailableMode, antags);
        }

        public string Summary =>
            Loc.GetString("{0}, {1} years old human. {2:Their} pronouns are {2:they}/{2:them}.", Name, Age, this);

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (maybeOther is not HumanoidCharacterProfile other) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (Gender != other.Gender) return false;
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (Clothing != other.Clothing) return false;
            if (Backpack != other.Backpack) return false;
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
                HashCode.Combine(
                    Name,
                    Age,
                    Sex,
                    Gender,
                    Appearance,
                    Clothing,
                    Backpack
                ),
                PreferenceUnavailable,
                _jobPriorities,
                _antagPreferences
                );
        }
    }
}
