using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasMixtureHolderComponent : Component
    {
        public override string Name => "GasMixtureHolder";

        [ViewVariables] public GasMixture GasMixture { get; set; } = new GasMixture();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "volume",
                0f,
                vol => GasMixture.Volume = vol,
                () => GasMixture.Volume);
        }
    }
}
