using System.Diagnostics.CodeAnalysis;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ThermobathSystem : SharedThermobathSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private ThermoregulatorSystem _thermoregulator = default!;

    [SubscribeLocalEvent]
    private void OnThermoregulatorUpdated(Entity<ThermobathComponent> ent, ref ThermoregulatorUpdatedEvent args)
    {
        var thermoregulator = Comp<ThermoregulatorComponent>(ent);
        if (TryGetSolutionFromContainer(ent, out var soln, out var solution) && solution.Volume > 0)
        {
            // TODO: Replace this with HeatContainerQuerySystem once #45554 is merged.
            var solutionHeatContainer = new HeatContainer(solution.GetHeatCapacity(_proto), solution.Temperature);
            _thermoregulator.ConductHeatWith((ent, thermoregulator), ref solutionHeatContainer);
            _solutionContainer.SetTemperature(soln.Value, solutionHeatContainer.Temperature);
        }

        UpdateAppearance(ent, thermoregulator);
    }

    private bool TryGetSolutionFromContainer(
        Entity<ThermobathComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        var beaker = _itemSlots.GetItemOrNull(ent.Owner, ThermobathComponent.BeakerSlotId);
        if (beaker != null)
            return _solutionContainer.TryGetFitsInDispenser(beaker.Value, out soln, out solution);

        soln = null;
        solution = null;
        return false;
    }
}
