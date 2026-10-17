namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Marks a target while a teleport request and all of its effects are being processed.
/// Prevents nested teleport requests for the same target.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportingComponent : Component
{
}
