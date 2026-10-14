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
    /// Percentage of machines which may be affected by this event. Minimum should be greater than zero, max shouldn't exceed 1
    /// </summary>
    [DataField]
    public MinMax AffectedMachines = new MinMax(0.05f, 0.2f);

    /// <summary>
    /// Minimum number of machines to hit. Should be at least 1.
    /// </summary>
    [DataField]
    public int MinimumToAffect = 5;

    /// <summary>
    /// Chance of the 'contraband' inventory of the vending machine being enabled.
    /// </summary>
    [DataField] 
    public float ContrabandChance = 0.5f;

    /// <summary>
    /// Quantity of items to be ejected by affected machines.
    /// </summary>
    [DataField]
    public MinMax ItemsToEject = new MinMax(1, 6);
}
