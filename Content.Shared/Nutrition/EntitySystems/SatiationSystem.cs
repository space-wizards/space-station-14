using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system manages the <see cref="SatiationComponent"/>. Broadly, what that means is that it handles the decay of
/// satiations in <see cref="Update"/>, and external changes to satiations through accessors like
/// <see cref="ModifyValue"/>.
/// </summary>
[SuppressMessage("ReSharper", "UseCollectionExpression")] // Collection expressions use non-whitelisted functions.
public sealed partial class SatiationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public static readonly ProtoId<SatiationTypePrototype> Hunger = "Hunger";
    public static readonly ProtoId<SatiationTypePrototype> Thirst = "Thirst";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitCaching();

        SubscribeLocalEvent<SatiationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SatiationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SatiationComponent, EntityUnpausedEvent>(OnEntityUnpaused);
        SubscribeLocalEvent<SatiationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<SatiationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// Sets starting satiation values.
    /// </summary>
    private void OnMapInit(Entity<SatiationComponent> entity, ref MapInitEvent args)
    {
        foreach (var (satiation, proto) in GetSatiationsAndTypes(entity))
        {
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine(new List<int>
                { (int)_timing.CurTick.Value, GetNetEntity(entity).Id });
            var rand = new System.Random(seed);
            var value = rand.NextFloat(proto.StartingValueMinimum, proto.StartingValueMaximum);

            SetAuthoritativeValue(entity, satiation, proto, value);
        }

        Dirty(entity);
    }

    /// <summary>
    /// Clears alerts.
    /// </summary>
    private void OnShutdown(Entity<SatiationComponent> entity, ref ComponentShutdown args)
    {
        foreach (var (_, proto) in GetSatiationsAndTypes(entity))
        {
            _alerts.ClearAlertCategory(entity.Owner, proto.AlertCategory);
        }
    }

    /// <summary>
    /// Handles pausing the <see cref="TimeSpan"/> fields in all of the values of <see cref="SatiationComponent.Satiations"/>.
    /// </summary>
    private static void OnEntityUnpaused(Entity<SatiationComponent> entity, ref EntityUnpausedEvent args)
    {
        foreach (var satiation in entity.Comp.Satiations.Values)
        {
            if (satiation.ProjectedThresholdChangeTime.HasValue)
            {
                satiation.ProjectedThresholdChangeTime =
                    satiation.ProjectedThresholdChangeTime.Value + args.PausedTime;
            }

            satiation.NextContinuousEffectTime += args.PausedTime;
        }
    }

    /// <summary>
    /// Applies a speed modifier when any satiation is at or below <see cref="SatiationThreshold.Concerned"/>.
    /// </summary>
    private void OnRefreshMovementSpeed(
        Entity<SatiationComponent> entity,
        ref RefreshMovementSpeedModifiersEvent args
    )
    {
        foreach (var satiation in entity.Comp.Satiations.Values)
        {
            var speedModifier = GetCurrentAndNextLowestThresholds(satiation).Current.SpeedModifier;
            if (speedModifier is not 1)
                args.ModifySpeed(speedModifier);
        }
    }

    /// <summary>
    /// Sets all satiations to their maximums.
    /// </summary>
    private void OnRejuvenate(Entity<SatiationComponent> entity, ref RejuvenateEvent args)
    {
        foreach (var type in entity.Comp.Satiations.Keys)
        {
            SetValue(entity, type, satiationValue: int.MaxValue);
        }
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            Entity<SatiationComponent> entity = (uid, component);
            foreach (var (satiation, proto) in GetSatiationsAndTypes(entity))
            {
                // If it's time to change the threshold, just update the authoritative value to what we expect the
                // current value to be. `SetAuthoritativeValue` will handle updating the threshold, applying threshold
                // effects, etc.
                if (_timing.CurTime >= satiation.ProjectedThresholdChangeTime)
                {
                    SetAuthoritativeValue(
                        entity,
                        satiation,
                        proto,
                        CalculateCurrentValue(satiation, proto)
                    );
                }

                // If it's time to do continuous effects, do continuous effects.
                if (_timing.CurTime >= satiation.NextContinuousEffectTime)
                {
                    satiation.NextContinuousEffectTime += satiation.ContinuousEffectFrequency;

                    if (!_mobState.IsDead(entity) &&
                        satiation.CurrentThresholdDamage is { } damage)
                    {
                        _damageable.TryChangeDamage(entity.Owner, damage, true, false);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Shared implementation for <see cref="GetCurrentAndNextLowestThresholds"/> and
    /// <see cref="TryGetValueByThreshold{T}(Robust.Shared.GameObjects.Entity{Content.Shared.Nutrition.Components.SatiationComponent},Robust.Shared.Prototypes.ProtoId{Content.Shared.Nutrition.Prototypes.SatiationTypePrototype},System.Collections.Generic.Dictionary{int,T},out T?)">GetValueByThreshold</see>
    /// which selects a value from <paramref name="values"/> based on <paramref name="currentSatiation"/>. Each value is
    /// assigned a threshold value by <paramref name="thresholdGetter"/>, and then the value with the lowest threshold
    /// greater than <paramref name="currentSatiation"/> is returned as <paramref name="currentValue"/>.
    /// <paramref name="nextLowerValue"/> is a similar value, except it is the one associated with the highest threshold
    /// less than <paramref name="currentSatiation"/> (or null if no such value exists).
    /// <br/>
    /// If <paramref name="values"/> is empty, returns false.
    /// If <paramref name="currentSatiation"/> is greater than all threshold values assigned, returns false.
    /// </summary>
    private bool TryGetValueByThreshold<T>(
        float currentSatiation,
        IEnumerable<T> values,
        Func<T, int> thresholdGetter,
        out T? currentValue,
        out T? nextLowerValue
    )
    {
        using var valuesByDescendingThreshold = values
            .Select(value => (threshold: thresholdGetter(value), value))
            .OrderByDescending(it => it.threshold)
            .GetEnumerator();

        nextLowerValue = default;
        if (!valuesByDescendingThreshold.MoveNext())
        {
            // `values` is empty, so there are no values to return.
            currentValue = default;
            return false;
        }

        var (firstThreshold, firstValue) = valuesByDescendingThreshold.Current;
        if (currentSatiation > firstThreshold)
        {
            // `currentSatiation` is higher than all thresholds, so return nothing.
            currentValue = default;
            return false;
        }

        currentValue = firstValue;
        while (valuesByDescendingThreshold.MoveNext())
        {
            nextLowerValue = valuesByDescendingThreshold.Current.value;
            var nextThreshold = valuesByDescendingThreshold.Current.threshold;
            if (currentSatiation > nextThreshold)
            {
                // This threshold is LOWER than the current satiation, so current satiation must be between this and the
                // previous threshold.
                break;
            }

            // This threshold is higher than the current value, so it's a candidate for the correct threshold.
            currentValue = nextLowerValue;

            // If we don't loop again, it's because there is no next lower value.
            nextLowerValue = default;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the <see cref="SatiationThresholdData"/> for <paramref name="satiation"/>, considering its prototype
    /// and current value. We also return the <c>NextLowest</c> for the hot-path case of
    /// <see cref="SetAuthoritativeValue"/>.
    /// </summary>
    private (SatiationThresholdData Current, SatiationThresholdData? NextLowest) GetCurrentAndNextLowestThresholds(
        Satiation satiation
    )
    {
        if (!_prototype.Resolve(satiation.Prototype, out var proto))
            return default;
        var thresholds = GetThresholds(satiation.Prototype);

        if (!TryGetValueByThreshold(
                CalculateCurrentValue(satiation, proto),
                thresholds,
                it => it.Threshold,
                out var currentValue,
                out var nextLowestValue
            ))
        {
            // False means the current value is higher than all thresholds, so return default values and the
            // highest/first threshold as "next highest"
            return (SatiationThresholdData.Default, thresholds.FirstOrNull());
        }

        return (currentValue, nextLowestValue);
    }

    /// <summary>
    /// This helper resolves <paramref name="type"/> and returns the corresponding <see cref="Satiation"/> from
    /// <paramref name="satiations"/> along with its <see cref="SatiationPrototype"/>.
    /// Returns null if the prototype fails to resolve, or if the component does not have the specified satiation.
    /// </summary>
    private (Satiation Satiation, SatiationPrototype Proto)? GetAndResolveSatiationOfType(
        SatiationComponent satiations,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    )
    {
        if (satiations.GetOrNull(type) is not { } satiation ||
            !_prototype.Resolve(satiation.Prototype, out var proto))
            return null;

        return (satiation, proto);
    }

    /// <summary>
    /// Similar to <see cref="GetAndResolveSatiationOfType"/>, this helper returns all <see cref="Satiation"/>s on
    /// <paramref name="satiations"/> along with their corresponding <see cref="SatiationPrototype"/>s.
    /// </summary>
    private IEnumerable<(Satiation, SatiationPrototype)> GetSatiationsAndTypes(SatiationComponent satiations)
    {
        foreach (var satiation in satiations.Satiations.Values)
        {
            if (!_prototype.Resolve(satiation.Prototype, out var proto))
                continue;

            yield return (satiation, proto);
        }
    }

    /// <summary>
    /// Calculates the current value of the given <see cref="Satiation"/> by linearly extrapolating the change of the
    /// value based on <see cref="Satiation.LastAuthoritativeValue"/>, <see cref="Satiation.LastAuthoritativeChangeTime"/>
    /// and <see cref="Satiation.ActualDecayRate"/>
    /// </summary>
    private float CalculateCurrentValue(Satiation satiation, SatiationPrototype proto)
    {
        var dt = _timing.CurTime - satiation.LastAuthoritativeChangeTime;
        var value = satiation.LastAuthoritativeValue - (float)dt.TotalSeconds * satiation.ActualDecayRate;
        return proto.ClampSatiationWithinThresholds(value);
    }

    /// <summary>
    /// The beating heart of this system, this function sets the given <paramref name="entity"/>'s
    /// <paramref name="satiation"/> to <paramref name="value"/>. This involves
    /// updating obvious fields on the <see cref="SatiationComponent"/>, but since changes to the value also affect the
    /// current threshold, we need to consider all of the effects that has as well.
    /// </summary>
    private void SetAuthoritativeValue(
        Entity<SatiationComponent> entity,
        Satiation satiation,
        SatiationPrototype proto,
        float value
    )
    {
        // Update the authoritative value itself.
        satiation.LastAuthoritativeChangeTime = _timing.CurTime;
        satiation.LastAuthoritativeValue = proto.ClampSatiationWithinThresholds(value);

        // Check if the threshold has changed.
        var (newThreshold, nextLowestThreshold) = GetCurrentAndNextLowestThresholds(satiation);
        if (newThreshold.Threshold != satiation.CurrentThresholdTop)
        {
            // Set the new threshold, and any other cached values related to the threshold.
            satiation.CurrentThresholdTop = newThreshold.Threshold;
            satiation.CurrentThresholdDamage = newThreshold.Damage;
            satiation.ActualDecayRate = proto.BaseDecayRate * newThreshold.DecayModifier;

            // Apply threshold effects.
            _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);
            if (newThreshold.Alert is { } alert)
            {
                _alerts.ShowAlert(entity.Owner, alert);
            }
            else
            {
                _alerts.ClearAlertCategory(entity.Owner, proto.AlertCategory);
            }
        }

        // Update when the threshold will decay to the next lower threshold.
        if (nextLowestThreshold?.Threshold is not { } nextThresholdValue)
        {
            // If there's no lower threshold, we can never decay lower.
            satiation.ProjectedThresholdChangeTime = null;
        }
        else
        {
            satiation.ProjectedThresholdChangeTime = _timing.CurTime +
                                                     TimeSpan.FromSeconds(
                                                         (value - nextThresholdValue) /
                                                         satiation.ActualDecayRate
                                                     );
        }

        Dirty(entity);
    }
}
