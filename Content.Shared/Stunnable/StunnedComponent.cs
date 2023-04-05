using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable
{
    [Access(typeof(SharedStunSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StunnedComponent : Component
    {
    }
}
