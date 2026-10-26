namespace Content.Server.Teleportation;

/// <summary>
/// Marks a portal created by a hand teleporter.
/// </summary>
[RegisterComponent]
public sealed partial class HandTeleporterPortalComponent : Component
{
    /// <summary>
    /// The creating device, used to clear its portal references when it still exists.
    /// </summary>
    [DataField]
    public EntityUid? Teleporter;
}
