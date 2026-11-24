using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

// This part provides functions for use in other systems.
public abstract partial class SatiationSystem
{
    /// <summary>
    /// Gets <paramref name="entity"/>'s current value of the satiation of <paramref name="type"/>. If this entity does
    /// not have that satiation, returns null.
    /// </summary>
    public float? GetValueOrNull(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    )
    {
        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return null;

        return CalculateCurrentValue(satiation, proto);
    }

    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to a value corresponding to
    /// <paramref name="satiationValue"/>. If this entity does not have that satiation, or the key does not correspond to
    /// any threshold, has no effect.
    /// </summary>
    public void SetValue(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        [ForbidLiteral] SatiationValue satiationValue
    )
    {
        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto) ||
            GetThresholdValueOrNull(satiation, satiationValue) is not { } value)
            return;

        SetAuthoritativeValue(entity, satiation, proto, value);
    }

    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to <paramref name="value"/>. If
    /// this entity does not have that satiation, has no effect.
    /// </summary>
    // [OverloadResolutionPriority(1)] // If you pass in an int, avoid instantiating a record to hold it. // Requires a newer language version :agony:
    public void SetValue(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        float value
    )
    {
        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return;

        SetAuthoritativeValue(entity, satiation, proto, value);
    }

    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to its current value plus
    /// <paramref name="amount"/>. If this entity does not have that satiation, has no effect.
    /// </summary>
    public void ModifyValue(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        float amount
    )
    {
        if (GetValueOrNull(entity, type) is { } currentValue)
        {
            SetValue(entity, type, currentValue + amount);
        }
    }

    /// <summary>
    /// Returns whether or not the current value (plus optional <paramref name="hypotheticalValueDelta"/>) is between
    /// the values described by <paramref name="above"/> and <paramref name="below"/>. If <paramref name="entity"/>
    /// does not have a <see cref="Satiation"/> of the specified <paramref name="type"/>, returns false. If either
    /// above- or below-key is null, any value is considered in-range compared to that key.
    /// If either key is specified but not present in <paramref name="type"/>'s <see cref="SatiationPrototype.Thresholds"/>,
    /// all values are considered out-of-range.
    /// </summary>
    public bool IsValueInRange(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        [ForbidLiteral] SatiationValue? above = null,
        [ForbidLiteral] SatiationValue? below = null,
        float hypotheticalValueDelta = 0
    )
    {
        if (above is null && below is null)
        {
            DebugTools.Assert("Range is unbounded. Is this a programming error?");
            return true;
        }

        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return false;

        // Resolve the bounds to integers we can actually compare against.
        int? valueAbove = null;
        if (above is { } a && (valueAbove = GetThresholdValueOrNull(satiation, a)) is null)
            return false; // `above` is not null, but we failed to resolve its value.

        int? valueAtOrBelow = null;
        if (below is { } b && (valueAtOrBelow = GetThresholdValueOrNull(satiation, b)) is null)
            return false; // `atOrBelow` is not null, but we failed to resolve its value.

        if (valueAbove > valueAtOrBelow)
        {
            DebugTools.Assert("Range is empty. Is this a programming error?");
            return false;
        }

        var currentValue = CalculateCurrentValue(satiation, proto);
        if (hypotheticalValueDelta is not 0)
            currentValue = proto.ClampSatiationWithinThresholds(currentValue + hypotheticalValueDelta);

        var isAboveBottom = valueAbove is null || currentValue > valueAbove;
        var isAtOrBelowTop = valueAtOrBelow is null || currentValue < valueAtOrBelow;
        ;

        return isAboveBottom && isAtOrBelowTop;
    }

    /// <summary>
    /// Returns the <typeparamref name="T"/> value for <paramref name="entity"/>'s current satiation value of the given
    /// <paramref name="type"/>, as informed by <paramref name="valuesByThreshold"/>.
    /// </summary>
    /// <param name="entity">The entity whose satiation is considered</param>
    /// <param name="type">The type of satiation to consider</param>
    /// <param name="valuesByThreshold">The values, keyed by <see cref="SatiationValue"/> thresholds</param>
    /// <param name="result">The value selected from <paramref name="valuesByThreshold"/></param>
    /// <param name="nextHigherThreshold">
    /// The keying-threshold-value of the next threshold above the selected value. Most consumers will not have a use
    /// for this value, but it is used by <see cref="BaseSatiationEffectSystem{TComp,T}"/>'s prediction of value change.
    /// </param>
    /// <param name="nextLowerThreshold">
    /// The keying-threshold-value of the next threshold below the selected value. Most consumers will not have a use
    /// for this value, but it is used by <see cref="BaseSatiationEffectSystem{TComp,T}"/>'s prediction of value change.
    /// </param>
    /// <typeparam name="T">The type of values in <paramref name="valuesByThreshold"/></typeparam>
    /// <returns>
    /// True if a value was selected. False if <paramref name="valuesByThreshold"/> is empty, if the current value of
    /// the specified satiation is higher than all thresholds, or the given entity does not have a satiation of the
    /// given type.
    /// </returns>
    public bool TryGetValueByThreshold<T>(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        Dictionary<SatiationValue, T> valuesByThreshold,
        out T? result,
        out int? nextHigherThreshold,
        out int? nextLowerThreshold
    )
    {
        if (GetValueOrNull(entity, type) is not { } currentValue ||
            GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
        {
            result = default;
            nextHigherThreshold = null;
            nextLowerThreshold = null;
            return false;
        }

        using var valuesByDescendingThreshold = valuesByThreshold
            // Resolve keys to threshold integers, discarding any keys which cannot be resolved.
            .Select(it => GetThresholdValueOrNull(satiation, it.Key) is { } value ? ((int, T)?)(value, it.Value) : null)
            .OfType<(int, T)>()
            // Order by descending threshold.
            .OrderByDescending(it => it.Item1)
            .GetEnumerator();
        if (!valuesByDescendingThreshold.MoveNext())
        {
            // `values` is empty, so there are no values to return.
            result = default;
            nextHigherThreshold = null;
            nextLowerThreshold = null;
            return false;
        }

        if (currentValue > valuesByDescendingThreshold.Current.Item1)
        {
            // `currentSatiation` is higher than all thresholds, so we have neither a value nor a higher threshold, but
            // we can return the next lower threshold.
            result = default;
            nextHigherThreshold = null;
            nextLowerThreshold = valuesByDescendingThreshold.Current.Item1;
            return false;
        }

        var nextHigher = valuesByDescendingThreshold.Current;
        while (valuesByDescendingThreshold.MoveNext())
        {
            var nextLower = valuesByDescendingThreshold.Current;
            if (currentValue > nextLower.Item1)
            {
                // The current value is below `nextHigher` and above `nextLower`, so `nextHigher` is the correct threshold.
                result = nextHigher.Item2;
                nextHigherThreshold = nextHigher.Item1;
                nextLowerThreshold = nextLower.Item1;
                return true;
            }

            // Loop, setting `nextLower` to the next iteration's `nextHigher`
            nextHigher = nextLower;
        }

        // We've run out of thresholds below.
        result = nextHigher.Item2;
        nextHigherThreshold = nextHigher.Item1;
        nextLowerThreshold = null;
        return true;
    }

    /// <summary>
    /// Calculates when the given <paramref name="entity"/>'s satiation of the given <paramref name="type"/> will
    /// evolve to either <paramref name="upperBound"/> or <paramref name="lowerBound"/>. Returns null if the given
    /// entity does not have the given satiation or when the given satiation will not never evolve to one of the given
    /// values. A null bound is treated as unreachable.
    /// </summary>
    public TimeSpan? GetTimeToBound(
        Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type,
        int? upperBound,
        int? lowerBound
    ) => GetAndResolveSatiationOfType(entity, type) is var (satiation, proto)
        ? EvolvesToBoundAt(satiation, proto, upperBound, lowerBound)
        : null;

    /// <summary>
    /// Looks up the <see cref="StatusIconPrototype"/> appropriate for the given entity's <see cref="Satiation"/> of the
    /// specified <paramref name="type"/>. If the entity does not have the specified satiation type, or if the satiation
    /// does not have an icon for its current state, returns null.
    /// </summary>
    /// <remarks>This should almost definitely never be used by anything other than the client's Overlay system</remarks>
    public StatusIconPrototype? GetStatusIconOrNull(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    )
    {
        if (entity.Comp.GetOrNull(type) is not { } satiation ||
            !ProtoMan.Resolve(satiation.Prototype, out var proto))
            return null;

        TryGetValueByThreshold(entity, type, proto.Icons, out var iconProtoId, out _, out _);
        return ProtoMan.Resolve(iconProtoId, out var icon) ? icon : null;
    }

    #region Commands

    /// <summary>
    /// Returns the all of the <see cref="SatiationPrototype.Thresholds">key strings</see> of the given
    /// <paramref name="type"/> for <paramref name="entity"/>, or empty if no such type exists.
    /// </summary>
    /// <remarks>
    /// It is expected that <paramref name="type"/> is validated with before calling this. If it fails to resolve, an
    /// error will be logged.
    /// </remarks>
    public IEnumerable<string> GetKeysForType(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    ) => GetAndResolveSatiationOfType(entity, type)?.Proto.Thresholds.Keys ?? Enumerable.Empty<string>();

    /// <summary>
    /// Returns the <see cref="SatiationPrototype.MaximumValue"/> of the given <paramref name="type"/> for
    /// <paramref name="entity"/>, or null if no such type exists.
    /// </summary>
    /// <remarks>
    /// It is expected that <paramref name="type"/> is validated with before calling this. If it fails to resolve, an
    /// error will be logged.
    /// </remarks>
    public int? GetMaximumValue(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    ) => GetAndResolveSatiationOfType(entity, type)?.Proto.MaximumValue;

    #endregion

    /// <summary>
    /// Adds a new <see cref="Satiation"/> of a given <see cref="SatiationTypePrototype"/> to the entity.
    /// </summary>
    /// <param name="entity">The entity to add the satiation to.</param>
    /// <param name="type">The type of satiation to add.</param>
    /// <param name="satiation">The satiation of that type to be added.</param>
    public void AddSatiation(Entity<SatiationComponent?> entity, ProtoId<SatiationTypePrototype> type, Satiation satiation)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!ProtoMan.Resolve(satiation.Prototype, out var proto))
            return;

        satiation.SatiationType = type;

        entity.Comp.Satiations.TryAdd(type, satiation);

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var value = rand.NextFloat(proto.StartingValueMinimum, proto.StartingValueMaximum);

        SetAuthoritativeValue((entity, entity.Comp), satiation, proto, value);

        DirtyField(entity, SatiationComponent.SatiationFieldName);
    }

    /// <summary>
    /// Removes an existing <see cref="SatiationTypePrototype"/> from the entity.
    /// </summary>
    /// <param name="entity">The entity to remove the satiation from.</param>
    /// <param name="type">The type of satiation to remove.</param>
    public void RemoveSatiationType(Entity<SatiationComponent?> entity, ProtoId<SatiationTypePrototype> type)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (GetAndResolveSatiationOfType(entity.Comp, type) is { } satiation)
        {
            _alerts.ClearAlertCategory(entity.Owner, satiation.Proto.AlertCategory);
            entity.Comp.Satiations.Remove(type);
        }

        DirtyField(entity, SatiationComponent.SatiationFieldName);
    }

    /// <summary>
    /// Sets the <see cref="Satiation.OverrideThresholds"> which will override the prototype thresholds.
    /// </summary>
    /// <param name="entity">The entity to change the override for.</param>
    /// <param name="type">The type of satiation to change.</param>
    /// <param name="key">The threshold to change, <see cref="SatiationValue.Key"> must not be null.</param>
    /// <param name="value">The value to change the threshold to.</param>
    /// <param name="preventReordering">If true will prevent the order of thresholds from changing by
    /// capping the current change at 1 below or above the next threshold.</param>
    public void SetOverrideThreshold(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        SatiationValue key,
        int value,
        bool preventReordering = false
    )
    {
        if (key.Key == null)
            return;

        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return;

        if (preventReordering
            && TryGetThresholds(entity, type, out var thresholds)
            && GetThresholdValueOrNull(satiation, key.Key) is { } currentValue)
        {
            thresholds.Remove(key);
            if (TryGetNearestThresholds(entity, type, currentValue, out var nextHigherThreshold, out var nextLowerThreshold, thresholds))
            {
                if (nextHigherThreshold != null && value > nextHigherThreshold)
                {
                    value = nextHigherThreshold.Value - 1;
                }
                else if (nextLowerThreshold != null && value < nextLowerThreshold)
                {
                    value = nextLowerThreshold.Value + 1;
                }
            }
        }

        SetOverrideThreshold(entity, satiation, proto, key.Key, value);
    }

    /// <summary>
    /// Changes the <see cref="Satiation.OverrideThresholds"> by an offset which will override the prototype thresholds.
    /// </summary>
    /// <param name="entity">The entity to change the override for.</param>
    /// <param name="type">The type of satiation to change.</param>
    /// <param name="key">The threshold to change, <see cref="SatiationValue.Key"> must not be null.</param>
    /// <param name="amount">The amount to offset the threshold by.</param>
    /// <param name="preventReordering">If true will prevent the order of thresholds from changing by
    /// capping the current change at 1 below or above the next threshold.</param>
    public void ModifyOverrideThreshold(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        SatiationValue key,
        int amount,
        bool preventReordering = false
    )
    {
        if (key.Key == null)
            return;

        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return;

        if (GetThresholdValueOrNull(satiation, key.Key) is not { } currentValue)
            return;

        var value = currentValue + amount;

        if (preventReordering
            && TryGetThresholds(entity, type, out var thresholds))
        {
            thresholds.Remove(key);
            if (TryGetNearestThresholds(entity, type, currentValue, out var nextHigherThreshold, out var nextLowerThreshold, thresholds))
            {
                if (nextHigherThreshold != null && value > nextHigherThreshold)
                {
                    value = nextHigherThreshold.Value - 1;
                }
                else if (nextLowerThreshold != null && value < nextLowerThreshold)
                {
                    value = nextLowerThreshold.Value + 1;
                }
            }
        }

        SetOverrideThreshold(entity, satiation, proto, key.Key, value);
    }

    /// <summary>
    /// Clears the <see cref="Satiation.OverrideThresholds"> for a specific key.
    /// </summary>
    /// <param name="entity">The entity to clear the override for.</param>
    /// <param name="type">The type of satiation.</param>
    /// <param name="key">The threshold to clear, <see cref="SatiationValue.Key"> must not be null.</param>
    public void ClearOverrideThreshold(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        SatiationValue key
    )
    {
        if (key.Key == null)
            return;

        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return;

        if (GetThresholdValueOrNull(satiation, key) is { } currentValue)
        {
            ClearOverrideThreshold(entity, satiation, key.Key);
        }
    }

    /// <summary>
    /// Clears all of the <see cref="Satiation.OverrideThresholds">.
    /// </summary>
    /// <param name="entity">The entity to clear the overrides for.</param>
    /// <param name="type">The type of satiation.</param>
    public void ClearAllOverrideThreshold(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    )
    {
        if (GetAndResolveSatiationOfType(entity, type) is not var (satiation, proto))
            return;

        ClearAllOverrideThreshold(entity, satiation);
    }

    /// <summary>
    /// Attempts to get an integer value from the given <paramref name="satiationValue"/>. If
    /// <paramref name="satiationValue"/> is an immediate value, use its contained integer value. If it is a key,
    /// attempts to look up the integer value of that key in <see cref="SatiationPrototype.Thresholds"/>; in the case that a key not
    /// present in this proto type is requested, returns null.
    /// The function differs from <see cref="SatiationPrototype.GetValueOrNull"/> in that
    /// <see cref="Satiation.OverrideThresholds"> will overwrite the threshold values.
    /// </summary>
    public int? GetThresholdValueOrNull(Entity<SatiationComponent> ent, [ForbidLiteral] ProtoId<SatiationTypePrototype> type, SatiationValue satiationValue)
    {
        if (GetAndResolveSatiationOfType(ent, type) is not var (satiation, _))
            return null;

        return GetThresholdValueOrNull(satiation, satiationValue);
    }

    /// <summary>
    /// Gets the threshold values and their keys of a given satiation. These thresholds are from the prototype and
    /// overwritten by <see cref="Satiation.OverrideThresholds">.
    /// </summary>
    public bool TryGetThresholds(Entity<SatiationComponent> ent, [ForbidLiteral] ProtoId<SatiationTypePrototype> type, [NotNullWhen(true)] out Dictionary<SatiationValue, int>? thresholds)
    {
        thresholds = null;

        if (GetAndResolveSatiationOfType(ent, type) is not var (satiation, _))
            return false;

        if (!ProtoMan.Resolve(satiation.Prototype, out var proto))
            return false;

        thresholds = new();

        foreach (var threshold in proto.Thresholds)
        {
            if (proto.GetValueOrNull(threshold.Key) is not { } value)
                continue;

            if (satiation.OverrideThresholds.TryGetValue(threshold.Key, out var overrideValue))
                value = overrideValue;

            thresholds[threshold.Key] = value;
        }

        return true;
    }
}
