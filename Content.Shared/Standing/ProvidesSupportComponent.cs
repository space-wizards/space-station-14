using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    /// <summary>
    /// Any entity with this component will provide standing support to its parent entity
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class ProvidesSupportComponent : Component
    {
    }
}
