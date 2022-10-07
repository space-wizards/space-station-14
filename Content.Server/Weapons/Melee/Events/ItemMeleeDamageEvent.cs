using Content.Shared.Damage;

namespace Content.Server.Weapons.Melee.Events;

public sealed class ItemMeleeDamageEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The base amount of damage dealt by the melee hit.
    /// </summary>
    public readonly DamageSpecifier BaseDamage = new();

    /// <summary>
    ///     Modifier sets to apply to the damage when it's all said and done.
    ///     This should be modified by adding a new entry to the list.
    /// </summary>
    public List<DamageModifierSet> ModifiersList = new();

    /// <summary>
    ///     Damage to add to the default melee weapon damage. Applied before modifiers.
    /// </summary>
    /// <remarks>
    ///     This might be required as damage modifier sets cannot add a new damage type to a DamageSpecifier.
    /// </remarks>
    public DamageSpecifier BonusDamage = new();

    public ItemMeleeDamageEvent(DamageSpecifier baseDamage)
    {
        BaseDamage = baseDamage;
    }
}
