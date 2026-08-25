using Content.Server.StationEvents.Events;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// A component for the breaker flip rule.
///
/// </summary>
[RegisterComponent, Access(typeof(BreakerFlipRule))]
public sealed partial class BreakerFlipRuleComponent : Component
{
    /// <summary>
    /// Blacklist for stations not eligible to trigger this game rule.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Blacklist for grids not eligible to trigger this game rule.
    /// </summary>
    [DataField]
    public EntityWhitelist? GridBlacklist;
}
