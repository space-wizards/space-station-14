using Content.Server.Atmos;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasMixtureHolder";

        [ViewVariables] public GasMixture Air { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Air = new GasMixture();

            serializer.DataField(this, x => x.Air, "air", new GasMixture());
        }
    }
}
