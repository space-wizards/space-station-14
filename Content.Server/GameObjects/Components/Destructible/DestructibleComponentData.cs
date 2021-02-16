#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible
{
    public partial class DestructibleComponentData
    {
        [DataClassTarget("thresholds")]
        public SortedDictionary<int, Threshold>? LowestToHighestThresholds;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, Threshold>(),
                thresholds => LowestToHighestThresholds = thresholds != null ? new SortedDictionary<int, Threshold>(thresholds) : null,
                () => LowestToHighestThresholds == null ? null : new Dictionary<int, Threshold>(LowestToHighestThresholds));
            if (LowestToHighestThresholds?.Count == 0) LowestToHighestThresholds = null;
        }
    }
}
