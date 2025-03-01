using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system manages the <see cref="SatiationComponent"/>. Broadly, what that means is that it handles the decay of
/// satiations in <see cref="Update"/>, and external changes to satiations through accessors like
/// <see cref="ModifyValue"/>.
/// </summary>
public sealed class SatiationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SatiationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SatiationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<SatiationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// Sets starting satiation values.
    /// </summary>
    private void OnMapInit(Entity<SatiationComponent> entity, ref MapInitEvent args)
    {
        foreach (var satiation in entity.Comp.Satiations.Values)
        {
            var proto = _prototype.Index(satiation.Prototype);
            var value = _random.Next(
                (int)proto.Thresholds[SatiationThreshold.Concerned] + 10,
                (int)proto.Thresholds[SatiationThreshold.Okay]);

            SetAuthoritativeValue(entity, satiation, proto, value);
        }

        Dirty(entity);
    }

    /// <summary>
    /// Clears alerts.
    /// </summary>
    private void OnShutdown(Entity<SatiationComponent> entity, ref ComponentShutdown args)
    {
        foreach (var satiation in entity.Comp.Satiations.Values)
        {
            _alerts.ClearAlertCategory(entity, _prototype.Index(satiation.Prototype).AlertCategory);
        }
    }

    /// <summary>
    /// Applies a speed modifier when any satiation is at or below <see cref="SatiationThreshold.Concerned"/>.
    /// </summary>
    private void OnRefreshMovementSpeed(Entity<SatiationComponent> entity,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(entity))
        {
            return;
        }

        foreach (var satiation in entity.Comp.Satiations.Values.Where(satiation =>
                     satiation.CurrentThreshold <= SatiationThreshold.Concerned))
        {
            args.ModifySpeed(_prototype.Index(satiation.Prototype).SlowdownModifier);
        }
    }

    /// <summary>
    /// Sets all satiations to <see cref="SatiationThreshold.Okay"/>.
    /// </summary>
    private void OnRejuvenate(Entity<SatiationComponent> entity, ref RejuvenateEvent args)
    {
        foreach (var type in entity.Comp.Satiations.Keys)
        {
            SetValue(entity, type, SatiationThreshold.Okay);
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
            foreach (var satiation in component.Satiations.Values)
            {
                // If it's time to change the threshold, just update the authoritative value to what we expect the
                // current value to be. `SetAuthoritativeValue` will handle updating the threshold, applying threshold
                // effects, etc.
                if (_timing.CurTime >= satiation.ProjectedThresholdChangeTime)
                {
                    var proto = _prototype.Index(satiation.Prototype);
                    SetAuthoritativeValue(entity,
                        satiation,
                        proto,
                        CalculateCurrentValue(satiation, proto));
                }

                // If it's time to do continuous effects, do continuous effects.
                if (_timing.CurTime >= satiation.NextContinuousEffectTime)
                {
                    satiation.NextContinuousEffectTime += satiation.ContinuousEffectFrequency;

                    if (!_mobState.IsDead(entity) &&
                        satiation.CurrentThresholdDamage is { } damage)
                    {
                        _damageable.TryChangeDamage(entity, damage, true, false);
                    }
                }
            }
        }
    }

    #region accessors

    /// <summary>
    /// Gets <paramref name="entity"/>'s current value of the satiation of <paramref name="type"/>. If this entity does
    /// not have that satiation, returns null.
    /// </summary>
    public float? GetValueOrNull(Entity<SatiationComponent> entity, ProtoId<SatiationTypePrototype> type) =>
        entity.Comp.Satiations.TryGetValue(type, out var satiation)
            ? CalculateCurrentValue(satiation, _prototype.Index(satiation.Prototype))
            : null;

    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to a value corresponding to
    /// <paramref name="threshold"/>. If this entity does not have that satiation, has no effect.
    /// </summary>
    public void SetValue(Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type,
        SatiationThreshold threshold)
    {
        if (!entity.Comp.Satiations.TryGetValue(type, out var satiation))
            return;

        var proto = _prototype.Index(satiation.Prototype);
        SetAuthoritativeValue(entity, satiation, proto, proto.Thresholds[threshold]);
    }


    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to <paramref name="value"/>. If
    /// this entity does not have that satiation, has no effect.
    /// </summary>
    public void SetValue(Entity<SatiationComponent> entity, ProtoId<SatiationTypePrototype> type, float value)
    {
        entity.Comp.Satiations.TryGetValue(type, out var satiation1);
        if (satiation1 is { } satiation)
        {
            SetAuthoritativeValue(entity, satiation, _prototype.Index(satiation.Prototype), value);
        }
    }

    /// <summary>
    /// Sets <paramref name="entity"/>'s current satiation of <paramref name="type"/> to its current value plus
    /// <paramref name="amount"/>. If this entity does not have that satiation, has no effect.
    /// </summary>
    public void ModifyValue(Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type,
        float amount)
    {
        if (GetValueOrNull(entity, type) is { } currentValue)
        {
            SetValue(entity, type, currentValue + amount);
        }
    }

    /// <summary>
    /// Gets the <see cref="SatiationThreshold"/> which <paramref name="entity"/> would have for the given
    /// <paramref name="type"/> if its current value were modified by <paramref name="delta"/>. If this entity does not
    /// have that satiation, returns null.
    /// </summary>
    /// <remarks>This is useful for situations where an action consumes satiation, so we need to check satiation values
    /// prior to executing the action, eg. Arachnids spinning web.</remarks>
    public SatiationThreshold? GetThresholdWithDeltaOrNull(Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type,
        float delta)
    {
        entity.Comp.Satiations.TryGetValue(type, out var satiation1);
        if (satiation1 is not { } satiation)
        {
            return null;
        }

        var proto = _prototype.Index(satiation.Prototype);
        return proto.ThresholdFor(CalculateCurrentValue(satiation, proto) + delta);
    }

    /// <summary>
    /// Gets <paramref name="entity"/>'s current <see cref="SatiationThreshold"/> for the given <paramref name="type"/>.
    /// If this entity does not have that satiation, returns null.
    /// </summary>
    public SatiationThreshold? GetThresholdOrNull(Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type) =>
        entity.Comp.Satiations.TryGetValue(type, out var satiation) ? satiation.CurrentThreshold : null;

    #endregion


    /// <summary>
    /// Looks up the <see cref="StatusIconPrototype"/> appropriate for the given entity's <see cref="Satiation"/> of the
    /// specified <paramref name="type"/>. If the entity does not have the specified satiation type, or if the satiation
    /// does not have an icon for its current state, returns null.
    /// </summary>
    /// <remarks>This should almost definitely never be used by anything other than the client's Overlay system</remarks>
    public StatusIconPrototype? GetStatusIconOrNull(Entity<SatiationComponent> entity,
        ProtoId<SatiationTypePrototype> type)
    {
        if (!entity.Comp.Satiations.TryGetValue(type, out var satiation))
        {
            return null;
        }

        return _prototype.Index(satiation.Prototype)
            .Icons.TryGetValue(satiation.CurrentThreshold, out var icon)
            ? _prototype.Index(icon)
            : null;
    }


    /// <summary>
    /// Resolves and returns the <see cref="SatiationTypePrototype"/> for the given <paramref name="protoId"/>. If no
    /// such prototype exists, returns null.
    /// </summary>
    public SatiationTypePrototype? GetTypeOrNull(string protoId)
    {
        _prototype.TryIndex<SatiationTypePrototype>(protoId, out var proto);
        return proto;
    }

    /// <summary>
    /// Returns all <see cref="SatiationTypePrototype"/>s.
    /// </summary>
    public IEnumerable<SatiationTypePrototype> GetTypes() => _prototype.GetInstances<SatiationTypePrototype>().Values;


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
    /// <paramref name="satiationWithProtoResolved">satiation</paramref> to <paramref name="value"/>. This involves
    /// updating obvious fields on the <see cref="SatiationComponent"/>, but since changes to the value also affect the
    /// current threshold, we need to consider all of the effects that has as well.
    /// </summary>
    private void SetAuthoritativeValue(Entity<SatiationComponent> entity,
        Satiation satiation,
        SatiationPrototype proto,
        float value)
    {
        // Check if the threshold has changed.
        var newThreshold = proto.ThresholdFor(value);
        if (newThreshold != satiation.CurrentThreshold)
        {
            // Set the new threshold, and any other cached values related to the threshold.
            satiation.CurrentThreshold = newThreshold;
            satiation.CurrentThresholdDamage = proto.ThresholdDamage.GetValueOrDefault(satiation.CurrentThreshold);
            if (proto.ThresholdDecayModifiers.TryGetValue(satiation.CurrentThreshold, out var modifier))
            {
                satiation.ActualDecayRate = proto.BaseDecayRate * modifier;
            }

            // Apply threshold effects.
            _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);
            if (proto.Alerts.TryGetValue(satiation.CurrentThreshold, out var alertId))
            {
                _alerts.ShowAlert(entity, alertId);
            }
            else
            {
                _alerts.ClearAlertCategory(entity, proto.AlertCategory);
            }
        }

        // Update the authoritative value itself.
        satiation.LastAuthoritativeChangeTime = _timing.CurTime;
        satiation.LastAuthoritativeValue = proto.ClampSatiationWithinThresholds(value);

        // Update when the threshold will decay to the next lower threshold.
        if (newThreshold.NextLower() is not { } nextLowerThreshold)
        {
            // If there's no lower threshold, we can never decay lower.
            satiation.ProjectedThresholdChangeTime = null;
        }
        else
        {
            satiation.ProjectedThresholdChangeTime = _timing.CurTime +
                                                     TimeSpan.FromSeconds(
                                                         (value - proto.Thresholds[nextLowerThreshold]) /
                                                         satiation.ActualDecayRate);
        }

        Dirty(entity);
    }
}
