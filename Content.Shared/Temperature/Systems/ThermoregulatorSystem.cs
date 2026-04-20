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
        HeatContainerHelpers.AddHeat(ref ent.Comp.HeatData, energy);

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

        HeatContainerHelpers.ConductHeatToTemp(ref ent.Comp.HeatData, newRegTemp);
        temperature = newTemp;
    }

    [PublicAPI]
    public static (float regulatorTemperature, float temperature) TransferHeatFromEntity(
        float regulatorHeatCapacity,
        float regulatorTemperature,
        float heatCapacity,
        float temperature,
        float deltaTime,
        float thermalConductivity = DefaultThermalConductivity)
    {
        // Explanation:
        // According to Newton's Law of Cooling:
        //     ΔQ/Δt = k * (T2(t) - T1(t))
        // Where:
        //     Q = heat transferred (J)
        //     t = time (s)
        //     k = thermal conductivity
        //
        // However, it isn't uncommon in this game for the deltatime to be to large.
        // We can instead expand the differential equations for the temperatures of the two bodies
        // The equation above is the flow of energy from body 2 to body 1.
        // This changes the internal heat capacity for body 1, which can be represented as U = m1*c1*T1(t) + some constant depending on how you define your system.
        //      So ΔQ/Δt = ΔU/Δt = Δ(C1*T1(t))/Δt = C1*Δ(T1(t))/Δt
        // Where:
        //      m1 = mass of body 1
        //      c1 = specific heat capacity of body 1
        //      C1 = m1 * c1 = Heat capacity of body 1.
        // The same is true for body 2 but the flow is opposite so: -ΔQ/Δt = ΔU/Δt, and the variables are C2 and T2.
        // We now have multiple definitions of ΔQ/Δt that are equivalent.
        //      C2 ΔT2/Δt = -k (T2 - T1); C1 ΔT1/Δt = k (T2 - T1)
        //      ΔT2/Δt = -k (T2 - T1)/C2 ; ΔT1/Δt = k (T2 - T1)/C1

        // Now we have ΔT2/Δt and ΔT1/Δt
        // Define Tdiff = T2 - T1
        //      Then derivative of it is ΔTdiff/Δt = ΔT2/Δt - ΔT1/Δt
        //      Substitute our ealier equations for ΔT2/Δt and ΔT1/Δt:
        //      So, ΔTdiff/Δt = -k Tdiff/C2 - k Tdiff/C1 = -kTdiff (1/C2 + 1/C1)

        // Define some r = k(1/C2 + 1/C1) then
        //      Then ΔTdiff/Δt = -r * Tdiff
        //      ^This is a common form, and has the following solution:
        //      Tdiff(t) = Tdiff(0)*e^(-r*t)

        // Now, it's always true that T(inf)(C1 + C2) == T1(t) * C1 + T2(t) * C2; (by conservation of energy)
        // Note: T(inf) is more accurately T1(inf) of T2(inf), but they're equal so, doesn't matter.
        // Take that and plug T1 = T2 - Tdiff for for T1
        //      T(inf)(C1 + C2) = (T2 - Tdiff) * C1 + T2(t) * C2;  (Now Isolate T2)
        //      T(inf)(C1 + C2) = T2*C1 - Tdiff*C1 + T2(t)*C2
        //      T(inf)(C1 + C2) = T2(C1+C2) - Tdiff*C1
        //      T(inf) = T2 - Tdiff*C1/(C1+C2)
        //      T2(t) = T(inf) + Tdiff(t)*C1/(C1+C2)  // sub our new formula for Tdiff(t)
        //      T2(t) = T(inf) + C1/(C1+C2) * Tdiff(0) * e^(-r*t)
        // Yippee!
        // The same can be done for T1

        // TODO:
        // Should the HeatContainers work this way too? After all, I'm basically reinventing HeatContainers ConductHeatFunction,
        // but with a dead-accurate (albiet more expensive) formula.

        var T1 = regulatorTemperature;
        var T2 = temperature;
        var C1 = regulatorHeatCapacity;
        var C2 = heatCapacity;
        var Tdiff = T2 - T1;                                        // The difference in temperature right now.
        var Tinf = (C1 * T1 + C2 * T2) / (C1 + C2);                  // Equilibrium temperature (Tinf)
        var exp_decay_rate=  thermalConductivity * (C1 + C2) / (C1 * C2); // Decay rate based on solved differential equation.
        var T1_t = Tinf + C2/(C1+C2) * Tdiff * MathF.Exp(-exp_decay_rate * deltaTime); // T1(deltaTime)
        var T2_t = Tinf + C1/(C1+C2) * Tdiff * MathF.Exp(-exp_decay_rate * deltaTime); // T2(deltaTime)

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
