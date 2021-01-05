using System;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    public partial class DisposalUnitComponentData
    {
        [CustomYamlField("autoEngageTime")]
        public TimeSpan AutomaticEngageTime;

        [CustomYamlField("flushDelay")]
        public TimeSpan FlushDelay;

        [CustomYamlField("air")] public GasMixture Air;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Air, "air", new GasMixture(Atmospherics.CellVolume));
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
