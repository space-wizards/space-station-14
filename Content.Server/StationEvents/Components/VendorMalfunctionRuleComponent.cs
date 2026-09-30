using Content.Server.StationEvents.Events;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Gamerule component to make some vending machines eject some of their contents
/// </summary>
[RegisterComponent, Access(typeof(VendorMalfunctionRule))]
public sealed partial class VendorMalfunctionRuleComponent : Component
{
    /// <summary>
    /// Blacklist of structures not eligible to trigger this game rule.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
