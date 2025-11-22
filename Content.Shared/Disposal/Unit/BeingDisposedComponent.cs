using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Unit;

/// <summary>
///     A component added to entities that are currently in disposals.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeingDisposedComponent : Component
{
    [ViewVariables]
    public EntityUid Holder;
}
