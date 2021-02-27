#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.Localization;
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
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [DataField("IsHead")]
        public bool IsHead { get; private set; }

        /// <summary>
        ///     The total amount of people that can start with this job round-start.
        /// </summary>
        [DataField("spawnPositions")]
        public int SpawnPositions { get; private set; }

        /// <summary>
        ///     The total amount of positions available.
        /// </summary>
        [DataField("positions")]
        public int TotalPositions { get; private set; }

        [DataField("startingGear")]
        public string? StartingGear { get; private set; }

        [DataField("icon")]
        public string? Icon { get; private set; }

        [DataField("special")]
        public JobSpecial? Special { get; private set; }

        [DataField("departments")] public IReadOnlyCollection<string> Departments { get; private set; } = Array.Empty<string>();
        [DataField("access")]
        public IReadOnlyCollection<string> Access { get; private set; } = ImmutableArray<string>.Empty;
    }
}
