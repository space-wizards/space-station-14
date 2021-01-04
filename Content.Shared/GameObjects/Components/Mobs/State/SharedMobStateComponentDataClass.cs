using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public partial class SharedMobStateComponentDataClass
    {
        [CustomYamlField("states")]
        private SortedDictionary<int, IMobState> _lowestToHighestStates = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, IMobState>(),
                thresholds =>
                {
                    _lowestToHighestStates = new SortedDictionary<int, IMobState>(thresholds);
                },
                () => new Dictionary<int, IMobState>(_lowestToHighestStates));
        }
    }
}
