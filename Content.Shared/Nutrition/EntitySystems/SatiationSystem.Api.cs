using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

// This part provides functions for use in other systems.
public sealed partial class SatiationSystem
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
    /// If either key is specified but not present in <paramref name="type"/>'s <see cref="SatiationPrototype.Keys"/>,
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
    /// Like <see cref="TryGetValueByThreshold{T}(Robust.Shared.GameObjects.Entity{Content.Shared.Nutrition.Components.SatiationComponent},Robust.Shared.Prototypes.ProtoId{Content.Shared.Nutrition.Prototypes.SatiationTypePrototype},System.Collections.Generic.Dictionary{int,T},out T?)">
    /// the other overload</see>, except that <paramref name="valuesByThreshold"/>'s keys are
    /// <see cref="SatiationPrototype.Keys">key strings</see>, and will be resolved to threshold values.
    /// In the case that any key string in <paramref name="valuesByThreshold"/> cannot be resolved to a value, its entry
    /// is simply ignored, possibly leading to the dictionary being treated as empty.
    /// </summary>
    public bool TryGetValueByThreshold<T>(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        Dictionary<string, T> valuesByThreshold,
        out T? result
    )
    {
        result = default;
        if (GetAndResolveSatiationOfType(entity, type) is not var (_, proto))
            return false;

        var newValues = new Dictionary<int, T>();
        foreach (var (key, value) in valuesByThreshold)
        {
            if (!proto.Keys.TryGetValue(key, out var threshold))
                continue;
            newValues[threshold] = value;
        }

        return TryGetValueByThreshold(entity, type, newValues, out result);
    }

    /// <summary>
    /// This function returns a value from <see cref="valuesByThreshold"/> in a manner similar to how the fields on a
    /// <see cref="SatiationPrototype"/> are retrieved. The current value of <paramref name="entity"/>'s satiation of the
    /// given <paramref name="type"/> is retrieved, and then the value in <paramref name="valuesByThreshold"/> with the
    /// lowest key greater than the current satiation value is returned.
    /// In the case that <paramref name="entity"/> does not have a satiation of the given <paramref name="type"/>,
    /// returns false.
    /// In the case that <paramref name="valuesByThreshold"/> is empty, returns false.
    /// </summary>
    public bool TryGetValueByThreshold<T>(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type,
        Dictionary<int, T> valuesByThreshold,
        out T? result
    )
    {
        result = default;
        if (GetValueOrNull(entity, type) is not { } currentValue)
            return false;

        var ret = TryGetValueByThreshold(currentValue, valuesByThreshold, it => it.Key, out var resultPair, out _);
        result = resultPair.Value;
        return ret;
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
        if (entity.Comp.GetOrNull(type) is not { } satiation)
            return null;

        var iconProtoId = GetCurrentAndNextLowestThresholds(satiation).Current.Icon;
        return _prototype.Resolve(iconProtoId, out var icon) ? icon : null;
    }
}
