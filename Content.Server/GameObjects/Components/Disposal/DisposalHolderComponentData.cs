using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    public partial class DisposalHolderComponentData
    {
        [CustomYamlField("air")] public GasMixture Air;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Air, "air", new GasMixture(Atmospherics.CellVolume));
        }
    }
}
