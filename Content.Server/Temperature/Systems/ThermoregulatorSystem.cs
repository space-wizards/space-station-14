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
            var powered = _power.IsPowered(uid);
            if (!powered)
            {
                SetActiveMode((uid, comp), ThermoregulatorActiveMode.Idle);
            }

            if (curTime < comp.NextUpdate)
                continue;

            UpdateThermoregulator((uid, comp), powered);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent, bool powered)
    {
        var dt = (float) ent.Comp.UpdateInterval.TotalSeconds;
        var energyToSetpoint = HeatContainerHelpers.ConductHeatToTempQuery(ref ent.Comp, ent.Comp.Setpoint);
        var newState = powered ? GetActiveMode(ent.Comp) : ThermoregulatorActiveMode.Idle;
        var energy = newState switch
        {
            ThermoregulatorActiveMode.Heating => Math.Clamp(energyToSetpoint, 0f, ent.Comp.HeatingPower * dt),
            ThermoregulatorActiveMode.Cooling => Math.Clamp(energyToSetpoint, -ent.Comp.CoolingPower * dt, 0f),
            _ => 0f
        };

        var originalTemperature = ent.Comp.Temperature;
        HeatContainerHelpers.AddHeat(ref ent.Comp, energy);
        SetActiveMode(ent, newState);

        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

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
