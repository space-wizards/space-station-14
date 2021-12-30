using Content.Server.Plants.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    [Friend(typeof(RandomPottedPlantSystem))]
    public class RandomPottedPlantComponent : Component
    {
        public override string Name => "RandomPottedPlant";

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("selected")]
        public string? State;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("plastic")]
        public bool Plastic;
    }
}
