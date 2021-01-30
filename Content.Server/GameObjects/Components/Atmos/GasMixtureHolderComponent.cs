using Content.Server.Atmos;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasMixtureHolder";

        [ViewVariables] [YamlField("air")] public GasMixture Air { get; set; } = new GasMixture();
    }
}
