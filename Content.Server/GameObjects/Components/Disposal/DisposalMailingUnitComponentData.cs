using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    public partial class DisposalMailingUnitComponentData
    {
        [DataClassTarget("autoEngageTime")]
        public TimeSpan? AutomaticEngageTime;

        [DataClassTarget("flushDelay")]
        public TimeSpan? FlushDelay;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction(
                "automaticEngageTime",
                30,
                seconds => AutomaticEngageTime = seconds != null ? TimeSpan.FromSeconds((int)seconds) : null,
                () => (int?) AutomaticEngageTime?.TotalSeconds);

            serializer.DataReadWriteFunction(
                "flushDelay",
                3,
                seconds => FlushDelay = seconds != null ? TimeSpan.FromSeconds((int)seconds) : null,
                () => (int?) FlushDelay?.TotalSeconds);
        }
    }
}
