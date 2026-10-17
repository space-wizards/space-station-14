using Content.Shared.Whitelist;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Requests teleportation when a target collides with a configured fixture.
/// </summary>
[RegisterComponent]
public sealed partial class CollisionTeleportTriggerComponent : Component
{
    /// <summary>
    /// Fixture that triggers teleportation. Any fixture can trigger it when not specified.
    /// </summary>
    [DataField]
    public string? TriggerFixtureId;

    /// <summary>
    /// Non-hard target fixtures that are allowed to trigger teleportation.
    /// </summary>
    [DataField]
    public HashSet<string> AllowedNonHardTargetFixtureIds = new();

    /// <summary>
    /// Entities allowed to trigger teleportation.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    /// Entities prevented from triggering teleportation.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;
}
