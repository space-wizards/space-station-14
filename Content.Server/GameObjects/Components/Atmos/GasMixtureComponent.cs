using System.Threading;
using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasMixtureComponent : Component
    {
        public override string Name => "GasMixture";
        public GasMixture GasMixture { get; set; } = new GasMixture();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => GasMixture.Volume, "volume", 0f);
        }
    }
}
