using Content.Server.Plants.Systems;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]

    public sealed class RandomPottedPlantComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("selected")]
        public string? State;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("plastic")]
        public bool Plastic;
    }
}
