using Content.Server.Plants.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    [Friend(typeof(RandomPottedPlantSystem))]
    [ComponentProtoName("RandomPottedPlant")]
    public class RandomPottedPlantComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("selected")]
        public string? State;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("plastic")]
        public bool Plastic;
    }
}
