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
        float? maxModifier = null;
        for (int i = 0; i < temperatureSpeed.OrderedThresholds.Length; i++)
        {
            var (thresholdValue, modifier) = temperatureSpeed.OrderedThresholds[i];

            // if temperature jumped up over threshold - we apply previous modifier (they are desc ordered), or remove it if it is first one
            if (args.CurrentTemperature > thresholdValue && args.LastTemperature < thresholdValue)
            {
                temperatureSpeed.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                temperatureSpeed.CurrentSpeedModifier = i == 0
                    ? null
                    : temperatureSpeed.OrderedThresholds[i - 1].Modifier;
                DirtyFields(ent.Owner, ent.Comp, null, nameof(TemperatureSpeedComponent.NextSlowdownUpdate), nameof(TemperatureSpeedComponent.CurrentSpeedModifier));
                return;
            }

            // if temperature jumped down over threshold - we apply modifier
            if (args.CurrentTemperature < thresholdValue && args.LastTemperature > thresholdValue)
            {
                temperatureSpeed.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                temperatureSpeed.CurrentSpeedModifier = modifier;
                DirtyFields(ent.Owner, ent.Comp, null, nameof(TemperatureSpeedComponent.NextSlowdownUpdate), nameof(TemperatureSpeedComponent.CurrentSpeedModifier));
                return;
            }

            if (args.CurrentTemperature < thresholdValue)
                maxModifier = modifier;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator - it is supposed to be set from same Dictionary and have exact same value
        if (temperatureSpeed.CurrentSpeedModifier != maxModifier)
        {
            // if we never jumped over - we still need to set modifier in case something spawned in cold
            temperatureSpeed.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
            temperatureSpeed.CurrentSpeedModifier = maxModifier;
            DirtyFields(ent.Owner, ent.Comp, null, nameof(TemperatureSpeedComponent.NextSlowdownUpdate), nameof(TemperatureSpeedComponent.CurrentSpeedModifier));
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
            DirtyField(uid, temp, nameof(TemperatureSpeedComponent.NextSlowdownUpdate));
        }
    }
}
