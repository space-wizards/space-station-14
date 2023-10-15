using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(FlammableSystem))]
public sealed partial class IgniteOnCollideComponent : Component
{
    /// <summary>
    /// How many more times the ignition can be applied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("count"), AutoNetworkedField]
    public int Count = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("fireStacks"), AutoNetworkedField]
    public float FireStacks;
}
