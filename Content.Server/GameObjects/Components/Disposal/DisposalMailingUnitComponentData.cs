using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    public partial class DisposalMailingUnitComponentData
    {
        [CustomYamlField("autoEngageTime")]
        public TimeSpan AutomaticEngageTime;

        [CustomYamlField("flushDelay")]
        public TimeSpan FlushDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "automaticEngageTime",
                30,
                seconds => AutomaticEngageTime = TimeSpan.FromSeconds(seconds),
                () => (int) AutomaticEngageTime.TotalSeconds);

            serializer.DataReadWriteFunction(
                "flushDelay",
                3,
                seconds => FlushDelay = TimeSpan.FromSeconds(seconds),
                () => (int) FlushDelay.TotalSeconds);
        }
    }
}
