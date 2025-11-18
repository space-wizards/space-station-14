using Robust.Shared.GameStates;

namespace Content.Shared.Containers
{
    /// <summary>
    /// Hostile entities contained in a container (or a container within this container) with this component will not
    /// attempt to attack the furthest up container and escape.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PacifyingContainerComponent : Component { }
}
