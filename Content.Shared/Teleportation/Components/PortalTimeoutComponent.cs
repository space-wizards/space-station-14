namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Attached to an entity after portal transit to mark that they should not immediately be portaled back
///     at the end destination.
/// </summary>
[RegisterComponent]
public sealed class PortalTimeoutComponent : Component
{
    /// <summary>
    ///     The portal that was entered. Null if coming from a hand teleporter, etc.
    /// </summary>
    public EntityUid? EnteredPortal = null;
}
