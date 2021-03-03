#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public class JobPrototype : IPrototype
    {
        public string ID { get; private set; } = string.Empty;

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        public bool IsHead { get; private set; }

        /// <summary>
        ///     The total amount of people that can start with this job round-start.
        /// </summary>
        public int SpawnPositions { get; private set; }

        /// <summary>
        ///     The total amount of positions available.
        /// </summary>
        public int TotalPositions { get; private set; }

        public string StartingGear { get; private set; } = string.Empty;

        public string Icon { get; private set; } = string.Empty;

        public JobSpecial? Special { get; private set; } = null;

        public IReadOnlyCollection<string> Departments { get; private set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Access { get; private set; } = Array.Empty<string>();

        public void LoadFrom(YamlMappingNode mapping)
        {
            var srz = YamlObjectSerializer.NewReader(mapping);
            ID = srz.ReadDataField<string>("id");
            Name = Loc.GetString(srz.ReadDataField<string>("name"));
            StartingGear = srz.ReadDataField<string>("startingGear");
            Departments = srz.ReadDataField<List<string>>("departments");
            TotalPositions = srz.ReadDataField<int>("positions");

            srz.DataField(this, p => p.SpawnPositions, "spawnPositions", TotalPositions);
            srz.DataField(this, p => p.IsHead, "head", false);
            srz.DataField(this, p => p.Access, "access", Array.Empty<string>());
            srz.DataField(this, p => p.Icon, "icon", string.Empty);
            srz.DataField(this, p => p.Special, "special", null);
        }
    }
}
