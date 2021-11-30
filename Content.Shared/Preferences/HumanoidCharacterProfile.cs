using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Content.Shared.CharacterAppearance;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
    /// </summary>
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        public const int MinimumAge = 18;
        public const int MaximumAge = 120;
        public const int MaxNameLength = 32;

        private readonly Dictionary<string, JobPriority> _jobPriorities;
        private readonly List<string> _antagPreferences;

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
            return new(
                "John Doe",
                MinimumAge,
                Sex.Male,
                Gender.Male,
                HumanoidCharacterAppearance.Default(),
                ClothingPreference.Jumpsuit,
                BackpackPreference.Backpack,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.FallbackOverflowJob, JobPriority.High}
                },
                PreferenceUnavailableMode.SpawnAsOverflow,
                new List<string>());
        }

        public static HumanoidCharacterProfile Random()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var sex = random.Prob(0.5f) ? Sex.Male : Sex.Female;
            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var firstName = random.Pick(sex.FirstNames(prototypeManager).Values);
            var lastName = random.Pick(prototypeManager.Index<DatasetPrototype>("names_last"));
            var name = $"{firstName} {lastName}";
            var age = random.Next(MinimumAge, MaximumAge);

            return new HumanoidCharacterProfile(name, age, sex, gender, HumanoidCharacterAppearance.Random(sex), ClothingPreference.Jumpsuit, BackpackPreference.Backpack,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.FallbackOverflowJob, JobPriority.High}
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

        public string Summary =>
            Loc.GetString(
                "humanoid-character-profile-summary",
                ("name", Name),
                ("gender", Gender.ToString().ToLowerInvariant()),
                ("age", Age)
            );

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

        public void EnsureValid()
        {
            var age = Math.Clamp(Age, MinimumAge, MaximumAge);

            var sex = Sex switch
            {
                Sex.Male => Sex.Male,
                Sex.Female => Sex.Female,
                _ => Sex.Male // Invalid enum values.
            };

            var gender = Gender switch
            {
                Gender.Epicene => Gender.Epicene,
                Gender.Female => Gender.Female,
                Gender.Male => Gender.Male,
                Gender.Neuter => Gender.Neuter,
                _ => Gender.Epicene // Invalid enum values.
            };

            string name;
            if (string.IsNullOrEmpty(Name))
            {
                name = RandomName();
            }
            else if (Name.Length > MaxNameLength)
            {
                name = Name[..MaxNameLength];
            }
            else
            {
                name = Name;
            }

            name = name.Trim();

            if (IoCManager.Resolve<IConfigurationManager>().GetCVar(CCVars.RestrictedNames))
            {
                name = Regex.Replace(name, @"[^A-Z,a-z,0-9, -]", string.Empty);
            }

            if (string.IsNullOrEmpty(name))
            {
                name = RandomName();
            }

            var appearance = HumanoidCharacterAppearance.EnsureValid(Appearance);

            var prefsUnavailableMode = PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            var clothing = Clothing switch
            {
                ClothingPreference.Jumpsuit => ClothingPreference.Jumpsuit,
                ClothingPreference.Jumpskirt => ClothingPreference.Jumpskirt,
                _ => ClothingPreference.Jumpsuit // Invalid enum values.
            };

            var backpack = Backpack switch
            {
                BackpackPreference.Backpack => BackpackPreference.Backpack,
                BackpackPreference.Satchel => BackpackPreference.Satchel,
                BackpackPreference.Duffelbag => BackpackPreference.Duffelbag,
                _ => BackpackPreference.Backpack // Invalid enum values.
            };

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var priorities = new Dictionary<string, JobPriority>(JobPriorities
                .Where(p => prototypeManager.HasIndex<JobPrototype>(p.Key) && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            var antags = AntagPreferences
                .Where(prototypeManager.HasIndex<AntagPrototype>)
                .ToList();

            Name = name;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            Clothing = clothing;
            Backpack = backpack;

            _jobPriorities.Clear();

            foreach (var (job, priority) in priorities)
            {
                _jobPriorities.Add(job, priority);
            }

            PreferenceUnavailable = prefsUnavailableMode;

            _antagPreferences.Clear();
            _antagPreferences.AddRange(antags);

            string RandomName()
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                var firstName = random.Pick(Sex.FirstNames(protoMan).Values);
                var lastName = random.Pick(protoMan.Index<DatasetPrototype>("names_last"));
                return $"{firstName} {lastName}";
            }
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
