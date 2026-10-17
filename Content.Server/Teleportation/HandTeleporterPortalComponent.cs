namespace Content.Server.Teleportation;

/// <summary>
/// Marks a hand teleporter portal that closes when its supporting tile is removed,
/// even if the device that created it no longer exists.
/// </summary>
[RegisterComponent]
public sealed partial class HandTeleporterPortalComponent : Component
{
    /// <summary>
    /// The creating device, used to clear its portal references when it still exists.
    /// </summary>
    [DataField]
    public EntityUid Teleporter;
}
