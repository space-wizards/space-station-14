using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters, int selectedCharacterIndex, Color adminOOCColor, Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            JobPriorities = jobPriorities;
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];
        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; set; }

        public Color AdminOOCColor { get; set; }

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }

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

        public Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJob(ProtoId<JobPrototype> job)
        {
            var result = new Dictionary<int, HumanoidCharacterProfile>();
            foreach (var (slot, profile) in Characters)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                if (humanoid.JobPreferences.Contains(job))
                    result.Add(slot, humanoid);
            }

            return result;
        }

        public bool HasAntagPreference(ICollection<ProtoId<AntagPrototype>> antagList)
        {
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                foreach (var antag in antagList)
                {
                    if (humanoid.AntagPreferences.Contains(antag))
                        return true;
                }
            }

            return false;
        }

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
