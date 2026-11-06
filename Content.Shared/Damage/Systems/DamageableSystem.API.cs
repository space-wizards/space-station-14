using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    #region Setters

    /// <summary>
    ///     Directly sets the damage in a damageable component.
    /// </summary>
    /// <remarks>
    ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
    ///     event is raised.
    /// </remarks>
    [PublicAPI]
    public void SetDamage(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            if (!damage.DamageDict.ContainsKey(type))
                ent.Comp.Damage.DamageDict.Remove(type);
        }

        foreach (var (type, amount) in damage.DamageDict)
        {
            ent.Comp.Damage.DamageDict[type] = amount;
        }

        OnEntityDamageChanged((ent, ent.Comp));
    }

    /// <summary>
    ///     Sets all damage types supported by a <see cref="Components.DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    ///     Does nothing If the given damage value is negative.
    /// </remarks>
    [PublicAPI]
    public void SetAllDamage(Entity<DamageableComponent?> ent, FixedPoint2 newValue)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (newValue < 0)
            return;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            ent.Comp.Damage.DamageDict[type] = newValue;
        }

        // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
        // empty damage delta.
        OnEntityDamageChanged((ent, ent.Comp), new DamageSpecifier());
    }

    [PublicAPI]
    public void ClearAllDamage(Entity<DamageableComponent?> ent)
    {
        SetAllDamage(ent, FixedPoint2.Zero);
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
    [PublicAPI]
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers);
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
    [PublicAPI]
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        out DamageSpecifier newDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        newDamage = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers);
        return !newDamage.Empty;
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
    ///     The actual amount of damage dealt, as a DamageSpecifier.
    /// </returns>
    [PublicAPI]
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false
    )
    {
        var damageDone = new DamageSpecifier();

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damageDone;

        if (damage.Empty)
            return damageDone;

        var before = new BeforeDamageChangedEvent(damage, origin);
        RaiseLocalEvent(ent, ref before);

        if (before.Cancelled)
            return damageDone;

        // Apply resistances
        if (!ignoreResistances)
        {
            if (GetDamageModifierSet(ent) is { } modifierSet)
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

        var evt = new DamageDealtEvent(damage, origin, interruptsDoAfters);
        RaiseLocalEvent(ent, ref evt);

        return damage;
    }

    /// <summary>
    /// Reduce damage on the entity equally distributed among given <see cref="DamageTypePrototype"/>s.
    /// If a type fully heals the extra is distributed evenly to the remaining types, or discarded once all are fully healed.
    /// </summary>
    /// <example>
    /// Healing for 10 on an entity with [5 Heat, 10 Cold, 5 Shock] will result in healing [3.33 Heat, 3.33 Cold, 3.33 Shock].
    /// </example>
    /// <param name="ent">Entity getting healed.</param>
    /// <param name="amount">How much to heal. <b>Must be negative.</b></param>
    /// <param name="types">Which damage types to heal, or all types if null.</param>
    /// <param name="origin">Entity that did the healing.</param>
    [PublicAPI]
    public DamageSpecifier HealEvenly(Entity<DamageableComponent?> ent,
                                    FixedPoint2 amount,
                                    ICollection<ProtoId<DamageTypePrototype>>? types = null,
                                    EntityUid? origin = null)
    {
        var damageChange = new DamageSpecifier();

        if (amount >= 0)
        {
            Log.Error($"HealEvenly() called with non-negative amount {amount} on entity {ent}.");
            return damageChange;
        }
        amount *= -1; // We immediately want it non-negative :o)

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damageChange;

        // Get our total damage and shortcut to a full heal if possible
        var currentDamage = GetPositiveDamage((ent, ent.Comp), types);
        if (currentDamage.GetTotal() <= amount)
            return ChangeDamage(ent, -currentDamage, true, false, origin);

        // make sure damageChange has the same damage types as damage
        var keys = currentDamage.DamageDict.Keys.ToList();
        damageChange.DamageDict.EnsureCapacity(keys.Count);
        foreach (var type in keys)
        {
            damageChange.DamageDict.Add(type, FixedPoint2.Zero);
        }

        while (amount > 0)
        {
            var count = keys.Count;
            // We do this to ensure that we always round up when dividing to avoid excess loops.
            // We already have logic to prevent healing more than we have.
            var maxHeal = count == 1 ? amount : (amount + FixedPoint2.Epsilon * (count - 1)) / count;

            // Iterate backwards since we're removing items.
            for (var i = count - 1; i >= 0; i--)
            {
                var type = keys[i];
                // This is the amount we're trying to heal, capped by maxHeal
                var heal = currentDamage.DamageDict[type] + damageChange.DamageDict[type];

                // Don't go above max
                if (heal > maxHeal)
                    heal = maxHeal;
                // But if we're not above max, we will heal it fully and don't need to enumerate anymore!
                else
                    keys.RemoveAt(i);

                if (heal >= amount)
                {
                    // Don't remove more than we can remove. Prevents us from healing more than we'd expect...
                    damageChange.DamageDict[type] -= amount;
                    amount = FixedPoint2.Zero;
                    break;
                }

                amount -= heal;
                damageChange.DamageDict[type] -= heal;
            }
        }

        return ChangeDamage(ent, damageChange, true, false, origin);
    }

    /// <inheritdoc cref="HealEvenly(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
    [PublicAPI]
    public DamageSpecifier HealEvenly(Entity<DamageableComponent?> ent,
                                    FixedPoint2 amount,
                                    ProtoId<DamageGroupPrototype> types,
                                    EntityUid? origin = null)
    {
        if (!ProtoMan.Resolve(types, out var groupProto))
            return new DamageSpecifier();

        return HealEvenly(ent, amount, groupProto.DamageTypes, origin);
    }

    /// <summary>
    /// Reduces damage on the entity distributed between <paramref name="types"/> proportional to their percentage of total damage.
    /// </summary>
    /// <example>
    /// Healing for 10 on an entity with [5 Heat, 10 Cold, 5 Shock] will result in healing [2.5 Heat, 5 Cold, 2.5 Shock].
    /// </example>
    /// <param name="ent">Entity getting healed.</param>
    /// <param name="amount">How much to heal. <b>Must be negative.</b></param>
    /// <param name="types">Which damage types to heal, or all types if null.</param>
    /// <param name="origin">Entity that did the healing.</param>
    [PublicAPI]
    public DamageSpecifier HealDistributed(Entity<DamageableComponent?> ent,
                                        FixedPoint2 amount,
                                        ICollection<ProtoId<DamageTypePrototype>>? types = null,
                                        EntityUid? origin = null)
    {
        var damageChange = new DamageSpecifier();

        if (amount >= 0)
        {
            Log.Error($"HealDistributed() called with non-negative amount {amount} on entity {ent}.");
            return damageChange;
        }

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damageChange;

        // Get our total damage and shortcut to a full heal if possible
        var currentDamage = GetPositiveDamage((ent, ent.Comp), types);
        if (currentDamage.GetTotal() <= -amount)
            return ChangeDamage(ent, -currentDamage, true, false, origin);

        // make sure damageChange has the same damage types as damageEntity
        damageChange.DamageDict.EnsureCapacity(currentDamage.DamageDict.Count);
        var total = currentDamage.GetTotal();

        // heal weighted by the percentage of damage for that type
        foreach (var (type, value) in currentDamage.DamageDict)
        {
            damageChange.DamageDict.Add(type, value / total * amount);
        }

        return ChangeDamage(ent, damageChange, true, false, origin);
    }

    /// <inheritdoc cref="HealDistributed(Entity{DamageableComponent?}, FixedPoint2, ICollection{ProtoId{DamageTypePrototype}}?, EntityUid?)"/>
    [PublicAPI]
    public DamageSpecifier HealDistributed(Entity<DamageableComponent?> ent,
                                        FixedPoint2 amount,
                                        ProtoId<DamageGroupPrototype> types,
                                        EntityUid? origin = null)
    {
        if (!ProtoMan.Resolve(types, out var groupProto))
            return new DamageSpecifier();

        return HealDistributed(ent, amount, groupProto.DamageTypes, origin);
    }

    #endregion
    #region Getters

    /// <summary>
    /// Gets the damages currently sustained by an entity.
    /// </summary>
    [PublicAPI]
    [Obsolete("Do not rely on the ability to determine a numerically quantifiable amount of damage")]
    public DamageSpecifier GetAllDamage(Entity<DamageableComponent?> ent)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp))
            return new();

        return ent.Comp.Damage.Clone();
    }

    /// <summary>
    /// Gets the total amount of damage currently sustained by an entity.
    /// </summary>
    [PublicAPI]
    [Obsolete("Do not rely on the ability to determine a numerically quantifiable amount of damage")]
    public FixedPoint2 GetTotalDamage(Entity<DamageableComponent?> ent)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return FixedPoint2.Zero;

        return ent.Comp.TotalDamage;
    }

    /// <summary>
    /// Returns a <see cref="DamageSpecifier"/> with only positive damage, filtered by <paramref name="types"/>.
    /// </summary>
    [PublicAPI]
    public DamageSpecifier GetPositiveDamage(Entity<DamageableComponent> ent, ICollection<ProtoId<DamageTypePrototype>>? types = null)
    {
        return types is null
            ? DamageSpecifier.GetPositive(ent.Comp.Damage)
            : DamageSpecifier.GetPositive(ent.Comp.Damage, types);
    }

    /// <inheritdoc cref="GetPositiveDamage(Entity{DamageableComponent}, ICollection{ProtoId{DamageTypePrototype}})"/>
    [PublicAPI]
    public DamageSpecifier GetPositiveDamage(Entity<DamageableComponent> ent, ProtoId<DamageGroupPrototype> types)
    {
        // No damage if no group exists...
        if (!ProtoMan.Resolve(types, out var groupProto))
            return new DamageSpecifier();

        return DamageSpecifier.GetPositive(ent.Comp.Damage, groupProto.DamageTypes);
    }

    /// <summary>
    /// Gets the total amount of damage currently sustained by an entity, indexed by damage group.
    /// </summary>
    [PublicAPI]
    [Obsolete("Do not rely on the ability to determine a numerically quantifiable amount of damage")]
    public IReadOnlyDictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GetDamagePerGroup(Entity<DamageableComponent?> ent)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp))
            return new Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>();

        return ent.Comp.DamagePerGroup;
    }

    /// <summary>
    /// Goes through an entity damage's and saves them inside a dictionary if the value is higher than 0
    /// The dictionary is structured with a string for the name of the damage type, and a FixedPoint2 for the numeric damage value
    /// </summary>
    [Obsolete("Highly specific method that take a circuitous route around simply storing damage in a DamageSpecifier.")]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> GetDamages(Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> damagePerGroup, DamageSpecifier damage)
    {
        var damageTypes = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

        foreach (var (damageGroupId, _) in damagePerGroup)  //go through each group
        {
            var group = ProtoMan.Index<DamageGroupPrototype>(damageGroupId);  //get group
            foreach (var type in group.DamageTypes) //go through each type inside that group
            {
                if (!damage.DamageDict.TryGetValue(type, out var damageValue) || damageValue == 0) //get value and make sure it isn't 0
                    continue;

                damageTypes.Add(type, damageValue);
            }
        }
        return damageTypes;
    }

    #endregion
    #region Damage Modifiers

    /// <summary>
    /// Set's the damage modifier set prototype for this entity.
    /// </summary>
    /// <param name="ent">The entity we're setting the modifier set of.</param>
    /// <param name="damageModifierSetId">The prototype we're setting.</param>
    [PublicAPI]
    public void SetDamageModifierSetId(Entity<DamageableComponent?> ent, ProtoId<DamageModifierSetPrototype>? damageModifierSetId)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.DamageModifierSetId = damageModifierSetId;

        Dirty(ent);
    }

    [PublicAPI]
    public DamageModifierSet? GetDamageModifierSet(Entity<DamageableComponent?> entity)
    {
        if (!_damageableQuery.Resolve(entity, ref entity.Comp, false)
            || entity.Comp.DamageModifierSetId is not { } proto
            || !ProtoMan.Resolve(proto, out var modifierSet)
           )
            return null;

        return modifierSet;
    }

    /// <summary>
    /// Applies the two universal "All" modifiers, if set.
    /// Individual damage source modifiers are set in their respective code.
    /// </summary>
    /// <param name="damage">The damage to be changed.</param>
    [PublicAPI]
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

    #endregion
    #region Utility

    /// <summary>
    /// Returns whether the entity can be damaged by the given type of damage
    /// </summary>
    [PublicAPI]
    [Obsolete("Do not rely on the ability to determine if an entity will be able to be damaged by something")]
    public bool CanBeDamagedBy(Entity<InjurableComponent?> ent, ProtoId<DamageTypePrototype> type)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        return SupportsType(ent.Comp.DamageContainer, type);
    }

    /// <summary>
    /// Proxy method used primarily for giving access to tests.
    /// </summary>
    [PublicAPI]
    public void TrimZeros(Entity<DamageableComponent> entity)
    {
        entity.Comp.Damage.TrimZeros();
    }

    /// <summary>
    /// Tries to get damage from an entity with an optional group specifier.
    /// </summary>
    /// <param name="ent">Entity we're checking the damage on</param>
    /// <param name="amount">Amount we want the damage to be greater than ideally</param>
    /// <param name="damage">Damage specifier we're returning with</param>
    /// <param name="group">An optional group, note that if it fails to index it will just use all damage.</param>
    /// <returns>True if the total damage is greater than the specified amount</returns>
    [Obsolete("This method is highly specific and misuses the 'TryX' name scheme.")]
    public bool TryGetDamageGreaterThan(Entity<DamageableComponent> ent,
        FixedPoint2 amount,
        out DamageSpecifier damage,
        ProtoId<DamageGroupPrototype>? group = null)
    {
        // get the damage should be healed (either all or only from one group)
        damage = group == null ? GetPositiveDamage(ent) : GetPositiveDamage(ent, group.Value);

        // If trying to heal more than the total damage of damageEntity just heal everything
        return damage.GetTotal() > amount;
    }

    #endregion

    /// <returns>If the damage container can take the given damage type</returns>
    private bool SupportsType(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageTypePrototype> type)
    {
        if (container is null)
            return true;

        return _supportedTypesByContainer[container.Value].Contains(type);
    }
}
