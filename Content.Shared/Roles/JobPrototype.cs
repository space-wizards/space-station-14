using System;
using System.Collections.Generic;
using Content.Server.Jobs;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Jobs
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public class JobPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        public string Name { get; private set; }

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

        public string StartingGear { get; private set; }

        public string Icon { get; private set; }

        public JobSpecial Special { get; private set; }

        public IReadOnlyCollection<string> Department { get; private set; }
        public IReadOnlyCollection<string> Access { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var srz = YamlObjectSerializer.NewReader(mapping);
            ID = srz.ReadDataField<string>("id");
            Name = Loc.GetString(srz.ReadDataField<string>("name"));
            StartingGear = srz.ReadDataField<string>("startingGear");
            Department = srz.ReadDataField<List<string>>("department");
            TotalPositions = srz.ReadDataField<int>("positions");

            srz.DataField(this, p => p.SpawnPositions, "spawnPositions", TotalPositions);
            srz.DataField(this, p => p.IsHead, "head", false);
            srz.DataField(this, p => p.Access, "access", Array.Empty<string>());
            srz.DataField(this, p => p.Icon, "icon", null);
            srz.DataField(this, p => p.Special, "special", null);
        }
    }
}
