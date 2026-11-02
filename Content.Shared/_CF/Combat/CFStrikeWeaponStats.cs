namespace Content.Shared.CF.Combat;

/// <summary>
/// Snapshot of PF2e weapon stats used for a single Strike (melee or ranged).
/// </summary>
public readonly record struct CFStrikeWeaponStats(
    int DamageDice,
    int DamageDie,
    string DamageType,
    string Proficiency,
    bool Finesse,
    int ItemBonus,
    bool Ranged = false,
    int? DeadlyDie = null,
    int RangeIncrementFeet = 0,
    int VolleyFeet = 0,
    bool Agile = false)
{
    public static CFStrikeWeaponStats FromMelee(CFMeleeWeaponComponent weapon)
    {
        return new CFStrikeWeaponStats(
            weapon.DamageDice,
            weapon.DamageDie,
            weapon.DamageType,
            weapon.Proficiency,
            weapon.Finesse,
            weapon.ItemBonus,
            Agile: weapon.Agile);
    }

    public static CFStrikeWeaponStats FromRanged(CFRangedWeaponComponent weapon)
    {
        return new CFStrikeWeaponStats(
            weapon.DamageDice,
            weapon.DamageDie,
            weapon.DamageType,
            weapon.Proficiency,
            Finesse: false,
            ItemBonus: weapon.ItemBonus,
            Ranged: true,
            DeadlyDie: weapon.DeadlyDie > 0 ? weapon.DeadlyDie : null,
            RangeIncrementFeet: weapon.RangeIncrementFeet,
            VolleyFeet: weapon.VolleyFeet,
            Agile: weapon.Agile);
    }

    public static CFStrikeWeaponStats Unarmed()
    {
        return new CFStrikeWeaponStats(
            CFMelee.UnarmedDamageDice,
            CFMelee.UnarmedDamageDie,
            CFMelee.UnarmedDamageType,
            CFMelee.UnarmedProficiency,
            Finesse: false,
            ItemBonus: 0,
            Agile: true);
    }
}
