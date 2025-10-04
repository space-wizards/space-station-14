using Robust.Shared.GameStates;

namespace Content.Shared.Conduit.Holder;

/// <summary>
/// A component added to entities that being held by a <see cref="ConduitHolderComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConduitHeldComponent : Component
{
    /// <summary>
    /// The entity holding the owner of this component.
    /// </summary>
    [ViewVariables]
    public EntityUid Holder;
}
