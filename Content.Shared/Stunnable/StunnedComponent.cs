using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedComponent : Component
{
    /// <summary>
    /// Is this stun visualized?
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool Visualized;
}
