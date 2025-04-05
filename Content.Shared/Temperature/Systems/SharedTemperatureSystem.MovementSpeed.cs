using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// This handles predicting temperature based speedup.
/// </summary>
public abstract partial class SharedTemperatureSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    /// <summary>
    /// Band-aid for unpredicted atmos. Delays the application for a short period so that laggy clients can get the replicated temperature.
    /// </summary>
    private static readonly TimeSpan SlowdownApplicationDelay = TimeSpan.FromSeconds(1f);

    private void InitializeMoveSpeed()
    {
        SubscribeLocalEvent<TemperatureSpeedComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<TemperatureSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnTemperatureChanged(Entity<TemperatureSpeedComponent> ent, ref OnTemperatureChangeEvent args)
    {
        TemperatureSpeedComponent temperatureSpeed = ent;

        float maxThreshold = 0;
        foreach (var (threshold, modifier) in temperatureSpeed.Thresholds)
        {
            if (threshold > maxThreshold)
                maxThreshold = threshold;

            if (args.CurrentTemperature < threshold && args.LastTemperature > threshold
                || args.CurrentTemperature > threshold && args.LastTemperature < threshold)
            {
                temperatureSpeed.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                temperatureSpeed.CurrentSpeedModifier = modifier;
                Dirty(ent);
                break;
            }
        }

        if (args.CurrentTemperature > maxThreshold && args.LastTemperature < maxThreshold)
        {
            temperatureSpeed.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
            temperatureSpeed.CurrentSpeedModifier = null;
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

    private void UpdateMoveSpeed(float frameTime)
    {
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
