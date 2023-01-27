using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable
{
    [Access(typeof(SharedStunSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StunnedComponent : Component
    {
    }
}
