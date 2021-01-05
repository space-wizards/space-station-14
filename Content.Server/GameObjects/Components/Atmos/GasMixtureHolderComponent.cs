using Content.Server.Atmos;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    [CustomDataClass(typeof(GasMixtureHolderComponentData))]
    public class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasMixtureHolder";

        [ViewVariables] [CustomYamlField("air")] public GasMixture Air { get; set; } = null!;
    }
}
