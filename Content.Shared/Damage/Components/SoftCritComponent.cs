using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SoftCritComponent : Component
{
    /// <summary>
    ///     The current damage used for things like state calculations
    /// </summary>
    [DataField]
    public DamageSpecifier DamageEffective = new();

    [ViewVariables]
    public FixedPoint2 TotalDamageEffective;

    /// <summary>
    ///     The amount of time it takes for <see cref="DamageEffective"/> to catch up with <see cref="Damage"/>
    ///     This value is decreased with respect to how much damage a biological container has taken,
    ///     becoming 0 if it has died
    ///     Ex: If this is 2 seconds, then a person that has instantly reduced to critical damage from no damage
    ///     would take 1 second to crit, because crit is halfway between perfectly healthy and dead.
    /// </summary>
    [DataField]
    public TimeSpan DamageLerpTimeZeroDamage = TimeSpan.FromSeconds(20);
}
