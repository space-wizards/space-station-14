#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public class JobPrototype : IPrototype, IIndexedPrototype
    {
        private string _name = string.Empty;

        [YamlField("id")]
        public string ID { get; private set; } = string.Empty;

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [YamlField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [YamlField("IsHead")]
        public bool IsHead { get; private set; }

        /// <summary>
        ///     The total amount of people that can start with this job round-start.
        /// </summary>
        [YamlField("spawnPositions")]
        public int SpawnPositions { get; private set; }

        /// <summary>
        ///     The total amount of positions available.
        /// </summary>
        [YamlField("positions")]
        public int TotalPositions { get; private set; }

        [YamlField("startingGear")]
        public string? StartingGear { get; private set; }

        [YamlField("icon")]
        public string? Icon { get; private set; }

        [YamlField("special")]
        public JobSpecial? Special { get; private set; }

        [YamlField("departments")] public IReadOnlyCollection<string> Departments { get; private set; } = Array.Empty<string>();
        [YamlField("access")]
        public IReadOnlyCollection<string> Access { get; private set; } = ImmutableArray<string>.Empty;
    }
}
