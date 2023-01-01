using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Attached to an entity after portal transit to mark that they should not immediately be portaled back
///     at the end destination.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PortalTimeoutComponent : Component
{
    /// <summary>
    ///     The portal that was entered. Null if coming from a hand teleporter, etc.
    /// </summary>
    [ViewVariables, DataField("enteredPortal")]
    public EntityUid? EnteredPortal = null;
}

[Serializable, NetSerializable]
public sealed class PortalTimeoutComponentState : ComponentState
{
    public EntityUid? EnteredPortal;

    public PortalTimeoutComponentState(EntityUid? enteredPortal)
    {
        EnteredPortal = enteredPortal;
    }
}
