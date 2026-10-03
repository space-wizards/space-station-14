using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Gamerule component to make some vending machines eject some of their contents
/// </summary>
[RegisterComponent, Access(typeof(VendorMalfunctionRule))]
public sealed partial class VendorMalfunctionRuleComponent : Component
{
    /// <summary>
    /// Blacklist of grids which are not eligible to trigger this game rule.
    /// </summary>
    /// <see cref="BreakerFlipRuleComponent.Blacklist"/>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// quantity of machines which may be affected by this event
    /// </summary>
    [DataField]
    public MinMax AffectedMachines;

    /// <summary>
    /// chance of the 'contraband' inventory of the vending machine being enabled
    /// </summary>
    [DataField] 
    public float ContrabandChance;

    /// <summary>
    /// quantity of items to be ejected by affected machines
    /// </summary>
    [DataField]
    public MinMax ItemsToEject;
}
