using Content.Server.Weapons.Melee;

namespace Content.Server.Mobs;

/// <summary>
/// Causes a mob to have an additive modifier to it's chance to be disarmed.
/// </summary>
[RegisterComponent, Access(typeof(MeleeWeaponSystem))]
public sealed partial class MobDisarmFailChanceComponent : Component
{
    /// <summary>
    /// Additive modifier to apply. Negative values raises the chance of disarm, while positive values lower it.
    /// Setting to -1 effecitvely garrentees a one hit shove, unless some sort of disarm malus is applied
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float FailChanceModifier = 0.0f;
}
