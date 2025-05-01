using Content.Shared.Temperature.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <inheritdoc cref="ThermoregulatorComponent"/>
// ReSharper disable InconsistentNaming
public sealed class ThermoregulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermoregulatorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.NextUpdate));
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ThermoregulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
            {
                // Set to idle if not already
                if (comp.ActiveMode != ThermoregulatorActiveMode.Idle)
                {
                    comp.ActiveMode = ThermoregulatorActiveMode.Idle;
                    DirtyField(uid, comp, nameof(ThermoregulatorComponent.ActiveMode));
                }
                continue;
            }

            if (curTime < comp.NextUpdate)
                continue;

            UpdateThermoregulator((uid, comp), curTime);
        }
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent, TimeSpan curTime)
    {
        var dt = ent.Comp.UpdateInterval.TotalSeconds;    // Time between updates
        var T = ent.Comp.Temperature;                        // Current temperature
        var C = ent.Comp.HeatCapacity;                       // Heat capacity
        var Ts = ent.Comp.Setpoint;                          // Temperature setpoint
        var H = ent.Comp.Hysteresis;                         // Hysteresis band
        var SB = ent.Comp.ScaleBand;                         // Power scaling range beyond hysteresis
        var PhMax = ent.Comp.HeatingPower;                   // Max heating power
        var PcMax = ent.Comp.CoolingPower;                   // Max cooling power

        // Determine heating/cooling state using hysteresis.
        // - Heating triggers below Ts - H
        // - Cooling triggers above Ts + H
        var heating = false;
        var cooling = false;

        switch (ent.Comp.Mode)
        {
            case ThermoregulatorMode.Auto:
                // Handle both heating and cooling
                heating = (ent.Comp.ActiveMode == ThermoregulatorActiveMode.Heating && T < Ts) ||
                          (ent.Comp.ActiveMode != ThermoregulatorActiveMode.Heating && T <= Ts - H);

                cooling = (ent.Comp.ActiveMode == ThermoregulatorActiveMode.Cooling && T > Ts) ||
                          (ent.Comp.ActiveMode != ThermoregulatorActiveMode.Cooling && T >= Ts + H);
                break;

            case ThermoregulatorMode.Heating:
                heating = (ent.Comp.ActiveMode == ThermoregulatorActiveMode.Heating && T < Ts) ||
                          (ent.Comp.ActiveMode != ThermoregulatorActiveMode.Heating && T <= Ts - H);
                break;

            case ThermoregulatorMode.Cooling:
                cooling = (ent.Comp.ActiveMode == ThermoregulatorActiveMode.Cooling && T > Ts) ||
                          (ent.Comp.ActiveMode != ThermoregulatorActiveMode.Cooling && T >= Ts + H);
                break;
        }

        // Compute distance from the setpoint
        var distance = MathF.Abs(T - Ts);

        // Compute a power scale between 0 and 1 using a nonlinear (quadratic) ramp:
        // This gives us a smooth curve: very low power near the hysteresis threshold,
        // and high power as you get farther from the setpoint.
        var raw = Math.Clamp((distance - H) / (SB - H), 0f, 1f);
        var scale = raw * raw; // quadratic response

        // If we're heating or cooling and the scaled power is too low (near zero),
        // clamp to a small minimum (10%) so that the system doesn't stall just outside the deadband.
        if ((heating || cooling) && scale < 0.1f)
            scale = 0.1f;

        // Calculate effective heating or cooling power
        var heatPower = heating ? PhMax * scale : 0f;
        var coolPower = cooling ? PcMax * scale : 0f;

        // Compute net power (positive = heating, negative = cooling)
        var netPower = heatPower - coolPower;

        // Convert net power (watts) to energy (joules) applied over Δt
        // Q = P × Δt
        var energy = netPower * (float) dt;

        // Apply the energy to update the temperature
        // ΔT = Q / C
        var deltaT = energy / C;
        var newTemperature = T + deltaT;

        // Update active state
        var newState = ThermoregulatorActiveMode.Idle;
        if (heating)
            newState = ThermoregulatorActiveMode.Heating;
        else if (cooling)
            newState = ThermoregulatorActiveMode.Cooling;

        if (!MathHelper.CloseTo(T, newTemperature))
        {
            ent.Comp.Temperature = newTemperature;
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.Temperature));
        }

        if (ent.Comp.ActiveMode != newState)
        {
            ent.Comp.ActiveMode = newState;
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.ActiveMode));
        }

        ent.Comp.NextUpdate = curTime + ent.Comp.UpdateInterval;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.NextUpdate));

        var ev = new ThermoregulatorUpdatedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);
    }

    [PublicAPI]
    public void TransferHeatFromEntity(
        Entity<ThermoregulatorComponent?> ent,
        float heatCapacity,
        ref float temperature)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        TransferHeatFromEntity(ent.Comp.HeatCapacity, ref ent.Comp.Temperature, heatCapacity, ref temperature);
    }

    [PublicAPI]
    public void TransferHeatFromEntity(
        float regulatorHeatCapacity,
        ref float regulatorTemperature,
        float heatCapacity,
        ref float temperature)
    {
        var T1 = regulatorTemperature;
        var C1 = regulatorHeatCapacity;

        var T2 = temperature;
        var C2 = heatCapacity;

        // Compute equilibrium temperature
        var T_eq = (C1 * T1 + C2 * T2) / (C1 + C2);

        temperature = T_eq;
        regulatorTemperature = T_eq;
    }

    /// <summary>
    /// Sets the setpoint of the thermoregulator.
    /// </summary>
    [PublicAPI]
    public void SetSetpoint(Entity<ThermoregulatorComponent?> ent, float setpoint)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var thermo = ent.Comp;
        thermo.Setpoint = Math.Clamp(setpoint, thermo.MinTemperature, thermo.MaxTemperature);
        DirtyField(ent, nameof(ThermoregulatorComponent.Setpoint));
    }

    /// <summary>
    /// Sets the operation mode of the thermoregulator.
    /// </summary>
    [PublicAPI]
    public void SetMode(Entity<ThermoregulatorComponent?> ent, ThermoregulatorMode mode)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Mode = mode;
        DirtyField(ent, nameof(ThermoregulatorComponent.Mode));
    }

    /// <summary>
    /// Sets the enabled state of the thermoregulator.
    /// </summary>
    [PublicAPI]
    public void SetEnabled(Entity<ThermoregulatorComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Enabled = enabled;
        DirtyField(ent, nameof(ThermoregulatorComponent.Enabled));

        // If disabling, set to idle
        if (!enabled && ent.Comp.ActiveMode != ThermoregulatorActiveMode.Idle)
        {
            ent.Comp.ActiveMode = ThermoregulatorActiveMode.Idle;
            DirtyField(ent, nameof(ThermoregulatorComponent.ActiveMode));
        }
    }
}

[ByRefEvent]
public readonly record struct ThermoregulatorUpdatedEvent(ThermoregulatorComponent Thermoregulator);
