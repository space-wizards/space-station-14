using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Temperature.Systems;

public sealed partial class ServerThermoregulatorSystem : ThermoRegulatorSystem
{
    [Dependency] private IGameTiming _timing = default!;

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
    private void OnMapInit(Entity<ThermoregulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdateThermoregulator(Entity<ThermoregulatorComponent> ent)
    {
        var control = GetControl(ent);
        var energyToSetpoint = HeatContainerHelpers.ConductHeatToTempQuery(ref ent.Comp, control.TargetTemperature);
        var energy = Math.Clamp(energyToSetpoint, control.MinEnergy, control.MaxEnergy);

        var originalTemperature = ent.Comp.Temperature;
        HeatContainerHelpers.AddHeat(ref ent.Comp, energy);

        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

        var ev = new ThermoregulatorUpdatedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);

        UpdateActiveMode(ent, control);

        if (!MathHelper.CloseTo(originalTemperature, ent.Comp.Temperature))
            DirtyField(ent.AsNullable(), nameof(ThermoregulatorComponent.Temperature));
    }

    private static ThermoregulatorActiveMode GetActiveMode(
        ThermoregulatorComponent comp,
        ThermoregulatorControlEvent control,
        ThermoregulatorActiveMode previousMode)
    {
        var difference = control.TargetTemperature - comp.Temperature;
        var canHeat = control.MaxEnergy > 0f;
        var canCool = control.MinEnergy < 0f;

        if (previousMode == ThermoregulatorActiveMode.Heating && canHeat && difference > 0f)
            return ThermoregulatorActiveMode.Heating;

        if (previousMode == ThermoregulatorActiveMode.Cooling && canCool && difference < 0f)
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

        var ev = new ThermoregulatorActiveModeChangedEvent(ent.Comp);
        RaiseLocalEvent(ent, ref ev);
    }

    protected override void OnSettingsChanged(Entity<ThermoregulatorComponent> ent)
    {
        RefreshActiveMode(ent.AsNullable());
    }

    private ThermoregulatorControlEvent GetControl(Entity<ThermoregulatorComponent> ent)
    {
        var control = new ThermoregulatorControlEvent(ent.Comp.Temperature);
        RaiseLocalEvent(ent, ref control);
        return control;
    }

    /// <summary>
    /// Recalculates the active mode after a device changes its available energy.
    /// </summary>
    public void RefreshActiveMode(Entity<ThermoregulatorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var regulator = (ent.Owner, ent.Comp);
        UpdateActiveMode(regulator, GetControl(regulator));
    }

    private void UpdateActiveMode(Entity<ThermoregulatorComponent> ent, ThermoregulatorControlEvent control)
    {
        SetActiveMode(ent, GetActiveMode(ent.Comp, control, ent.Comp.ActiveMode));
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

[ByRefEvent]
public readonly record struct ThermoregulatorActiveModeChangedEvent(ThermoregulatorComponent Thermoregulator);

[ByRefEvent]
public record struct ThermoregulatorControlEvent(float TargetTemperature, float MinEnergy = 0f, float MaxEnergy = 0f);
