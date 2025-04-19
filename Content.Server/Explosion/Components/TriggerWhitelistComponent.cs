using Content.Shared.Whitelist;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Checks if the user of a Trigger satisfies a whitelist and blacklist condition.
/// Cancels the trigger otherwise.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerWhitelistComponent : Component
{
    /// <summary>
    /// Whitelist for what entites can cause this trigger.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for what entites can cause this trigger.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
