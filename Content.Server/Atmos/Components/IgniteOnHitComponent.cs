using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

[RegisterComponent, Access(typeof(FlammableSystem))]
public sealed partial class IgniteOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("fireStacks")]
    public float FireStacks;
}
