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
    /// Blacklist of grids which are not eligible to trigger this game rule.
    /// </summary>
    /// <see cref="BreakerFlipRuleComponent.Blacklist"/>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// minimum quantity of affected machines
    /// </summary>
    [DataField]
    public int MinimumAffected;

    /// <summary>
    /// maxmimum quantity of affected machines
    /// </summary>
    [DataField]
    public int MaximumAffected;

    /// <summary>
    /// chance of the 'contraband' inventory of the vending machine being enabled
    /// </summary>
    [DataField] public float ContrabandChance;

    /// <summary>
    /// minimum quantity to eject
    /// </summary>
    [DataField] public int MinEjectedItems = 1;
    /// <summary>
    /// maximum quantity to eject
    /// </summary>
    [DataField] public int MaxEjectedItems;
}
