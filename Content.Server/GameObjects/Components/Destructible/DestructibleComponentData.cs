using System.Collections.Generic;
using Content.Server.GameObjects.Components.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible
{
    public partial class DestructibleComponentData
    {
        [CustomYamlField("thresholds")]
        public SortedDictionary<int, Threshold> LowestToHighestThresholds = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, Threshold>(),
                thresholds => LowestToHighestThresholds = new SortedDictionary<int, Threshold>(thresholds),
                () => new Dictionary<int, Threshold>(LowestToHighestThresholds));
        }
    }
}
