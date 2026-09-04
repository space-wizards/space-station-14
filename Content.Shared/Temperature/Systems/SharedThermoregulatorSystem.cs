using Content.Shared.Temperature.Components;
using JetBrains.Annotations;

namespace Content.Shared.Temperature.Systems;

/// Handles shared thermoregulator state changes.
public abstract partial class SharedThermoregulatorSystem : EntitySystem
{
    [PublicAPI]
    public void SetSetpoint(Entity<ThermoregulatorComponent?> ent, float setpoint)
    {
        if (!float.IsFinite(setpoint))
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        var thermo = ent.Comp;
        var clampedSetpoint = Math.Clamp(setpoint, thermo.MinTemperature, thermo.MaxTemperature);
        if (MathHelper.CloseTo(thermo.Setpoint, clampedSetpoint))
            return;

        thermo.Setpoint = clampedSetpoint;
        DirtyField(ent, nameof(ThermoregulatorComponent.Setpoint));
    }

    [PublicAPI]
    public void SetMode(Entity<ThermoregulatorComponent?> ent, ThermoregulatorMode mode)
    {
        if (!Enum.IsDefined(mode))
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Mode == mode)
            return;

        ent.Comp.Mode = mode;
        DirtyField(ent, nameof(ThermoregulatorComponent.Mode));
    }
}
