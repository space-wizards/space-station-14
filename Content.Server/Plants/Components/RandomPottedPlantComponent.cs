using Content.Server.Plants.Systems;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    [Access(typeof(RandomPottedPlantSystem))]
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
