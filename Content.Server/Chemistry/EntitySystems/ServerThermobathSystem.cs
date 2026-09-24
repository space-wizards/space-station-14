using System.Diagnostics.CodeAnalysis;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ServerThermobathSystem : ThermobathSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private ServerThermoregulatorSystem _thermoregulator = default!;

    [SubscribeLocalEvent]
    private void OnThermoregulatorControl(Entity<ThermobathComponent> ent, ref ThermoregulatorControlEvent args)
    {
        var regulator = CompOrNull<ThermoregulatorComponent>(ent);
        if (regulator == null)
            return;

        args.TargetTemperature = regulator.Setpoint;
        if (!_power.IsPowered(ent.Owner))
            return;

        var dt = (float) regulator.UpdateInterval.TotalSeconds;
        args.MinEnergy = regulator.Mode != ThermoregulatorMode.Heating
            ? -Math.Max(0f, ent.Comp.CoolingPower) * dt
            : 0f;
        args.MaxEnergy = regulator.Mode != ThermoregulatorMode.Cooling
            ? Math.Max(0f, ent.Comp.HeatingPower) * dt
            : 0f;
    }

    protected override void OnPowerStateChanged(Entity<ThermobathComponent> ent)
    {
        _thermoregulator.RefreshActiveMode(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnThermoregulatorUpdated(Entity<ThermobathComponent> ent, ref ThermoregulatorUpdatedEvent args)
    {
        var thermoregulator = args.Thermoregulator;
        if (!TryGetSolutionFromContainer(ent, out var soln))
            return;

        var solution = soln.Value.Comp.Solution;
        if (solution.Volume <= 0)
            return;

        // TODO: Replace this with HeatContainerQuerySystem once #45554 is merged.
        var solutionHeatContainer = new HeatContainer(solution.GetHeatCapacity(_proto), solution.Temperature);
        _thermoregulator.ConductHeatWith((ent, thermoregulator), ref solutionHeatContainer);
        _solutionContainer.SetTemperature(soln.Value, solutionHeatContainer.Temperature);
    }

    [SubscribeLocalEvent]
    private void OnThermoregulatorActiveModeChanged(Entity<ThermobathComponent> ent, ref ThermoregulatorActiveModeChangedEvent args)
    {
        UpdateAppearance(ent, args.Thermoregulator);
    }

    private bool TryGetSolutionFromContainer(
        Entity<ThermobathComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln)
    {
        var beaker = _itemSlots.GetItemOrNull(ent.Owner, ThermobathComponent.BeakerSlotId);
        if (beaker != null)
            return _solutionContainer.TryGetFitsInDispenser(beaker.Value, out soln, out _);

        soln = null;
        return false;
    }
}
