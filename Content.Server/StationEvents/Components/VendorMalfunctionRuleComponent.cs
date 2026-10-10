using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Gamerule component to make some vending machines toggle their contraband inventory and eject some of their contents
/// </summary>
[RegisterComponent, Access(typeof(VendorMalfunctionRule))]
public sealed partial class VendorMalfunctionRuleComponent : Component
{
    /// <summary>
    /// percentage of machines which may be affected by this event. minimum should be greater than zero, max shouldn't exceed 1
    /// </summary>
    [DataField]
    public MinMax AffectedMachines;

    /// <summary>
    /// minimum number of machines to hit if AffectedMachines somehow ends up at zero. should be at least 1.
    /// </summary>
    [DataField]
    public int MinimumToAffect = 5;

    /// <summary>
    /// chance of the 'contraband' inventory of the vending machine being enabled.
    /// </summary>
    [DataField] 
    public float ContrabandChance;

    /// <summary>
    /// quantity of items to be ejected by affected machines.
    /// </summary>
    [DataField]
    public MinMax ItemsToEject;
}
