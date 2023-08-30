using Robust.Shared.GameStates;

namespace Content.Shared.Strip.Components
{
    /// <summary>
    ///     Give to an entity to say they can strip another entity.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StrippingComponent : Component {}
}
