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
            proto.GetValueOrNull(satiationValue) is not { } value)
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
        if (above is { } a && (valueAbove = proto.GetValueOrNull(a)) is null)
            return false; // `above` is not null, but we failed to resolve its value.

        int? valueAtOrBelow = null;
        if (below is { } b && (valueAtOrBelow = proto.GetValueOrNull(b)) is null)
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
    /// This function returns a value from <see cref="valuesByThreshold"/> based on the current value of
    /// <paramref name="entity"/>'s satiation of the given <paramref name="type"/>. The value in
    /// <paramref name="valuesByThreshold"/> with the lowest key greater than the current satiation value is returned.
    /// </summary>
    /// <param name="entity">The entity whose satiation is considered</param>
    /// <param name="type">The type of satiation to consider</param>
    /// <param name="valuesByThreshold">The values, keyed by <see cref="SatiationValue"/> thresholds</param>
    /// <param name="result">The value selected from <paramref name="valuesByThreshold"/></param>
    /// <param name="nextLowerThreshold">
    /// The keying-threshold-value of the next threshold below the selected value. Most consumers will not have a use
    /// for this value, but it is used by <see cref="BaseSatiationEffectSystem{TComp,T}"/>'s prediction of value decay.
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
        out int? nextLowerThreshold
    )
    {
        result = default;
        nextLowerThreshold = null;
        if (GetValueOrNull(entity, type) is not { } currentValue ||
            GetAndResolveSatiationOfType(entity, type) is not var (_, proto))
            return false;

        using var valuesByDescendingThreshold = valuesByThreshold
            // Resolve keys to threshold integers, discarding any keys which cannot be resolved.
            .Select(it => proto.GetValueOrNull(it.Key) is { } value ? ((int, T)?)(value, it.Value) : null)
            .OfType<(int, T)>()
            // Order by descending threshold.
            .OrderByDescending(it => it.Item1)
            .GetEnumerator();
        if (!valuesByDescendingThreshold.MoveNext())
        {
            // `values` is empty, so there are no values to return.
            result = default;
            nextLowerThreshold = null;
            return false;
        }

        if (currentValue > valuesByDescendingThreshold.Current.Item1)
        {
            // `currentSatiation` is higher than all thresholds, so we don't have a value, but we can return a next
            // lower threshold.
            result = default;
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
                nextLowerThreshold = nextLower.Item1;
                return true;
            }

            // Loop, setting `nextLower` to the next iteration's `nextHigher`
            nextHigher = nextLower;
        }

        // We've run out of thresholds below.
        result = nextHigher.Item2;
        nextLowerThreshold = null;
        return true;
    }

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

        TryGetValueByThreshold(entity, type, proto.Icons, out var iconProtoId, out _);
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
    )
    {
        return GetAndResolveSatiationOfType(entity, type)?.Proto.Thresholds.Keys ?? Enumerable.Empty<string>();
    }

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
    /// <returns>True if the satiation was added, otherwise False.</returns>
    public bool TryAddSatiation(Entity<SatiationComponent?> entity, ProtoId<SatiationTypePrototype> type, Satiation satiation)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!ProtoMan.Resolve(satiation.Prototype, out var proto))
            return false;

        satiation.SatiationType = type;

        if (!entity.Comp.Satiations.TryAdd(type, satiation))
            return false;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var value = rand.NextFloat(proto.StartingValueMinimum, proto.StartingValueMaximum);

        SetAuthoritativeValue((entity, entity.Comp), satiation, proto, value);

        DirtyField(entity, entity.Comp, nameof(SatiationComponent.Satiations));

        return true;
    }

    /// <summary>
    /// Adds a new <see cref="Satiation"/> of a given <see cref="SatiationTypePrototype"/> to the entity.
    /// </summary>
    /// <param name="entity">The entity to add the satiation to.</param>
    /// <param name="type">The type of satiation to add.</param>
    /// <param name="satiation">The satiation of that type to be added.</param>
    /// <returns>True if the satiation was added, otherwise False.</returns>
    public bool TryRemoveSatiationType(Entity<SatiationComponent?> entity, ProtoId<SatiationTypePrototype> type)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!entity.Comp.Satiations.Remove(type))
            return false;

        if (GetAndResolveSatiationOfType(entity.Comp, type) is { } satiation)
            _alerts.ClearAlertCategory(entity.Owner, satiation.Proto.AlertCategory);

        DirtyField(entity, entity.Comp, nameof(SatiationComponent.Satiations));

        return true;
    }
}
