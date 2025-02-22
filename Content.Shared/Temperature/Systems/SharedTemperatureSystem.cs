using System.Linq;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// This handles predicting temperature based speedup.
/// </summary>
public sealed class SharedTemperatureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    /// <summary>
    /// Band-aid for unpredicted atmos. Delays the application for a short period so that laggy clients can get the replicated temperature.
    /// </summary>
    private static readonly TimeSpan SlowdownApplicationDelay = TimeSpan.FromSeconds(1f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureSpeedComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<TemperatureSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);

        // IMP EDIT BEGIN | This allows us to easily make things immune.
        SubscribeLocalEvent<TemperatureImmunityComponent, ComponentInit>(OnTemperatureImmuneInit);
        SubscribeLocalEvent<TemperatureImmunityComponent, ComponentRemove>(OnTemperatureImmuneRemove);
        // IMP EDIT END
    }
    // IMP EDIT BEGIN
    private void OnTemperatureImmuneInit(EntityUid uid, TemperatureImmunityComponent temperatureImmunity, ComponentInit args)
    {
        if (TryComp<TemperatureSpeedComponent>(uid, out var comp)) comp.HasImmunity = true;
    }

    private void OnTemperatureImmuneRemove(EntityUid uid, TemperatureImmunityComponent temperatureImmunity, ComponentRemove args)
    {
        if (TryComp<TemperatureSpeedComponent>(uid, out var comp)) comp.HasImmunity = false;
    }
    // IMP EDIT END
    private void OnTemperatureChanged(Entity<TemperatureSpeedComponent> ent, ref OnTemperatureChangeEvent args)
    {
        // IMP EDIT BEGIN
        if (ent.Comp.HasImmunity)
        {
            ent.Comp.CurrentSpeedModifier = null;
            Dirty(ent);
            return;
        }
        // IMP EDIT END
        foreach (var (threshold, modifier) in ent.Comp.Thresholds)
        {
            if (args.CurrentTemperature < threshold && args.LastTemperature > threshold ||
                args.CurrentTemperature > threshold && args.LastTemperature < threshold)
            {
                ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                ent.Comp.CurrentSpeedModifier = modifier;
                Dirty(ent);
                break;
            }
        }

        var maxThreshold = ent.Comp.Thresholds.Max(p => p.Key);
        if (args.CurrentTemperature > maxThreshold && args.LastTemperature < maxThreshold)
        {
            ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
            ent.Comp.CurrentSpeedModifier = null;
            Dirty(ent);
        }
    }

    private void OnRefreshMovementSpeedModifiers(Entity<TemperatureSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // Don't update speed and mispredict while we're compensating for lag.
        if (ent.Comp.NextSlowdownUpdate != null || ent.Comp.CurrentSpeedModifier == null)
            return;

        args.ModifySpeed(ent.Comp.CurrentSpeedModifier.Value, ent.Comp.CurrentSpeedModifier.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemperatureSpeedComponent, MovementSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var temp, out var movement))
        {
            if (temp.NextSlowdownUpdate == null)
                continue;

            if (_timing.CurTime < temp.NextSlowdownUpdate)
                continue;

            temp.NextSlowdownUpdate = null;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, movement);
            Dirty(uid, temp);
        }
    }
}
