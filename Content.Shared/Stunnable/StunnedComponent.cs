using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable
{
    [Friend(typeof(SharedStunSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StunnedComponent : Component
    {
    }
}
