using Content.Server.StationEvents.Events;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// A component for a game rule that disables breakers for active APCs.
/// When it runs, it selects a random station and disables some random number of APCs,
/// optionally checking blacklists for the stations/grids that they're a part of.
/// </summary>
[RegisterComponent, Access(typeof(BreakerFlipRule))]
public sealed partial class BreakerFlipRuleComponent : Component
{
    /// <summary>
    /// Blacklist to exclude grids that triggered APCs should not be on.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
