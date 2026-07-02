using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// The system for UI and heat transfer logic for a device that can heat or cool beakers.
/// </summary>
public abstract partial class SharedThermobathSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private ThermoregulatorSystem _thermoregulator = default!;

    private const string BeakerSlotId = "beakerSlot";
    private const string SolutionId = "beaker";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermobathComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ThermobathComponent, ThermobathPowerChangedMessage>(OnPowerChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathSetpointChangedMessage>(OnSetpointChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathModeChangedMessage>(OnModeChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<ThermobathComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<ThermobathComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ThermobathComponent, ThermoregulatorUpdatedEvent>(OnThermoregulatorUpdated);

        Subs.BuiEvents<ThermobathComponent>(ThermobathUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });
    }

    private void OnThermoregulatorUpdated(Entity<ThermobathComponent> ent, ref ThermoregulatorUpdatedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // Skip if not powered
        if (!_power.IsPowered(ent.Owner))
            return;

        if (ent.Comp.HasBeaker && TryGetSolutionFromContainer(ent, out var soln, out var solution) && solution.Volume > 0)
        {
            var solutionHeatContainer = new HeatContainer(solution.GetHeatCapacity(_proto), solution.Temperature);
            _thermoregulator.TransferHeatFromEntity((ent, args.Thermoregulator),
                ref solutionHeatContainer);
            _solutionContainer.SetTemperature(soln.Value, solutionHeatContainer.Temperature);
            ent.Comp.SolutionTemperature = solutionHeatContainer.Temperature;

            DirtyField(ent.AsNullable(), nameof(ThermobathComponent.SolutionTemperature));
        }

        // Update the UI regardless since our temperature has changed
        UpdateUi(ent);

        _appearance.SetData(ent, ThermobathVisuals.IsHeating, args.Thermoregulator.ActiveMode == ThermoregulatorActiveMode.Heating);
        _appearance.SetData(ent, ThermobathVisuals.IsCooling, args.Thermoregulator.ActiveMode == ThermoregulatorActiveMode.Cooling);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndIdle, args.Thermoregulator.ActiveMode == ThermoregulatorActiveMode.Idle && ent.Comp.HasBeaker);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndHeating, args.Thermoregulator.ActiveMode == ThermoregulatorActiveMode.Heating && ent.Comp.HasBeaker);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndCooling, args.Thermoregulator.ActiveMode == ThermoregulatorActiveMode.Cooling && ent.Comp.HasBeaker);
    }

    private void OnUiOpened(Entity<ThermobathComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnStartup(Entity<ThermobathComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, BeakerSlotId);
    }

    private void OnEntInsertedIntoContainer(Entity<ThermobathComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        if (!TryGetSolutionFromContainer(ent, out _, out var solution))
            return;

        ent.Comp.HasBeaker = true;
        ent.Comp.SolutionTemperature = solution.Temperature;
        DirtyFields(ent.AsNullable(), null, nameof(ThermobathComponent.SolutionTemperature), nameof(ThermobathComponent.HasBeaker));
        UpdateUi(ent);

        _appearance.SetData(ent, ThermobathVisuals.HasBeaker, true);
        _appearance.SetData(ent, ThermobathVisuals.DoesNotHaveBeaker, false);
        if (TryComp<ThermoregulatorComponent>(ent, out var comp))
        {
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndIdle, comp.ActiveMode == ThermoregulatorActiveMode.Idle);
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndHeating, comp.ActiveMode == ThermoregulatorActiveMode.Heating);
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndCooling, comp.ActiveMode == ThermoregulatorActiveMode.Cooling);
        }
        else
        {
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndIdle, true);
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndHeating, false);
            _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndCooling, false);
        }
    }

    private void OnEntRemovedFromContainer(Entity<ThermobathComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        ent.Comp.HasBeaker = args.Container.Count > 0;
        ent.Comp.SolutionTemperature = null;
        DirtyFields(ent.AsNullable(), null, nameof(ThermobathComponent.SolutionTemperature), nameof(ThermobathComponent.HasBeaker));
        UpdateUi(ent);
        _appearance.SetData(ent, ThermobathVisuals.HasBeaker, false);
        _appearance.SetData(ent, ThermobathVisuals.DoesNotHaveBeaker, true);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndIdle, false);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndHeating, false);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndCooling, false);
    }

    private void OnPowerChanged(Entity<ThermobathComponent> ent, ref PowerChangedEvent args)
    {
        _thermoregulator.SetEnabled(ent.Owner, args.Powered);
        UpdateUi(ent);
    }

    private void OnPowerChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathPowerChangedMessage args)
    {
        _power.TogglePower(ent, user: args.Actor);
        // Would be handled by OnPowerChanged but currently it's raised from the server
        _thermoregulator.SetEnabled(ent.Owner, args.Powered);
        UpdateUi(ent);
        _appearance.SetData(ent, ThermobathVisuals.IsOn, args.Powered);
        _appearance.SetData(ent, ThermobathVisuals.IsOff, !args.Powered);

        if (args.Powered) // May need to set some of the HasBeakerAnd__ values when powered.
            return;
        _appearance.SetData(ent, ThermobathVisuals.IsHeating, false);
        _appearance.SetData(ent, ThermobathVisuals.IsCooling, false);

        if (!ent.Comp.HasBeaker)
            return;
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndIdle, true);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndHeating, false);
        _appearance.SetData(ent, ThermobathVisuals.HasBeakerAndCooling, false);
    }

    private void OnSetpointChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathSetpointChangedMessage args)
    {
        if (_timing.ApplyingState)
            return;

        _thermoregulator.SetSetpoint(ent.Owner, args.Setpoint);
        UpdateUi(ent);
    }

    private void OnModeChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathModeChangedMessage args)
    {
        _thermoregulator.SetMode(ent.Owner, args.Mode);
        UpdateUi(ent);
    }

    /// <summary>
    /// Helper method to fetch a solution from the container.
    /// </summary>
    private bool TryGetSolutionFromContainer(
        Entity<ThermobathComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        soln = null;
        solution = null;

        if (!_container.TryGetContainer(ent, BeakerSlotId, out var beakerSlot))
            return false;

        foreach (var entity in beakerSlot.ContainedEntities)
        {
            if (!_solutionContainer.TryGetSolution(entity, SolutionId, out soln, out solution))
                continue;

            return true;
        }

        return false;
    }

    protected virtual void UpdateUi(Entity<ThermobathComponent> ent)
    {
        // This is implemented in client system
    }
}
