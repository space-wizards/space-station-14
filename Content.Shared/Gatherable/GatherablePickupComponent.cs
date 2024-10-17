using Robust.Shared.GameStates;

namespace Content.Shared.Gatherable;

[RegisterComponent, NetworkedComponent, Access(typeof(GatherablePickupSystem))]
public sealed partial class GatherablePickupComponent : Component
{
    /// <summary>
    /// Whether something was gathered in this tick.
    /// Reset every tick to avoid multiple items spamming pickup sounds.
    /// </summary>
    public bool Gathered;
}
