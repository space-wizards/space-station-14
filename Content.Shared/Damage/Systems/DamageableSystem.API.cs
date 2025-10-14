using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage;

public sealed partial class DamageableSystem
{
    /// <summary>
    ///     Directly sets the damage specifier of a damageable component.
    /// </summary>
    /// <remarks>
    ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
    ///     event is raised.
    /// </remarks>
    public void SetDamage(Entity<DamageableComponent> ent, DamageSpecifier damage)
    {
        ent.Comp.Damage = damage;

        OnEntityDamageChanged(ent);
    }

    [Obsolete("Use the Entity<Comp> variant instead")]
    public void SetDamage(EntityUid uid, DamageableComponent damageable, DamageSpecifier damage)
    {
        SetDamage((uid, damageable), damage);
    }

    [Obsolete("Use TryChangeDamage(Entity<DamageableComponent>...) instead")]
    public DamageSpecifier? TryChangeDamage(
        EntityUid? uid,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        DamageableComponent? damageable = null,
        EntityUid? origin = null
    )
    {
        return uid is null
            ? null
            : TryChangeDamage(uid.Value, damage, ignoreResistances, interruptsDoAfters, damageable, origin);
    }

    // This function is only here because the C# type engine deduces that non-nullable Entities are more correctly matched
    // to the signature that uses the non-nullable Entity<DamageableComponent> in the non-obsolete TryChangeDamage below
    // instead of using the signature above which actually compiles.
    [Obsolete("Use TryChangeDamage(Entity<DamageableComponent>...) instead")]
    public DamageSpecifier? TryChangeDamage(
        EntityUid uid,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        DamageableComponent? damageable = null,
        EntityUid? origin = null
    )
    {
        if (!_damageableQuery.Resolve(uid, ref damageable, false))
            return null;

        return TryChangeDamage((uid, damageable), damage, ignoreResistances, interruptsDoAfters, origin)
            ? damage
            : null;
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        return !ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers).Empty;
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false
    )
    {
        var damageDone = new DamageSpecifier();

        if (damage.Empty)
            return damageDone;

        var before = new BeforeDamageChangedEvent(damage, origin);
        RaiseLocalEvent(ent, ref before);

        if (before.Cancelled)
            return damageDone;

        // Apply resistances
        if (!ignoreResistances)
        {
            if (
                ent.Comp.DamageModifierSetId != null &&
                _prototypeManager.Resolve(ent.Comp.DamageModifierSetId, out var modifierSet)
            )
                damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);

            // TODO DAMAGE
            // byref struct event.
            var ev = new DamageModifyEvent(damage, origin);
            RaiseLocalEvent(ent, ev);
            damage = ev.Damage;

            if (damage.Empty)
                return damageDone;
        }

        if (!ignoreGlobalModifiers)
            damage = ApplyUniversalAllModifiers(damage);


        damageDone.DamageDict.EnsureCapacity(damage.DamageDict.Count);

        var dict = ent.Comp.Damage.DamageDict;
        foreach (var (type, value) in damage.DamageDict)
        {
            // CollectionsMarshal my beloved.
            if (!dict.TryGetValue(type, out var oldValue))
                continue;

            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            dict[type] = newValue;
            damageDone.DamageDict[type] = newValue - oldValue;
        }

        if (!damageDone.Empty)
            OnEntityDamageChanged(ent, damageDone, interruptsDoAfters, origin);

        return damageDone;
    }

    /// <summary>
    /// Applies the two universal "All" modifiers, if set.
    /// Individual damage source modifiers are set in their respective code.
    /// </summary>
    /// <param name="damage">The damage to be changed.</param>
    public DamageSpecifier ApplyUniversalAllModifiers(DamageSpecifier damage)
    {
        // Checks for changes first since they're unlikely in normal play.
        if (
            MathHelper.CloseToPercent(UniversalAllDamageModifier, 1f) &&
            MathHelper.CloseToPercent(UniversalAllHealModifier, 1f)
        )
            return damage;

        foreach (var (key, value) in damage.DamageDict)
        {
            if (value == 0)
                continue;

            if (value > 0)
            {
                damage.DamageDict[key] *= UniversalAllDamageModifier;

                continue;
            }

            if (value < 0)
                damage.DamageDict[key] *= UniversalAllHealModifier;
        }

        return damage;
    }

    [Obsolete("Use the Entity<Comp> variant instead")]
    public void SetAllDamage(EntityUid uid, DamageableComponent component, FixedPoint2 newValue)
    {
        SetAllDamage((uid, component), newValue);
    }

    /// <summary>
    ///     Sets all damage types supported by a <see cref="DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    ///     Does nothing If the given damage value is negative.
    /// </remarks>
    public void SetAllDamage(Entity<DamageableComponent> ent, FixedPoint2 newValue)
    {
        if (newValue < 0)
            return;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            ent.Comp.Damage.DamageDict[type] = newValue;
        }

        // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
        // empty damage delta.
        OnEntityDamageChanged(ent, new DamageSpecifier());
    }

    [Obsolete("Use the Entity<Comp> variant instead")]
    public void SetDamageModifierSetId(EntityUid uid, string? damageModifierSetId, DamageableComponent? comp = null)
    {
        if (!_damageableQuery.Resolve(uid, ref comp))
            return;

        SetDamageModifierSetId((uid, comp), damageModifierSetId);
    }

    public void SetDamageModifierSetId(
        Entity<DamageableComponent> ent,
        ProtoId<DamageModifierSetPrototype>? damageModifierSetId
    )
    {
        ent.Comp.DamageModifierSetId = damageModifierSetId;

        Dirty(ent);
    }
}
