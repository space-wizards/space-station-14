using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
    /// </summary>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class HumanoidCharacterProfile : ICharacterProfile
    {
        public const int MaxNameLength = 32;
        public const int MaxDescLength = 512;

        private readonly Dictionary<string, JobPriority> _jobPriorities;
        private readonly List<string> _antagPreferences;
        private readonly List<string> _traitPreferences;

        public IReadOnlyDictionary<string, RoleLoadout> Loadouts => _loadouts;

        private Dictionary<string, RoleLoadout> _loadouts;

        // What in the lord is happening here.
        private HumanoidCharacterProfile(
            string name,
            string flavortext,
            string species,
            int age,
            Sex sex,
            Gender gender,
            HumanoidCharacterAppearance appearance,
            SpawnPriorityPreference spawnPriority,
            Dictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            List<string> antagPreferences,
            List<string> traitPreferences,
            Dictionary<string, RoleLoadout> loadouts)
        {
            Name = name;
            FlavorText = flavortext;
            Species = species;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            SpawnPriority = spawnPriority;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
            _antagPreferences = antagPreferences;
            _traitPreferences = traitPreferences;
            _loadouts = loadouts;
        }

        /// <summary>Copy constructor but with overridable references (to prevent useless copies)</summary>
        private HumanoidCharacterProfile(
            HumanoidCharacterProfile other,
            Dictionary<string, JobPriority> jobPriorities,
            List<string> antagPreferences,
            List<string> traitPreferences,
            Dictionary<string, RoleLoadout> loadouts)
            : this(other.Name, other.FlavorText, other.Species, other.Age, other.Sex, other.Gender, other.Appearance, other.SpawnPriority,
                jobPriorities, other.PreferenceUnavailable, antagPreferences, traitPreferences, loadouts)
        {
        }

        /// <summary>Copy constructor</summary>
        private HumanoidCharacterProfile(HumanoidCharacterProfile other)
            : this(other, new Dictionary<string, JobPriority>(other.JobPriorities), new List<string>(other.AntagPreferences), new List<string>(other.TraitPreferences), new Dictionary<string, RoleLoadout>(other.Loadouts))
        {
        }

        public HumanoidCharacterProfile(
            string name,
            string flavortext,
            string species,
            int age,
            Sex sex,
            Gender gender,
            HumanoidCharacterAppearance appearance,
            SpawnPriorityPreference spawnPriority,
            IReadOnlyDictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            IReadOnlyList<string> antagPreferences,
            IReadOnlyList<string> traitPreferences,
            Dictionary<string, RoleLoadout> loadouts)
            : this(name, flavortext, species, age, sex, gender, appearance, spawnPriority, new Dictionary<string, JobPriority>(jobPriorities),
                preferenceUnavailable, new List<string>(antagPreferences), new List<string>(traitPreferences), new Dictionary<string, RoleLoadout>(loadouts))
        {
        }

        /// <summary>
        ///     Get the default humanoid character profile, using internal constant values.
        ///     Defaults to <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/> for the species.
        /// </summary>
        /// <returns></returns>
        public HumanoidCharacterProfile() : this(
            "John Doe",
            "",
            SharedHumanoidAppearanceSystem.DefaultSpecies,
            18,
            Sex.Male,
            Gender.Male,
            new HumanoidCharacterAppearance(),
            SpawnPriorityPreference.None,
            new Dictionary<string, JobPriority>
            {
                {SharedGameTicker.FallbackOverflowJob, JobPriority.High}
            },
            PreferenceUnavailableMode.SpawnAsOverflow,
            new List<string>(),
            new List<string>(),
            new Dictionary<string, RoleLoadout>())
        {
        }

        /// <summary>
        ///     Return a default character profile, based on species.
        /// </summary>
        /// <param name="species">The species to use in this default profile. The default species is <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/>.</param>
        /// <returns>Humanoid character profile with default settings.</returns>
        public static HumanoidCharacterProfile DefaultWithSpecies(string species = SharedHumanoidAppearanceSystem.DefaultSpecies)
        {
            return new(
                "John Doe",
                "",
                species,
                18,
                Sex.Male,
                Gender.Male,
                HumanoidCharacterAppearance.DefaultWithSpecies(species),
                SpawnPriorityPreference.None,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.FallbackOverflowJob, JobPriority.High}
                },
                PreferenceUnavailableMode.SpawnAsOverflow,
                new List<string>(),
                new List<string>(),
                new Dictionary<string, RoleLoadout>());
        }

        // TODO: This should eventually not be a visual change only.
        public static HumanoidCharacterProfile Random(HashSet<string>? ignoredSpecies = null)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var species = random.Pick(prototypeManager
                .EnumeratePrototypes<SpeciesPrototype>()
                .Where(x => ignoredSpecies == null ? x.RoundStart : x.RoundStart && !ignoredSpecies.Contains(x.ID))
                .ToArray()
            ).ID;

            return RandomWithSpecies(species);
        }

        public static HumanoidCharacterProfile RandomWithSpecies(string species = SharedHumanoidAppearanceSystem.DefaultSpecies)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var sex = Sex.Unsexed;
            var age = 18;
            if (prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype))
            {
                sex = random.Pick(speciesPrototype.Sexes);
                age = random.Next(speciesPrototype.MinAge, speciesPrototype.OldAge); // people don't look and keep making 119 year old characters with zero rp, cap it at middle aged
            }

            var gender = Gender.Epicene;

            switch (sex)
            {
                case Sex.Male:
                    gender = Gender.Male;
                    break;
                case Sex.Female:
                    gender = Gender.Female;
                    break;
            }

            var name = GetName(species, gender);

            return new HumanoidCharacterProfile(name, "", species, age, sex, gender, HumanoidCharacterAppearance.Random(species, sex), SpawnPriorityPreference.None,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.FallbackOverflowJob, JobPriority.High},
                }, PreferenceUnavailableMode.StayInLobby, new List<string>(), new List<string>(), new Dictionary<string, RoleLoadout>());
        }

        public string Name { get; private set; }
        public string FlavorText { get; private set; }
        public string Species { get; private set; }

        [DataField("age")]
        public int Age { get; private set; }

        [DataField("sex")]
        public Sex Sex { get; private set; }

        [DataField("gender")]
        public Gender Gender { get; private set; }

        public ICharacterAppearance CharacterAppearance => Appearance;

        [DataField("appearance")]
        public HumanoidCharacterAppearance Appearance { get; private set; }
        public SpawnPriorityPreference SpawnPriority { get; private set; }
        public IReadOnlyDictionary<string, JobPriority> JobPriorities => _jobPriorities;
        public IReadOnlyList<string> AntagPreferences => _antagPreferences;
        public IReadOnlyList<string> TraitPreferences => _traitPreferences;
        public PreferenceUnavailableMode PreferenceUnavailable { get; private set; }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new(this) { Name = name };
        }

        public HumanoidCharacterProfile WithFlavorText(string flavorText)
        {
            return new(this) { FlavorText = flavorText };
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

        public HumanoidCharacterProfile WithSpecies(string species)
        {
            return new(this) { Species = species };
        }


        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new(this) { Appearance = appearance };
        }

        public HumanoidCharacterProfile WithSpawnPriorityPreference(SpawnPriorityPreference spawnPriority)
        {
            return new(this) { SpawnPriority = spawnPriority };
        }

        public HumanoidCharacterProfile WithJobPriorities(IEnumerable<KeyValuePair<string, JobPriority>> jobPriorities)
        {
            return new(this, new Dictionary<string, JobPriority>(jobPriorities), _antagPreferences, _traitPreferences, _loadouts);
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
            return new(this, dictionary, _antagPreferences, _traitPreferences, _loadouts);
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new(this) { PreferenceUnavailable = mode };
        }

        public HumanoidCharacterProfile WithAntagPreferences(IEnumerable<string> antagPreferences)
        {
            return new(this, _jobPriorities, new List<string>(antagPreferences), _traitPreferences, _loadouts);
        }

        public HumanoidCharacterProfile WithAntagPreference(string antagId, bool pref)
        {
            var list = new List<string>(_antagPreferences);
            if (pref)
            {
                if (!list.Contains(antagId))
                {
                    list.Add(antagId);
                }
            }
            else
            {
                if (list.Contains(antagId))
                {
                    list.Remove(antagId);
                }
            }

            return new(this, _jobPriorities, list, _traitPreferences, _loadouts);
        }

        public HumanoidCharacterProfile WithTraitPreference(string traitId, bool pref)
        {
            var list = new List<string>(_traitPreferences);

            // TODO: Maybe just refactor this to HashSet? Same with _antagPreferences
            if (pref)
            {
                if (!list.Contains(traitId))
                {
                    list.Add(traitId);
                }
            }
            else
            {
                if (list.Contains(traitId))
                {
                    list.Remove(traitId);
                }
            }
            return new(this, _jobPriorities, _antagPreferences, list, _loadouts);
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
            if (SpawnPriority != other.SpawnPriority) return false;
            if (!_jobPriorities.SequenceEqual(other._jobPriorities)) return false;
            if (!_antagPreferences.SequenceEqual(other._antagPreferences)) return false;
            if (!_traitPreferences.SequenceEqual(other._traitPreferences)) return false;
            if (!Loadouts.SequenceEqual(other.Loadouts)) return false;
            return Appearance.MemberwiseEquals(other.Appearance);
        }

        public void EnsureValid(ICommonSession session, IDependencyCollection collection)
        {
            var configManager = collection.Resolve<IConfigurationManager>();
            var prototypeManager = collection.Resolve<IPrototypeManager>();

            if (!prototypeManager.TryIndex<SpeciesPrototype>(Species, out var speciesPrototype) || speciesPrototype.RoundStart == false)
            {
                Species = SharedHumanoidAppearanceSystem.DefaultSpecies;
                speciesPrototype = prototypeManager.Index<SpeciesPrototype>(Species);
            }

            var sex = Sex switch
            {
                Sex.Male => Sex.Male,
                Sex.Female => Sex.Female,
                Sex.Unsexed => Sex.Unsexed,
                _ => Sex.Male // Invalid enum values.
            };

            // ensure the species can be that sex and their age fits the founds
            if (!speciesPrototype.Sexes.Contains(sex))
                sex = speciesPrototype.Sexes[0];

            var age = Math.Clamp(Age, speciesPrototype.MinAge, speciesPrototype.MaxAge);

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
                name = GetName(Species, gender);
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

            if (configManager.GetCVar(CCVars.RestrictedNames))
            {
                name = Regex.Replace(name, @"[^A-Z,a-z,0-9, -]", string.Empty);
            }

            if (configManager.GetCVar(CCVars.ICNameCase))
            {
                // This regex replaces the first character of the first and last words of the name with their uppercase version
                name = Regex.Replace(name,
                @"^(?<word>\w)|\b(?<word>\w)(?=\w*$)",
                m => m.Groups["word"].Value.ToUpper());
            }

            if (string.IsNullOrEmpty(name))
            {
                name = GetName(Species, gender);
            }

            string flavortext;
            if (FlavorText.Length > MaxDescLength)
            {
                flavortext = FormattedMessage.RemoveMarkup(FlavorText)[..MaxDescLength];
            }
            else
            {
                flavortext = FormattedMessage.RemoveMarkup(FlavorText);
            }

            var appearance = HumanoidCharacterAppearance.EnsureValid(Appearance, Species, Sex);

            var prefsUnavailableMode = PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            var spawnPriority = SpawnPriority switch
            {
                SpawnPriorityPreference.None => SpawnPriorityPreference.None,
                SpawnPriorityPreference.Arrivals => SpawnPriorityPreference.Arrivals,
                SpawnPriorityPreference.Cryosleep => SpawnPriorityPreference.Cryosleep,
                _ => SpawnPriorityPreference.None // Invalid enum values.
            };

            var priorities = new Dictionary<string, JobPriority>(JobPriorities
                .Where(p => prototypeManager.TryIndex<JobPrototype>(p.Key, out var job) && job.SetPreference && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            var antags = AntagPreferences
                .Where(id => prototypeManager.TryIndex<AntagPrototype>(id, out var antag) && antag.SetPreference)
                .ToList();

            var traits = TraitPreferences
                         .Where(prototypeManager.HasIndex<TraitPrototype>)
                         .ToList();

            Name = name;
            FlavorText = flavortext;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            SpawnPriority = spawnPriority;

            _jobPriorities.Clear();

            foreach (var (job, priority) in priorities)
            {
                _jobPriorities.Add(job, priority);
            }

            PreferenceUnavailable = prefsUnavailableMode;

            _antagPreferences.Clear();
            _antagPreferences.AddRange(antags);

            _traitPreferences.Clear();
            _traitPreferences.AddRange(traits);

            // Checks prototypes exist for all loadouts and dump / set to default if not.
            var toRemove = new ValueList<string>();

            foreach (var (roleName, loadouts) in _loadouts)
            {
                if (!prototypeManager.HasIndex<RoleLoadoutPrototype>(roleName))
                {
                    toRemove.Add(roleName);
                    continue;
                }

                loadouts.EnsureValid(session, collection);
            }

            foreach (var value in toRemove)
            {
                _loadouts.Remove(value);
            }
        }

        public ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection)
        {
            var profile = new HumanoidCharacterProfile(this);
            profile.EnsureValid(session, collection);
            return profile;
        }

        // sorry this is kind of weird and duplicated,
        /// working inside these non entity systems is a bit wack
        public static string GetName(string species, Gender gender)
        {
            var namingSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NamingSystem>();
            return namingSystem.GetName(species, gender);
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
                    Species,
                    Age,
                    Sex,
                    Gender,
                    Appearance
                ),
                SpawnPriority,
                PreferenceUnavailable,
                _jobPriorities,
                _antagPreferences,
                _traitPreferences,
                _loadouts
            );
        }

        public void SetLoadout(RoleLoadout loadout)
        {
            _loadouts[loadout.Role.Id] = loadout;
        }

        public HumanoidCharacterProfile WithLoadout(RoleLoadout loadout)
        {
            // Deep copies so we don't modify the DB profile.
            var copied = new Dictionary<string, RoleLoadout>();

            foreach (var proto in _loadouts)
            {
                if (proto.Key == loadout.Role)
                    continue;

                copied[proto.Key] = proto.Value.Clone();
            }

            copied[loadout.Role] = loadout.Clone();
            return new(this, _jobPriorities, _antagPreferences, _traitPreferences, copied);
        }

        public RoleLoadout GetLoadoutOrDefault(string id, IEntityManager entManager, IPrototypeManager protoManager)
        {
            if (!_loadouts.TryGetValue(id, out var loadout))
            {
                loadout = new RoleLoadout(id);
                loadout.SetDefault(protoManager, force: true);
            }

            loadout.SetDefault(protoManager);
            return loadout;
        }
    }
}
