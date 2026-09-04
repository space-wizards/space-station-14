using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Temperature.Systems;

public sealed partial class ThermoregulatorSystem : SharedThermoregulatorSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ThermoregulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_power.IsPowered(uid))
            {
                SetActiveMode((uid, comp), ThermoregulatorActiveMode.Idle);
                continue;
            }

            if (curTime < comp.NextUpdate)
                continue;

            UpdateThermoregulator((uid, comp), curTime);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ValidateConfiguration(ent);
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void ValidateConfiguration(Entity<ThermoregulatorComponent> ent)
    {
        var comp = ent.Comp;

        if (!float.IsFinite(comp.MinTemperature) ||
            !float.IsFinite(comp.MaxTemperature) ||
            comp.MinTemperature > comp.MaxTemperature)
        {
            throw new InvalidOperationException(
                $"Invalid thermoregulator temperature range on {ToPrettyString(ent)}: " +
                $"{comp.MinTemperature} to {comp.MaxTemperature}.");
        }

        if (!float.IsFinite(comp.Setpoint) ||
            comp.Setpoint < comp.MinTemperature ||
            comp.Setpoint > comp.MaxTemperature)
        {
            throw new InvalidOperationException(
                $"Invalid thermoregulator setpoint on {ToPrettyString(ent)}: {comp.Setpoint}.");
        }

        if (!float.IsFinite(comp.TemperatureTolerance) || comp.TemperatureTolerance < 0f)
            throw new InvalidOperationException($"Invalid thermoregulator temperature tolerance on {ToPrettyString(ent)}.");

        if (!float.IsFinite(comp.Temperature))
            throw new InvalidOperationException($"Invalid thermoregulator temperature on {ToPrettyString(ent)}.");

        if (!float.IsFinite(comp.HeatCapacity) || comp.HeatCapacity <= 0f)
            throw new InvalidOperationException($"Invalid thermoregulator heat capacity on {ToPrettyString(ent)}.");

        if (!float.IsFinite(comp.HeatingPower) || comp.HeatingPower < 0f ||
            !float.IsFinite(comp.CoolingPower) || comp.CoolingPower < 0f)
        {
            throw new InvalidOperationException($"Invalid thermoregulator power on {ToPrettyString(ent)}.");
        }

        if (!float.IsFinite(comp.ThermalConductance) || comp.ThermalConductance < 0f)
            throw new InvalidOperationException($"Invalid thermoregulator conductance on {ToPrettyString(ent)}.");

        if (comp.UpdateInterval <= TimeSpan.Zero)
            throw new InvalidOperationException($"Invalid thermoregulator update interval on {ToPrettyString(ent)}.");

        if (!Enum.IsDefined(comp.Mode))
            throw new InvalidOperationException($"Invalid thermoregulator mode on {ToPrettyString(ent)}.");
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent, TimeSpan curTime)
    {
        var dt = (float) ent.Comp.UpdateInterval.TotalSeconds;
        var energyToSetpoint = HeatContainerHelpers.ConductHeatToTempQuery(ref ent.Comp, ent.Comp.Setpoint);
        var newState = GetActiveMode(ent.Comp);
        var energy = newState switch
        {
            ThermoregulatorActiveMode.Heating => Math.Clamp(energyToSetpoint, 0f, ent.Comp.HeatingPower * dt),
            ThermoregulatorActiveMode.Cooling => Math.Clamp(energyToSetpoint, -ent.Comp.CoolingPower * dt, 0f),
            _ => 0f
        };

        var originalTemperature = ent.Comp.Temperature;
        HeatContainerHelpers.AddHeat(ref ent.Comp, energy);
        SetActiveMode(ent, newState);

        ent.Comp.NextUpdate = curTime + ent.Comp.UpdateInterval;

        var ev = new ThermoregulatorUpdatedEvent();
        RaiseLocalEvent(ent, ref ev);

        if (!MathHelper.CloseTo(originalTemperature, ent.Comp.Temperature))
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.Temperature));
    }

    private static ThermoregulatorActiveMode GetActiveMode(ThermoregulatorComponent comp)
    {
        var difference = comp.Setpoint - comp.Temperature;
        var canHeat = comp.Mode != ThermoregulatorMode.Cooling && comp.HeatingPower > 0f;
        var canCool = comp.Mode != ThermoregulatorMode.Heating && comp.CoolingPower > 0f;

        if (comp.ActiveMode == ThermoregulatorActiveMode.Heating && canHeat && difference > 0f)
            return ThermoregulatorActiveMode.Heating;

        if (comp.ActiveMode == ThermoregulatorActiveMode.Cooling && canCool && difference < 0f)
            return ThermoregulatorActiveMode.Cooling;

        if (canHeat && difference > comp.TemperatureTolerance)
            return ThermoregulatorActiveMode.Heating;

        if (canCool && difference < -comp.TemperatureTolerance)
            return ThermoregulatorActiveMode.Cooling;

        return ThermoregulatorActiveMode.Idle;
    }

    private void SetActiveMode(Entity<ThermoregulatorComponent> ent, ThermoregulatorActiveMode mode)
    {
        if (ent.Comp.ActiveMode == mode)
            return;

        ent.Comp.ActiveMode = mode;
        DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.ActiveMode));
    }

    public void ConductHeatWith(
        Entity<ThermoregulatorComponent?> ent,
        ref HeatContainer otherHeatContainer)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        HeatContainerHelpers.ConductHeat(
            ref ent.Comp,
            ref otherHeatContainer,
            (float) ent.Comp.UpdateInterval.TotalSeconds,
            ent.Comp.ThermalConductance);
    }
}

[ByRefEvent]
public readonly record struct ThermoregulatorUpdatedEvent;
