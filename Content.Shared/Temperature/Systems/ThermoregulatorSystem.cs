using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <inheritdoc cref="ThermoregulatorComponent"/>
// ReSharper disable InconsistentNaming
public sealed class ThermoregulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public const float DefaultThermalConductivity = 2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermoregulatorComponent, MapInitEvent>(OnMapInit);
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
                // Set to idle if it somehow changed through VV or something
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

    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.NextUpdate));
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent, TimeSpan curTime)
    {
        var dt = ent.Comp.UpdateInterval.TotalSeconds;     // Time between updates
        var T = ent.Comp.HeatData.Temperature;                        // Current temperature
        var C = ent.Comp.HeatData.HeatCapacity;                       // Heat capacity
        var Ts = ent.Comp.Setpoint;                          // Temperature setpoint
        var H = ent.Comp.Hysteresis;                         // Hysteresis band
        var SB = ent.Comp.ScaleBand;                         // Power scaling range beyond hysteresis
        var PhMax = ent.Comp.HeatingPower;                   // Max heating power
        var PcMax = ent.Comp.CoolingPower;                   // Max cooling power

        // Figure out which way we need to go
        var needsHeating = T < Ts;

        // Pick the right power value and direction multiplier
        var maxPower = needsHeating ? PhMax : PcMax;
        var sign = needsHeating ? 1f : -1f;

        // Make sure the current mode allows us to operate in this direction
        var modeAllows = ent.Comp.Mode == ThermoregulatorMode.Auto ||
                         (needsHeating && ent.Comp.Mode == ThermoregulatorMode.Heating) ||
                         (!needsHeating && ent.Comp.Mode == ThermoregulatorMode.Cooling);

        // Apply hysteresis to avoid rapid on/off cycling
        // If already active, stay on until we cross the setpoint
        // If inactive, don't turn on until we're H degrees away from setpoint
        var active = false;
        if (modeAllows)
        {
            var expectedActiveMode = needsHeating ? ThermoregulatorActiveMode.Heating : ThermoregulatorActiveMode.Cooling;
            var alreadyActive = ent.Comp.ActiveMode == expectedActiveMode;
            var threshold = Ts + (needsHeating ? -H : H);
            var crossedThreshold = needsHeating ? T <= threshold : T >= threshold;
            var stillNeeded = needsHeating ? T < Ts : T > Ts;

            active = (alreadyActive && stillNeeded) || (!alreadyActive && crossedThreshold);
        }

        // Scale power based on how far we are from the setpoint
        // Quadratic ramp gives smooth control near the target
        var distance = MathF.Abs(T - Ts);
        var raw = Math.Clamp((distance - H) / (SB - H), 0f, 1f);
        var scale = raw * raw;

        // Minimum 10% power when active to avoid stalling
        if (active && scale < 0.1f)
            scale = 0.1f;

        // Calculate the actual power: sign flips between heating (+) and cooling (-)
        var power = active ? sign * maxPower * scale : 0f;

        // Convert power to temperature change via Q = P × Δt and ΔT = Q / C
        var energy = power * (float)dt;

        // Update the active state for the UI
        var newState = ThermoregulatorActiveMode.Idle;
        if (active)
            newState = needsHeating ? ThermoregulatorActiveMode.Heating : ThermoregulatorActiveMode.Cooling;

        // Update temperature but DON'T dirty it yet - event handlers might modify it
        var originalTemperature = ent.Comp.HeatData.Temperature;
        ent.Comp.HeatData.AddHeat(energy);

        // Update active mode
        if (ent.Comp.ActiveMode != newState)
        {
            ent.Comp.ActiveMode = newState;
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.ActiveMode));
        }

        ent.Comp.NextUpdate = curTime + ent.Comp.UpdateInterval;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.NextUpdate));

        // Raise event - handlers may further modify Temperature
        var ev = new ThermoregulatorUpdatedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);

        // Now dirty Temperature once with the final value (after all modifications)
        if (!MathHelper.CloseTo(originalTemperature, ent.Comp.HeatData.Temperature))
        {
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.HeatData));
        }
    }

    [PublicAPI]
    public void TransferHeatFromEntity(
        Entity<ThermoregulatorComponent?> ent,
        float heatCapacity,
        ref float temperature)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var (newRegTemp, newTemp) = TransferHeatFromEntity(
            ent.Comp.HeatData.HeatCapacity,
            ent.Comp.HeatData.Temperature,
            heatCapacity,
            temperature,
            (float) ent.Comp.UpdateInterval.TotalSeconds);

        // TODO: THIS IS JANK. There are two less jank options:
        // 1) Run HeatContainers Transfer heat function for subdivisions of the delta time
        // 2) Slam the Temperatures to their intersection point.
        // The former is more accurate. The latter is more efficient. Though, it should be noted that,
        // for two heat containers exchanging heat without external stimuli, the latter comes to the correct steady-state temperature.
        // Also, I'd argue this choice should be abstracted. The Heat Container helper functions should probably make the judgement call on whether to subdivide heat transfer calculations or not.
        ent.Comp.HeatData.ConductHeatToTemp(newRegTemp);
        temperature = newTemp;
    }

    [PublicAPI]
    public (float regulatorTemperature, float temperature) TransferHeatFromEntity(
        float regulatorHeatCapacity,
        float regulatorTemperature,
        float heatCapacity,
        float temperature,
        float deltaTime,
        float thermalConductivity = DefaultThermalConductivity)
    {
        var T1 = regulatorTemperature;
        var T2 = temperature;
        var C1 = regulatorHeatCapacity;
        var C2 = heatCapacity;
        var Tdiff = T2 - T1;
        var Teq = (C1 * T1 + C2 * T2) / (C1 + C2);
        var exp_decay_characteristic_time= C1 * C2 / (thermalConductivity * (C1 + C2));
        var T1_t = Teq + C2/(C1+C2) * Tdiff * MathF.Exp(-deltaTime / exp_decay_characteristic_time);
        var T2_t = Teq + C1/(C1+C2) * Tdiff * MathF.Exp(-deltaTime / exp_decay_characteristic_time);

        // TODO: Delete old explanation. Write new explanation.
        // According to Newton's Law of Cooling:
        //     ΔQ/Δt = k * (T2 - T1)
        // Where:
        //     ΔQ = heat transferred
        //     Δt = time
        //     k = thermal conductivity
        //
        // Over a small time interval deltaTime, total heat flow is:
        //     ΔQ = k * (T2 - T1) * deltaTime

        // Our deltaTime (for sufficiently small heat capacities), is not small enough for this to work.
        // We overshoot at 1u heating and cooling.
        // var tempDiff = (T2 - T1);
        // var heatFlow = thermalConductivity * tempDiff * deltaTime;

        // Now we distribute this heat between the two bodies.
        // ΔT = ΔQ / C   (change in temperature = heat / heat capacity)
        //
        // One body gains heat, the other loses it.


        // These are two lines.
        // y = (heatflow / C1) * x + T1;
        // y = -(heatflow / C2) * x + T2;
        // (heatflow / C1) * x = -(heatflow / C2) * x + T2
        // (heatflow / C1) * x = -(heatflow / C2) * x + T2 - T1
        // ((heatflow / C1)-(heatflow / C2)) * x = T2 - T1
        // x = (T2 - T1)/((heatflow / C1)-(heatflow / C2))
        // y = (heatflow / C1) * (T2 - T1)/((heatflow / C1)-(heatflow / C2)) + T1;
        // heatFlow/C1 ==

        // MMMM, I should be able to directly compute the Temperatures with math:
        // // If this approximation overshot, set the temperature of both objects to the intersection of their temperature derivatives.
        // var deltaT1 = heatFlow / C1;
        // var deltaT2 = heatFlow / C2;
        // var intersectDeltaTime = (T2 - T1) / (deltaT1 - deltaT2); // Where a value of 1 means deltaTime was exactly enough time for the temperatures to meet
        // if (intersectDeltaTime < 1)
        // {
        //     var intersectTemperature = deltaT1 * intersectDeltaTime + T1;
        //     return (intersectTemperature, intersectTemperature);
        // }
        // var newRegulatorTemperature = T1 + heatFlow / C1;
        // var newTemperature = T2 - heatFlow / C2;

        return (T1_t, T2_t);
    }

    /// <summary>
    /// Sets the setpoint of the thermoregulator.
    /// This will be capped between the min and max temperatures.
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
