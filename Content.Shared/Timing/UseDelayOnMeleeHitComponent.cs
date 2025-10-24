using Content.Shared.Weapons.Melee;
using Robust.Shared.GameStates;

namespace Content.Shared.Timing;

/// <summary>
///     Activates UseDelay when a Melee Weapon is used to hit something.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UseDelaySystem))]
public sealed partial class UseDelayOnMeleeHitComponent : Component
{
    /// <summary>
    /// If true, even misses trigger the use delay.
    /// </summary>
    [DataField]
    public bool IncludeMiss = false;

    /// <summary>
    /// <see cref="UseDelayInfo"/> ID this applies to.
    /// </summary>
    [DataField]
    public string UseDelayId = UseDelaySystem.DefaultId;
}
