using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the player's selected job priorities
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            Color adminOOCColor,
            List<ProtoId<ConstructionPrototype>> constructionFavorites,
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            AdminOOCColor = adminOOCColor;
            ConstructionFavorites = constructionFavorites;
            JobPriorities = SanitizeJobPriorities(jobPriorities);
        }

        private static Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            return jobPriorities.Where(kvp => kvp.Value != JobPriority.Never).ToDictionary();
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; set; }

        public Color AdminOOCColor { get; set; }

        /// <summary>
        ///    List of favorite items in the construction menu.
        /// </summary>
        public List<ProtoId<ConstructionPrototype>> ConstructionFavorites { get; set; } = [];

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }

        /// <summary>
        /// Get job priorities, but filtered by the presence of enabled characters asking for that job
        /// </summary>
        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPrioritiesFiltered()
        {
            var allCharacterJobs = new HashSet<ProtoId<JobPrototype>>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                allCharacterJobs.UnionWith(humanoid.JobPreferences);
            }

            var filteredPlayerJobs = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            foreach (var (job, priority) in JobPriorities)
            {
                if (!allCharacterJobs.Contains(job))
                    continue;
                filteredPlayerJobs.Add(job, priority);
            }

            return filteredPlayerJobs;
        }

        /// <summary>
        /// Given a job, return a random enabled character asking for this job
        /// </summary>
        public HumanoidCharacterProfile? SelectProfileForJob(ProtoId<JobPrototype> job)
        {
            List<HumanoidCharacterProfile> pool = [];
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                if (!humanoid.JobPreferences.Contains(job))
                    continue;
                pool.Add(humanoid);
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            return pool.Count == 0 ? null : random.Pick(pool);
        }

        /// <summary>
        /// Get all enabled profiles asking for a job
        /// </summary>
        public Dictionary<int, HumanoidCharacterProfile> GetAllEnabledProfilesForJob(ProtoId<JobPrototype> job)
        {
            return GetAllProfilesForJobInternal(job, onlyEnabled: true);
        }

        /// <summary>
        /// Get all profiles asking for a job
        /// </summary>
        public Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJob(ProtoId<JobPrototype> job)
        {
            return GetAllProfilesForJobInternal(job, onlyEnabled: false);
        }

        private Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJobInternal(ProtoId<JobPrototype> job, bool onlyEnabled)
        {
            var result = new Dictionary<int, HumanoidCharacterProfile>();
            foreach (var (slot, profile) in Characters)
            {
                if (profile is not HumanoidCharacterProfile humanoid)
                    continue;
                if (onlyEnabled && !humanoid.Enabled)
                    continue;
                if (humanoid.JobPreferences.Contains(job))
                    result.Add(slot, humanoid);
            }

            return result;
        }


        /// <summary>
        /// Get any random enabled profile
        /// </summary>
        public HumanoidCharacterProfile? GetRandomEnabledProfile()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var pool = Characters.Values.Where(p => p is HumanoidCharacterProfile { Enabled: true }).ToList();
            return pool.Count == 0 ? null : random.Pick(pool) as HumanoidCharacterProfile;
        }

        /// <summary>
        /// Given an antag, return a random enabled character asking for this antag
        /// </summary>
        public HumanoidCharacterProfile? SelectProfileForAntag(ICollection<ProtoId<AntagPrototype>> antags)
        {
            var pool = new HashSet<HumanoidCharacterProfile>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                foreach (var antag in antags)
                {
                    if (humanoid.AntagPreferences.Contains(antag))
                        pool.Add(humanoid);
                }
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            return pool.Count == 0 ? null : random.Pick(pool);
        }

        /// <summary>
        /// Return true if the profile in the slot exists and is a HumanoidCharacterProfile
        /// </summary>
        public bool TryGetHumanoidInSlot(int slot, [NotNullWhen(true)] out HumanoidCharacterProfile? humanoid)
        {
            humanoid = null;
            if (!Characters.TryGetValue(slot, out var profile))
                return false;
            humanoid = profile as HumanoidCharacterProfile;
            return humanoid != null;
        }
    }
}
