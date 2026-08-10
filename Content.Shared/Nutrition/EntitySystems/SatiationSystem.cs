using Content.Shared.Alert;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system manages <see cref="SatiationComponent"/>. It handles the change of satiations in <see cref="Update"/>
/// and external changes to satiations through accessors like <see cref="ModifyValue"/>.
/// </summary>
public sealed partial class SatiationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private AlertsSystem _alerts = default!;

    /// <summary>
    /// The ID of the <c>Hunger</c> satiation type. Provided because it is so commonly used in Content.
    /// </summary>
    public static readonly ProtoId<SatiationTypePrototype> Hunger = "Hunger";

    /// <summary>
    /// The ID of the <c>Thirst</c> satiation type. Provided because it is so commonly used in Content.
    /// </summary>
    public static readonly ProtoId<SatiationTypePrototype> Thirst = "Thirst";

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SatiationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            Entity<SatiationComponent> entity = (uid, component);
            foreach (var (satiation, proto) in GetSatiationsAndTypes(entity))
            {
                if (_timing.CurTime >= satiation.NextAlertUpdateTime)
                {
                    UpdateAlerts(entity, satiation, proto);
                }

                if (_timing.CurTime >= satiation.NextChangeRateModUpdateTime)
                {
                    SetAuthoritativeValue(entity, satiation, proto, CalculateCurrentValue(satiation, proto));
                }
            }
        }
    }

    /// <summary>
    /// Sets starting satiation values.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatiationComponent> entity, ref MapInitEvent args)
    {
        foreach (var (type, satiation) in entity.Comp.Satiations)
        {
            if (!ProtoMan.Resolve(satiation.Prototype, out var proto))
                continue;

            satiation.SatiationType = type;

            // TODO: Replace with RandomPredicted once the engine PR is merged
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
            var value = rand.NextFloat(proto.StartingValueMinimum, proto.StartingValueMaximum);

            SetAuthoritativeValue(entity, satiation, proto, value);
        }

        DirtyField(entity.AsNullable(), nameof(SatiationComponent.Satiations));
    }

    /// <summary>
    /// Clears alerts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SatiationComponent> entity, ref ComponentShutdown args)
    {
        foreach (var (_, proto) in GetSatiationsAndTypes(entity))
        {
            _alerts.ClearAlertCategory(entity.Owner, proto.AlertCategory);
        }
    }

    /// <summary>
    /// Sets all satiations to their maximums.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<SatiationComponent> entity, ref RejuvenateEvent args)
    {
        foreach (var type in entity.Comp.Satiations.Keys)
        {
            SetValue(entity, type, satiationValue: int.MaxValue);
        }
    }

    /// <remarks>
    /// This is basically a special-case reimplementation of <see cref="BaseSatiationEffectSystem{TComp,T}.OnSatiationUpdate"/>.
    /// </remarks>
    [SubscribeLocalEvent]
    private void UpdateAlertsOnSatiationUpdated(Entity<SatiationComponent> entity, ref SatiationUpdateEvent args)
    {
        if (entity.Comp.GetOrNull(args.Type) is not { } satiation ||
            !ProtoMan.Resolve(satiation.Prototype, out var proto))
            return;

        UpdateAlerts(entity, satiation, proto);
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
            !ProtoMan.Resolve(satiation.Prototype, out var proto))
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
            if (!ProtoMan.Resolve(satiation.Prototype, out var proto))
                continue;

            yield return (satiation, proto);
        }
    }

    /// <summary>
    /// Calculates the current value of the given <see cref="Satiation"/> by linearly extrapolating the change of the
    /// value based on <see cref="Satiation.LastAuthoritativeValue"/>, <see cref="Satiation.LastAuthoritativeChangeTime"/>
    /// and <see cref="Satiation.ActualChangeRate"/>
    /// </summary>
    private float CalculateCurrentValue(Satiation satiation, SatiationPrototype proto)
    {
        var dt = _timing.CurTime - satiation.LastAuthoritativeChangeTime;
        var value = satiation.LastAuthoritativeValue + (float)dt.TotalSeconds * satiation.ActualChangeRate;
        return proto.ClampSatiationWithinThresholds(value);
    }

    /// <summary>
    /// Calculates when <paramref name="satiation"/>'s value will reach either <paramref name="upperBound"/> or
    /// <paramref name="lowerBound"/>, or <c>null</c> if neither will happen based on the current expected linear
    /// evolution of the satiation value. A null bound is treated as unreachable, so if both bounds are null, this
    /// this function returns null.
    /// </summary>
    /// <seealso cref="EvolvesToTargetAt"/>
    private TimeSpan? EvolvesToBoundAt(
        Satiation satiation,
        SatiationPrototype proto,
        int? upperBound,
        int? lowerBound
    ) => satiation.ActualChangeRate switch
    {
        > 0 when upperBound is { } t => EvolvesToTargetAt(satiation, proto, t),
        < 0 when lowerBound is { } t => EvolvesToTargetAt(satiation, proto, t),
        // Change rate is zero or there's no threshold to decay/grow into: we'll never change without outside modification
        _ => null,
    };

    /// <summary>
    /// Calculates when <paramref name="satiation"/>'s value will reach <paramref name="target"/>, or <c>null</c> if
    /// the current linear evolution will not reach that value.
    /// </summary>
    /// <seealso cref="EvolvesToBoundAt"/>
    private TimeSpan? EvolvesToTargetAt(Satiation satiation, SatiationPrototype proto, int target)
    {
        var seconds = (target - CalculateCurrentValue(satiation, proto)) / satiation.ActualChangeRate;
        if (!seconds.IsValid() || seconds < 0f)
            return null;

        return _timing.CurTime + TimeSpan.FromSeconds(seconds);
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

        if (!TryGetValueByThreshold(
                entity,
                satiation.SatiationType,
                proto.ChangeModifiers,
                out var currentChangeMod,
                out var nextHigherThreshold,
                out var nextLowerThreshold
            ))
        {
            currentChangeMod = 1f;
        }

        satiation.ActualChangeRate = proto.BaseChangeRate * currentChangeMod;
        satiation.NextChangeRateModUpdateTime = EvolvesToBoundAt(
            satiation,
            proto,
            nextHigherThreshold,
            nextLowerThreshold
        );

        var updateEvent = new SatiationUpdateEvent(satiation.SatiationType);
        RaiseLocalEvent(entity, ref updateEvent);

        DirtyField(entity.AsNullable(), nameof(SatiationComponent.Satiations));
    }

    private void UpdateAlerts(
        Entity<SatiationComponent> entity,
        Satiation satiation,
        SatiationPrototype proto
    )
    {
        TryGetValueByThreshold(
            entity,
            satiation.SatiationType,
            proto.Alerts,
            out var result,
            out var nextHigherThreshold,
            out var nextLowerThreshold
        );

        if (result is { } alert)
        {
            _alerts.ShowAlert(entity.Owner, alert);
        }
        else
        {
            _alerts.ClearAlertCategory(entity.Owner, proto.AlertCategory);
        }

        satiation.NextAlertUpdateTime = EvolvesToBoundAt(
            satiation,
            proto,
            nextHigherThreshold,
            nextLowerThreshold
        );
        DirtyField(entity.AsNullable(), nameof(SatiationComponent.Satiations));
    }
}

/// <summary>
/// This event is raised on entities with <see cref="SatiationComponent"/> when their satiation of <paramref name="Type"/>
/// is directly set or when the <see cref="Satiation.ActualChangeRate">rate of change</see> to that satiation is changed.
/// </summary>
/// <remarks> This event may be raised even when no change has occurred.</remarks>
[ByRefEvent]
public readonly record struct SatiationUpdateEvent(ProtoId<SatiationTypePrototype> Type);
