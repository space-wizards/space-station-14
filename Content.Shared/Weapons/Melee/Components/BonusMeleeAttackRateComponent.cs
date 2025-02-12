using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class BonusMeleeAttackRateComponent : Component
{
    /// <summary>
    /// The value added onto the attack rate of a melee weapon
    /// </summary>
    [DataField, ViewVariables]
    public float FlatModifier;

    /// <summary>
    /// A value that is multiplied by the attack rate of a melee weapon
    /// </summary>
    [DataField, ViewVariables]
    public float Multiplier = 1;

    /// <summary>
    /// A value that is added on to a weapon's heavy windup time.
    /// </summary>
    [DataField, ViewVariables]
    public float HeavyWindupFlatModifier;

    /// <summary>
    /// A value that is multiplied by a weapon's heavy windup time
    /// </summary>
    [DataField, ViewVariables]
    public float HeavyWindupMultiplier = 1;
}
