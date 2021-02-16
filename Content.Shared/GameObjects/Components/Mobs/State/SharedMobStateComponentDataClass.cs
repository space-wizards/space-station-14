#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public partial class SharedMobStateComponentDataClass
    {
        [DataClassTarget("states")]
        private SortedDictionary<int, IMobState>? _lowestToHighestStates;

        public void ExposeData(ObjectSerializer serializer)
        {
            _lowestToHighestStates ??= new();
            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, IMobState>(),
                thresholds =>
                {
                    _lowestToHighestStates = new SortedDictionary<int, IMobState>(thresholds);
                },
                () => new Dictionary<int, IMobState>(_lowestToHighestStates));
            if (_lowestToHighestStates.Count == 0) _lowestToHighestStates = null;
        }
    }
}
