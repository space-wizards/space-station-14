namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Attached to an entity after portal transit to signal that they should not immediately be portaled back
///     at the end destination.
/// </summary>
[RegisterComponent]
public sealed class PortalTimeoutComponent : Component
{

}
