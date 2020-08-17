using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasMixtureComponent : Component
    {
        public override string Name => "GasMixture";

        [ViewVariables] public GasMixture GasMixture { get; set; } = new GasMixture();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => GasMixture.Volume, "volume", 0f);
        }
    }
}
