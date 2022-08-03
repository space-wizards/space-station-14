using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    /// <summary>
    /// If an entity with a <see cref="StandingStateComponent" /> also has this component
    /// it will fall down if it is not supported by any child entities
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class NeedsSupportComponent : Component
    {
    }
}
