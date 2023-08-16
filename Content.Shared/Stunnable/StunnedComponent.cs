using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed class StunnedComponent : Component
{
}
