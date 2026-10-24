using Content.Shared.Whitelist;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Selects the fixtures and targets that can trigger teleportation on collision.
/// </summary>
[RegisterComponent]
public sealed partial class CollisionTeleportTriggerComponent : Component
{
    /// <summary>
    /// The trigger fixture's ID. Null allows any fixture.
    /// </summary>
    [DataField]
    public string? TriggerFixtureId;

    /// <summary>
    /// IDs of non-hard target fixtures that may trigger teleportation.
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
