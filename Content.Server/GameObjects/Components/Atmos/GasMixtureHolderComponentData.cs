using Content.Server.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    public partial class GasMixtureHolderComponentData
    {
        [CustomYamlField("air")] public GasMixture Air;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Air, "air", new GasMixture());
        }
    }
}
