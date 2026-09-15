using Content.Shared.Power;
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
            if (curTime < comp.NextUpdate)
                continue;

            UpdateThermoregulator((uid, comp));
        }
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<ThermoregulatorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Powered = _power.IsPowered(ent.Owner);
        UpdateEnergyLimits(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<ThermoregulatorComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.Powered = args.Powered;
        UpdateEnergyLimits(ent.Comp);
        if (!args.Powered)
            SetActiveMode(ent, ThermoregulatorActiveMode.Idle);
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent)
    {
        var newState = ent.Comp.Powered ? GetActiveMode(ent.Comp) : ThermoregulatorActiveMode.Idle;
        var energyToSetpoint = newState == ThermoregulatorActiveMode.Idle
            ? 0f
            : HeatContainerHelpers.ConductHeatToTempQuery(ref ent.Comp, ent.Comp.Setpoint);
        var energy = Math.Clamp(energyToSetpoint, ent.Comp.MinEnergy, ent.Comp.MaxEnergy);

        var originalTemperature = ent.Comp.Temperature;
        HeatContainerHelpers.AddHeat(ref ent.Comp, energy);
        SetActiveMode(ent, newState);

        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

        var ev = new ThermoregulatorUpdatedEvent(ent.Comp);
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

    protected override void OnModeChanged(Entity<ThermoregulatorComponent> ent)
    {
        UpdateEnergyLimits(ent.Comp);
    }

    private static void UpdateEnergyLimits(ThermoregulatorComponent comp)
    {
        var dt = (float) comp.UpdateInterval.TotalSeconds;
        comp.MinEnergy = comp.Powered && comp.Mode != ThermoregulatorMode.Heating
            ? -Math.Max(0f, comp.CoolingPower) * dt
            : 0f;
        comp.MaxEnergy = comp.Powered && comp.Mode != ThermoregulatorMode.Cooling
            ? Math.Max(0f, comp.HeatingPower) * dt
            : 0f;
    }

    /// <summary>
    /// Conducts heat between the thermoregulator and another heat container.
    /// </summary>
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
public readonly record struct ThermoregulatorUpdatedEvent(ThermoregulatorComponent Thermoregulator);
