using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
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

        public IReadOnlyCollection<string> Department { get; private set; }
        public IReadOnlyCollection<string> Access { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            Name = Loc.GetString(mapping.GetNode("name").ToString());
            StartingGear = mapping.GetNode("startingGear").ToString();
            Department = mapping.GetNode("department").AllNodes.Select(i => i.ToString()).ToList();
            TotalPositions = mapping.GetNode("positions").AsInt();

            if (mapping.TryGetNode("spawnPositions", out var positionsNode))
            {
                SpawnPositions = positionsNode.AsInt();
            }
            else
            {
                SpawnPositions = TotalPositions;
            }

            if (mapping.TryGetNode("head", out var headNode))
            {
                IsHead = headNode.AsBool();
            }

            if (mapping.TryGetNode("access", out YamlSequenceNode accessNode))
            {
                Access = accessNode.Select(i => i.ToString()).ToList();
            }
            else
            {
                Access = Array.Empty<string>();
            }

            if (mapping.TryGetNode("icon", out var iconNode))
            {
                Icon = iconNode.AsString();
            }
        }
    }
}
