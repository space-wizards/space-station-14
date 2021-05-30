#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public class JobPrototype : IPrototype
    {
        private string _name = string.Empty;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [DataField("head")]
        public bool IsHead { get; private set; }

        /// <summary>
        ///     The total amount of people that can start with this job round-start.
        /// </summary>
        public int SpawnPositions => _spawnPositions ?? TotalPositions;

        [DataField("spawnPositions")]
        private int? _spawnPositions;

        /// <summary>
        ///     The total amount of positions available.
        /// </summary>
        [DataField("positions")]
        public int TotalPositions { get; private set; }

        [DataField("startingGear")]
        public string? StartingGear { get; private set; }

        [DataField("icon")] public string Icon { get; } = string.Empty;

        [DataField("special")]
        public JobSpecial? Special { get; private set; }

        [DataField("departments")]
        public IReadOnlyCollection<string> Departments { get; } = Array.Empty<string>();

        [DataField("access")]
        public IReadOnlyCollection<string> Access { get; } = Array.Empty<string>();
    }
}
